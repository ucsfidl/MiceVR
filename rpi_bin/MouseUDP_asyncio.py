import asyncio, evdev

@asyncio.coroutine
def print_events(device):
	while True:
		events = yield from device.async_read()
		for event in events:
			print(device.fn, evdev.categorize(event), sep=': ')

mouse = evdev.InputDevice('/dev/input/event0')

asyncio.async(print_events(mouse))

loop = asyncio.get_event_loop()
loop.run_forever()
