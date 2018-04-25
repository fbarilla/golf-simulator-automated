import os
import sys 
import time
import random
import fcntl
import signal

# results file format
# transaction id, distance from the hole, Relative positiob to the hole (Vector3) 
# i.e.  "1, 2.09, (-1.89, -10.91, -3.71)"
watch_file = '/tmp/unity_results.txt'

# data file format
# transaction id, hole position, ball position, acceleration, angle (initial angle to the hole = 0)
# i.e. "1,3,2,170,-10"
data_file = '/tmp/unity_data.txt'

# log data
log_file = '/tmp/unity_log.txt'

# variables
min_accel = 70
max_accel = 160
min_angle = -10
max_angle = 10


class Watcher:
	previous_timestamp = ''
	distance_from_the_hole = ''
	relative_position = ''
	ball_pos = 1   # ball position. [1, 2, 3]
	hole_pos = 1   # hole position. [1, 2, 3]
	transaction_id = ''
	counter = 0
	angle = 0
	accel = 150


# create the log file if it does not exists yet
try:
	os.stat(log_file)
except:
	with open(log_file, 'w') as file:
		file.close()

# create the result file if it does not exists yet
try:
	os.stat(watch_file)
except:
	with open(watch_file, 'w') as file:
		file.close()



def readResultFile():
	print ("Read results")
	with open(watch_file, 'r') as file:	
		line = file.readline()
	file.close()
	if(len(line.strip()) != 0):
		arr = line.split(',')
		Watcher.transaction_id = arr[0]
		print ("Transaction id: " + Watcher.transaction_id)
		Watcher.distance_from_the_hole = arr[1]
		print ("Distance from the hole: " + Watcher.distance_from_the_hole)
		if(float(Watcher.distance_from_the_hole) != 0 and float(Watcher.distance_from_the_hole) != -1):
			Watcher.relative_position = line[line.find("(")+1:line.rfind(")")]
			print ("Relative position: " + "(" + Watcher.relative_position + ")")

def writeLogFile():
    print ("Writing log file")
    with open(log_file, 'a') as file:	
	print ("Transaction id: " + Watcher.transaction_id)
	# print ("Hole position: " + str(Watcher.hole_pos))
	# print ("Ball position: " + str(Watcher.ball_pos))
	# print ("Acceleration: " + str(Watcher.accel))
	# print ("Angle: " + str(Watcher.angle))
	# print ("Distance from hole: " + Watcher.distance_from_the_hole)
	# print ("Relative position: " + Watcher.relative_position)

	line = Watcher.transaction_id + "," + str(Watcher.hole_pos) + "," + str(Watcher.ball_pos) \
		+ "," + str(Watcher.accel) + ","  + str(Watcher.angle) + "," + Watcher.distance_from_the_hole \
		+ ", (" + Watcher.relative_position + ")\n" 
	# print("LINE: " + line)
	file.write(line)
    file.close()


def writeInputDataFile():
	print ("Writing input data file")
	
	# compute random acceleration and angle
	Watcher.accel = random.randrange(min_accel, max_accel, 5)
    	# print ("Acceleration: " + str(Watcher.accel))
    	Watcher.angle = random.randrange(min_angle, max_angle)
	# print ("Angle: " + str(Watcher.angle)

	# TEST TEST TEST
	# Watcher.accel = 160
	# Watcher.angle = -1


	# pickup randomly hole and ball position among three locations [1,2,3]
	Watcher.hole_pos = random.randint(1,3)
	Watcher.ball_pos = random.randint(1,3)

	# print ("Transaction id: " + str(Watcher.counter))
    	# print ("Hole position: " + str(Watcher.hole_pos))
    	# print ("Ball position: " + str(Watcher.ball_pos))
    	# print ("Acceleration: " + str(Watcher.accel))
    	# print ("Angle: " + str(Watcher.angle))

    	# print ("Data File: " + data_file)
    	with open(data_file, 'w') as file:
   		data_str = str(Watcher.counter) + "," + str(Watcher.hole_pos) + "," + str(Watcher.ball_pos) \
			 + "," + str(Watcher.accel) + "," + str(Watcher.angle) + "\n"
		file.write(data_str)
    	file.close()
	print("Data: " + data_str)

# an approach based on message queue would be much better....
while True:
    ts = os.stat(watch_file).st_mtime
    if(ts != Watcher.previous_timestamp):
	print("File has changed...")
	# increment transaction id	
	Watcher.counter += 1
	Watcher.previous_timestamp = ts
	# read results
	readResultFile()
	# write logs
	writeLogFile()
	# write next set of data
	writeInputDataFile()

    time.sleep(2)

