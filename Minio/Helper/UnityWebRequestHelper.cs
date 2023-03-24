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
        }

        private static HttpResponseMessage CreateResponse(UnityWebRequest unityRequest, HttpClient httpClient,
            HttpRequestMessage httpRequestMessage)
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage((HttpStatusCode) unityRequest.responseCode);
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
                    Debug.Log($"I want to place content");
                    var contentBytes = unityRequest.downloadHandler.data;
                    httpResponseMessage.Content = new ByteArrayContent(contentBytes);
                    Debug.Log($"I placed content!");
                    foreach (var (headerName, headerContent) in unityRequest.GetResponseHeaders())
                    {
                        Debug.Log($"I want to place {headerName} with {headerContent}...");
                        if (headerName.ToLower().StartsWith("content-"))
                        {
                            httpResponseMessage.Content.Headers.Add(headerName, headerContent);
                            Debug.Log($"Placed to content: {headerName} with {headerContent}...");
                        }
                        else
                        {
                            httpResponseMessage.Headers.Add(headerName, headerContent);
                            Debug.Log($"Placed to headers: {headerName} with {headerContent}...");
                        }
                    }
                    break;
            }

            return httpResponseMessage;
        }

        public static async Task<HttpResponseMessage> UnitySendAsync(this HttpClient httpClient,
            HttpRequestMessage httpRequestMessage,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread();
            
            if (httpRequestMessage.Method == HttpMethod.Get)
            {
                return await UnityGetAsync(httpClient, httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }

            throw new Exception("Method was not get lol");
        }

        public static async Task<HttpResponseMessage> UnityGetAsync(this HttpClient httpClient,
            HttpRequestMessage httpRequestMessage,
            HttpCompletionOption httpCompletionOption,
            CancellationToken cancellationToken)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(httpRequestMessage.RequestUri);
            FillUnityRequest(webRequest, httpClient, httpRequestMessage);

            Debug.Log($"Get Url: {webRequest.url}");
            var result = await webRequest.SendWebRequest().WithCancellation(cancellationToken);
            
            Debug.Log($"Got result from Url: {result.result}. Time to make response...");
            var response = CreateResponse(result, httpClient, httpRequestMessage);
            Debug.Log($"Got result from Url: {response} \n Content: {response.Content}");

            return response;
        }
    }
}