import os
import sys 
import time

# results file format
# transaction id, distance from the hole, Relative positiob to the hole (Vector3) 
# i.e.  "1, 2.09, (-1.89, -10.91, -3.71)"
watch_file = '/tmp/unity_results.txt'
distance_from_the_hole = 0
relative_position = []

# data file format
# transaction id, hole position, ball position, acceleration, angle (initial angle to the hole = 0)
# i.e. "1,3,2,170,-10"
data_file = '/tmp/unity_data.txt'

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

# limit the acceleration
acceleration = 0
max_accel = 70
min_accel = 200
if min_accel <= acceleration <= max_accel:
	pass 
elif acceleration < min_accel: 
	acceleration = min_accel
elif acceleration > max_accel: 
	acceleration = max_accel

# ball position. [1, 2, 3]
ball_pos = "1"

# hole position. [1, 2, 3]
hole_pos = "1"

# transaction id
counter = 0

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

    # Look for changes
    def look(self):
        stamp = os.stat(self.filename).st_mtime
        if stamp != self._cached_stamp:
            self._cached_stamp = stamp
            # File has changed, so do something...
            print('File changed')
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

    # write data to the file
    def writeDataToFile():
	with open(data_file, 'w') as file:	
		str = counter + "," + hole_poistion + "," + ball_position + "," + acceleration + "," + angle
		file.write(str)
	file.close() 

# Call this function each time a change happens
def custom_action(text):
    print(text)

# watcher = Watcher(watch_file)  # simple
watcher = Watcher(watch_file, custom_action, text='yes, changed')  # also call custom action function
watcher.watch()  # start the watch going

