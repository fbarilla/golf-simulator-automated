'''
User input communicating with the Unity game through a socket
'''
 
import socket
import sys
from thread import *

HOST = ''   # Symbolic name meaning all available interfaces
PORT = 8989 # Arbitrary non-privileged port

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
     
    #infinite loop so that function do not terminate and thread do not end.
    while True:
 
	print '******* Input data ********'
	# get user input
	userInput = getUserInput()

	print 'Values to send: ' + userInput

	# send 
	conn.send(userInput)
	break

    while True:        

	        #Receiving from client
	        data = conn.recv(65536)

	        if "END" in data: 
		    print 'Episode Status: ' + data
	            break
		else:
         	    print data

    # restart new episode
    clientthread(conn)
    
    #came out of loop
    conn.close()

def getUserInput():
	# get hole position
	while True:
		try:
			holePos = input('Hole position [1]: ')
		except SyntaxError:
			holePos = 1
			break
		if (holePos > 3 or holePos == 0):
			print 'Hole positions are [1,3]'
			continue
		else:
			break
	# get ball position
	while True:	
		try:
			ballPos = input('Ball position [1]: ')
		except SyntaxError:
			ballPos = 1
			break
		if (ballPos > 3 or ballPos == 0):
			print 'Ball positions are [1,3]'
			continue
		else:
			break

	# get angle
	try:
		angle = input('Angle [-15,15]: ')
	except SyntaxError:
		angle = 0

	# get accelaration
	try:
		accel = input('Acceleration [80,200]: ')
	except SyntaxError:
		accel = 150

	return str(holePos) + ',' + str(ballPos) + ',' + str(angle) + ',' + str(accel)
	
#now keep talking with the client
while 1:
    #wait to accept a connection - blocking call
    conn, addr = s.accept()
    print 'Connected with ' + addr[0] + ':' + str(addr[1])
     
    #start new thread takes 1st argument as a function name to be run, second is the tuple of arguments to the function.
    start_new_thread(clientthread ,(conn,))

    print 'Done ...'
 
s.close()

