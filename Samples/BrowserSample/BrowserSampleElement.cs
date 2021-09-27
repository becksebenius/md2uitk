using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Md2Uitk.Samples.BrowserSample
{
    public class BrowserSampleElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BrowserSampleElement> {}

        private VisualElement topbar;
        private TextField urlInput;
        private Button loadButton;
        private ScrollView scrollView;
        private Label loading;
        private Label error;
        private bool isRunning = false;

        public BrowserSampleElement()
        {
            style.backgroundColor = Color.white;
            style.flexGrow = 1f;

            Add(topbar = new VisualElement());
            {
                topbar.style.flexDirection = FlexDirection.Row;
                topbar.style.paddingLeft = 20f;
                topbar.style.paddingRight = 20f;
                topbar.style.paddingTop = 20f;
                topbar.style.paddingBottom = 20f;
                topbar.style.backgroundColor = new Color(0f, 0f, 0f, 0.1f);
                topbar.style.borderBottomColor = Color.black;
                topbar.style.borderBottomWidth = 1f;
            }
            topbar.Add(urlInput = new TextField("Url"));
            {
                urlInput.style.flexGrow = 1f;
            }
            topbar.Add(loadButton = new Button(Load));
            {
                loadButton.text = "Load";
                loadButton.style.width = 100f;
            }
            Add(scrollView = new ScrollView());
            {
                scrollView.style.flexGrow = 1f;
                scrollView.style.display = DisplayStyle.None;
                scrollView.contentContainer.style.paddingLeft = 20;
                scrollView.contentContainer.style.paddingRight = 20;
                scrollView.contentContainer.style.paddingBottom = 20;
                scrollView.contentContainer.style.paddingTop = 20;
            }
            Add(loading = new Label("Loading..."));
            {
                loading.style.left = 0;
                loading.style.right = 0;
                loading.style.top = 0;
                loading.style.bottom = 0;
                loading.style.unityTextAlign = TextAnchor.UpperCenter;
                loading.style.fontSize = 24;
                loading.style.paddingTop = 80f;
                loading.style.display = DisplayStyle.None;
                loading.style.flexGrow = 1f;
            }
            Add(error = new Label());
            {
                error.style.left = 0;
                error.style.right = 0;
                error.style.top = 0;
                error.style.bottom = 0;
                error.style.unityTextAlign = TextAnchor.UpperCenter;
                error.style.fontSize = 18;
                error.style.color = Color.red;
                error.style.paddingTop = 80f;
                error.style.display = DisplayStyle.None;
            }
        }

        private void Load()
        {
            if (isRunning)
            {
                return;
            }

            scrollView.Clear();
            scrollView.style.display = DisplayStyle.None;
            loading.style.display = DisplayStyle.Flex;
            error.style.display = DisplayStyle.None;
            isRunning = true;
            
            var buffer = new DownloadHandlerBuffer();
            var webRequest = new UnityWebRequest(urlInput.text, "GET", buffer, default);
            var asyncOperation = webRequest.SendWebRequest();
            asyncOperation.completed += op =>
            {
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    scrollView.style.display = DisplayStyle.None;
                    loading.style.display = DisplayStyle.None;
                    error.style.display = DisplayStyle.Flex;
                    error.text = asyncOperation.webRequest.error;
                }
                else
                {
                    var text = buffer.text;

                    try
                    {
                        var parser = new MarkdownParser();
                        var element = parser.Parse(text);

                        scrollView.style.display = DisplayStyle.Flex;
                        loading.style.display = DisplayStyle.None;
                        error.style.display = DisplayStyle.None;

                        scrollView.Add(element);
                    }
                    catch (Exception e)
                    {
                        scrollView.style.display = DisplayStyle.None;
                        loading.style.display = DisplayStyle.None;
                        error.style.display = DisplayStyle.Flex;

                        error.text = e.ToString();
                    }
                }

                isRunning = false;
            };
        }
    }
}