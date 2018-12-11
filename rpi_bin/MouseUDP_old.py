import socket
import sys
from struct import *
import threading
import time

pauseDir = 0;
bShort =False;
#Line for setting up crontab on RaspberryPi
#@reboot sudo python /home/pi/bin/MouseUDP.py & >> /var/log/TestReboot.log 2>&1


# Set up mice
# mice generally named as first mouse0 and then mouse1
fo = open("/home/pi/bin/mice.txt","r"); 
m0dir =fo.readline();
m1dir = fo.readline();
mouse0 = file(m0dir[:-1]);
mouse1 = file(m1dir[:-1]);

#Set Up IP Addresses
self_IP = "169.230.188.46"
self_PORT = 8888

#UDP_IP will be set at Start
UDP_IP = "192.168.1.6" 
UDP_PORT = 8936

#See if waiting will let socket be assigned
time.sleep(10)
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR,1)
sock.bind((self_IP,self_PORT))
runningMouseUDP = False

def readM0(arg1, stopEvent0):
	while(True):
		if( not stopEvent0.is_set()):
			status, dx, dy = tuple(ord(c) for c in mouse0.read(3))
			buffer = pack('BBBB',ord('M'),1,dx,dy)
			buffer = unpack('bbbb',buffer)
			if(not bShort):
				buffer = pack('bbbb',buffer[0],buffer[1],buffer[2],buffer[3])
			else:
				buffer =pack('hhhh',buffer[0],buffer[1],buffer[2],buffer[3]) 
			print('M', 1, dx, dy)
			if( not stopEvent0.is_set()):
				sock.sendto(buffer,(UDP_IP, UDP_PORT))
				time.sleep(pauseDir)
def readM1(arg1, stopEvent1):
	while(True):
		if( not stopEvent1.is_set()):
                	status, dx, dy = tuple(ord(c) for c in mouse1.read(3))
			buffer = pack('BBBB',ord('M'),2,dx,dy)
			buffer = unpack('bbbb',buffer)
                	if(not bShort):
                        	buffer = pack('bbbb',buffer[0],buffer[1],buffer[2],buffer[3])
                	else:
                        	buffer =pack('hhhh',buffer[0],buffer[1],buffer[2],buffer[3])
			print('M',2, dx,dy)
			if( not stopEvent1.is_set()):
                		sock.sendto(buffer,(UDP_IP, UDP_PORT))
				time.sleep(pauseDir)
 
M0_stop = threading.Event()
M0 = threading.Thread(target = readM0, args =(1,M0_stop))
M1_stop = threading.Event()
M1 = threading.Thread(target = readM1, args =(2,M1_stop))
M0.start()
M1.start()
M0_stop.set()
M1_stop.set()


while True:
	while(runningMouseUDP):
		data, addr =sock.recvfrom(1024)
		buffer = data.decode()
		print(buffer)
		if (buffer=="stop"):
			print "stopping send"
			M0_stop.set()
			M1_stop.set()
			runningMouseUDP = False
		elif(buffer=="swap"):
			print "stopping send and swapping mice"
			M0_stop.set()
			M1_stop.set()
			runningMouseUDP = False
			holder = mouse0
			mouse0 = mouse1
			mouse1 = holder
		elif(buffer=="shutdown"):
			print "shutting down"
			M0_stop.set()
			M1_stop.set()
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
			M0_stop.clear()
			M1_stop.clear()
			runningMouseUDP =True
                elif(buffer=="swap"):
                        print "stopping send and swapping mice"
                        M0_stop.set()
                        M1_stop.set()
                        runningMouseUDP = False
                        holder = mouse0
                        mouse0 = mouse1
                        mouse1 = holder
                elif(buffer=="shutdown"):
                        print "shutting down"
                        M0_stop.set()
                        M1_stop.set()
                        os.system("sudo shutdown -h now")
		else:
			print "Unexpected Input"
			print "Options: start swap shutdown"
			
