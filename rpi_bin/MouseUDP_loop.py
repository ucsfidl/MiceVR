from evdev import InputDevice, categorize, ecodes

dev = InputDevice('/dev/input/event0')
evfmt = 'type {} ({}), code {:4}, value {}'

dev.grab()  # keeps the ball from moving the mouse - nice!
print(dev)

dx = 0
dy = 0
for e in dev.read_loop():
	if e.type == ecodes.EV_REL:
		if e.code == ecodes.REL_X:
			if dx != 0:
				print dx, dy
				dy = 0
			dx = e.value;
		elif e.code == ecodes.REL_Y:
			if dy != 0:
				print dx, dy
				dx = 0
			dy = e.value

		print 'X' if e.code == ecodes.REL_X else 'Y', e.value;
#		print(categorize(e))
#			print(evfmt.format(e.type, ecodes.EV[e.type], e.code, e.value))

