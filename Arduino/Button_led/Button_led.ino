int ledPin = 13;
int buttonPin = 10;

void setup() {
  pinMode(ledPin, OUTPUT);
  pinMode(buttonPin, INPUT);
}

void loop(){
  int val = digitalRead(buttonPin);
  if(val == 0) {
    digitalWrite(ledPin,HIGH);
  } else {
    digitalWrite(ledPin,LOW);
  }
}
  
