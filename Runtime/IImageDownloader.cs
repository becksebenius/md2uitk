using System;
using UnityEngine;

namespace Md2Uitk
{
    public interface IImageDownloader
    {
        void DownloadImage(string address, Action<Texture2D> onSuccess, Action<string> onError);
    }
}