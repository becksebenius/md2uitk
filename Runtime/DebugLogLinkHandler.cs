namespace Md2Uitk
{
    public class DebugLogLinkHandler : ILinkHandler
    {
        public void OnLinkActivated(string url)
        {
            UnityEngine.Debug.Log("Link Clicked: " + url);
        }
    }
}