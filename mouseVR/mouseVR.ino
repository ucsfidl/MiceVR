
// pins for the LEDs:
const int ledPin = 13;
const int waterPin = 8;
const int syncPin = 7;
const int wallPin = 4;
const int vPin = 3;  // hack to keep the valve from flickering on program load, which leaks water everywhere
const int touchPin = 2;
const int camTrigPin1 = 5;  // For triggering the camera on the left eye
const int camTrigPin2 = 6;  // For triggering the camera on the right eye

void setup() {
  
  // initialize serial:
  Serial.begin(9600);
  //while(!Serial);  // Wait on serial to be running
  Serial.setTimeout(10);
  
  pinMode(ledPin, OUTPUT);
  pinMode(waterPin, OUTPUT);
  pinMode(syncPin, OUTPUT);
  pinMode(wallPin, OUTPUT);
  pinMode(vPin, OUTPUT);
  attachInterrupt(digitalPinToInterrupt(touchPin), sendTouch,RISING);

  digitalWrite(vPin, HIGH);  // The built-in 5V pin for some reason fluctuates during program upload, so use this instead for the valve controller
}

void loop() {
  if (Serial.available() > 0) {
    int data = Serial.parseInt();
    Serial.println(data);
    if ( data != '\n') {
      if ( data == 0 ) {            // sync msg
        Serial.println("Ard:Synced!");
        digitalWrite(syncPin, HIGH);
      } else if ( data == -1 ) {    // wall collision
        digitalWrite(wallPin, HIGH);
      } else if (data == -2) {      // ForceStopSolenoid
        Serial.println("ForceStopped!");       
        digitalWrite(waterPin, LOW);
        digitalWrite(ledPin, LOW);
      } else if (data == -3) {      // Trigger the cameras
        digitalWrite(camTrigPin1, HIGH);
        digitalWrite(camTrigPin2, HIGH);
        delay(1);  // Adjust this delay if a 1 ms high is insufficient to trigger camera exposure
        digitalWrite(camTrigPin1, LOW);
        digitalWrite(camTrigPin2, LOW);        
      } else {        // Water
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





