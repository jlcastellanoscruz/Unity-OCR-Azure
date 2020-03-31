using System.Collections;
using System.Collections.Generic;
using RestClient.Models;
using RestClient.Singletons;
using UnityEngine;
using UnityEngine.Networking;
 
namespace RestClient
{
    public class RestWebClient : Singleton<RestWebClient>
    {
        private const string defaultContentType = "application/octet-stream";
        public IEnumerator HttpPost(string url, byte[] body, System.Action<Response> callback, IEnumerable<RequestHeader> headers = null)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(url))
            {
                webRequest.method = UnityWebRequest.kHttpVerbPOST;

                webRequest.downloadHandler = new DownloadHandlerBuffer();

                webRequest.uploadHandler = new UploadHandlerRaw(body);
                webRequest.uploadHandler.contentType = defaultContentType;

                if (headers != null)
                {
                    foreach (RequestHeader header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    callback(new Response
                    {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error
                    });
                }

                if (webRequest.isDone)
                {
                    string data = System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data);
                    callback(new Response
                    {
                        StatusCode = webRequest.responseCode,
                        Error = webRequest.error,
                        Data = data
                    });
                }
            }
        }
    }
}