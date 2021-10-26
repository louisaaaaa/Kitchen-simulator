import socket
import socketserver
import sys
import time
import json

line=''

class MyTCPHandler(socketserver.BaseRequestHandler):
    """
    The request handler class for our server.

    It is instantiated once per connection to the server, and must
    override the handle() method to implement communication to the
    client.
    """


    def handle(self):
        # self.request is the TCP socket connected to the client
        self.data = b""
        while True:  # needs to always be open

            self.data += self.request.recv(1024)
            parts = self.data.split(b"\n")
            packets = parts[:-1]
            self.data = parts[-1]
            print(self.data)



            for packet in packets:
                print("{} wrote:".format(self.client_address[0]))
                print(packet)
                mydata = json.loads(packet)
                print(mydata)


            data = {
                "MyAction": "buy tomato",
                "Recipe": "Boil Water and add salt"
            }
            send_data = json.dumps(data)
            send_data = send_data + "\n"
            # line="hahahaha\n"
            # self.request.sendall(self.data())

            self.request.sendall(send_data.encode())
            # self.request.sendall(line.encode())

            # self.data = self.request.recv(1024).strip()
            # print("{} wrote:".format(self.client_address[0]))
            # print(self.data)



if __name__ == "__main__":
    HOST, PORT = "localhost", 9999

    # Create the server, binding to localhost on port 9999
    with socketserver.TCPServer((HOST, PORT), MyTCPHandler) as server:
        # Activate the server; this will keep running until you
        # interrupt the program with Ctrl-C
        server.serve_forever()
        print("Connected")
        # while True:
        #     line = input('input:')
        #     print(line)

