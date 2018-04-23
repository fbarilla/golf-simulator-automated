import os
import sys 
import time
import random

# results file format
# transaction id, distance from the hole, Relative positiob to the hole (Vector3) 
# i.e.  "1, 2.09, (-1.89, -10.91, -3.71)"
watch_file = '/tmp/unity_results.txt'
distance_from_the_hole = '0'
relative_position = []

# data file format
# transaction id, hole position, ball position, acceleration, angle (initial angle to the hole = 0)
# i.e. "1,3,2,170,-10"
data_file = '/tmp/unity_data.txt'

# log data
log_file = '/tmp/unity_log.txt'

# global variables
# ball position. [1, 2, 3]
ball_position = 1
ball_pos_str = str(ball_position)
# hole position. [1, 2, 3]
hole_position = 1
hole_pos_str = str(hole_position)

# limit the angle value to -10 degrees, +10 degrees
angle = 0
max_angle = 10
min_angle = -10
if min_angle <= angle <= max_angle:
	pass 
elif angle < min_angle: 
	angle = min_angle
elif angle > max_angle: 
	angle = max_angle
angle_str = str(angle)
max_angle_str = str(max_angle)
min_angle_str = str(min_angle)

# limit the acceleration
acceleration = 150
max_accel = 200
min_accel = 70
if min_accel <= acceleration <= max_accel:
	pass 
elif acceleration < min_accel: 
	acceleration = min_accel
elif acceleration > max_accel: 
	acceleration = max_accel
accel_str = str(acceleration)
max_accel_str = str(max_accel)
min_accel_str = str(min_accel)

# transaction id
global counter_str
counter = 0
counter_str = str(counter)

# data string
data_str = ""

# create the log file if it does not exists yet
try:
	os.stat(log_file)
except:
	file.open(log_file, 'w')
	file.close()

class Watcher(object):
    running = True
    refresh_delay_secs = 1

    # Constructor
    def __init__(self, watch_file, call_func_on_change=None, *args, **kwargs):
        self._cached_stamp = 0
        self.filename = watch_file
        self.call_func_on_change = call_func_on_change
        self.args = args
        self.kwargs = kwargs
	self.count = counter

    # Look for changes
    def look(self):
        stamp = os.stat(self.filename).st_mtime
        if stamp != self._cached_stamp:
            self._cached_stamp = stamp
            # File has changed, so do something...
            # print('File changed')
	    # Read results
	    print ("Read results")
	    with open(watch_file, 'r') as file:	
		str = file.readline()
	    file.close()
	    arr = str.split(',')
	    transaction_id = arr[0]
	    # print ("Transaction id: " + transaction_id)
	    distance_from_the_hole = arr[1]
	    # print ("Distance from the hole: " + distance_from_the_hole)
	    if(distance_from_the_hole != 0 and distance_from_the_hole != -1):
		relative_position = str[str.find("(")+1:str.rfind(")")]
		# print ("Relative position: " + relative_position)

	    # Write logs
	    print ("Writing log file")
	    with open(log_file, 'a') as file:	
		print ("Transaction id: " + transaction_id)
		# print ("Hole position: " + hole_pos_str)
		# print ("Ball position: " + ball_pos_str)
		# print ("Acceleration: " + accel_str)
		# print ("Angle: " + angle_str)
		# print ("Distance from hole: " + distance_from_the_hole)
		# print ("Relative position: " + relative_position)

		str = transaction_id + "," + hole_pos_str + "," + ball_pos_str + "," + accel_str + "," \
			 + angle_str + "," + distance_from_the_hole + "," + relative_position + "\n" 
		file.write(str)
	    file.close()
            
	    if self.call_func_on_change is not None:
                self.call_func_on_change(*self.args, **self.kwargs)

    # Keep watching in a loop        
    def watch(self):
        while self.running: 
            try: 
                # Look for changes
                time.sleep(self.refresh_delay_secs) 
                self.look() 
            except KeyboardInterrupt: 
                print('\nDone') 
                break 
            except FileNotFoundError:
                # Action on file not found
                pass
            except: 
                print('Unhandled error: %s' % sys.exc_info()[0]) 

# Call this function each time a change happens
def custom_action(text):
    print(text)
    accel = random.randrange(int(min_accel_str), int(max_accel_str), 10)
    # print ("Acceleration: " + str(accel_str))
    angle = random.randrange(int(min_angle_str), int(max_angle_str))
    count = int(counter_str)
    count +=1
    Watcher.counter_str = str(count)

    # print ("Transaction id: " + str(count))
    # print ("Hole position: " + hole_pos_str)
    # print ("Ball position: " + ball_pos_str)
    # print ("Acceleration: " + str(accel))
    # print ("Angle: " + str(angle))

    # print ("Data File: " + data_file)
    with open(data_file, 'w') as file:
   	data_str = str(count) + "," + hole_pos_str + "," + ball_pos_str + "," + str(accel) + "," + str(angle) + "\n"
	#data_str = "TEST"	
	file.write(data_str)
    file.close()

# watcher = Watcher(watch_file)  # simple
watcher = Watcher(watch_file, custom_action, text='generate new set of data')  # also call custom action function
watcher.watch()  # start the watch going


