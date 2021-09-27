using UnityEngine.UIElements;

namespace Md2Uitk
{
    internal class LinkManipulator : Manipulator
    {
        private readonly ILinkHandler linkHandler;
        private readonly string url;

        public LinkManipulator(ILinkHandler linkHandler, string url)
        {
            this.linkHandler = linkHandler;
            this.url = url;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            linkHandler?.OnLinkActivated(url);
        }
    }
}