// pins for the LEDs:
const int ledPin = 13;
const int waterPin = 8;
const int syncPin = 7;
const int wallPin = 4;
const int vPin = 3;  // hack to keep the valve from flickering on program load, which leaks water everywhere
const int touchPin = 2;
const int camTrigPin = 5;  // For triggering the cameras pointing to each eye
const int camGndPin = 9;  // Out of ground pins on Arduino board
const int optoLeftPin = 10;  // Output to turn on optogenetic LED over left cortex
const int optoRightPin = 11;  // Output to turn on optogenetic LED over right cortex

// These variables are used to dim the LED off instead of abrupting turning it off
const int LEFT_LED = 0;
const int RIGHT_LED = 1;
const int BOTH_LEDS = 2;
const int dimDur = 500;  // Dim the LED over 500 ms
int powerDownLeft = 0;
unsigned long startDimTime;
int whichLED = -1;  // -1 indicates leds are already off
int ledPower = 255;  // max 255

// For receiving data with a terminator - allow a maximum of 32 chars in a string
const char numChars = 32;
char receivedChars[numChars];

boolean newData = false;

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

void loop() {
  dimOptoLED();
  recvWithEndMarker();
  takeAction();
}

// One paper dims the optoLED on trials when it is off instead of just abruptly disabling it.  The thinking is that you will get less rebound activity.
// So far this does not seem to make a difference in my experiments, but leave it in as it doesn't cost us much.
void dimOptoLED() {
  // Always check to dim LEDs if powering down slowly
  if (powerDownLeft > 0) {
    powerDownLeft = dimDur - (millis() - startDimTime);
    
    if (powerDownLeft <= 0) {
      powerDownLeft = 0;
      whichLED = -1;
    }
    int ledVal = ledPower * ((float)powerDownLeft / dimDur);
    if (whichLED == LEFT_LED) {
      analogWrite(optoLeftPin, ledVal);
    } else if (whichLED == RIGHT_LED) {
      analogWrite(optoRightPin, ledVal);
    } else if (whichLED == BOTH_LEDS) {
      analogWrite(optoLeftPin, ledVal);
      analogWrite(optoRightPin, ledVal);
    }
  }
}

// Non-blocking reading of serial line taken from here: https://forum.arduino.cc/index.php?topic=396450.0
void recvWithEndMarker() {
  static byte ndx = 0;
  char endMarker = '\n';
  char rc;
   
  while (Serial.available() > 0 && newData == false) {
    rc = Serial.read();

    if (rc != endMarker) {
      receivedChars[ndx] = rc;
      ndx++;
      if (ndx >= numChars) {
        ndx = numChars - 1;  // If the buffer is overrun, all excess chars will be lost except for the very last one
      }
    } else {
      receivedChars[ndx] = '\0'; // terminate the string
      ndx = 0;
      newData = true;
    }
  }
}

void takeAction() {
  if (newData == true) {
    //Serial.println("got new data");
    newData = false;
    int data = atoi(receivedChars);
    if ( data == 0 ) {  // sync msg - inherited, not sure what this was used for but it is not currently used
      //Serial.println("Ard:Synced!");
      digitalWrite(syncPin, HIGH);
    } else if ( data == -1 ) {    // wall collision - not used
      digitalWrite(wallPin, HIGH);
    } else if (data == -2) {      // ForceStopSolenoid - not used
      //Serial.println("ForceStopped!");       
      digitalWrite(waterPin, LOW);
      digitalWrite(ledPin, LOW);
    } else if (data == -3) {      // Trigger the cameras
      //Serial.println("Sent trigger");
      digitalWrite(camTrigPin, HIGH);
      digitalWrite(camTrigPin, LOW);
    } else if (data == -4) {    // Turn on optoLeft LED
      //Serial.println("LeftLED!");
      analogWrite(optoLeftPin, ledPower);
      whichLED = LEFT_LED;
    } else if (data == -5) {    // Turn on optoRight LED
      //Serial.println("RightLED!");
      analogWrite(optoRightPin, ledPower);
      whichLED = RIGHT_LED;
    } else if (data == -6) {    // Turn on both opto LEDs
      //Serial.println("BothLEDs!");
      analogWrite(optoLeftPin, ledPower);
      analogWrite(optoRightPin, ledPower);
      whichLED = BOTH_LEDS;
    } else if (data == -7) {    // Turn OFF both LEDs
      if (whichLED >= LEFT_LED) {
        powerDownLeft = dimDur;
        startDimTime = millis();          
      }
    } else if (data > 0) {        // Water for data milliseconds
      digitalWrite(waterPin, HIGH);
      digitalWrite(ledPin, HIGH);
      delay(data); // 40ms = 2.8ul, 25ms = ~2 ul
      digitalWrite(waterPin, LOW);
      digitalWrite(ledPin, LOW);
    }
  } 
}

void sendTouch(){
  Serial.println("touch");
}





