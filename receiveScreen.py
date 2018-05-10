import numpy as np
import cv2
from PIL import Image
from gtk import gdk
import zlib
import pickle
import socket,struct,sys,time

HOST = ''   # Symbolic name meaning all available interfaces
PORT = 8990 # Arbitrary non-privileged port

def recv_size(the_socket):
    #data length is packed into 4 bytes
    total_len=0;total_data=[];size=sys.maxint
    size_data=sock_data='';recv_size=8192
    while total_len<size:
        sock_data=the_socket.recv(recv_size)
        if not total_data:
            if len(sock_data)>4:
                size_data+=sock_data
                size=struct.unpack('>i', size_data[:4])[0]
                recv_size=size
                if recv_size>524288:recv_size=524288
                total_data.append(size_data[4:])
            else:
                size_data+=sock_data
        else:
            total_data.append(sock_data)
        total_len=sum([len(i) for i in total_data ])
    return ''.join(total_data)


def start_server():
    sock=socket.socket(socket.AF_INET,socket.SOCK_STREAM)
    sock.bind((HOST,PORT))
    sock.listen(5)
    print 'started on',PORT
    while True:
        newsock,address=sock.accept()
 	result=recv_size(newsock)
	# deserialize 
	im = pickle.loads(result)
	# load the image into an array tocomply to the opencv format
	open_cv_image = np.array(im) 
	# Convert RGB to BGR 
	opencv_image = open_cv_image[:, :, ::-1].copy() 
	# display the image
	cv2.imshow('test', np.array(opencv_image))
	# Press "q" to quit
	if cv2.waitKey(25) & 0xFF == ord('q'):
		cv2.destroyAllWindows()


if __name__=='__main__':
    #start server
    start_server()



