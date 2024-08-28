using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;
using System.IO;
using System.Linq;
using NRKernal.Record;

namespace NRKernal.NRExamples{
    #if UNITY_ANDROID && !UNITY_EDITOR
        using GalleryDataProvider = NativeGalleryDataProvider;
    #else
        using GalleryDataProvider = MockGalleryDataProvider;
    #endif
	public class HandGesturePhotoCapture : MonoBehaviour
	{
	    private float victory_count;
	    private bool takeflag;
	    private NRPhotoCapture m_PhotoCaptureObject;
	    private Resolution m_CameraResolution;
	    GalleryDataProvider galleryDataTool;
	    // Start is called before the first frame update
	    void Start()
	    {
		victory_count = 0f;
		takeflag = false;
	    }

	    // Update is called once per frame
	    void Update()
	    {
		// 手のトラッキングが実行中かどうかを確認
		if (NRInput.Hands.IsRunning)
		{
		    // 右手の状態を取得
		    HandState handState = NRInput.Hands.GetHandState(HandEnum.RightHand);

		    // 右手がトラッキングされているかどうか、ピースジェスチャーが行われているかをチェック
		    if (handState.isTracked && handState.isVictory)
		    {
			if (takeflag == false){
			    if (victory_count <= 50)
			    {
				victory_count++;
			    }
			    else{
				CapturePhoto();
			    }
			}
		    }
		    else
		    {
			takeflag = false;
			victory_count = 0;
		    }
		}
	    }
	    private void CapturePhoto()
	    {
		if (takeflag)
		{
			return;
		}
		takeflag = true;
		Debug.Log("写真をとります");
		if (m_PhotoCaptureObject == null)
		{
			this.Create((capture) =>
                {
			capture.TakePhotoAsync(OnCapturedPhotoToMemory);
                });
		}
		else
		{
			m_PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
		}
	    }
	    void Create(Action<NRPhotoCapture> onCreated)
	    {
		    if (m_PhotoCaptureObject != null)
		    {
			return;
		    }

		    // Create a PhotoCapture object
		    NRPhotoCapture.CreateAsync(false, delegate (NRPhotoCapture captureObject)
		    {
			m_CameraResolution = NRPhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

			if (captureObject == null)
			{
			    return;
			}

			m_PhotoCaptureObject = captureObject;

			CameraParameters cameraParameters = new CameraParameters();
			cameraParameters.cameraResolutionWidth = m_CameraResolution.width;
			cameraParameters.cameraResolutionHeight = m_CameraResolution.height;
			cameraParameters.pixelFormat = CapturePixelFormat.PNG;
			cameraParameters.frameRate = NativeConstants.RECORD_FPS_DEFAULT;
			cameraParameters.blendMode = BlendMode.Blend;

			// Activate the camera
			m_PhotoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (NRPhotoCapture.PhotoCaptureResult result)
			{
			    if (result.success)
			    {
				onCreated?.Invoke(m_PhotoCaptureObject);
			    }
			    else
			    {
				this.Close();
			    }
			}, true);
		    });
		}
		void OnCapturedPhotoToMemory(NRPhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
		{
		    var targetTexture = new Texture2D(m_CameraResolution.width, m_CameraResolution.height);
		    // Copy the raw image data into our target texture
		    photoCaptureFrame.UploadImageDataToTexture(targetTexture);

		    // Create a gameobject that we can apply our texture to
		    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		    Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
		    quadRenderer.material = new Material(Resources.Load<Shader>("Record/Shaders/CaptureScreen"));

		    var headTran = NRSessionManager.Instance.NRHMDPoseTracker.centerAnchor;
		    quad.name = "picture";
		    quad.transform.localPosition = headTran.position + headTran.forward * 3f;
		    quad.transform.forward = headTran.forward;
		    quad.transform.localScale = new Vector3(1.6f, 0.9f, 0);
		    quadRenderer.material.SetTexture("_MainTex", targetTexture);
		    SaveTextureAsPNG(photoCaptureFrame);

		    SaveTextureToGallery(photoCaptureFrame);
		    // Release camera resource after capture the photo.
		    this.Close();
		}
		void SaveTextureAsPNG(PhotoCaptureFrame photoCaptureFrame)
        {
            if (photoCaptureFrame.TextureData == null)
                return;
            try
            {
                string filename = string.Format("Xreal_Shot_{0}.png", NRTools.GetTimeStamp().ToString());
                string path = string.Format("{0}/XrealShots", Application.persistentDataPath);
                string filePath = string.Format("{0}/{1}", path, filename);

                byte[] _bytes = photoCaptureFrame.TextureData;
                NRDebugger.Info("Photo capture: {0}Kb was saved to [{1}]",  _bytes.Length / 1024, filePath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                File.WriteAllBytes(string.Format("{0}/{1}", path, filename), _bytes);

            }
            catch (Exception e)
            {
                NRDebugger.Error("Save picture faild!");
                throw e;
            }
        }

        /// <summary> Closes this object. </summary>
        void Close()
        {
            if (m_PhotoCaptureObject == null)
            {
                NRDebugger.Error("The NRPhotoCapture has not been created.");
                return;
            }
            // Deactivate our camera
            m_PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        /// <summary> Executes the 'stopped photo mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedPhotoMode(NRPhotoCapture.PhotoCaptureResult result)
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
        }

        /// <summary> Executes the 'destroy' action. </summary>
        void OnDestroy()
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
        }

        public void SaveTextureToGallery(PhotoCaptureFrame photoCaptureFrame)
        {
            if (photoCaptureFrame.TextureData == null)
                return;
            try
            {
                string filename = string.Format("Xreal_Shot_{0}.png", NRTools.GetTimeStamp().ToString());
                byte[] _bytes = photoCaptureFrame.TextureData;
                NRDebugger.Info(_bytes.Length / 1024 + "Kb was saved as: " + filename);
                if (galleryDataTool == null)
                {
                    galleryDataTool = new GalleryDataProvider();
                }

                galleryDataTool.InsertImage(_bytes, filename, "Screenshots");
            }
            catch (Exception e)
            {
                NRDebugger.Error("[TakePicture] Save picture faild!");
                throw e;
            }
         }
	}
}