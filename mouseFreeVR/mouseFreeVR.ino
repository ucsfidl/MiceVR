/* ===========
 * mouseFreeVR
 * ===========
 * by Nikhil Bhatla
 * April 2017
 * 
 * This program is used by the free moving visual stimulation rig, in conjunction with Unity MouseFreeVR.
 * This program is designed to run on an Arduino MEGA, since it relies on 6 interrup pins.
 */

#include <math.h>

/* =============
 * PIN CONSTANTS 
 * =============
 */
const int ledPin = 13;

// Note that all the mouse input Pins are the special interrupt pins on an Arduino Mega
const int nosePokePin = 2;  
const int leftLick1Pin = 3;
const int rightLick1Pin = 18;
const int centerLickPin = 19;
const int leftLick2Pin = 20;
const int rightLick2Pin = 21;

const int nosePokeValvePin = 5;  // Valve for nose poke lickport
const int leftValve1Pin = 6; // Valve for left lickport
const int rightValve1Pin = 7; // Valve for right lickport
const int centerValvePin = 8; // Valve for center lickport
const int leftValve2Pin = 9;  // Valve for left lickport #2
const int rightValve2Pin = 10;  // Valve for right lickport #2

// Signals to send to Unity
const int nosePokeSig = 0;
const int leftLick1Sig = 1;
const int rightLick1Sig = 2;
const int centerLickSig = 3;
const int leftLick2Sig = 4;
const int rightLick2Sig = 5;

volatile int sigToSend = -1;

// Signals to receive from Unity
const int nosePokeReward = 0;
const int left1Reward = 1;
const int right1Reward = 2;
const int centerReward = 3;
const int left2Reward = 4;
const int right2Reward = 5;

/* ==== 
 * SETUP
 * =====
 */
void setup() {
  // initialize serial:
  Serial.begin(9600);
  Serial.setTimeout(10);  // A 10 ms timeout allows full strings to be read as sent by Unity

  // init all pins, outputs and inputs
  pinMode(ledPin, OUTPUT);
  
  pinMode(nosePokePin, INPUT);
  attachInterrupt(digitalPinToInterrupt(nosePokePin), nosePoke, FALLING);
  pinMode(nosePokeValvePin, OUTPUT);

  pinMode(leftLick1Pin, INPUT);
  attachInterrupt(digitalPinToInterrupt(leftLick1Pin), leftLick1, FALLING);
  pinMode(leftValve1Pin, OUTPUT);

  pinMode(rightLick1Pin, INPUT);
  attachInterrupt(digitalPinToInterrupt(rightLick1Pin), rightLick1, FALLING);
  pinMode(rightValve1Pin, OUTPUT);

  pinMode(centerLickPin, INPUT);
  attachInterrupt(digitalPinToInterrupt(centerLickPin), centerLick, FALLING);
  pinMode(centerValvePin, OUTPUT);

  pinMode(leftLick2Pin, INPUT);
  attachInterrupt(digitalPinToInterrupt(leftLick2Pin), leftLick2, FALLING);
  pinMode(leftValve2Pin, OUTPUT);

  pinMode(rightLick2Pin, INPUT);
  attachInterrupt(digitalPinToInterrupt(rightLick2Pin), rightLick2, FALLING);
  pinMode(rightValve2Pin, OUTPUT);

}

/* ==== 
 * LOOP - Send interrupt signals, and receive commands to dispense water
 * =====
 */
void loop() {
  // (1) Check to see if there are any nose poke or lick signals to send to Unity
  if (sigToSend != -1) {
    Serial.println(sigToSend);
    // For debugging, before Unity functioning
    //digitalWrite(nosePokeValvePin, HIGH);
    //delay(100);
    //digitalWrite(nosePokeValvePin, LOW);
    //Serial.flush();  // Is this command necessary?
    sigToSend = -1;  // Reset the signal after it is sent
  }

  // (2) Read commands from Unity sent over USB
  if (Serial.available() > 0) {
    // Unity will send data as follows: "valveID openDur", as a single serial command
    int rewardID = Serial.parseInt();
    int openDur = Serial.parseInt();
    int currValvePin;
    if (rewardID >= 0 && openDur > 0) {
      switch (rewardID) {
        case nosePokeReward:
          currValvePin = nosePokeValvePin;
          break;
        case left1Reward:
          currValvePin = leftValve1Pin;
          break;
        case right1Reward:
          currValvePin = rightValve1Pin;
          break;
        case centerReward:
          currValvePin = centerValvePin;
          break;
        case left2Reward:
          currValvePin = leftValve2Pin;
          break;
        case right2Reward:
          currValvePin = rightValve2Pin;
          break;
        default:
          currValvePin = -1;
          break;
      }

      if (currValvePin != -1) {
        digitalWrite(ledPin, HIGH);
        digitalWrite(currValvePin, HIGH);
        delay(openDur);
        digitalWrite(currValvePin, LOW);
        digitalWrite(ledPin, LOW);      
      }
    }
  }
}

/* ===================
 * INTERRUPT FUNCTIONS
 * ===================
 */
void nosePoke(){
  sigToSend = nosePokeSig;
}
void leftLick1() {
  sigToSend = leftLick1Sig;
}
void rightLick1() {
  sigToSend = rightLick1Sig;
}
void centerLick() {
  sigToSend = centerLickSig;
}
void leftLick2() {
  sigToSend = leftLick2Sig;
}
void rightLick2() {
  sigToSend = rightLick2Sig;
}

