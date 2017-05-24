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
const int nosePokeInSig = 0;
const int nosePokeOutSig = 1;
const int leftLick1InSig = 2;
const int leftLick1OutSig = 3;
const int rightLick1InSig = 4;
const int rightLick1OutSig = 5;
const int centerLickInSig = 6;
const int centerLickOutSig = 7;
const int leftLick2Sig = 8;
const int rightLick2Sig = 10;

volatile int sigToSend = -1;
volatile int lastSig = -1;

// Signals to receive from Unity
const int nosePokeReward = nosePokeInSig;
const int left1Reward = leftLick1InSig;
const int right1Reward = rightLick1InSig;
const int centerReward = centerLickInSig;
const int left2Reward = leftLick2Sig;
const int right2Reward = rightLick2Sig;

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
  
  pinMode(nosePokePin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(nosePokePin), nosePoke, CHANGE);
  pinMode(nosePokeValvePin, OUTPUT);

  pinMode(leftLick1Pin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(leftLick1Pin), leftLick1, CHANGE);
  pinMode(leftValve1Pin, OUTPUT);

  pinMode(rightLick1Pin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(rightLick1Pin), rightLick1, CHANGE);
  pinMode(rightValve1Pin, OUTPUT);

  pinMode(centerLickPin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(centerLickPin), centerLick, CHANGE);
  pinMode(centerValvePin, OUTPUT);

  pinMode(leftLick2Pin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(leftLick2Pin), leftLick2, FALLING);
  pinMode(leftValve2Pin, OUTPUT);

  pinMode(rightLick2Pin, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(rightLick2Pin), rightLick2, FALLING);
  pinMode(rightValve2Pin, OUTPUT);

}

/* ==== 
 * LOOP - Send interrupt signals, and receive commands to dispense water
 * =====
 */
void loop() {
  // (1) Check to see if there are any nose poke or lick signals to send to Unity
  if (sigToSend != lastSig) {
    Serial.println(sigToSend);
    lastSig = sigToSend;
    // For debugging, before Unity functioning
    //digitalWrite(nosePokeValvePin, HIGH);
    //delay(100);
    //digitalWrite(nosePokeValvePin, LOW);
    //Serial.flush();  // Is this command necessary?
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
  if (lastSig == nosePokeInSig) {
    sigToSend = nosePokeOutSig;
  } else {
    sigToSend = nosePokeInSig;
  }
}
void leftLick1() {
  if (lastSig == leftLick1InSig) {
    sigToSend = leftLick1OutSig;
  } else {
    sigToSend = leftLick1InSig;
  }
}
void rightLick1() {
  if (lastSig == rightLick1InSig) {
    sigToSend = rightLick1OutSig;
  } else {
    sigToSend = rightLick1InSig;
  }
}
void centerLick() {
  if (lastSig == centerLickInSig) {
    sigToSend = centerLickOutSig;
  } else {
    sigToSend = centerLickInSig;
  }
}

void leftLick2() {
  sigToSend = leftLick2Sig;
}
void rightLick2() {
  sigToSend = rightLick2Sig;
}

