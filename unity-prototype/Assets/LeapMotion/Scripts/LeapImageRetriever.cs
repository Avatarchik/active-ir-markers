//#define OVERRIDE_MANTIS

/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

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
using Assets.Scripts;


public struct LMDevice
{
  public static int PERIPERAL_WIDTH = 640;
  public static int PERIPERAL_HEIGHT = 240;
  public static int DRAGONFLY_WIDTH = 608;
  public static int DRAGONFLY_HEIGHT = 540;
  public static int MANTIS_WIDTH = 640;
  public static int MANTIS_HEIGHT = 240;

  public int width;
  public int height;
  public int pixels;
  public bool isRobustMode;
  public LM_DEVICE type;

  public const bool MARKER_TRACKING_DEBUG = false;

  public LMDevice (LM_DEVICE device /*= LM_DEVICE.INVALID*/)
  {
    type = device;
    switch (type)
    {
      case LM_DEVICE.PERIPHERAL:
        width = PERIPERAL_WIDTH;
        height = PERIPERAL_HEIGHT;
        break;
      case LM_DEVICE.DRAGONFLY:
        width = DRAGONFLY_WIDTH;
        height = DRAGONFLY_HEIGHT;
        break;
      case LM_DEVICE.MANTIS:
        width = MANTIS_WIDTH;
        height = MANTIS_HEIGHT;
        break;
      default:
        width = 0;
        height = 0;
        break;
    }
    this.pixels = width * height;
    isRobustMode = false;
  }

  public void UpdateRobustMode(int height)
  {
    switch (type)
    {
      case LM_DEVICE.PERIPHERAL:
        isRobustMode = (height < PERIPERAL_HEIGHT) ? true : false;
        break;
      case LM_DEVICE.DRAGONFLY:
        isRobustMode = (height < DRAGONFLY_HEIGHT) ? true : false;
        break;
      case LM_DEVICE.MANTIS:
        isRobustMode = (height < MANTIS_HEIGHT) ? true : false;
        break;
      default:
        isRobustMode = false;
        break;
    }
  }
}

public enum LM_DEVICE
{
  INVALID = -1,
  PERIPHERAL = 0,
  DRAGONFLY = 1,
  MANTIS = 2
}


public class EBlob
{
    public const float FREQ = 1f / 5f;
    public const int MAX_LEN = 3;

    public Vector2 pos;
    public double size;

    public int ID = -1;

    public int positves = 0;

    public float lastTime = 0f;
    public List<bool> digital = new List<bool>();
}

// To use the LeapImageRetriever you must be on version 2.1+
// and enable "Allow Images" in the Leap Motion settings.
public class LeapImageRetriever : MonoBehaviour
{
    private List<EBlob> blobsLeft = new List<EBlob>();
    private List<EBlob> blobsRight = new List<EBlob>();
    private float lastBlobCheck = 0f;

    private OVRCameraRig _ovrCameraRig;
    private CamCom _camCom;
    private HandController _handController;
    private GameObject _demoHouse;
    private GameObject _demoPongRacket;
    private GameObject _begemot;

    private float lasImageFlush = 0f;

    private Dictionary<int, GameObject> markers = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector3> markersPositions = new Dictionary<int, Vector3>();

    public const bool DEBUG_SHOW_HOUSE = false;
    public const bool DEBUG_SHOW_RAYS = false;
    public bool DEBUG_SHOW_MARKERS = true;
    public const bool DEBUG_SHOW_MARKERS_ONLY_ALL = false;
    public const float MAX_DISTANCE_BETWEEN_POINTS = 0.04f;

    public const float MAX_DISTANCE_BETWEEN_POINTS_PONG = 0.05f;
    public const float MIN_DISTANCE_BETWEEN_POINTS_PONG = 0.01f;

    private string _textInfo = "";
    private string _textInfoLast = "";
    private TextMesh _textInfo3d = null;

  private Shader IR_NORMAL_SHADER;
  private Shader IR_UNDISTORT_SHADER;
  private Shader IR_UNDISTORT_SHADER_FOREGROUND;
  private Shader RGB_NORMAL_SHADER;
  private Shader RGB_UNDISTORT_SHADER;

  public bool doUpdate = true;
  public bool rescaleController = true;

  public const int DEFAULT_DISTORTION_WIDTH = 64;
  public const int DEFAULT_DISTORTION_HEIGHT = 64;
  public const int IMAGE_WARNING_WAIT = 10;

  public int imageIndex = 0;
  public Color imageColor = Color.white;
  public float gammaCorrection = 1.0f;
  public bool overlayImage = false;
  public bool undistortImage = true;
  public bool blackIsTransparent = false;

  private HandController controller_ = null;
  private LMDevice attached_device_ = new LMDevice();

  // Main texture.
  protected Texture2D main_texture_;
  protected Color32[] image_pixels_;
  protected int image_misses_ = 0;

  // Distortion textures.
  protected Texture2D distortionX_;
  protected Texture2D distortionY_;
  protected Color32[] dist_pixelsX_;
  protected Color32[] dist_pixelsY_;

  private LM_DEVICE GetDevice(int width)
  {
#if OVERRIDE_MANTIS
      return LM_DEVICE.MANTIS;
#endif
    if (width == LMDevice.PERIPERAL_WIDTH)
    {
      return LM_DEVICE.PERIPHERAL;
    }
    else if (width == LMDevice.DRAGONFLY_WIDTH)
    {
      return LM_DEVICE.DRAGONFLY;
    }
    else if (width == LMDevice.MANTIS_WIDTH)
    {
      return LM_DEVICE.MANTIS;
    }
    return LM_DEVICE.INVALID;
  }

  protected void SetShader()
  {
    DestroyImmediate(GetComponent<Renderer>().material);

    if(!LMDevice.MARKER_TRACKING_DEBUG)
    {
        switch (attached_device_.type)
        {
          case LM_DEVICE.PERIPHERAL:
            GetComponent<Renderer>().material = (undistortImage) ? new Material((overlayImage) ? IR_UNDISTORT_SHADER_FOREGROUND : IR_UNDISTORT_SHADER) : new Material(IR_NORMAL_SHADER);
            if ( rescaleController ) { controller_.transform.localScale = Vector3.one * 1.6f; }
            break;
          case LM_DEVICE.DRAGONFLY:
            GetComponent<Renderer>().material = (undistortImage) ? new Material(RGB_UNDISTORT_SHADER) : new Material(RGB_NORMAL_SHADER);
            if ( rescaleController ) { controller_.transform.localScale = Vector3.one; }
            break;
          case LM_DEVICE.MANTIS:
            GetComponent<Renderer>().material = (undistortImage) ? new Material((overlayImage) ? IR_UNDISTORT_SHADER_FOREGROUND : IR_UNDISTORT_SHADER) : new Material(IR_NORMAL_SHADER);
            if ( rescaleController ) { controller_.transform.localScale = Vector3.one; }
            break;
          default:
            break;
        }
    }
    else
    {
        GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        //GetComponent<Renderer>().material = (undistortImage) ? new Material(RGB_UNDISTORT_SHADER) : new Material(RGB_NORMAL_SHADER);
        if (rescaleController) { controller_.transform.localScale = Vector3.one; }

        main_texture_.wrapMode = TextureWrapMode.Clamp;
        image_pixels_ = new Color32[attached_device_.pixels];
    }
  }

  protected void SetRenderer(ref Image image)
  {
    GetComponent<Renderer>().material.mainTexture = main_texture_;
    GetComponent<Renderer>().material.SetColor("_Color", imageColor);
    if (!LMDevice.MARKER_TRACKING_DEBUG)
    {
        GetComponent<Renderer>().material.SetInt("_DeviceType", Convert.ToInt32(attached_device_.type));
    }
    else
    {
        GetComponent<Renderer>().material.SetInt("_DeviceType", Convert.ToInt32(LM_DEVICE.DRAGONFLY));
    }
    GetComponent<Renderer>().material.SetFloat("_GammaCorrection", gammaCorrection);
    GetComponent<Renderer>().material.SetInt("_BlackIsTransparent", blackIsTransparent ? 1 : 0);

    GetComponent<Renderer>().material.SetTexture("_DistortX", distortionX_);
    GetComponent<Renderer>().material.SetTexture("_DistortY", distortionY_);
    GetComponent<Renderer>().material.SetFloat("_RayOffsetX", image.RayOffsetX);
    GetComponent<Renderer>().material.SetFloat("_RayOffsetY", image.RayOffsetY);
    GetComponent<Renderer>().material.SetFloat("_RayScaleX", image.RayScaleX);
    GetComponent<Renderer>().material.SetFloat("_RayScaleY", image.RayScaleY);
  }

  protected void InitiateShaders() 
  {
    IR_NORMAL_SHADER = Resources.Load<Shader>("LeapIRDistorted");
    IR_UNDISTORT_SHADER = Resources.Load<Shader>("LeapIRUndistorted");
    IR_UNDISTORT_SHADER_FOREGROUND = Resources.Load<Shader>("LeapIRUndistorted_Foreground");
    RGB_NORMAL_SHADER = Resources.Load<Shader>("LeapRGBDistorted");
    RGB_UNDISTORT_SHADER = Resources.Load<Shader>("LeapRGBUndistorted");
  }

  protected bool InitiateTexture(ref Image image)
  {
    int width = image.Width;
    int height = image.Height;

    attached_device_ = new LMDevice(GetDevice(width));
    attached_device_.UpdateRobustMode(height);
    if (attached_device_.width == 0 || attached_device_.height == 0)
    {
      attached_device_ = new LMDevice();
      Debug.LogWarning("No data in the image texture.");
      return false;
    }
    else
    {

        if (!LMDevice.MARKER_TRACKING_DEBUG)
        {
            switch (attached_device_.type)
            {
                case LM_DEVICE.PERIPHERAL:
                    main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.Alpha8, false);
                    break;
                case LM_DEVICE.DRAGONFLY:
                    main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.RGBA32, false);
                    break;
                case LM_DEVICE.MANTIS:
                    //main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.Alpha8, false);
                    main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.RGBA32, false);
                    break;
                default:
                    main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.Alpha8, false);
                    break;
            }
        }
        else
        {
            main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.RGBA32, false);  
        }

      

      main_texture_.wrapMode = TextureWrapMode.Clamp;
      image_pixels_ = new Color32[attached_device_.pixels];
    }
    return true;
  }

  protected bool InitiateDistortion(ref Image image)
  {
    int width = image.DistortionWidth / 2;
    int height = image.DistortionHeight;

    if (width == 0 || height == 0)
    {
      Debug.LogWarning("No data in image distortion");
      return false;
    }
    else
    {
      dist_pixelsX_ = new Color32[width * height];
      dist_pixelsY_ = new Color32[width * height];
      DestroyImmediate(distortionX_);
      DestroyImmediate(distortionY_);
      distortionX_ = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
      distortionY_ = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
      distortionX_.wrapMode = TextureWrapMode.Clamp;
      distortionY_.wrapMode = TextureWrapMode.Clamp;
    }

    return true;
  }

  protected bool InitiatePassthrough(ref Image image)
  {
    if (!InitiateTexture(ref image))
      return false;

    if (!InitiateDistortion(ref image))
      return false;

    SetShader();
    SetRenderer(ref image);

    return true;
  }

  protected void LoadMainTexture(ref Image image)
  {

    byte[] image_data = image.Data;

    switch (attached_device_.type)
    {
      case LM_DEVICE.PERIPHERAL:
      case LM_DEVICE.MANTIS:
        if (attached_device_.isRobustMode) 
        {
          int width = attached_device_.width;
          int height = attached_device_.height;
          int data_index = 0;

          for (int j = 0; j < height; j += 2)
          {
            for (int i = 0; i < width; ++i)  
            {
              image_pixels_[i + (j + 0) * width].a = image_data[data_index];
              image_pixels_[i + (j + 1) * width].a = image_data[data_index];

              data_index++;
            }
          }
        }
        else
        {
          for (int i = 0; i < image_data.Length; ++i)
          {
              if (!LMDevice.MARKER_TRACKING_DEBUG)
              {
                  image_pixels_[i].a = image_data[i];
              }
              else
              {
                  image_pixels_[i] = Color.white;
                  image_pixels_[i].r = image_data[i];
                  image_pixels_[i].g = image_data[i];
                  image_pixels_[i].b = image_data[i];
              }
                
          }
        }


        
        /*
        ObjectTrackerGroup[] groups = ObjectTrackerManager.GetTrackedPixels(image_data);

        int iColor = 0;
        foreach (ObjectTrackerGroup group in groups)
        {
            foreach (ObjectTrackerGroup groupEach in group.pointsGroups)
            {
                Color32 col = ColourValues[iColor];

                foreach (ObjectTrackerPixel pix in groupEach.pixels)
                {                   
                   if (!LMDevice.MARKER_TRACKING_DEBUG)
                   {
                       image_pixels_[pix.i].a = 0;
                   }
                   else
                   {
                       image_pixels_[pix.i] = col;
                       image_pixels_[pix.i].a = 255;
                   }
                   
                }

                iColor++;
                if (iColor == ColourValues.Length)
                {
                    iColor = 0;
                }
                 
            }
        }
         */

        //Centres
        /*
         int iColor = 0;
        foreach (ObjectTrackerGroup group in groups)
        {
            {
                Color32 col = ColourValues[iColor];

                foreach (Vector2 pos in group.points)
                {
                    image_pixels_[(int)pos.x + (int)pos.y * ObjectTrackerManager.Width] = col;

                    iColor++;
                    if (iColor == ColourValues.Length)
                    {
                        iColor = 0;
                    }
                }

            }
        }
        // */

       // Debug.Log(groups.Length);

        break;
      case LM_DEVICE.DRAGONFLY:
        int image_index = 0;
        for (int i = 0; i < image_data.Length; image_index++)
        {
          image_pixels_[image_index].r = image_data[i++];
          image_pixels_[image_index].g = image_data[i++];
          image_pixels_[image_index].b = image_data[i++];
          image_pixels_[image_index].a = image_data[i++];
        }
        gammaCorrection = Mathf.Max(gammaCorrection, 1.7f);
        break;
      default:
        for (int i = 0; i < image_data.Length; ++i)
          image_pixels_[i].a = image_data[i];
        break;
    }

    main_texture_.SetPixels32(image_pixels_);
    main_texture_.Apply();
  }


  protected bool LoadDistortion(ref Image image)
  {
    if (image.DistortionWidth == 0 || image.DistortionHeight == 0)
    {
      Debug.LogWarning("No data in the distortion texture.");
      return false;
    }

    if (undistortImage)
    {
      float[] distortion_data = image.Distortion;
      int num_distortion_floats = 2 * distortionX_.width * distortionX_.height;

      // Move distortion data to distortion x textures.
      for (int i = 0; i < num_distortion_floats; i += 2)
      {
        // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
        float dval = (distortion_data[i] + 0.6f) / 2.3f;
        float enc_x = dval;
        float enc_y = dval * 255.0f;
        float enc_z = dval * 65025.0f;
        float enc_w = dval * 160581375.0f;

        enc_x = enc_x - (int)enc_x;
        enc_y = enc_y - (int)enc_y;
        enc_z = enc_z - (int)enc_z;
        enc_w = enc_w - (int)enc_w;

        enc_x -= 1.0f / 255.0f * enc_y;
        enc_y -= 1.0f / 255.0f * enc_z;
        enc_z -= 1.0f / 255.0f * enc_w;

        int index = i >> 1;
        dist_pixelsX_[index].r = (byte)(256 * enc_x);
        dist_pixelsX_[index].g = (byte)(256 * enc_y);
        dist_pixelsX_[index].b = (byte)(256 * enc_z);
        dist_pixelsX_[index].a = (byte)(256 * enc_w);
      }
      distortionX_.SetPixels32(dist_pixelsX_);
      distortionX_.Apply();

      // Move distortion data to distortion y textures.
      for (int i = 1; i < num_distortion_floats; i += 2)
      {
        // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
        float dval = (distortion_data[i] + 0.6f) / 2.3f;
        float enc_x = dval;
        float enc_y = dval * 255.0f;
        float enc_z = dval * 65025.0f;
        float enc_w = dval * 160581375.0f;

        enc_x = enc_x - (int)enc_x;
        enc_y = enc_y - (int)enc_y;
        enc_z = enc_z - (int)enc_z;
        enc_w = enc_w - (int)enc_w;

        enc_x -= 1.0f / 255.0f * enc_y;
        enc_y -= 1.0f / 255.0f * enc_z;
        enc_z -= 1.0f / 255.0f * enc_w;

        int index = i >> 1;
        dist_pixelsY_[index].r = (byte)(256 * enc_x);
        dist_pixelsY_[index].g = (byte)(256 * enc_y);
        dist_pixelsY_[index].b = (byte)(256 * enc_z);
        dist_pixelsY_[index].a = (byte)(256 * enc_w);
      }
      distortionY_.SetPixels32(dist_pixelsY_);
      distortionY_.Apply();
    }

    return true;
  }

  void Start()
  {
      if (imageIndex == 0)
      {
          _ovrCameraRig = GameObject.Find("OVRCameraRig").GetComponent<OVRCameraRig>();
          _camCom = GameObject.Find("CamCom").GetComponent<CamCom>();
          _handController = GameObject.Find("HandController").GetComponent<HandController>();

          _demoHouse = GameObject.Find("DemoHouse");
          _demoHouse.SetActive(false);

          _demoPongRacket = GameObject.Find("PongRacket");
          if (CamCom.SAMPLE_TYPE != SampleType.PingPong)
          {
              _demoPongRacket.SetActive(false);
          }

          _begemot = GameObject.Find("Begemot");
          if (CamCom.SAMPLE_TYPE != SampleType.Begemot)
          {
              _begemot.SetActive(false);
          }
          

          _textInfo3d = GameObject.Find("TextInfo").GetComponent<TextMesh>();
          _textInfo3d.gameObject.SetActive(false);

          /*
          _guiObject = new GameObject();
          _guiObject.name = "GUIObject";
          _guiObject.transform.parent = GameObject.Find("LeftEyeAnchor").transform;

          RectTransform r = _guiObject.AddComponent<RectTransform>();
          r.sizeDelta = new Vector2(100f, 100f);
          r.localPosition = new Vector3(0.01f, 0.17f, 0.53f);
          r.localEulerAngles = Vector3.zero;
          r.localScale = new Vector3(0.001f, 0.001f, 0.001f);
          Canvas c = _guiObject.AddComponent<Canvas>();
          c.renderMode = RenderMode.WorldSpace;
          c.pixelPerfect = false;
          OVRUGUI.RiftPresentGUI(_guiObject);

          _guiObject.SetActive(false);
           */
      }

    GameObject hand_controller = GameObject.Find("HandController");
    if (hand_controller && hand_controller.GetComponent<HandController>())
      controller_ = hand_controller.GetComponent<HandController>();

    if (controller_ == null)
      return;

    controller_.GetLeapController().SetPolicyFlags(Controller.PolicyFlag.POLICY_IMAGES);
    InitiateShaders();

  }

  Frame frame;

  void Update()
  {
    if (controller_ == null)
      return;

    if ( doUpdate == false ) { return; }

    frame = controller_.GetFrame();

    if (frame.Images.Count == 0)
    {
      image_misses_++;
      if (image_misses_ == IMAGE_WARNING_WAIT)
      {
        // TODO: Make this visible IN applications
        Debug.LogWarning("Can't find any images. " +
                          "Make sure you enabled 'Allow Images' in the Leap Motion Settings, " +
                          "you are on tracking version 2.1+ and " +
                          "your Leap Motion device is plugged in.");
      }
      return;
    }

    // Check main texture dimensions.
    Image image = frame.Images[imageIndex];

    if (attached_device_.width != image.Width || attached_device_.height != image.Height)
    {
      if (!InitiatePassthrough(ref image)) {
        Debug.Log ("InitiatePassthrough FAILED");
        return;
      }
    }

    LoadMainTexture(ref image);
    LoadDistortion(ref image);

    if (imageIndex == 0)
    {

        if (_camCom.IsTrackingEnabled && (_handController.GetAllGraphicsHands().Length > 0) && frame.Hands.Count > 0)
        {
            //_textInfo3d.gameObject.SetActive(true);
            // _textInfo3d.text = String.Format("Hands detected: {0}", frame.Hands[0].Confidence);
        }
        //_camCom.SetEnableTracking(_handController.GetAllGraphicsHands().Length == 0);

        if(Input.GetKey(KeyCode.Space))
        {
            _camCom.ResetTracking();
        }

        if(Input.GetKeyDown(KeyCode.W))
        {
            DEBUG_SHOW_MARKERS = !DEBUG_SHOW_MARKERS;
            if(!DEBUG_SHOW_MARKERS)
            {
                markers.Clear();
            }
        }

        if (CamCom.SAMPLE_TYPE == SampleType.PingPong)
        {
            if(Input.GetKeyDown(KeyCode.Q))
            {
                MeshRenderer[] renderers = _demoPongRacket.GetComponentsInChildren<MeshRenderer>();
                bool isActive = renderers[0].enabled;

                foreach(MeshRenderer rend in renderers)
                {
                    rend.enabled = !isActive;
                }
            }
        }

        List<Color> colorsPatterns = new List<Color>();
        colorsPatterns.Add(Color.red);
        colorsPatterns.Add(Color.green);
        colorsPatterns.Add(Color.blue);
        colorsPatterns.Add(Color.magenta);
        colorsPatterns.Add(new Color(1f, 0.5f, 0f));

        CamBlob[] blobsObjLeft = _camCom.BlobsObjectLeft;
        CamBlob[] blobsObjRight = _camCom.BlobsObjectRight;

        GameObject[] lines = GameObject.FindGameObjectsWithTag("lines");
        foreach (GameObject line in lines)
        {
            Destroy(line);
        }

        GameObject[] list = GameObject.FindGameObjectsWithTag("markers");
        foreach (GameObject marker in list)
        {
            if (!markers.ContainsValue(marker))
            {
                Destroy(marker);
            }
        }

        /*
        OVRPose pose = OVRManager.tracker.GetPose(0d); // OVRManager.display.GetCameraPose(0d);
        //GameObject.Find("LeftEyeAnchor").transform.TransformPoint(pose.position);
        //pose.position.y += 0.6f;
        pose.position.y += 0.3f;
        pose.position.z -= 0.5f;
        _demoHouse.transform.position = pose.position;
        _demoHouse.SetActive(true);
        //drawSphere(pose.position, Color.red);
         */

        if(CamCom.SAMPLE_TYPE == SampleType.PingPong)
        {
            markersPositions.Clear();
        }

        bool foundBlob = false;

        foreach (CamBlob blobLeft in blobsObjLeft)
        {
            foreach (CamBlob blobRight in blobsObjRight)
            {
                if (blobLeft != null &&
                    blobRight != null &&
                    blobLeft.ID != -1 &&
                    blobLeft.ID == blobRight.ID)
                {
                    //_guiObject.SetActive(true);
                    foundBlob = true;                    
                    
                    _textInfo = "Detecting object, please don't move";

                    float diffBetweenEyes = 0.06f;
                    //float diffBetweenEyes = 0.057f;

                    //0.057f
                    Vector3 CameraOffset = new Vector3(diffBetweenEyes, 0f, 0f);

                    float resizeUp = 1f / (float)CamCom.RESIZE_RATIO;

                    //Rectify = real pos from center
                    Vector vecRectifiedLeft = frame.Images[0].Rectify(new Vector(blobLeft.Position.x * resizeUp, blobLeft.Position.y * resizeUp, 0f));
                    Vector vecRectifiedRight = frame.Images[1].Rectify(new Vector(blobRight.Position.x * resizeUp, blobRight.Position.y * resizeUp, 0f));

                    Vector3 camForwardLeft = GameObject.Find("LeftEyeAnchor").transform.forward;
                    Vector3 camForwardRight = GameObject.Find("RightEyeAnchor").transform.forward;

                    float f = 1f;
                    float flipY = -1f;

                    vecRectifiedLeft.y = flipY * vecRectifiedLeft.y;
                    vecRectifiedRight.y = flipY * vecRectifiedRight.y;

                    if (DEBUG_SHOW_RAYS)
                    {
                        Vector3 LDir = new Vector3(vecRectifiedLeft.x, vecRectifiedLeft.y, 0f) * f + camForwardLeft.normalized;
                        LDir.Normalize();

                        Vector3 RDir = new Vector3(vecRectifiedRight.x, vecRectifiedRight.y, 0f) * f + camForwardRight.normalized;
                        RDir.Normalize();

                        drawLine(_ovrCameraRig.leftEyeAnchor.position, LDir, colorsPatterns[blobLeft.ID]);
                        drawLine(_ovrCameraRig.rightEyeAnchor.position, RDir, colorsPatterns[blobLeft.ID]);
                        drawLine(_ovrCameraRig.leftEyeAnchor.position, transform.parent.transform.forward, Color.yellow);
                        drawLine(_ovrCameraRig.rightEyeAnchor.position, transform.parent.transform.forward, Color.yellow);
                    }

                    
                    Vector3 posGizmo = new Vector3();
                    posGizmo.z = f * CameraOffset.x / (vecRectifiedLeft.x - vecRectifiedRight.x);
                    posGizmo.x = posGizmo.z * (vecRectifiedLeft.x) / f;
                    posGizmo.y = posGizmo.z * (vecRectifiedLeft.y) / f;

                    Vector3 postionMarker = GameObject.Find("LeftEyeAnchor").transform.TransformPoint(posGizmo);

                    if (DEBUG_SHOW_MARKERS && !DEBUG_SHOW_MARKERS_ONLY_ALL)
                    {
                        GameObject sphere = drawSphere(postionMarker, colorsPatterns[blobLeft.ID]);
                        markers[blobLeft.ID] = sphere;
                    }

                    markersPositions[blobLeft.ID] = postionMarker;


                    List<Vector3> correctPositions = new List<Vector3>();
                    Vector3 totalCurPos = new Vector3();

                    if (markersPositions.Count >= 2)
                    {
                        //Filter positions
                        //Kalman kalman = 


                        bool isCorrectTriangle = true;
                        foreach (Vector3 pos1 in markersPositions.Values)
                        {
                            foreach (Vector3 pos2 in markersPositions.Values)
                            {
                                if (pos1 != pos2)
                                {
                                    float dist = (pos1 - pos2).sqrMagnitude;
                                    if ((CamCom.SAMPLE_TYPE == SampleType.PingPong && (dist >= MAX_DISTANCE_BETWEEN_POINTS_PONG || dist < MIN_DISTANCE_BETWEEN_POINTS_PONG)) ||
                                         (CamCom.SAMPLE_TYPE == SampleType.TriangleHouse && dist >= MAX_DISTANCE_BETWEEN_POINTS)
                                        )
                                    {
                                        isCorrectTriangle = false;


                                       // Debug.Log(String.Format("dist: {0} {1} {2}", (markersPositions[i] - markersPositions[j]).sqrMagnitude, i, j));

                                        break;
                                    }
                                    else
                                    {
                                        correctPositions.Add(pos1);
                                        totalCurPos += pos1;
                                        // Debug.Log(String.Format("dist: {0}", (markersPositions[0] - markersPositions[i]).sqrMagnitude));
                                    }
                                    // Debug.Log(String.Format("dist: {0}", (markersPositions[0] - markersPositions[i]).sqrMagnitude ));
                                }
                            }
                        }

                        if (CamCom.SAMPLE_TYPE == SampleType.Begemot)
                        {

                            if (correctPositions.Count > 0)
                            {
                                //Position of demo house
                                totalCurPos /= (float)correctPositions.Count;

                                // if (totalCurPos.sqrMagnitude > 0.5)
                                {
                                    _begemot.transform.position = Vector3.Lerp(_begemot.transform.position, totalCurPos, Time.deltaTime * 20f);
                                }

                            }

                        }
                        else if (CamCom.SAMPLE_TYPE == SampleType.PingPong)
                        {


                            if (correctPositions.Count > 0)
                            {
                                //Position of demo house
                                totalCurPos /= (float)correctPositions.Count;

                                // if (totalCurPos.sqrMagnitude > 0.5)
                                {
                                    _demoPongRacket.transform.position = Vector3.Lerp(_demoPongRacket.transform.position, totalCurPos, Time.deltaTime * 10f);
                                }

                            }

                            if (isCorrectTriangle && markersPositions.Count == 3)
                            {
                                if (DEBUG_SHOW_MARKERS && DEBUG_SHOW_MARKERS_ONLY_ALL)
                                {
                                    GameObject sphere = drawSphere(postionMarker, colorsPatterns[blobLeft.ID]);
                                    markers[blobLeft.ID] = sphere;
                                }


                                //if (totalCurPos.sqrMagnitude > 0.5)
                                {
                                    //Debug.Log(String.Format("{0}", middlePos.sqrMagnitude));

                                    Vector3 up = Vector3.Cross(
                                           (markersPositions[2] - markersPositions[0]).normalized,
                                           (markersPositions[1] - markersPositions[0]).normalized);
                                    Vector3 heading = ((markersPositions[1] - markersPositions[0]) + (markersPositions[2] - markersPositions[0])).normalized;
                                    Vector3 pitch = Vector3.Cross(up, heading);

                                    // float angleZ = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg +90;

                                    /*
                                    Debug.DrawRay(middlePos, up, Color.red, 30f);
                                    Debug.DrawRay(middlePos, heading, Color.green, 30f);
                                    Debug.DrawRay(middlePos, pitch, Color.blue, 30f);
                                    */


                                    //Debug.Log(String.Format("{0}", angleZ));
                                    if (heading.sqrMagnitude > 0 && up.sqrMagnitude > 0)
                                    {
                                        _demoPongRacket.transform.rotation = Quaternion.Lerp(_demoPongRacket.transform.rotation, Quaternion.LookRotation(-up, -heading), Time.deltaTime * 30f);
                                    }
                                    //_demoPongRacket.transform.Rotate(up, angleZ);
                                    // _demoPongRacket.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(_demoPongRacket.transform.rotation.eulerAngles.z, angleZ, Time.deltaTime * 2f));
                                }
                            }
                        }
                        else
                        {
                            if (isCorrectTriangle)
                            {
                                if (DEBUG_SHOW_MARKERS && DEBUG_SHOW_MARKERS_ONLY_ALL)
                                {
                                    GameObject sphere = drawSphere(postionMarker, colorsPatterns[blobLeft.ID]);
                                    markers[blobLeft.ID] = sphere;
                                }

                                _textInfo = "Success, transformation detected";

                                if (DEBUG_SHOW_HOUSE && markersPositions.Count == 3)
                                {
                                    //Position of demo house
                                    Vector3 middlePos = markersPositions[0] + markersPositions[1] + markersPositions[2];
                                    middlePos /= 3f;

                                    _demoHouse.transform.position = Vector3.Lerp(_demoHouse.transform.position, middlePos, Time.deltaTime * 10f);

                                    Vector3 up = Vector3.Cross(
                                        (markersPositions[2] - markersPositions[0]).normalized,
                                        (markersPositions[1] - markersPositions[0]).normalized);
                                    Vector3 heading = ((markersPositions[1] - markersPositions[0]) + (markersPositions[2] - markersPositions[0])).normalized;
                                    Vector3 pitch = Vector3.Cross(up, heading);

                                    float angleY = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg - 40f;


                                    _demoHouse.transform.rotation = Quaternion.Euler(0f, Mathf.Lerp(angleY, _demoHouse.transform.rotation.y, Time.deltaTime * 2f), 0f);


                                    //Debug.Log(String.Format("{0}", angleY));


                                    /*
                                    Debug.DrawRay(middlePos, up, Color.red, 30f);
                                    Debug.DrawRay(middlePos, heading, Color.green, 30f);
                                    Debug.DrawRay(middlePos, pitch, Color.blue, 30f);
                                    //*/

                                    _demoHouse.SetActive(true);
                                }
                            }
                        }
                    }
                    
                }
            }
        } //foreach itteration
        
        if(!foundBlob)
        {
            _textInfo = "";            
        }

        if (_textInfoLast != _textInfo)
        {
            _textInfoLast = _textInfo;

            //_textInfo3d.gameObject.SetActive(_textInfo.Length > 0);
            //_textInfo3d.text = _textInfo;            
        }
    }
  }

  float SignedAngle(Vector3 v1 ,Vector3 v2, Vector3  normal)  
  {
     var perp = Vector3.Cross(normal, v1);
     var angle = Vector3.Angle(v1, v2);
     angle *= Mathf.Sign(Vector3.Dot(perp, v2));
     return angle * Mathf.Rad2Deg;
 }

  void drawLine(Vector3 startPos, Vector3 dir, Color color)
  {
      float width = 0.002f;
    GameObject sub = new GameObject();
    sub.tag = "lines";
    LineRenderer lineLeft = sub.AddComponent<LineRenderer>();
    lineLeft.material = new Material (Shader.Find("Sprites/Default"));
    lineLeft.SetWidth(width, width);
    lineLeft.SetColors(color, color);
      
    lineLeft.SetPosition(0, startPos);
    Vector3 endPos = startPos + (dir * 10f);
    lineLeft.SetPosition(1, endPos);
  }

  GameObject drawSphere(Vector3 pos, Color color)
  {
      float scale = 0.02f;
      GameObject sub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      sub.tag = "markers";
      sub.GetComponent<Renderer>().material = Resources.Load("Def", typeof(Material)) as Material;
      sub.GetComponent<Renderer>().material.color = color;
      sub.GetComponent<SphereCollider>().enabled = false;
      sub.transform.position = pos;
      sub.transform.localScale = new Vector3(scale, scale, scale);

      return sub;
  }


  void OnDrawGizmos()
  {
      if (_ovrCameraRig != null && _ovrCameraRig.leftEyeAnchor != null)
      {
          float rad = 0.005f;
          Gizmos.color = Color.red;
          Gizmos.DrawSphere(_ovrCameraRig.leftEyeAnchor.position, rad);
          Gizmos.color = Color.green;
          Gizmos.DrawSphere(_ovrCameraRig.rightEyeAnchor.position, rad);
      }
  }


  void OnApplicationFocus(bool focusStatus) {
    if (focusStatus) {
            // Ensure reinitialization in Update
            attached_device_.width = 0;
            attached_device_.height = 0;
        }
  }
}
