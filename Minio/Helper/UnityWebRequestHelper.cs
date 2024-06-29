using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Minio.Helper
{
    public static class UnityWebRequestHelper
    {
        private static void FillUnityRequest(UnityWebRequest unityWebRequest, HttpClient httpClient,
            HttpRequestMessage httpRequestMessage)
        {
            // Fill headers
            foreach (var (headerName, headerContentParts) in httpRequestMessage.Headers)
            {
                var headerContent = string.Join(",", headerContentParts.ToArray());
                unityWebRequest.SetRequestHeader(headerName, headerContent);
            }

            if (httpRequestMessage.Content == null)
            {
                return;
            }
            
            foreach (var (headerName, headerContentParts) in httpRequestMessage.Content.Headers)
            {
                var headerContent = string.Join(",", headerContentParts.ToArray());
                unityWebRequest.SetRequestHeader(headerName, headerContent);
            }
        }

        private static HttpResponseMessage CreateResponse(UnityWebRequest unityRequest, HttpClient httpClient,
            HttpRequestMessage httpRequestMessage)
        {
            HttpResponseMessage httpResponseMessage =
                new HttpResponseMessage((HttpStatusCode)unityRequest.responseCode);
            httpResponseMessage.RequestMessage = httpRequestMessage;

            switch (unityRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log($"I suck: {unityRequest.error}");
                    httpResponseMessage.ReasonPhrase = unityRequest.error;
                    break;
                case UnityWebRequest.Result.Success:
                    // Debug.Log($"I want to place content");
                    if (unityRequest.downloadHandler != null && unityRequest.downloadHandler.data != null)
                    {
                        var contentBytes = unityRequest.downloadHandler.data;
                        httpResponseMessage.Content = new ByteArrayContent(contentBytes);
                        // Debug.Log($"I placed downloaded content!");
                    }
                    else
                    {
                        httpResponseMessage.Content = new StringContent("");
                        // Debug.Log($"I used own empty content!");
                    }

                    foreach (var (headerName, headerContent) in unityRequest.GetResponseHeaders())
                    {
                        var headerContentMut = headerContent;
                        // Debug.Log($"I want to place {headerName} with {headerContentMut}...");
                        
                        // E-tag. Format change from W/"..." to "..."
                        if (headerName.ToLower() == "etag")
                        {
                            if (headerContentMut.StartsWith("W/"))
                            {
                                headerContentMut = headerContentMut.Remove(0, 2);
                            }
                        }
                        
                        httpResponseMessage.Headers.TryAddWithoutValidation(headerName, headerContentMut);
                        // Debug.Log($"Placed to raw header: {headerName} with {headerContentMut}...");
                    }

                    break;
            }

            return httpResponseMessage;
        }

        private static async Task<HttpResponseMessage> UnityGetResponse(UnityWebRequest webRequest,
            HttpClient httpClient,
            HttpRequestMessage httpRequestMessage,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken)
        {
            FillUnityRequest(webRequest, httpClient, httpRequestMessage);

            var result = await webRequest.SendWebRequest().WithCancellation(cancellationToken);

            var response = CreateResponse(result, httpClient, httpRequestMessage);

            return response;
        }

        public static async Task<HttpResponseMessage> UnitySendAsync(this HttpClient httpClient,
            HttpRequestMessage httpRequestMessage,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread();

            UnityWebRequest unityWebRequest = null;
            if (httpRequestMessage.Method == HttpMethod.Get)
            {
                unityWebRequest = UnityWebRequest.Get(httpRequestMessage.RequestUri);
            } else if (httpRequestMessage.Method == HttpMethod.Head)
            {
                unityWebRequest = UnityWebRequest.Head(httpRequestMessage.RequestUri);
            } else if (httpRequestMessage.Method == HttpMethod.Put)
            {
                var byteArray = await httpRequestMessage.Content.ReadAsByteArrayAsync();
                unityWebRequest = UnityWebRequest.Put(httpRequestMessage.RequestUri, byteArray);
            } else if (httpRequestMessage.Method == HttpMethod.Post)
            {
                if (httpRequestMessage.Content is ByteArrayContent)
                {
                    // UnityWebRequest.Post doesn't have ctor with byteArray, so using Put.
                    var byteArray = await httpRequestMessage.Content.ReadAsByteArrayAsync();
                    unityWebRequest = UnityWebRequest.Put(httpRequestMessage.RequestUri, byteArray);
                    unityWebRequest.method = "POST";
                }
                else
                {
                    var stringArray = await httpRequestMessage.Content.ReadAsStringAsync();
                    unityWebRequest = UnityWebRequest.Post(httpRequestMessage.RequestUri, stringArray);
                }
            }

            if (unityWebRequest == null)
            {
                throw new NotImplementedException($"Method {httpRequestMessage.Method} is not implemented yet");
            }

            return await UnityGetResponse(unityWebRequest, httpClient, httpRequestMessage, httpCompletionOption,
                cancellationToken);
        }
    }
}