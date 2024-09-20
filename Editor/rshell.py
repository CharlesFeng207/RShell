import socket
import threading
import sys
import os
import time
import json
from enum import Enum

debug_rshell = False
class ConnectState(Enum):
    Default = 1,
    Connecting = 2,
    Connected = 3,

def print_debug(msg):
    if debug_rshell:
        print(msg)

def receive_message(shell):
    while True:
        try:
            if(shell.client_socket is None):
                break
            data, addr = shell.client_socket.recvfrom(1024 * 1024)
            msg = json.loads(data.decode('utf-8'))
            shell.connect_state = ConnectState.Connected
            shell.add_to_buffer(msg)
        except Exception as e:
            if debug_rshell:
                print("Error occurred while receiving message:", str(e))
            shell.connect_state = ConnectState.Default
            break

class RShell(object):
    
    def add_to_buffer(self, msg):
        buffer = []
        msgid = msg['MsgId']

        if msgid not in self.msgbuffer:
            self.msgbuffer[msgid] = buffer
        else:
            buffer = self.msgbuffer[msgid]
        
        buffer.append(msg)
        
        if len(buffer) == msg['FragCount']: 
            del self.msgbuffer[msgid]
            buffer.sort(key=lambda x: x['FragIndex'])
            content = "".join([msg['Content'] for msg in buffer])
            
            if content != "welcome":
                sendmsg = msg["CheckMsg"] if "CheckMsg" in msg else None
                self._add_respond_msg(sendmsg, content)
                
            if self.on_message_received is not None:
                self.on_message_received(content)
        else:
            print_debug("transporting {0} of {1} fragments ... ".format(len(buffer), msg['FragCount']))
            pass


    def __init__(self, address = None):
        self.target_ip = "127.0.0.1"
        self.target_port = 9999
        self.on_message_received = None
        self.connect_state = 0
        self.client_socket = None

        if address is not None:
            args = address.split(':')
            self.target_ip = args[0]
            if len(args) > 1:
                self.target_port = int(args[1])
        
        self.init_socket()
        pass

    def close_socket(self):
        if self.client_socket is not None:
            self.client_socket.close()
            self.client_socket = None
        pass

    def init_socket(self, reconnect = False):
        if self.connect_state != ConnectState.Connecting:
            reconnectinfo = "(reconnect)" if reconnect else ""
            print(f"RShell is connecting ... {self.target_ip}:{self.target_port} {reconnectinfo}")
            self.connect_state = ConnectState.Connecting

        self._reset_respond_msg(None)
        self.msgbuffer = {}

        self.close_socket()
        self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.client_socket.sendto("hi".encode('utf-8'), (self.target_ip, self.target_port))

        self.receive_thread = threading.Thread(target=receive_message, name='rshell', args=(self,))
        self.receive_thread.start()
        pass

    def send(self, message):
        if self.connect_state != ConnectState.Connected:
            self.init_socket()
        self._send(message)
        pass

    def sendwait(self, message, waittime = 10, retry = 10):
        self._reset_respond_msg(message)
        self._send(message)
        start_time = time.time()

        while True:
            respond = self._get_respond_msg(message)
            if respond is None:
                if time.time() - start_time > waittime: # timeout
                    if retry == 0:
                        raise Exception("Timeout while waiting for response.")
                    print_debug("Timeout while waiting for response. try again.")
                    self.init_socket(True)
                    return self.sendwait(message, waittime, retry - 1)
                else: # wait
                    time.sleep(0.1)
            else:
                return respond
    
    def sendwait_print(self, message, waittime = 10, retry = 10):
        msg = self.sendwait(message, waittime, retry)
        print(f"rshell send: {message} -> respond:{msg}")
        return msg
    
    def _send(self, message):
        try:
            self.client_socket.sendto(message.encode('utf-8'), (self.target_ip, self.target_port))    
        except Exception as e:
            print_debug("Error occurred while sending message:", str(e))
        pass

    def _reset_respond_msg(self, sendmsg):
        self.respond_msg = None

        if sendmsg:
            self.respond_msg_dic[sendmsg] = None
        else:
            self.respond_msg_dic = {}
        pass

    def _get_respond_msg(self, sendmsg):
        if self.respond_msg is not None:
            return self.respond_msg
        return self.respond_msg_dic[sendmsg] if sendmsg in self.respond_msg_dic else None

    def _add_respond_msg(self, sendmsg, respond):
        if sendmsg:
            self.respond_msg_dic[sendmsg] = respond
        else:
            self.respond_msg = respond
        pass


# rshell interactive mode
if __name__ == '__main__': 
    import subprocess
    import pyfiglet
    from prompt_toolkit import PromptSession
    from prompt_toolkit.completion import Completer, Completion

    commands_with_descriptions = None
    class CustomCompleter(Completer):
        def get_completions(self, document, complete_event):
            word_before_cursor = document.get_word_before_cursor().strip().lower()
            if commands_with_descriptions is None:
                return
            for cmd, description in commands_with_descriptions.items():
                if word_before_cursor in cmd.lower() or word_before_cursor in description.lower():
                    yield Completion(cmd, start_position=-len(word_before_cursor), display=cmd, display_meta=description)

    def read_commands(path):
        cmds = {}
        if not os.path.exists(path):
            return cmds
        
        with open(path, "r", encoding="utf-8") as f:
            for line in f:
                line_striped = line.strip()
                if line_striped == "":
                    continue
                
                if "//" in line_striped:
                    cmd, description = line_striped.split("//")
                    cmds[cmd] = description
                else:
                    cmds[line_striped] = ""
                    pass
                pass
            pass
        return cmds

    os.chdir(sys.path[0])

    commands_with_descriptions = read_commands("commds.txt")

    address = sys.argv[1] if len(sys.argv) > 1 else None
    debug_rshell = True

    rshell = RShell(address)

    # Generate ASCII art with a specific font
    ascii_art = pyfiglet.figlet_format("RShell", font="slant")
    session = PromptSession(completer=CustomCompleter())

    # Print the ASCII art
    print(ascii_art)
    print("Type 'h' or 'help' to see available commands.\nControl + C to exit.\n")

    try:
        while True:
            message = session.prompt(f"{rshell.target_ip}:{rshell.target_port}>").strip()
            
            if message == 'help' or message == 'h':
                subprocess.Popen("commds.txt", shell=True)
                continue

            if message.endswith(".txt") and os.path.exists(message):
                rshell.on_message_received = None
                cmds = read_commands(message)
                for c, d in cmds.items():
                    print(f"// {d}")
                    rshell.sendwait_print(c)
            else:
                rshell.on_message_received = lambda msg: print(msg)
                if message == "":
                    message = "hi"
                rshell.send(message)
                pass
            time.sleep(0.1)
            pass
    except KeyboardInterrupt:
        print("Exiting ...")
    finally:
        rshell.close_socket()
        pass
        
