using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap;
using Assets.LeapMotion.DemoResources.Scripts;
using Emgu.CV.Structure;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Threading;
using Assets.Scripts;

public enum SampleType
{
    TriangleHouse = 0,
    Begemot,
    PingPong
}

public class CamCom : MonoBehaviour {

    #region Variables internal

    public const SampleType SAMPLE_TYPE = SampleType.PingPong;

    public const double PRECENTAGE_RESET_ID = 0.6;
    public const double PRECENTAGE_QUALIFY = 0.6;

    public const double FREQ_FRAME_CAPTURE = (1.0 / 111.0); //Faster - leap works by 9milisec frames // 55.55 //111.0
    public const double FREQ_FRAME_COM = (1.0 / 37.0); //Slower //18.52 //37.0
    public const int SAMPLE_RATE = 3;

    

    /*/
    public const double TIMEOUT_EMPTY_DATA_FRAMES = 16d; //16d; //silence since last data (1/2 at real)
    public const double MAX_DATA_FRAMES_BUFFER = 256;// 256; //At capture speed
    public const double DISTANCE_DEVIATION = 4.0f; //5.0f;
    private const int THERESHOLD_STEP = 10;
    private const int THERESHOLD_MIN = 65; 
    private const int THERESHOLD_MAX = 255;
    private const bool USE_MANCHESTER_CODE = true;
    //*/

    /*
    //pong parity
    public const bool USE_PARITY_BITS = true;
    public const double MAX_DATA_FRAMES_BUFFER = 30 * 3;// 256; //At capture speed
    public const double DISTANCE_DEVIATION = 40.0f; //5.0f;
    private const int THERESHOLD_STEP = 20;
    private const int THERESHOLD_MIN = 195;
    private const int THERESHOLD_MAX = 255;
    private const bool USE_MANCHESTER_CODE = false;
    private const int ZERO_BREAK = 3; //Zeroes before transmission
    //*/

    
    //pong simple
    public const bool USE_PARITY_BITS = false;
    public const double MAX_DATA_FRAMES_BUFFER = 11 * 3;// 256; //At capture speed
    public const double DISTANCE_DEVIATION = 40.0f; //5.0f;
    private const int THERESHOLD_STEP = 20;
    private const int THERESHOLD_MIN = 195;
    private const int THERESHOLD_MAX = 255;
    private const bool USE_MANCHESTER_CODE = false;
    private const int ZERO_BREAK = 0; //Zeroes before transmission
     //*/

    /*
    //pong manchester
    public const bool USE_PARITY_BITS = false;
    public const double MAX_DATA_FRAMES_BUFFER = 30 * 3;// 256; //At capture speed
    public const double DISTANCE_DEVIATION = 40.0f; //5.0f;
    private const int THERESHOLD_STEP = 20;
    private const int THERESHOLD_MIN = 195;
    private const int THERESHOLD_MAX = 255;
    private const bool USE_MANCHESTER_CODE = true;
    private const int ZERO_BREAK = 3; //Zeroes before transmission
    //*/

    public const int MEDIAN_MATRIX_SIZE = 3;
    public const double RESIZE_RATIO = 0.5d;
    public const double AREA_MIN = 1.0f;
    public const double AREA_MAX = 80.0f;

    private const int MAX_BUFFER_SIZE = 3 * 30;

    

    private const int COUNT_TO_COUNT_WITHOUT_MANCHESTER = 1;
    

    private const int  DEBUG_MIN_BINARY_STRING_TO_DISPLAY_AS_YELLOW = 2;
    private const bool DEBUG_LOG_ID_BLOB_THERESHOLD = false;
    private const bool DEBUG_SAVE_ALL_FRAMES_STRIDE = true; //pressing key "R"
    private const bool DEBUG_SAVE_ALL_FRAMES_3_SEK = false;
    private const bool DEBUG_LOG_LEVEL_COUNT = false;
    private const bool DEBUG_LOG_LEVEL_DECODED = false;
    private const bool DEBUG_LOG_LEVEL_ENCODED = false;    
    private const bool DEBUG_LOG_TIME_TO_CAPTURE = false;

    private static int _width = 0;
    private static int _height = 0;
    private static bool _debugSaveStride = false;
    private static int _debugSaveStrideCount = 0;

    private static long _debugLastCapture = 0;

    private static HandController _handController = null;

    private static bool _isRunning = true;
    private static bool _isTrackingEnabled = true;
    private static bool _isResetTrackingLeft = false;
    private static bool _isResetTrackingRight = false;

    private static object _mutexFramesRaw = new object();
    private static Queue<KeyValuePair<long, byte[]>> _framesLeft = new Queue<KeyValuePair<long, byte[]>>();
    private static Queue<KeyValuePair<long, byte[]>> _framesRight = new Queue<KeyValuePair<long, byte[]>>();

    private static List<CamBlob> _blobsLeft = new List<CamBlob>();
    private static List<CamBlob> _blobsRight = new List<CamBlob>();

    private static long _debugLastImageFlush = 0;
    private static float _debugLastLogTime = 0f;
    private static string _debugLog = "";
    private static object _mutexDebuglog = new object();

    private static object _mutexBlobsObject = new object();
    private static Dictionary<int, CamBlob> _blobsObjectLeft = new Dictionary<int, CamBlob>();
    private static Dictionary<int, CamBlob> _blobsObjectRight = new Dictionary<int, CamBlob>();

    private string _debugLogTime = "";
    private DateTime _debugTime = DateTime.Now;
    private bool _debugTracked = false;
    private static int _debugErrorCount = 0;
    private static int _debugCountGood = 0;

    #endregion

    #region Variables blob definitions

    

    /*
    private static byte[] ledBottomLeftID = new byte[8] { 1, 0, 0, 1, 0, 0, 0, 1 };
    private static byte[] ledBottomRightID = new byte[8] { 1, 0, 0, 0, 0, 1, 0, 1 };
    private static byte[] ledTopMiddleID = new byte[8] { 1, 0, 0, 0, 0, 0, 1, 1 }; 
     */

    
    //public static int MARKER_LENGTH = 4;
    private static byte[] ledBottomLeftID = new byte[4] { 0, 0, 0, 1 };
    private static byte[] ledBottomRightID = new byte[4] { 0, 1, 0, 1 };
    private static byte[] ledTopMiddleID = new byte[4] {  0, 0, 1, 1 };

    //public static int MARKER_LENGTH = 4;
    private static byte[] ledBegemotHeadLeftID = new byte[5] { 1, 0, 0, 0, 1}; //red 10001 1000110001
    private static byte[] ledBegemotHeadRightID = new byte[5] { 1, 0, 0, 1, 0 }; //green 10010 1001010010
    private static byte[] ledBegemotBottomLeftID = new byte[5] { 1, 0, 0, 1, 1 };  //blue 10011 1001110011
    private static byte[] ledBegemotBottomRightID = new byte[5] { 1, 0, 1, 1, 1 };  //magenta 10111 101111011
    private static byte[] ledBegemotBackMiddleID = new byte[5] { 1, 0, 1, 0, 1 };  //orange 10101 1010110101
    //*/

    /*
    public static int MARKER_LENGTH = 5;
    private static byte[] ledPong1 = new byte[2] { 1, 0 }; //red 10100 101001010010100
    private static byte[] ledPong2 = new byte[2] { 0, 1 }; //green 10101 101011010110101
    private static byte[] ledPong3 = new byte[2] { 1, 1 };  //blue 11100 1110011100
    */

    /*
    public static int MARKER_LENGTH = 15;
    private static byte[] ledPong1 = new byte[15] { 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1 }; //red 10100 101001010010100
    private static byte[] ledPong2 = new byte[15] { 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 1, 0, 0, 1, 1 }; //green 10101 101011010110101
    private static byte[] ledPong3 = new byte[15] { 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 1, 0, 1, 0, 1 };  //blue 11100 1110011100
    //*/

    
    public static int MARKER_LENGTH = 5;
    private static byte[] ledPong1 = new byte[5] { 1, 0, 1, 0, 0 }; //red 10100 101001010010100
    private static byte[] ledPong2 = new byte[5] { 1, 0, 1, 0, 1 }; //green 10101 101011010110101
    private static byte[] ledPong3 = new byte[5] { 1, 1, 1, 0, 0 };  //blue 11100 1110011100
    //*/

    /*
    //PARITY
    public static int MARKER_LENGTH = 5; //Inlcuding parity bits
    private static byte[] ledPong1 = new byte[2] {  0, 1 }; //red 10100 1010010100
    private static byte[] ledPong2 = new byte[2] {  1, 0 }; //green 10110 1011010110
    private static byte[] ledPong3 = new byte[2] {  1, 1 };  //blue 11100 1110011100
    //*/

    /*
    public static int MARKER_LENGTH = 4;
    private static byte[] ledPong1 = new byte[4] { 0, 1, 0, 1 }; //red   0101 , machester 
    private static byte[] ledPong2 = new byte[4] { 0, 1, 1, 1 }; //green 0111
    private static byte[] ledPong3 = new byte[4] { 0, 0, 0, 1 };  //blue 0011
    //*/

    #endregion

    #region Properties

    public bool IsTrackingEnabled
    {
        get
        {
            return _isTrackingEnabled;
        }
    }
    
    public CamBlob[] BlobsObjectLeft
    {
        get
        {
            lock (_mutexBlobsObject)
            {
                CamBlob[] Temp = new CamBlob[_blobsObjectLeft.Count];
                _blobsObjectLeft.Values.CopyTo(Temp, 0);

                return Temp;
            }
        }
    }

    public CamBlob[] BlobsObjectRight
    {
        get
        {
            lock (_mutexBlobsObject)
            {
                CamBlob[] Temp = new CamBlob[_blobsObjectRight.Count];
                _blobsObjectRight.Values.CopyTo(Temp, 0);

                return Temp;
            }
        }
    } 

    #endregion

    #region Threads

    //Make buffer of Leap frames
    private Thread _threadBuffer = new Thread(delegate()
    {
        long lastTimeStep = 0;
        long lastTimeStepTicks = 0;

        while (_isRunning)
        {
            try
            {                
                if (_isTrackingEnabled)
                {

                    Frame frame = _handController.GetFrame();
                    //frame.Id
                    if (frame.Images.Count > 0)
                    {
                        //long timeMilisec = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        long timeMilisec = frame.Timestamp / 1000;
                        long deltaMilisec = timeMilisec - lastTimeStep;
                        long bestDelta = deltaMilisec;

                        if (deltaMilisec >= FREQ_FRAME_CAPTURE * 1000)
                        {
                            bool better = false;
                            for (int i = 1; i < 59; i++)
                            {
                                Frame frameTest = _handController.GetFrame(i);
                                long id = frameTest.Id;

                                if (id > 0)
                                {
                                    timeMilisec = frameTest.Timestamp / 1000;
                                    if (timeMilisec > lastTimeStep)
                                    {
                                        deltaMilisec = timeMilisec - lastTimeStep;

                                        if (Math.Abs(deltaMilisec - FREQ_FRAME_CAPTURE * 1000) < Math.Abs(bestDelta - FREQ_FRAME_CAPTURE * 1000))
                                        {
                                            bestDelta = deltaMilisec;
                                            frame = frameTest;
                                            better = true;
                                        }
                                        else
                                        {
                                            if (better)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            lastTimeStep = frame.Timestamp / 1000; ;

                            _width = frame.Images[0].Width;
                            _height = frame.Images[0].Height;

                            byte[] bytesLeft = new byte[frame.Images[0].Data.Length];
                            Buffer.BlockCopy(frame.Images[0].Data, 0, bytesLeft, 0, bytesLeft.Length);

                            byte[] bytesRight = new byte[frame.Images[1].Data.Length];
                            Buffer.BlockCopy(frame.Images[1].Data, 0, bytesRight, 0, bytesRight.Length);

                            lock (_mutexFramesRaw)
                            {
                                _framesLeft.Enqueue(new KeyValuePair<long, byte[]>(lastTimeStep, bytesLeft));
                                _framesRight.Enqueue(new KeyValuePair<long, byte[]>(lastTimeStep, bytesRight));
                            }
                        }
                    }


                    Thread.Sleep((int)(FREQ_FRAME_CAPTURE * 300));
                }
                else
                {
                    Thread.Sleep(200);
                }

            }
            catch (Exception exception)
            {
                lock (_mutexDebuglog)
                {
                    _debugLog += exception.Message + "\n" + exception.StackTrace + "\n";
                }
            }
        }
    });

    //Process buffer
    private Thread _threadProcessLeft = new Thread(delegate()
    {
        while (_isRunning)
        {
            try
            {
                if (_isTrackingEnabled)
                {
                    KeyValuePair<long, byte[]>? frameLeft = null;

                    lock (_mutexFramesRaw)
                    {
                        if (_framesLeft.Count > 0)
                        {
                            frameLeft = _framesLeft.Dequeue();
                        }
                    }

                    if (frameLeft != null)
                    {
                        if (_isResetTrackingLeft && _blobsLeft.Count > 0)
                        {
                            _isResetTrackingLeft = false;
                            _blobsLeft.Clear();
                        }

                        Emgu.CV.Image<Gray, byte> imageLeft = GetImageCV(frameLeft.Value.Value);
                        UpdateBlob(frameLeft.Value.Key, imageLeft, _blobsLeft, _blobsObjectLeft);

                    }
                }
                else
                {
                    Thread.Sleep(200);
                }

            }
            catch (Exception exception)
            {
                lock (_mutexDebuglog)
                {
                    _debugLog += exception.Message + "\n" + exception.StackTrace + "\n";
                }
            }

        }
    });

    private Thread _threadProcessRight = new Thread(delegate()
    {
        while (_isRunning)
        {
            try
            {

                if (_isTrackingEnabled)
                {
                    KeyValuePair<long, byte[]>? frameRight = null;

                    lock (_mutexFramesRaw)
                    {
                        if (_framesRight.Count > 0)
                        {
                            frameRight = _framesRight.Dequeue();
                        }
                    }

                    if (frameRight != null)
                    {
                        if (_isResetTrackingRight && _blobsRight.Count > 0)
                        {
                            _isResetTrackingRight = false;
                            _blobsRight.Clear();
                        }

                        Emgu.CV.Image<Gray, byte> imageRight = GetImageCV(frameRight.Value.Value);
                        UpdateBlob(frameRight.Value.Key, imageRight, _blobsRight, _blobsObjectRight);

                    }
                }
                else
                {
                    if (_blobsRight.Count > 0)
                    {
                        _blobsRight.Clear();
                    }
                    Thread.Sleep(200);
                }

            }
            catch (Exception exception)
            {
                lock (_mutexDebuglog)
                {
                    _debugLog = exception.Message + "\n" + exception.StackTrace + "\n";
                    if(exception.InnerException != null)
                    {
                        _debugLog += exception.InnerException.Message;
                    }
                }
            }

           
        }
    });
    
    #endregion

    #region Functions internal

    // Blob detection @ frame
    private static void UpdateBlob(long time, Emgu.CV.Image<Gray, byte> image, List<CamBlob> blobs, Dictionary<int, CamBlob> blobsObject)
    {
        bool debugTheresholds = false;
        bool debugLogDigits = DEBUG_LOG_LEVEL_DECODED;
        bool debugLogDigitsCount = DEBUG_LOG_LEVEL_COUNT;
        bool debugLogDigitsTime = DEBUG_LOG_LEVEL_ENCODED;
        bool debugSaveBlobsAll = false;

        if (DEBUG_SAVE_ALL_FRAMES_STRIDE && blobs == _blobsLeft)
        {
            if (!_debugSaveStride)
            {
                _debugSaveStrideCount = 0;
            }
            else
            {
                _debugSaveStrideCount++;
                debugSaveBlobsAll = true;
            }
            debugTheresholds = _debugSaveStride;
        }

        if (_debugLastImageFlush == 0)
        {
            _debugLastImageFlush = time;
        }
        if (DEBUG_SAVE_ALL_FRAMES_3_SEK &&
            time - _debugLastImageFlush > 2 * 1000 && 
            blobsObject == _blobsObjectLeft)
        {
            debugSaveBlobsAll = true;
        }

        if (blobsObject == _blobsObjectLeft && _debugLastCapture == 0f)
        {
            _debugLastCapture = time;
        }

        if (debugTheresholds)
        {
            image.Save(@"C:\temp\orig\" + _debugSaveStrideCount.ToString() + ".png");
        }
        
        //Apply median filter to remove small kernels around korners
        //int medianKernelSize = MEDIAN_MATRIX_SIZE;
        //image = image.SmoothMedian(medianKernelSize);

        if (RESIZE_RATIO != 1d)
        {
            //THEN reduce image size
          //  image = image.Resize(RESIZE_RATIO, INTER.CV_INTER_NN);
        }

        if (debugTheresholds)
        {
            image.Save(@"C:\temp\after\" + _debugSaveStrideCount.ToString() + ".png");
        }

        //Thereshold filters
        int theresholdDelta = THERESHOLD_STEP;
        for (int thereshold = THERESHOLD_MAX; thereshold >= THERESHOLD_MIN; thereshold -= theresholdDelta)
        {
            //Then go over theresholds
            //Emgu.CV.Image<Gray, byte> greyThreshImg = image.ThresholdBinaryInv(new Gray(thereshold), new Gray(255));
            Emgu.CV.Image<Gray, byte> greyThreshImg = image.ThresholdTrunc(new Gray(thereshold)).ThresholdBinary(new Gray(thereshold - theresholdDelta), new Gray(255));
            greyThreshImg = greyThreshImg.SmoothGaussian(5);
            greyThreshImg = greyThreshImg.SmoothMedian(3);
            //greyThreshImg = greyThreshImg.ThresholdBinaryInv(new Gray(0), new Gray(255));
            greyThreshImg = greyThreshImg.ThresholdBinary(new Gray(0), new Gray(255));

            if (RESIZE_RATIO != 1d)
            {
                //THEN reduce image size
                greyThreshImg = greyThreshImg.Resize(RESIZE_RATIO, INTER.CV_INTER_NN);
            }


            //greyThreshImg = greyThreshImg.SmoothMedian(MEDIAN_MATRIX_SIZE);

            if (debugTheresholds)
            {
                greyThreshImg.Save(@"C:\temp\thereshold\" + _debugSaveStrideCount.ToString() + "-" + thereshold.ToString() + ".png");
            }

            //Find contours

            Emgu.CV.Cvb.CvBlobs resultingImgBlobs = new Emgu.CV.Cvb.CvBlobs();
            Emgu.CV.Cvb.CvBlobDetector bDetect = new Emgu.CV.Cvb.CvBlobDetector();
            uint numWebcamBlobsFound = bDetect.Detect(greyThreshImg, resultingImgBlobs);

           // for (Emgu.CV.Contour<System.Drawing.Point> contours = greyThreshImg.FindContours(); contours != null; contours = contours.HNext)
            foreach (Emgu.CV.Cvb.CvBlob targetBlob in resultingImgBlobs.Values)
            {
                //System.Drawing.Rectangle rect = contours.BoundingRectangle;
                System.Drawing.Rectangle rect = targetBlob.BoundingBox;
                float area = rect.Width * rect.Height;

                //Then select based on area
                if (area <= AREA_MAX * CamCom.RESIZE_RATIO && area >= AREA_MIN)
                {
                    Vector2 positionMiddle = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
                    CamBlob found = null;

                    int iBlob = 0;
                    while (iBlob < blobs.Count)
                    {
                        CamBlob blob = blobs[iBlob];

                        if (blob.Thereshold == thereshold &&
                            (blob.Position - positionMiddle).sqrMagnitude <= DISTANCE_DEVIATION)
                        {
                            found = blob;
                            found.Position = positionMiddle;
                        }

                        iBlob++;
                    }

                    if (found == null)
                    {
                        found = new CamBlob()
                        {
                            Position = positionMiddle,
                            Size = area,
                            Thereshold = (byte)thereshold
                        };
                        blobs.Add(found);
                    }

                    //Very important CHECK (might overlap blobs)
                    if (found.LastTime != time)
                    {
                        found.LastTime = time;
                        found.LastTimePositive = time;

                        found.Data.Add(new KeyValuePair<long, byte>(time, 1));
                    }
                } //area
            }
        }

        
        List<byte[]> patterns = new List<byte[]>();

        switch(SAMPLE_TYPE)
        {
            case SampleType.TriangleHouse:
                patterns.Add(ledBottomLeftID);
                patterns.Add(ledBottomRightID);
                patterns.Add(ledTopMiddleID);
                break;
            case SampleType.Begemot:
                patterns.Add(ledBegemotHeadLeftID);
                patterns.Add(ledBegemotHeadRightID);
                patterns.Add(ledBegemotBottomLeftID);
                patterns.Add(ledBegemotBottomRightID);
                patterns.Add(ledBegemotBackMiddleID);
                break;
            case SampleType.PingPong:
                patterns.Add(ledPong1);
                patterns.Add(ledPong2);
                patterns.Add(ledPong3);
                break;
        }
        
        List<Bgr> colorsPatterns = new List<Bgr>();
        colorsPatterns.Add(new Bgr(0, 0, 255));
        colorsPatterns.Add(new Bgr(0, 255, 0));
        colorsPatterns.Add(new Bgr(255, 0, 0));
        colorsPatterns.Add(new Bgr(255, 0, 255));
        colorsPatterns.Add(new Bgr(0, 120, 255));

        //Detect blobs by data
        {
            int blobFoundCount = 0;
            Emgu.CV.Image<Bgr, byte> output = null;

            if (debugSaveBlobsAll)
                output = image.Convert<Bgr, byte>();

            foreach (CamBlob blob in blobs)
            {

                int iStartPos = 0;                

                if (ZERO_BREAK > 0)
                {
                    if (blob.Data.Count > 0 &&
                        blob.Data[blob.Data.Count - 1].Value == 1) //Last value must be 1 in order to detect corret zero break
                    {
                        int zeroBreak = ZERO_BREAK * SAMPLE_RATE;
                        int zeroCount = 0;

                        //Trim out Zero breaks form end to front, in case of using manchester encoding where 
                        //each byte goes like 1 0 1 0, then zero beaks 000
                        int i = blob.Data.Count - 1;
                        blob.StartMarkerCount = 0;

                        while (i >= 0)
                        {
                            long timeValue = blob.Data[i].Key;
                            byte value = blob.Data[i].Value;

                            if (value == 0 ||
                                value == 3 ||
                                value == 8)
                            {
                                if (value == 3)
                                {
                                    blob.StartMarkerCount++;
                                }
                                zeroCount++;
                            }
                            else
                            {
                                //value == 1

                                if (zeroCount >= zeroBreak - SAMPLE_RATE / 2d)
                                {
                                    int iAdd = 1;
                                    if (zeroCount > zeroBreak)
                                    {
                                        iAdd += (zeroCount - zeroBreak);
                                        zeroCount = zeroBreak;
                                    }

                                    iStartPos = i + iAdd;

                                    if (iStartPos + zeroCount <= blob.Data.Count)
                                    {
                                        //zero break found, then remove it 
                                        blob.Data.RemoveRange(iStartPos, zeroCount);
                                        blob.Data.Insert(iStartPos, new KeyValuePair<long, byte>(timeValue + iAdd * (long)(1000d * FREQ_FRAME_CAPTURE), 3)); // 3 - marker for transmission                                        
                                        blob.StartMarkerCount++;
                                    }
                                    else
                                    {
                                        Debug.Log("RemoveRange");
                                    }
                                }

                                zeroCount = 0;
                            }

                            i--;
                        }
                    }
                    
                }
                else
                {
                    blob.PositiveCount = 0;

                    //First find data start position
                    bool zeroFound = false;
                    bool isStartFound = false;
                    for (int i = 0; i < blob.Data.Count; i++)
                    {
                        if (!zeroFound)
                        {
                            if (blob.Data[i].Value == 0)
                            {
                                zeroFound = true;
                            }
                        }
                        else
                        {
                            if (blob.Data[i].Value == 1)
                            {
                                if (!isStartFound)
                                {
                                    isStartFound = true;
                                    iStartPos = i;
                                }
                            }
                        }

                        if (blob.Data[i].Value == 1)
                        {
                            blob.PositiveCount++;
                        }                        
                    }                    
                }


                blob.LastStartPos = iStartPos;

                
                //Fill missing frames as '?' bits => 8
                {
                    int i = 0;
                    long milisecBefore = 0;
                    while(i < blob.Data.Count)
                    {
                        KeyValuePair<long, byte> val = blob.Data[i];

                        if (i > 0 && 
                            val.Key > 0 &&
                            milisecBefore > 0 &&
                            val.Value != 3)
                        {
                            long delta = val.Key - milisecBefore;
                            if (delta > 1.5 * FREQ_FRAME_COM * 1000 &&
                                delta < 4 * FREQ_FRAME_COM)
                            {
                                double framesInBetween = Math.Round((delta / ((double)FREQ_FRAME_COM * 1000.0)) - 1.0);
                                for(int j = 0; j < framesInBetween; j++)
                                {
                                    //Insert random bit
                                    blob.Data.Insert(i + 1, new KeyValuePair<long, byte>(milisecBefore + (long)((j + 1) * ((double)FREQ_FRAME_COM * 1000.0)), (byte)(time % 2 == 0 ? 0 : 1)));
                                }
                            }
                        }

                        i++;
                        milisecBefore = val.Key;
                    }
                }

                //Error detection
                //Fix if single frame error 101 => 111
                {
                    
                    for (int i = iStartPos + 1; i < blob.Data.Count - 1; i++)
                    {
                        KeyValuePair<long, byte> val = blob.Data[i];
                        byte bit = val.Value;
                        long timePos = val.Key;

                        if(blob.Data[i].Value != blob.Data[i + 1].Value && 
                            blob.Data[i].Value != blob.Data[i - 1].Value &&
                            blob.Data[i].Value != 3 && //Cannot take out 3
                            blob.Data[i - 1].Value != 8 &&
                            blob.Data[i - 1].Value != 3) 
                        {
                            blob.Data[i] = new KeyValuePair<long,byte>(blob.Data[i].Key, blob.Data[i - 1].Value);
                        }                        
                    }
                    
                }

                /*
                List<string> debugChar = new List<string>();
                foreach(KeyValuePair<long, byte> val in blob.Data)
                {
                    debugChar.Add(val.Value.ToString());
                }
                string debugString = String.Join("", debugChar.ToArray());
                if(debugString.Contains("111000000111000111000111"))
                {
                    Debug.Log("Debug #1");
                }
                 */

                //Decoding
                {
                    int sampleRate = SAMPLE_RATE;
                    int count = 0;
                    bool sync = false;
                    bool skip = false;
                    bool isZeroMarkerFound = false;
                    bool addToDecoded = false;
                    long lastTime = 0;
                    byte lastBit = 0;

                    blob.DecodedData.Clear();
                    blob.DecodedCount = 0;
                    blob.FailedCount = 0;

                    byte decodedBit = 0;
                    int unknownBitCount = 0;

                    for (int i = iStartPos; i < blob.Data.Count; i++)
                    {
                        KeyValuePair<long, byte> val = blob.Data[i];
                        byte bit = val.Value;
                        long timePos = val.Key;
                        addToDecoded = false;

                        if (bit == 8)
                        {
                            bit = lastBit;
                            unknownBitCount++;
                        }
                        else
                        {
                            unknownBitCount = 0;
                        }

                        if (!USE_MANCHESTER_CODE)
                        {
                            #region No manchester
                            isZeroMarkerFound = true;

                            if (i > iStartPos)
                            {
                                if (lastBit != bit ||
                                    count >= sampleRate)
                                {
                                    if (lastBit == 3 ||
                                        count > 1 || 
                                        unknownBitCount > 0)
                                    {
                                        addToDecoded = true;
                                    }

                                    if (count >= sampleRate)
                                    {
                                        unknownBitCount = 0;
                                    }

                                    lastTime = timePos;
                                    count = 0;

                                    decodedBit = lastBit;
                                }
                            }

                            count++;
                            lastBit = bit;
                            #endregion
                        }
                        else
                        {
                            #region Using manchester

                            //Machester each

                            if (i > iStartPos)
                            {
                                if (lastBit != bit)
                                {
                                    if (bit == 3)
                                    {
                                        bit = 1;
                                        count = 3;
                                        addToDecoded = false;
                                        sync = false;
                                        isZeroMarkerFound = true;
                                    }
                                    //Long edge
                                    else if (count > sampleRate + sampleRate / 2 && count <= sampleRate * 2 + (int)Math.Ceiling(sampleRate / 2.0))
                                    {
                                        if (!sync)
                                        {
                                            skip = false;
                                            sync = true;

                                            blob.FailedCount++;
                                            blob.DecodedData.Add(7); //sync marker

                                            if (lastBit < bit)
                                            {
                                                //rise = 1
                                                decodedBit = 1;
                                            }
                                            else
                                            {
                                                decodedBit = 0;
                                            }
                                        }
                                        else
                                        {
                                            //is sync

                                            //flip
                                            decodedBit = (byte)(decodedBit == 1 ? 0 : 1);
                                        }

                                        addToDecoded = true; //only when bit changes       
                                        count = 0;
                                    }
                                    //Short edge
                                    else if (count <= sampleRate)
                                    {
                                        if (sync)
                                        {
                                            //half rate
                                            //decodedBit = decodedBit
                                            if (!skip)
                                            {
                                                addToDecoded = true; //only when bit changes
                                            }
                                            skip = !skip; //skipClock
                                        }
                                        count = 0;
                                    }
                                    else
                                    {
                                        if (sync)
                                        {
                                            sync = false;
                                            addToDecoded = false;
                                            isZeroMarkerFound = false;
                                        }
                                        count = 0;
                                    }
                                }
                            }

                            lastBit = bit;
                            count++;

                            #endregion
                        }

                        if (addToDecoded)
                        {
                            if (ZERO_BREAK == 0 ||
                                isZeroMarkerFound)
                            {
                                if (blob.DecodedData.Count == 0 && decodedBit == 0 && USE_MANCHESTER_CODE)
                                {
                                    Console.WriteLine("Error occured, must check why");
                                }
                                blob.DecodedData.Add(decodedBit);
                                blob.DecodedCount++;

                                /*
                                string decodedString = "";
                                foreach(byte each in blob.DecodedData )
                                {
                                    decodedString += each.ToString();
                                */
                            }
                        }

                    } //For decoded
                } //decoding


                if(USE_PARITY_BITS)
                {
                    List<byte> codedParity = new List<byte>();
                    List<byte> decodedParity = new List<byte>();
                    bool isParityBegin = false;
                    for(int i = 0; i < blob.DecodedData.Count; i++)
                    {
                        byte bit = blob.DecodedData[i];
                        if (bit == 3 || bit == 7)
                        {
                            if (isParityBegin)
                            {
                                if (DecodeParity(codedParity, decodedParity))
                                {
                                    break;
                                }
                            }

                            codedParity.Clear();
                            isParityBegin = true;
                        }
                        else
                        { 
                            if(isParityBegin)
                            {
                                codedParity.Add(bit);
                            }
                        }
                    }

                    if (decodedParity.Count == 0 && codedParity.Count == MARKER_LENGTH)
                    {
                        DecodeParity(codedParity, decodedParity);
                    }

                    blob.DecodedData = decodedParity;
                }


                int bestPattern = -1;
                int bestCountFinds = 0;

                int iPattern = 0;
                int countFinds = 0;
                bool found = false;
                

                foreach (byte[] pattern in patterns)
                {
                    int iPatternPos = 0;
                    countFinds = 0;

                    foreach (byte decodedBit in blob.DecodedData)
                    {
                        if (decodedBit == pattern[iPatternPos])
                        {
                            iPatternPos++;

                            if (iPatternPos == pattern.Length)
                            {
                                countFinds++;
                                iPatternPos = 0;

                                if (!USE_MANCHESTER_CODE || countFinds >= COUNT_TO_COUNT_WITHOUT_MANCHESTER)
                                {
                                    found = true;
                                    //break;
                                }
                            }
                        }
                        else
                        {
                            iPatternPos = 0;

                            if (decodedBit == pattern[iPatternPos])
                            {
                                iPatternPos++;
                            }
                        }
                    }

                    if (countFinds > bestCountFinds)
                    {
                        bestCountFinds = countFinds;
                        bestPattern = iPattern;
                    }

                    iPattern++;
                }

                if (found)
                {
                    blob.BestPatternCount = bestCountFinds;
                    blob.ID = blob.LastID = bestPattern;
                    blobFoundCount++;

                    //Ambiguous blobs
                    if (!blob.ClassifierCount.ContainsKey(blob.ID))
                    {
                        blob.ClassifierCount[blob.ID] = 0;
                    }
                    blob.ClassifierCount[blob.ID]++;

                }

                if (debugSaveBlobsAll)
                {
                    if (blob.DecodedCount >= DEBUG_MIN_BINARY_STRING_TO_DISPLAY_AS_YELLOW)
                    {
                        var color = new Bgr(51, 255, 255);
                        CvInvoke.cvCircle(output.Ptr, new System.Drawing.Point((int)blob.Position.x, (int)blob.Position.y), 3, color.MCvScalar, 1, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
                    }
                }

            } //for each blobs - decoding

            lock (_mutexBlobsObject)
            {
                //Remove blobs that are invalid
                {
                    var i = 0;
                    while( i < blobs.Count)
                    {
                        bool isRemoved = false;
                        CamBlob blob = blobs[i];                    

                        //When buffer full
                        if(blob.Data.Count >= MAX_DATA_FRAMES_BUFFER)
                        {
                            //manchester 
                            if(USE_MANCHESTER_CODE)
                            {
                                if(blob.DecodedData.Count < MARKER_LENGTH)
                                {
                                  //  Debug.Log(String.Format("drop : {0}", blob.DecodedData.Count));
                                    blobs.RemoveAt(i);
                                    isRemoved = true;
                                }                                
                            }
                            else
                            {
                                //Drop if 111 or 000 more than 60%
                                double min = Math.Max(blob.PositiveCount, blob.Data.Count - blob.PositiveCount);
                                double max = (double)blob.Data.Count;

                                if (min / max > 0.5 && blob.ID == -1)
                                {
                                    //Debug.Log(String.Format("drop2 : {0} {1}", blob.PositiveCount, blob.Data.Count));
                                    blobs.RemoveAt(i);
                                    isRemoved = true;
                                }                                
                            }
                        }

                        if(!isRemoved)
                        {
                            i++;
                        }
                    }
                }

                //Sum up same blobs over different theresholds  
                foreach (CamBlob blob in blobs)
                {
                    foreach (CamBlob blobTest in blobs)
                    {
                        if (blob.ID == blobTest.ID && blob.ID > 0 &&
                            blob != blobTest && blob.Thereshold != blobTest.Thereshold)
                        {
                            if ((blob.Position - blobTest.Position).sqrMagnitude < DISTANCE_DEVIATION)
                            {
                                blob.BestPatternCount += blobTest.BestPatternCount;
                            }
                        }
                    }
                }

                //Find best decription blob
                blobsObject.Clear();
                foreach (CamBlob blob in blobs)
                {
                    if (blob.ID >= 0)
                    {
                        bool addBlob = true;

                        if (blobsObject.ContainsKey(blob.ID))
                        {
                            if (blobsObject[blob.ID].BestPatternCount < blob.BestPatternCount)
                            {
                                blobsObject.Remove(blob.ID);
                            }
                            else
                            {
                                addBlob = false;
                            }
                        }

                        if (addBlob)
                        {
                            _debugCountGood++;
                            //Copy instance for static data
                            blobsObject.Add(blob.ID, new CamBlob()
                            {
                                ID = blob.ID,
                                Size = blob.Size,
                                Position = new Vector2(blob.Position.x, blob.Position.y),
                                BestPatternCount = blob.BestPatternCount,
                                Thereshold = blob.Thereshold
                            });
                        }
                    }
                }

                if (blobsObject == _blobsObjectLeft && blobsObject.Count >= 3 && (time - _debugLastCapture) > 500)
                {
                    if (DEBUG_LOG_TIME_TO_CAPTURE)
                    {
                        Debug.Log(String.Format("Capture time: {0}", (time - _debugLastCapture) / 1000.0) );
                    }

                    _debugLastCapture = time;
                }
            }


            if (debugSaveBlobsAll && _blobsLeft == blobs)
            {
                _debugLastImageFlush = time;

                lock (_mutexBlobsObject)
                {
                    foreach (KeyValuePair<int, CamBlob> each in blobsObject)
                    {
                        var color = colorsPatterns[each.Key];
                        CamBlob blob = each.Value;

                        if (DEBUG_LOG_ID_BLOB_THERESHOLD)
                        {
                            Debug.Log(String.Format("ID: {0} Thereshold: {1} Size: {2}", blob.ID, blob.Thereshold, blob.Size));                            
                        }

                        CvInvoke.cvCircle(output.Ptr, new System.Drawing.Point((int)blob.Position.x, (int)blob.Position.y), 3, color.MCvScalar, 2, Emgu.CV.CvEnum.LINE_TYPE.CV_AA, 0);
                    }
                }

                output.Save(@"C:\temp\result\" + _debugSaveStrideCount.ToString() + ".png");
            }
        }


        //Add non detected data & sanitize lists
        {
            string log = "";

            if (debugLogDigitsCount)
            {
                log = "_framesLeft = " + _framesLeft.Count.ToString() + "\n blobs = " + blobs.Count.ToString() + "\n";
            }

            int iBlob = 0;
            while (iBlob < blobs.Count)
            {
                CamBlob blob = blobs[iBlob];
              
                {
                    if (blob.LastTime != time)
                    {
                        blob.LastTime = time;
                        blob.Data.Add(new KeyValuePair<long, byte>(time, 0));
                    }

                    while (blob.Data.Count > MAX_DATA_FRAMES_BUFFER)
                    {
                        //Or too long observed frames
                        blob.Data.RemoveAt(0);
                    }

                    if (debugLogDigits)
                    {
                        //if (blob.LastID >= 0)
                        {
                            log += "id: " + blob.ID.ToString() + " \t " + blob.LastID.ToString() + " \t " + blob.Thereshold.ToString() + " \t " +
                            ((int)blob.Position.x).ToString() + " , " + ((int)blob.Position.y).ToString() + " hits: " + blob.BestPatternCount.ToString() + " startpos: " + blob.LastStartPos.ToString() + "\n";


                            for (int i = 0; i < blob.DecodedData.Count; i++)
                            {
                                log += blob.DecodedData[i].ToString();
                            }

                            log += "\n";

                            if (debugLogDigitsTime)
                            {
                                string logTime = "";
                                long firstPos = blob.Data[0].Value;

                                for (int i = 0; i < blob.Data.Count; i++)
                                {
                                    KeyValuePair<long, byte> val = blob.Data[i];
                                    byte bit = val.Value;
                                    long timePos = val.Key;

                                    log += bit.ToString();
                                    logTime += (timePos - firstPos).ToString() + " ";
                                    firstPos = timePos;
                                }

                                log += "\n" + logTime + "\n";
                            }
                        }




                    }
                }

                blob.ClassifierAlive++;

                blob.ID = -1;
                blob.BestPatternCount = 0;

                iBlob++;
            } //while loop


            lock (_mutexDebuglog)
            {
                _debugLog = log;
            }
        }

    }

    // Convert leap image into OpenCV image 
    private static Emgu.CV.Image<Gray, byte> GetImageCV(byte[] rawImageData)
    {
        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
        //set palette
        System.Drawing.Imaging.ColorPalette grayscale = bitmap.Palette;
        for (int i = 0; i < 256; i++)
        {
            grayscale.Entries[i] = System.Drawing.Color.FromArgb((int)255, i, i, i);
        }
        bitmap.Palette = grayscale;
        System.Drawing.Rectangle lockArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
        System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(lockArea, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
        System.Runtime.InteropServices.Marshal.Copy(rawImageData, 0, bitmapData.Scan0, _width * _height);
        bitmap.UnlockBits(bitmapData);

        Emgu.CV.Image<Gray, byte> imageOut = new Emgu.CV.Image<Gray, byte>(bitmap);

        return imageOut;
    }
    
    #endregion

    #region Functions Unity

    void OnApplicationQuit()
    {
        _isRunning = false;
    }

    //First before enabled
    void Awake()
    {
        _handController = GameObject.Find("HandController").GetComponent<HandController>();
    }

    // Use this for initialization
    // Before first update
    void Start()
    {
        if (!_threadProcessRight.IsAlive)
        {
            _threadBuffer.Start();
            _threadProcessLeft.Start();
            _threadProcessRight.Start();
        }
    }

    // Update is called once per frame
    void Update()
    {

        lock (_mutexDebuglog)
        {
            if (Time.time - _debugLastLogTime > 1f && _debugLog.Length > 0)
            {
                _debugLastLogTime = Time.time;


                Debug.Log(_debugLog);
                _debugLog = "";
            }
        }

        if(DEBUG_SAVE_ALL_FRAMES_STRIDE)
        {
            _debugSaveStride = Input.GetKey(KeyCode.R);            
        }

        if(Input.GetKey(KeyCode.L))
        {
            _debugTime = DateTime.Now;
            _debugTracked = false;
            ResetTracking();
        }

        if (_framesLeft.Count > MAX_BUFFER_SIZE)
        {
            ResetTracking();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            _debugLogTime = "^^^";
        }

        lock(_mutexBlobsObject)
        {
            if(_blobsObjectLeft.Count >= 1 ||
                _blobsObjectRight.Count >= 1)
            {
                if(!_debugTracked)
                {
                    _debugTracked = true;
                    _debugLogTime = String.Format("{0}\n", (DateTime.Now - _debugTime).TotalMilliseconds) + _debugLogTime;
                    Debug.Log(_debugLogTime);
                    Debug.Log("err:" + _debugErrorCount.ToString() + " , " + _debugCountGood.ToString());
                }
            }
        }
    }

    private static bool DecodeParity(List<byte> codedParity, List<byte> decodedParity)
    {
        if(codedParity.Count == MARKER_LENGTH)
        {
            byte p1 = (byte)(codedParity[2] ^ codedParity[4]);
            byte p2 = (byte)(codedParity[2]);
            byte p3 = (byte)(codedParity[4]);

            int c1 = p1 ^ codedParity[0];
            int c2 = p2 ^ codedParity[1];
            int c3 = p3 ^ codedParity[3];

            c1 = c1 << 2;
            c2 = c2 << 1;
            
            int sindrome = c1 ^ c2 ^ c3;

            if (sindrome == 0 ||
                sindrome == 3 ||
                sindrome == 5)
            {
                if (sindrome > 0)
                {
                    _debugErrorCount++;
                    codedParity[sindrome - 1] = (byte)((codedParity[sindrome - 1] == 1) ? 0 : 1);
                }

                decodedParity.Clear();
                decodedParity.Add(codedParity[2]);
                decodedParity.Add(codedParity[4]);

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Functions public

    /// <summary>
    /// Reset tracking on camera movement and hands
    /// </summary>
    public void ResetTracking()
    {
        lock (_mutexFramesRaw)
        {
            _framesLeft.Clear();
            _framesRight.Clear();
        }

        lock(_mutexBlobsObject)
        {            
            _blobsObjectLeft.Clear();
            _blobsObjectRight.Clear();
        }

        _isResetTrackingLeft = true;
        _isResetTrackingRight = true;

        _debugLastCapture = 0;
    }

    public void SetEnableTracking(bool isEnabled)
    {
        if (_isTrackingEnabled != isEnabled)
        {            
            _isTrackingEnabled = isEnabled;
            ResetTracking();
        }
    }

    #endregion
}
