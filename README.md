# mk2uitk

The purpose of this library is to provide an API to convert markdown files into a UIToolkit VisualElement tree in Unity.

A sample of usage is included in the `Samples/BrowserSample/` directory.

A basic `.uss` file is provided that covers the important stylings for the generated elements, located at `Runtime/markdown.uss`. 
You can use this as-is to render your markdown, or copy it and tweak it for your own purposes.

## Example Usage

```cs
var buffer = new DownloadHandlerBuffer();
var webRequest = new UnityWebRequest("url/to/markdown.md", "GET", buffer, default);
var asyncOperation = webRequest.SendWebRequest();
asyncOperation.completed += op =>
{
    var text = buffer.text;
    var parser = new MarkdownParser();
    var markdownElement = parser.Parse(text);
    rootElement.Add(markdownElement);
}
```

## Links

In order to follow links, you must provide a `ILinkHandler` implementation to the MarkdownParser. By default, a Debug link handler is used.

```cs
var parser = new MarkdownParser();
parser.LinkHandler = new DebugLogLinkHandler();
var element = parser.Parse(text);
```

## Images

Images can be downloaded asynchronously by providing a `IImageDownloader` implementation. By default, a WebRequest image downloader is used.

```cs
var parser = new MarkdownParser();
parser.ImageDownloader = new WebRequestImageDownloader();
var element = parser.Parse(text);
```

## Future Plans

Not sure yet. Happy to take suggestions.
