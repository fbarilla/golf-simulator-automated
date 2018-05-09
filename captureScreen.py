import gtk.gdk
import wnck
import pygtk
pygtk.require('2.0')
import time
import numpy as np
import cv2
from PIL import Image
from gtk import gdk
import zlib
import pickle

compress = False

def pixbuf2Image(pb):
   width,height = pb.get_width(),pb.get_height()
   return Image.frombytes("RGB",(width,height),pb.get_pixels() )


# get screen 
default = wnck.screen_get_default()
# purge the events
while gtk.events_pending():
       	gtk.main_iteration(False)
# get the window list and make the Unity window active
window_list = default.get_windows()
if len(window_list) == 0:
       	print "No Windows Found"
for win in window_list:
       	if (win.get_name() == 'golf-simulator-current'):
		# print win.get_name()
		# print win.get_xid()
		now = gtk.gdk.x11_get_server_time(gtk.gdk.get_default_root_window())
		wnck.window_get(win.get_xid()).activate(now)
	
while gtk.events_pending():
	gtk.main_iteration()
time.sleep(0.5)

# get the active window
w = gdk.get_default_root_window().get_screen().get_active_window()
sz = w.get_size()
# print "The size of the window is %d x %d" % sz

while True:

	pb = gtk.gdk.Pixbuf(gtk.gdk.COLORSPACE_RGB,False,8,sz[0],sz[1])
	pb = pb.get_from_drawable(w,w.get_colormap(),0,0,0,0,sz[0],sz[1])

	if(compress):
		# serialize buffer
		pb_serialized = pickle.dumps(pb.get_pixels_array())
		#print 'buffer size: ' + str(len(pb_serialized))

		# compress
		compressed_buffer = zlib.compress(pb_serialized, 9)
		#print 'compressed buffer size: ' + str(len(compressed_buffer))

		# decompress
		decompressed_buffer = zlib.decompress(compressed_buffer)
		#print 'decompressed buffer size: ' + str(len(decompressed_buffer))	

		# deserialize 
		im = pickle.loads(decompressed_buffer)
	else:
		im = pixbuf2Image(pb)

	# load the image into an array tocomply to the opencv format
	open_cv_image = np.array(im) 
	# Convert RGB to BGR 
	opencv_image = open_cv_image[:, :, ::-1].copy() 
	
	# display the image
	cv2.imshow('test', np.array(opencv_image))
	# Press "q" to quit
	if cv2.waitKey(25) & 0xFF == ord('q'):
		cv2.destroyAllWindows()

	
