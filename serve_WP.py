import socket
import time
import binascii

WELCOME = "0d0a0d0a454c4d3332372076312e350d0a0d0a3e"
SEARCHING = "534541524348494e472e2e2e0d"
UNABLE_TO_CONNECT = "554e41424c4520544f20434f4e4e4543540d0d3e"


class ClientSocket(socket.socket):
  def __init__(self, port):
    super().__init__()
    ip = socket.gethostbyname(socket.gethostname())
    self.bind((ip, port))
    self.listen(5)
    print("Started listening at ", ip, port)

def send_chunks(clientsocket, message):
  totalsent = 0
  while totalsent < len(message):
    sent = clientsocket.send(message[totalsent:].encode('utf-8'))
    print("Sent: ", sent, message[totalsent:])
    if sent == 0:
      raise RuntimeError("socket connection broken")
    totalsent = totalsent + sent

def send_hex(clientsocket, message):
  send_chunks(clientsocket, binascii.unhexlify(message).decode())



sock = ClientSocket(35000)
while 1:
  #accept connections
  print("Waiting for new connection")
  (sock, address) = sock.accept()
  print("Connection open!")
  #send_hex(sock, WELCOME)
  #time.sleep(3)
  #send_hex(sock, SEARCHING)
  #time.sleep(3)
  #send_hex(sock, UNABLE_TO_CONNECT)

  empty_array = bytearray(b" " * 1024)
  buffer = bytearray(empty_array)
  while buffer[:5] != bytearray(b'01 00'):
    buffer = bytearray(empty_array)
    sock.recv_into(buffer)
    print(buffer[:5])
    send_chunks(sock, buffer[:5].decode('utf-8'))

  # send_hex(sock, "374538203036203431203230204130203037204230203131200d")
  #7E8 06 41 20 A0 07 B0 11 \r
  
  send_hex(sock, "374538203036203431203030204245203346204138203131200d0a0d0a3e")
  #7E8 06 41 00 BE 3F A8 11 \r\n\r\n>
  
  while 1:
    sock.recv_into(buffer)
    print(buffer)
    buffer = bytearray(empty_array)

  while 1:
    #Start spamming
    try:
      send_chunks(sock, "7E8 03 41 04 FF\r")
    except:
      print("Connection closed")
      break
    time.sleep(1)
