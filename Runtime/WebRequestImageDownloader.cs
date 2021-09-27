using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Md2Uitk
{
    public class WebRequestImageDownloader : IImageDownloader
    {
        public void DownloadImage(string address, Action<Texture2D> onSuccess, Action<string> onError)
        {
            var buffer = new DownloadHandlerTexture();
            var webRequest = new UnityWebRequest(address, "GET", buffer, default);
            var asyncOperation = webRequest.SendWebRequest();
            asyncOperation.completed += op =>
            {
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    onError?.Invoke(webRequest.error);
                }
                else
                {
                    onSuccess?.Invoke(buffer.texture);
                }
            };
        }
    }
}