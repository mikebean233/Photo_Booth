// This code has been tested with an Arduino Uno R3 only

const byte PINS[] = {2,3,4,5,6,7,8,9};
const byte PINCOUNT = sizeof(pins);
byte oldValue = 0;
int i;

void setup() {
    for(i = 0; i < PINCOUNT; ++i)
      pinMode(PINS[i], OUTPUT);

    Serial.begin(9600);
    while(!Serial){};
}

void loop() {
  byte newValue = 0;
  
  for(i = 0; i < PINCOUNT; ++i)
    newValue |= digitalRead(PINS[i]) << i;
    
  if(newValue != oldValue){
    Serial.write(newValue);
    oldValue = newValue;
    Serial.flush();
  }
    
  delay(50);
}
