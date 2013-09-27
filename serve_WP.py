import socket
import time

def serve_local(port):
  sock = socket.socket()
  ip = socket.gethostbyname(socket.gethostname())
  sock.bind((ip, port))
  sock.listen(5)
  print("Started listening at ", ip, port)
  return sock

def mysend_sock(clientsocket, message):
  totalsent = 0
  while totalsent < len(message):
    sent = clientsocket.send(message[totalsent:].encode('utf-8'))
    print("Sent: ", sent, message[totalsent:])
    if sent == 0:
      raise RuntimeError("socket connection broken")
    totalsent = totalsent + sent

sock = serve_local(35000)
while 1:
  #accept connections
  print("Waiting for new connection")
  (clientsocket, address) = sock.accept()
  print("Connection open!")
  while 1:
    #Start spamming
    try:
      mysend_sock(clientsocket, "7E8 03 41 04 FF\r")
    except:
      print("Connection closed")
      break
    time.sleep(1)
