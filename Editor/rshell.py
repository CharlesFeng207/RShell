import socket
import threading
import sys
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
                self.waitingMsg = content
                
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

    def init_socket(self):
        if self.connect_state != ConnectState.Connecting:
            print(f"RShell is connecting ... {self.target_ip}:{self.target_port}")
            self.connect_state = ConnectState.Connecting

        self.waitingMsg = None
        self.msgbuffer = {}

        self.close_socket()
        self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.client_socket.sendto("hi".encode('utf-8'), (self.target_ip, self.target_port))

        self.receive_thread = threading.Thread(target=receive_message, name='rshell', args=(self,))
        self.receive_thread.start()
        pass

    def send(self, message):
        try:
            if self.connect_state != ConnectState.Connected:
                self.init_socket()
            self.client_socket.sendto(message.encode('utf-8'), (self.target_ip, self.target_port))    
        except Exception as e:
            print_debug("Error occurred while sending message:", str(e))
        pass

    def sendwait(self, message, retry = 10):
        self.waitingMsg = None
        self.send(message)
        start_time = time.time()
        while self.waitingMsg is None:
            if time.time() - start_time > 5:
                if retry == 0:
                    raise Exception("Timeout while waiting for response.")
                print_debug("Timeout while waiting for response. try again.")
                self.init_socket()
                return self.sendwait(message, retry - 1)
            time.sleep(0.1)
            pass
        return self.waitingMsg

if __name__ == '__main__':
    address = sys.argv[1] if len(sys.argv) > 1 else None
    debug_rshell = True

    rshell = RShell(address)
    rshell.on_message_received = lambda msg: print(msg)

    try:
        while True:
            message = input(f"{rshell.target_ip}:{rshell.target_port}>")
            if message == "":
                message = "hi"
            rshell.send(message)
            time.sleep(0.1)
            pass
    except KeyboardInterrupt:
        rshell.close_socket()
        print("KeyboardInterrupt")
        pass