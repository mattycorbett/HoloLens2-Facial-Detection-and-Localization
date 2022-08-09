// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices.WindowsRuntime;

#if ENABLE_WINMD_SUPPORT
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Foundation;
using Windows.Media.FaceAnalysis;



public struct DetectedFaces
{
    public SoftwareBitmap originalImageBitmap { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public Rect[] Faces { get; set; }
}

#endif

public struct Rect
{
    public uint X { get; set; }
    public uint Y { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
}

public class NetworkModel
{

#if ENABLE_WINMD_SUPPORT
    private MediaCapture _media_capture;
    FaceDetector detector;
    IList<DetectedFace> detectedFaces;

#endif



#if ENABLE_WINMD_SUPPORT

    public async Task<DetectedFaces> EvaluateVideoFrameAsync(SoftwareBitmap bitmap)
    {
        DetectedFaces result = new DetectedFaces();
      
        try{

            // Perform network model inference using the input data tensor, cache output and time operation
            result = await EvaluateFrame(bitmap);

        return result;
        }

         catch (Exception ex)
        {
            throw;
            return result;
        }

    }

   private async Task<DetectedFaces> EvaluateFrame(SoftwareBitmap bitmap)
   {
			if (detector == null)
            {
                detector = await FaceDetector.CreateAsync();
            }
            //use NV12 for detections
			const BitmapPixelFormat faceDetectionPixelFormat = BitmapPixelFormat.Nv12;
            SoftwareBitmap convertedBitmap;
            //if frame not in NV12, convert
            if (bitmap.BitmapPixelFormat != faceDetectionPixelFormat)
            {
                convertedBitmap = SoftwareBitmap.Convert(bitmap, faceDetectionPixelFormat);
            }
            else
            {
                convertedBitmap = bitmap;
            }
			detectedFaces = await detector.DetectFacesAsync(convertedBitmap);
       
            return new DetectedFaces
			{
                originalImageBitmap = bitmap,
			    Faces = detectedFaces.Select(f => 
			        new Rect {X = f.FaceBox.X, Y = f.FaceBox.Y, Width = f.FaceBox.Width, Height = f.FaceBox.Height}).ToArray()
			};

   }


#endif

}