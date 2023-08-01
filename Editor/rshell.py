import socket
import threading
import sys
import time
import json


def receive_message(shell):
    while True:
        try:
            data, addr = shell.client_socket.recvfrom(1024 * 1024)
            msg = json.loads(data.decode('utf-8'))
            shell.add_to_buffer(msg)
        except Exception as e:
            print("Error occurred while receiving message:", str(e))
            break

def wait_for_thread_exit(shell):
    while True:
        if not threading.active_count() > 2:
            shell.client_socket.close()
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
            print("transporting {0} of {1} fragments ... ".format(len(buffer), msg['FragCount']))
            pass


    def __init__(self, address = None):
        self.target_ip = "127.0.0.1"
        self.target_port = 9999
        self.on_message_received = None

        if address is not None:
            args = address.split(':')
            self.target_ip = args[0]
            if len(args) > 1:
                self.target_port = int(args[1])
        
        self.init_socket()
        pass

    def __del__(self):

        if self.client_socket is not None:
            self.client_socket.close()

        if self.receive_thread is not None:
            self.receive_thread.abort()
        
        if self.wait_thread is not None:
            self.wait_thread.abort()
            
        pass

    def init_socket(self):
        self.waitingMsg = None
        self.msgbuffer = {}
        self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.client_socket.sendto("hi".encode('utf-8'), (self.target_ip, self.target_port))

        self.receive_thread = threading.Thread(target=receive_message, args=(self,))
        self.receive_thread.start()

        self.wait_thread = threading.Thread(target=wait_for_thread_exit, args=(self,))
        self.wait_thread.start()
        
        print("RShell init.")
        pass

    def send(self, message):
        try:
            self.client_socket.sendto(message.encode('utf-8'), (self.target_ip, self.target_port))    
        except Exception as e:
            print("Error occurred while sending message:", str(e))
            self.init_socket()
            time.sleep(1)
            self.send(message)
        pass

    def sendwait(self, message, retry = 3):
        self.waitingMsg = None
        self.send(message)
        start_time = time.time()
        while self.waitingMsg is None:
            if time.time() - start_time > 10:
                if retry <= 0:
                    raise Exception("Timeout while waiting for response.")
                print("Timeout while waiting for response. try again.")
                return self.sendwait(message, retry - 1)
            time.sleep(0.1)
            pass
        return self.waitingMsg

if __name__ == '__main__':    
    
    address = sys.argv[1] if len(sys.argv) > 1 else None

    rshell = RShell(address)
    rshell.on_message_received = lambda msg: print(msg)
    rshell.send("hi")
    
    while True:
        message = input(f"{rshell.target_ip}:{rshell.target_port}> ")
        rshell.send(message)
        time.sleep(0.1)
        pass
    
        