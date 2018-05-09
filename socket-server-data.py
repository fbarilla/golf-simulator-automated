'''
Simple socket server using threads
'''
 
import socket
import sys
from thread import *
import cv2
import numpy as np
import struct

IMAGE_SIZE = 4
HOST = ''   # Symbolic name meaning all available interfaces
PORT = 8990 # Arbitrary non-privileged port
 
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
print 'Socket created'
 
#Bind socket to local host and port
try:
    s.bind((HOST, PORT))
except socket.error as msg:
    print 'Bind failed. Error Code : ' + str(msg[0]) + ' Message ' + msg[1]
    sys.exit()
     
print 'Socket bind complete'
 
#Start listening on socket
s.listen(10)
print 'Socket now listening: ' + str(PORT)
 
#Function for handling connections. This will be used to create threads
def clientthread(conn):
    count = 0
    #Sending message to connected client
     
    #infinite loop so that function do not terminate and thread do not end.
    while True:
         
	count += 1

        #Receiving from client le lenght of the image sent
	data = conn.recv(IMAGE_SIZE)

	value = struct.unpack("I", bytearray(data))
	print 'Receiving size: ' + str(int(value[0])) 
	
	# receiving the image
#	data = conn.recv(value[0])

	print "Image recieved..."

#	image = cv2.imread(data) 
#	if len(data) == value[0]:
#		# print "Received frame ...."		
#		file_bytes = np.asarray(bytearray(data), dtype=np.uint8)
#		# img_data_ndarray = cv2.imdecode(file_bytes, cv2.CV_LOAD_IMAGE_UNCHANGED)
#		image = cv2.imdecode(file_bytes, 1)
#		cv2.imshow('frame', image)
#		cv2.waitKey(0)

        if not data: 
            break
     
    #came out of loop
    conn.close()
 
#now keep talking with the client
while 1:
    #wait to accept a connection - blocking call
    conn, addr = s.accept()
    print 'Connected with ' + addr[0] + ':' + str(addr[1])
     
    #start new thread takes 1st argument as a function name to be run, second is the tuple of arguments to the function.
    start_new_thread(clientthread ,(conn,))
 
s.close()
cv2.destroyAllWindows()
