// Adapted from the Window's MR Holographic face tracking sample
// https://docs.microsoft.com/en-us/samples/microsoft/windows-universal-samples/holographicfacetracking/
//And this repo by Mitchell Doughty
//https://github.com/doughtmw/YoloDetectionHoloLens-Unity
//using this Research mode port to C#
//https://github.com/petergu684/HoloLens2-ResearchMode-Unity

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.OpenXR;



#if ENABLE_WINMD_SUPPORT
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Foundation;
using Windows.Media.Devices.Core;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;

using HL2UnityPlugin;

#endif

//https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/unity-xrdevice-advanced?tabs=mrtk
public static class NumericsConversionExtensions
{
    public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 v) => new UnityEngine.Vector3(v.X, v.Y, -v.Z);
    public static UnityEngine.Quaternion ToUnity(this System.Numerics.Quaternion q) => new UnityEngine.Quaternion(q.X, q.Y, -q.Z, -q.W);
    public static UnityEngine.Matrix4x4 ToUnity(this System.Numerics.Matrix4x4 m) => new UnityEngine.Matrix4x4(
        new Vector4(m.M11, m.M12, -m.M13, m.M14),
        new Vector4(m.M21, m.M22, -m.M23, m.M24),
        new Vector4(-m.M31, -m.M32, m.M33, -m.M34),
        new Vector4(m.M41, m.M42, -m.M43, m.M44));

    public static System.Numerics.Vector3 ToSystem(this UnityEngine.Vector3 v) => new System.Numerics.Vector3(v.x, v.y, -v.z);
    public static System.Numerics.Quaternion ToSystem(this UnityEngine.Quaternion q) => new System.Numerics.Quaternion(q.x, q.y, -q.z, -q.w);
    public static System.Numerics.Matrix4x4 ToSystem(this UnityEngine.Matrix4x4 m) => new System.Numerics.Matrix4x4(
        m.m00, m.m10, -m.m20, m.m30,
        m.m01, m.m11, -m.m21, m.m31,
       -m.m02, -m.m12, m.m22, -m.m32,
        m.m03, m.m13, -m.m23, m.m33);
}

public class FaceDetection : MonoBehaviour
{
    // Public fields
    public int samplingInterval;
    public GameObject objectOutlineCube;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;


#if ENABLE_WINMD_SUPPORT
    private Windows.Perception.Spatial.SpatialCoordinateSystem worldSpatialCoordinateSystem;
    private Frame returnFrame;

#endif

    int counter;
    float averageFaceWidthInMeters = 0.15f;


    #region UnityMethods

    private void Awake()
    {

    }

    async void Start()
    {
        counter = samplingInterval;

#if ENABLE_WINMD_SUPPORT
        try
        {
            _networkModel = new NetworkModel();
        }
        catch (Exception ex)
        {
            Debug.Log("Error initializing inference model:" + ex.Message);
            Debug.Log($"Failed to start model inference: {ex}");
        }

        // Configure camera to return frames fitting the model input size
        try
        {
                Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
                _mediaCaptureUtility = new MediaCaptureUtility();
                await _mediaCaptureUtility.InitializeMediaFrameReaderAsync();
                //Debug.Log("Camera started. Running!");
                Debug.Log("Successfully initialized frame reader.");
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to start camera: {ex.Message}. Using loaded/picked image.");
        }

        worldSpatialCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(Pose.identity) as SpatialCoordinateSystem;

#endif
    }
    private async void OnDestroy()
    {
        if (_mediaCaptureUtility != null)
        {
            await _mediaCaptureUtility.StopMediaFrameReaderAsync();
        }
    }

    async void Update()
    {
#if ENABLE_WINMD_SUPPORT
        counter += 1;
        if (_mediaCaptureUtility.IsCapturing)
        {
            returnFrame = await _mediaCaptureUtility.GetLatestFrame();


            if (returnFrame != null)
            {

                if (counter >= samplingInterval)
                {
                    counter = 0;

                    Task thread = Task.Run(async () =>
                    {
                        try
                        {
                            // Get the prediction from the model
                            var result = await _networkModel.EvaluateVideoFrameAsync(returnFrame.bitmap);
                            //If faces exist in frame, identify in 3D
                            if (result.Faces.Any())
                            {
                                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                                {
                                    //Visualize the detections in 3D to create GameObjects for eye gaze to interact with
                                    RGBDetectionToWorldspace(result, returnFrame);
                                }, true);
                            }

                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Exception:" + ex.Message);
                        }

                    });
                }

            }

        }
#endif
    }

    #endregion



#if ENABLE_WINMD_SUPPORT

   private void RGBDetectionToWorldspace(DetectedFaces result, Frame returnFrame)
   {

        //Debug.Log("Number of faces: " + result.Faces.Count());
        //create a trasnform from camera space to world space
        var cameraToWorld = (System.Numerics.Matrix4x4)returnFrame.spatialCoordinateSystem.TryGetTransformTo(worldSpatialCoordinateSystem);
        UnityEngine.Matrix4x4 unityCameraToWorld = NumericsConversionExtensions.ToUnity(cameraToWorld);
        var pixelsPerMeterAlongX = returnFrame.cameraIntrinsics.FocalLength.X;
        var averagePixelsForFaceAt1Meter = pixelsPerMeterAlongX * averageFaceWidthInMeters;
        
        foreach (Rect face in result.Faces)
        {
            double xCoord = (double)face.X + ((double)face.Width / 2.0F);
            double yCoord = (double)face.Y + ((double)face.Height / 2.0F);
            
            //vetor toward face at 1m
            System.Numerics.Vector2 projectedVector = returnFrame.cameraIntrinsics.UnprojectAtUnitDepth(new Point(xCoord, yCoord));
            UnityEngine.Vector3 normalizedVector = NumericsConversionExtensions.ToUnity(new System.Numerics.Vector3(projectedVector.X, projectedVector.Y, -1.0f));
            normalizedVector.Normalize();
            //calculate estimated depth based on average face width and pixel width in detection
            float estimatedFaceDepth = averagePixelsForFaceAt1Meter / (float)face.Width;
            Vector3 targetPositionInCameraSpace = normalizedVector * estimatedFaceDepth;
            Vector3 bestRectPositionInWorldspace = unityCameraToWorld.MultiplyPoint(targetPositionInCameraSpace);
            //create object at established 3D coords
            var newObject = Instantiate(objectOutlineCube, bestRectPositionInWorldspace, Quaternion.identity);
        }
 
    }

#endif
}