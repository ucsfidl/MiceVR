#Line for setting up crontab on RaspberryPi
#@reboot sudo python /home/pi/bin/MouseUDP_ev.py & >> /var/log/TestReboot.log 2>&1

import socket
import sys
from struct import *
import threading
import time
from evdev import InputDevice, categorize, ecodes

#Set Up IP Addresses
self_IP = "192.168.1.11"
self_PORT = 8888

#UDP_IP will be set at Start
UDP_IP = "192.168.1.6" 
UDP_PORT = 8936

# Setup the network comm
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR,1)
sock.bind((self_IP,self_PORT))
runningMouseUDP = False

# Setup mouse reading through the event interface, NOT the mouse interface, which is limited to 1 byte output (event is 4 bytes/event)
mouse = InputDevice('/dev/input/event0')

# Loop for now - there may be a better way later
# Blend events into single UDPs if necessary - if it is stuttery
def readMouse(arg0, runEvent):
	dx = 0
	dy = 0
	for e in mouse.read_loop():
		runEvent.wait()  # Enter read loop, but block here
		if e.type == ecodes.EV_REL:
			if e.code == ecodes.REL_X:
				if dx != 0:
					sendDeltas(arg0, dx, dy)
					print arg0, dx, dy
					dy = 0
				dx = e.value
			elif e.code == ecodes.REL_Y:
				if dy != 0:
					sendDeltas(arg0, dx, dy)
					print arg0, dx, dy
					dx = 0
				dy = e.value
#			print 'X' if e.code == ecodes.REL_X else 'Y', e.value

def sendDeltas(mouseid, dx, dy):
	buffer = pack('BBII', ord('M'), mouseid, dx, dy)
	sz = sys.getsizeof(buffer)
	print sz
	buffer = unpack('bbii', buffer)
	sock.sendto(buffer,(UDP_IP, UDP_PORT))

def readM0(arg1, runEvent0):
	while(True):
		runEvent0.wait();
		status, dx, dy = tuple(ord(c) for c in mouse0.read(3))
#		print(status, dx, dy)
		buffer = pack('BBBB',ord('M'),1,dx,dy)
		buffer = unpack('bbbb',buffer)
		print('M', 0, buffer[2], buffer[3])
		if(not bShort):
			buffer = pack('bbbb',buffer[0],buffer[1],buffer[2],buffer[3])
		else:
			buffer =pack('hhhh',buffer[0],buffer[1],buffer[2],buffer[3]) 
#		print('M', 0, dx, dy)
		runEvent0.wait();
		time.sleep(pauseDir)

mouse_run = threading.Event()
mouse_thread = threading.Thread(target = readMouse, args=(1, mouse_run)) 

def startMice():
	global runningMouseUDP
	mouse.grab() # Keeps the RPi mouse from moving after reading for Unity
	mouse_run.set()
	runningMouseUDP = True

def stopMice():
	global runningMouseUDP
	mouse.ungrab()  # Give the mouse back to desktop control
	mouse_run.clear()
	runningMouseUDP = False

mouse_thread.start()

while True:
	while(runningMouseUDP):
		data, addr =sock.recvfrom(1024)
		buffer = data.decode()
		print(buffer)
		if (buffer=="stop"):
			print "stopping send"
			stopMice()
#		elif(buffer=="swap"):
#			print "stopping send and swapping mice"
#			stopMice()
#			holder = mouse0
#			mouse0 = mouse1
#			mouse1 = holder
		elif(buffer=="shutdown"):
			print "shutting down"
			stopMice()
			os.system("sudo shutdown -h now")
		else:
			print"incorrect input."
			print"Options: stop, swap, shutdown"

	while(not runningMouseUDP):
		data, addr =sock.recvfrom(1024)
		buffer= data.decode()
		print (buffer)
		if(buffer=="start"):
			print ("starting program using IP Address", addr)
			UDP_IP = addr[0]
			UDP_PORT = addr[1]
			startMice()
 #               elif(buffer=="swap"):
 #                       print "stopping send and swapping mice"
#			stopMice()
#                        holder = mouse0
#                        mouse0 = mouse1
#                        mouse1 = holder
                elif(buffer=="shutdown"):
                        print "shutting down"
			stopMice()
                        os.system("sudo shutdown -h now")
		else:
			print "Unexpected Input"
			print "Options: start swap shutdown"

