// the setup routine runs once when you press reset:

/*
int ledOnBoard = 13;
int ledBottomLeft = 5;
int ledBottomMiddle = 7;
int ledBottomRight = 8;
int ledTopMiddle = 9;
//*/


int ledOnBoard = 1;
int ledHeadLeft = 0;
int ledHeadRight = 1;
int ledBottomLeft = 2;
int ledBottomRight = 3;
int ledBackMiddle = 4;
//*/

double freq = (1.0 / 37.0) * 1000.0;
//double freq = 1 * 1000.0;
//


int stringLen = 5;
uint8_t ledHeadLeftID[5] =     { 1, 0, 0, 1, 0}; //red 10001 1000110001
uint8_t ledHeadRightID[5] =    { 1, 0, 0, 0, 1}; //green 10010 1001010010
uint8_t ledBottomLeftID[5] =   { 1, 0, 0, 1, 1};  //blue 10011 1001110011
uint8_t ledBottomRightID[5] =  { 1, 0, 1, 1, 1};  //magenta 10111 1011110111
uint8_t ledBackMiddleID[5] =   { 1, 0, 1, 0, 1};  //orange 10101 1010110101



bool testLed = false;
bool manchester = true;

int frameCount = 0;
int transmitCount = 0;
uint8_t clockFlip = 1;

uint8_t getEncoded(uint8_t val)
{
  if(manchester)
  {
    return val ^ clockFlip;
  }
  else
  {
    return val;
  }
}

void loop() {
 
  digitalWrite(ledOnBoard, getEncoded(ledHeadLeftID[frameCount]) ? HIGH : LOW);
  
  digitalWrite(ledHeadLeft, getEncoded(ledHeadLeftID[frameCount]) ? HIGH : LOW);
  digitalWrite(ledHeadRight, getEncoded(ledHeadRightID[frameCount]) ? HIGH : LOW);  
  digitalWrite(ledBottomLeft, getEncoded(ledBottomLeftID[frameCount]) ? HIGH : LOW);
  digitalWrite(ledBottomRight, getEncoded(ledBottomRightID[frameCount]) ? HIGH : LOW);
  digitalWrite(ledBackMiddle, getEncoded(ledBackMiddleID[frameCount]) ? HIGH : LOW);
  
  clockFlip = !clockFlip;
  transmitCount++;
  
  testLed = !testLed;

  delay(freq);
  
  if(!manchester || transmitCount == 2)
  {
    transmitCount = 0;
    frameCount++;
    if(frameCount == stringLen)
    {
      frameCount = 0;
      
      if(!manchester)
      {
        digitalWrite(ledHeadLeft, LOW);
        digitalWrite(ledHeadRight, LOW);    
        digitalWrite(ledBottomLeft, LOW);
        digitalWrite(ledBottomRight, LOW);
        digitalWrite(ledBackMiddle, LOW);
        
        delay(freq * 4);
      }    
    }
  }
}


void setup() {                
  
  //16MHz
  
  // initialize the digital pin as an output.
  //pinMode(0, OUTPUT); //LED on Model B
  pinMode(ledOnBoard, OUTPUT); //LED on Model A
  
  pinMode(ledHeadLeft, OUTPUT);
  pinMode(ledHeadRight, OUTPUT);
  pinMode(ledBottomLeft, OUTPUT);
  pinMode(ledBottomRight, OUTPUT);
  pinMode(ledBackMiddle, OUTPUT);
  
}
