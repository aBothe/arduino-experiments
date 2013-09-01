int brightnessLed=9;
int brightnessSensorValue = 0;

int tempLed = 10;
float tempLedSensorValue = 0;

int L1 = 3, L2 = 5, L3 = 6;
int wait=0;

void setup() {
  
  Serial.begin(9600);
  
  // declare the ledPin as an OUTPUT:
  pinMode(L1, OUTPUT);
  pinMode(L2, OUTPUT);
  pinMode(L3, OUTPUT);
  
  pinMode(brightnessLed, OUTPUT);
  pinMode(tempLed, OUTPUT);
}

void loop() {
  sendSensorUpdates();
  
  if(Serial.available())
  {
      char c = Serial.read();   
      byte r = Serial.read();
      byte g = Serial.read();
      byte b = Serial.read(); 
    
    switch(c)
    {
      case '#':
      brightnessSensorValue = 0;
      tempLedSensorValue = 0;
      sendSensorUpdates();
      break;
      case '$':
      analogWrite(L3, 255-r);
      analogWrite(L1, 255-g);
      analogWrite(L2, 255-b);
      break;
      
    case 'a':
    case 1:
     digitalWrite(L1,HIGH); 
     analogWrite(L2,255); 
     digitalWrite(L3,HIGH);
    break;
    case 'b':
    case 2:
     digitalWrite(L1,0);
     digitalWrite(L2,0);
     digitalWrite(L3,HIGH);
    break;
    case 'c':
    case 3:
     analogWrite(L1,0); 
     analogWrite(L2,0); 
     analogWrite(L3,255);
    break;
    case '\n':
    case '\r':
    case 0:
    break;
    default:
    break;
     analogWrite(L1,HIGH); 
     analogWrite(L2,HIGH); 
     analogWrite(L3,HIGH);
     break;
    }
  }
  delay(100);  
}

float newVal = 0;
float inp= 0;
float celsius = 0; 

void sendSensorUpdates()
{
  int n = analogRead(A0);
  
  if(abs(n - brightnessSensorValue) > 1)
  {
    analogWrite(brightnessLed, ((float)n / 1024) * 255);
    brightnessSensorValue = n;
    //Serial.print("Brightness:\t");    Serial.println(n);
    Serial.write(255);
    Serial.write((byte*)&n,2);
  }
  delay(2);
  analogRead(A1);
  delay(2);
  newVal = analogRead(A2);
    
  if(fabs(tempLedSensorValue - newVal) > 1)
  {
    tempLedSensorValue = newVal;
    inp = (newVal * 5000.0) / 1024.0;
    celsius = inp / 10.0;
    analogWrite(tempLed, ((float)newVal / 1024.0) * 255.0);
    //Serial.print("Temperature:\t");    Serial.println(celsius,2);
    
    Serial.write(254);
    Serial.write((byte)celsius);
    Serial.write(celsius < 0 ? 1 : 0);
  }
}
