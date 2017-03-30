
// pins for the LEDs:
const int ledPin = 13;
const int waterPin = 8;
const int syncPin = 7;
const int wallPin = 4;
const int vPin = 3;  // hack to keep the valve from flickering on program load, which leaks water everywhere
const int touchPin = 2;

void setup() {
  
  // initialize serial:
  Serial.begin(9600);
  //while(!Serial);  // Wait on serial to be running
  Serial.setTimeout(1);
  
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
      // sync msg
      if ( data == 0 )
      {
        Serial.println("Ard:Synced!");
        digitalWrite(syncPin, HIGH);
      }
      // wall collision
      else if ( data == -1 )
      {
        digitalWrite(wallPin, HIGH);
      }
      // ForceStopSolenoid
      else if (data == -2)
      {
        Serial.println("ForceStopped!");       
        digitalWrite(waterPin, LOW);
        digitalWrite(ledPin, LOW);
      }
      // Water
      else
      {
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





