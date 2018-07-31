// pins for the LEDs:
const int ledPin = 13;
const int waterPin = 8;
const int syncPin = 7;
const int wallPin = 4;
const int vPin = 3;  // hack to keep the valve from flickering on program load, which leaks water everywhere
const int touchPin = 2;
const int camTrigPin = 5;  // For triggering the cameras pointing to each eye
//const int camTrigPin2 = 6;
const int camGndPin = 9;  // Out of ground pins on Arduino board
const int optoLeftPin = 10;  // Output to turn on optogenetic LED over left cortex
const int optoRightPin = 11;  // Output to turn on optogenetic LED over right cortex

void setup() {
  Serial.begin(2000000);
  //while(!Serial);  // Wait on serial to be running
  Serial.setTimeout(1);  // This is important, now that we run serial at 2 MHz
  
  pinMode(ledPin, OUTPUT);
  pinMode(waterPin, OUTPUT);
  pinMode(syncPin, OUTPUT);
  pinMode(wallPin, OUTPUT);
  pinMode(vPin, OUTPUT);
  // Disabled at UCB, because it was being triggered by valve actuation, and eventually hung!
  //attachInterrupt(digitalPinToInterrupt(touchPin), sendTouch, FALLING);

  pinMode(camTrigPin, OUTPUT);
  digitalWrite(camTrigPin, LOW);
  //pinMode(camTrigPin2, OUTPUT);
  //digitalWrite(camTrigPin2, LOW);
  pinMode(camGndPin, OUTPUT);
  digitalWrite(camGndPin, LOW);

  pinMode(optoLeftPin, OUTPUT);
  pinMode(optoRightPin, OUTPUT);
  digitalWrite(optoLeftPin, LOW);
  digitalWrite(optoRightPin, LOW);

  digitalWrite(vPin, HIGH);  // The built-in 5V pin for some reason fluctuates during program upload, so use this instead for the valve controller
}

int flag = 0;

void loop() {
  if (Serial.available() > 0) {
    int data = Serial.parseInt();
    //Serial.println(data);
    if ( data != '\n') {
      if ( data == 0 ) {            // sync msg
        Serial.println("Ard:Synced!");
        digitalWrite(syncPin, HIGH);
        //digitalWrite(camTrigPin2, HIGH);
      } else if ( data == -1 ) {    // wall collision
        digitalWrite(wallPin, HIGH);
      } else if (data == -2) {      // ForceStopSolenoid
        Serial.println("ForceStopped!");       
        digitalWrite(waterPin, LOW);
        digitalWrite(ledPin, LOW);
      } else if (data == -3) {      // Trigger the cameras
        //Serial.println("Sent trigger");
        digitalWrite(camTrigPin, HIGH);
        digitalWrite(camTrigPin, LOW);
      } else if (data == -4) {    // Turn on optoLeft LED
        //Serial.println("LeftLED!");
        digitalWrite(optoLeftPin, HIGH);
      } else if (data == -5) {    // Turn on optoRight LED
        //Serial.println("RightLED!");
        digitalWrite(optoRightPin, HIGH);
      } else if (data == -6) {    // Turn on both opto LEDs
        //Serial.println("BothLEDs!");
        digitalWrite(optoLeftPin, HIGH);
        digitalWrite(optoRightPin, HIGH);
      } else if (data == -7) {    // Turn OFF both LEDs
        //Serial.println("OffLEDs!");
        digitalWrite(optoLeftPin, LOW);
        digitalWrite(optoRightPin, LOW);
      } else if (data > 0) {        // Water
        digitalWrite(waterPin, HIGH);
        digitalWrite(ledPin, HIGH);
        delay(data); // 40ms = 2.8ul, 25ms = ~2 ul
        digitalWrite(waterPin, LOW);
        digitalWrite(ledPin, LOW);
      }
    }
  } 
}

void sendTouch(){
  Serial.println("touch");
}





