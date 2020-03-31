using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using RestClient.Models;
using RestClient;
using System.Linq;
using System.Text.RegularExpressions;

public class PhoneCamera : MonoBehaviour
{
    private bool camAvailable;
    private WebCamTexture webCam;
    private Texture texture; // in case camera does not open

    public RawImage rawImage;
    public AspectRatioFitter fitter;
    public Text textOCR;

    // azure info
    //private string baseUrl = "https://brazilsouth.api.cognitive.microsoft.com/vision/v2.0/ocr?language=es&detectOrientation=true";
    private string baseUrl = "https://westus.api.cognitive.microsoft.com/vision/v2.0/ocr?language=es&detectOrientation=true";
    private string clientId = "Ocp-Apim-Subscription-Key";
    private string clientSecret = "your key";

    void Start()
    {
        texture = rawImage.texture; // whatever image is in the scene view, that will be the defaultBackground
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("Phone does not have a camera");
            camAvailable = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                webCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
                break;
            }
        }

        if (webCam == null)
        {
            Debug.Log("No back facing camera found");
            return;
        }

        webCam.Play(); // we can start rendering
        rawImage.texture = webCam; // to be used as a normal texture

        camAvailable = true;
    }

    void Update()
    {
        if (!camAvailable)
        {
            // camera still not available
            return;
        }

        float ratio = (float)webCam.width / (float)webCam.height;
        fitter.aspectRatio = ratio;

        float scaleY = webCam.videoVerticallyMirrored ? -1f : 1f;
        rawImage.rectTransform.localScale = new Vector3(1f, scaleY, 1f); // flip in the Y axes if backCam is mirrored vertically

        int orient = -webCam.videoRotationAngle;
        rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
    }
    public void btnClick()
    {
        OCR();
    }

    private void OCR()
    {
        // setup the request header
        RequestHeader clientSecurityHeader = new RequestHeader
        {
            Key = clientId,
            Value = clientSecret
        };

        // setup the request header
        RequestHeader contentTypeHeader = new RequestHeader
        {
            Key = "Content-Type",
            Value = "application/octet-stream"
        };

        // send a post request
        StartCoroutine(RestWebClient.Instance.HttpPost(baseUrl, getBytesFromImage(), (r) => OnRequestComplete(r), new List<RequestHeader>
        {
            clientSecurityHeader,
            contentTypeHeader
        }));
    }

    void OnRequestComplete(Response response)
    {
        Debug.Log($"Status Code: {response.StatusCode}");
        Debug.Log($"Data: {response.Data}");
        Debug.Log($"Error: {response.Error}");

        if (string.IsNullOrEmpty(response.Error) && !string.IsNullOrEmpty(response.Data))
        {
            AzureOCRResponse azureOCRResponse = JsonUtility.FromJson<AzureOCRResponse>(response.Data);

            string words = string.Empty;
            foreach (var region in azureOCRResponse.regions)
            {
                foreach (var line in region.lines)
                {
                    foreach (var word in line.words)
                    {
                        words += word.text + "; ";
                    }
                    words = words + "\n";
                    //
                }
            }
            textOCR.text = words;

            Debug.Log($"data processed: {words}");
        }
    }

    private byte[] getBytesFromImage()
    {
        Texture2D snap = new Texture2D(webCam.width, webCam.height);
        snap.SetPixels(webCam.GetPixels());
        snap.Apply();

        webCam.Pause();
        camAvailable = false;

        byte[] bytes = snap.EncodeToPNG();
        textOCR.text = "Byte length: " + bytes.Length;
        return bytes;
    }
}