using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Md2Uitk
{
    internal class ElementFactory
    {
        private readonly ILinkHandler linkHandler;
        private readonly IImageDownloader imageDownloader;

        public ElementFactory(ILinkHandler linkHandler, IImageDownloader imageDownloader)
        {
            this.linkHandler = linkHandler;
            this.imageDownloader = imageDownloader;
        }

        public VisualElement Root()
        {
            return Element("md");
        }

        public VisualElement Blockquote()
        {
            return Element("bq");
        }

        public VisualElement Hr()
        {
            return Element("hr");
        }

        public VisualElement H(Span text, int depth)
        {
            return WithClasses(CreateSpanElement(text), "h", $"h{depth}");
        }

        public VisualElement List()
        {
            return Element("list");
        }

        public VisualElement UnorderedListItem()
        {
            return Element("uli", "li");
        }

        public VisualElement OrderedListItem()
        {
            return Element("oli", "li");
        }

        public VisualElement Indent()
        {
            return Element("indent");
        }

        public VisualElement Bullet()
        {
            return Element("bullet");
        }

        public VisualElement OrderedListNumber(int number)
        {
            return WithClasses(new Label($"{number}."), "number");
        }

        public VisualElement UnorderedListContents()
        {
            return Element("contents");
        }

        public VisualElement OrderedListContents()
        {
            return Element("contents");
        }

        public VisualElement Code()
        {
            return Element("code");
        }

        public VisualElement CodeContents(string text)
        {
            return WithClasses(new Label(text), "contents");
        }

        public VisualElement P(Span text)
        {
            return WithClasses(CreateSpanElement(text), "p");
        }

        struct RichLabelState
        {
            public ILinkHandler linkHandler;
            
            public List<VisualElement> labels;
            public StringBuilder stringBuilder;

            public Span strongTag;
            public bool strong;
            public Span emTag;
            public bool em;

            public bool link;
            public string linkUrl;

            public bool code;
            public int inlineCodeNumTicks;
            
            public int lastFlushedStringIndex;
            
            public void FlushLabels()
            {
                if (stringBuilder.Length == lastFlushedStringIndex)
                {
                    return;
                }

                var processText = new Span(
                    stringBuilder,
                    lastFlushedStringIndex,
                    stringBuilder.Length - lastFlushedStringIndex);
                int lastWordIndex = 0;
                for (int i = 0; i < processText.Length; ++i)
                {
                    var ch = processText[i];
                    if (ch == ' ' || i == processText.Length - 1)
                    {
                        var word = processText.Substring(lastWordIndex, i - lastWordIndex + 1)
                            .TrimEnd(Chars.Whitespace);
                        var label = new Label(word.ToString());
                        if (code)
                        {
                            label.AddToClassList("code");
                        }
                        else
                        {
                            if (em)
                            {
                                label.AddToClassList("em");
                            }

                            if (strong)
                            {
                                label.AddToClassList("strong");
                            }

                            if (link)
                            {
                                label.AddToClassList("a");
                                label.AddManipulator(new LinkManipulator(linkHandler, linkUrl));
                            }
                        }

                        if (0 < labels.Count &&
                            (0 < lastWordIndex || 0 < lastFlushedStringIndex && stringBuilder[lastFlushedStringIndex - 1] == ' '))
                        {
                            var previousLabel = labels[labels.Count - 1];
                            previousLabel.style.paddingRight = 3f;
                        }

                        labels.Add(label);
                        lastWordIndex = i + 1;
                    }
                }

                lastFlushedStringIndex = stringBuilder.Length;
            }
            
            public bool TryParseSimpleFormattingTag(Span text, Span tag, ref int index, ref Span valueTag, ref bool value)
            {
                if (text.ContainsAtIndex(tag, index))
                {
                    if (value)
                    {
                        if (valueTag.Equals(tag))
                        {
                            FlushLabels();
                            value = false;
                            index += tag.Length;
                            return true;
                        }
                    }
                    else
                    {
                        FlushLabels();
                        value = true;
                        valueTag = tag;
                        index += tag.Length;
                        return true;
                    }
                }

                return false;
            }
        }
        
        private VisualElement CreateSpanElement(Span text)
        {
            var state = new RichLabelState();
            state.labels = new List<VisualElement>();
            state.stringBuilder = StringUtility.GrabTempStringBuilder();
            state.linkHandler = linkHandler;
            
            CreateSpanElement(text, ref state);
            
            var element = new VisualElement();
            if (0 < state.labels.Count)
            {
                state.FlushLabels();

                foreach (var label in state.labels)
                {
                    element.Add(label);
                }
            }
            else
            {
                element.Add(new Label(state.stringBuilder.ToString()));
            }

            return element;
        }

        private void CreateSpanElement(
            Span text,
            ref RichLabelState state)
        {
            for (int i = 0; i < text.Length;)
            {
                if (!state.code)
                {
                    if (state.TryParseSimpleFormattingTag(text, Chars.StrongTag1, ref i, ref state.strongTag, ref state.strong)
                    || state.TryParseSimpleFormattingTag(text, Chars.StrongTag2, ref i, ref state.strongTag, ref state.strong)
                    || state.TryParseSimpleFormattingTag(text, Chars.EmTag1, ref i, ref state.emTag, ref state.em)
                    || state.TryParseSimpleFormattingTag(text, Chars.EmTag2, ref i, ref state.emTag, ref state.em))
                    {
                        continue;
                    }

                    if (!state.link && TryParseLink(ref i, text, out var title, out var url))
                    {
                        state.FlushLabels();

                        state.link = true;
                        state.linkUrl = url.ToString();

                        CreateSpanElement(title, ref state);
                        state.FlushLabels();

                        state.linkUrl = default;
                        state.link = false;

                        continue;
                    }

                    if (TryParseImage(ref i, text, out title, out url))
                    {
                        state.FlushLabels();

                        var imageElement = new VisualElement();
                        imageElement.AddToClassList("image");
                        imageElement.tooltip = title.ToString();
                        imageDownloader.DownloadImage(
                            url.ToString(), 
                            image =>
                            {
                                imageElement.style.backgroundImage = image;
                                imageElement.style.width = image.width;
                                imageElement.style.height = image.height;
                            }, 
                            error => UnityEngine.Debug.LogError("Failed to load image: " + error + " (" + url + ")"));
                        state.labels.Add(imageElement);
                        
                        continue;
                    }
                }

                int numCodeTicks = text.Count(Chars.InlineCodeChar, i);
                if (0 < numCodeTicks)
                {
                    if (state.code)
                    {
                        if (numCodeTicks == state.inlineCodeNumTicks)
                        {
                            state.FlushLabels();
                            state.code = false;
                            i += numCodeTicks;
                            continue;
                        }
                    }
                    else
                    {
                        state.FlushLabels();
                        state.code = true;
                        i += numCodeTicks;
                        state.inlineCodeNumTicks = numCodeTicks;
                        continue;
                    }
                }

                state.stringBuilder.Append(text[i]);
                i++;
            }
        }

        private bool TryParseImage(ref int cursor, Span text, out Span title, out Span url)
        {
            title = default;
            url = default;

            if (text[cursor] != '!')
            {
                return false;
            }

            int c = cursor + 1;
            if (c < text.Length && TryParseLink(ref c, text, out title, out url))
            {
                cursor = c;
                return true;
            }

            return false;
        }

        private bool TryParseLink(ref int cursor, Span text, out Span title, out Span url)
        {
            title = default;
            url = default;

            if (text[cursor] != '[')
            {
                return false;
            }
            
            bool fail = true;
            int titleStart = cursor + 1;
            int titleEnd = default;
            int linkDepth = 0;
            for (int i = titleStart; i < text.Length; ++i)
            {
                var ch = text[i];
                if (ch == '[')
                {
                    linkDepth++;
                }
                if (ch == ']')
                {
                    if (0 < linkDepth)
                    {
                        linkDepth--;
                    }
                    else
                    {
                        titleEnd = i;
                        title = text.Substring(titleStart, titleEnd - titleStart);
                        fail = false;
                        break;
                    }
                }
            }

            if (fail)
            {
                return false;
            }

            int urlStart = titleEnd + 1;
            fail = true;
            for (; urlStart < text.Length; ++urlStart)
            {
                var ch = text[urlStart];
                if (ch == '(')
                {
                    urlStart++;
                    fail = false;
                    break;
                }

                if (StringUtility.CharArrayContains(Chars.Whitespace, ch))
                {
                    continue;
                }

                break;
            }

            if (fail)
            {
                return false;
            }

            int urlEnd = default;
            fail = true;
            for (int i = urlStart; i < text.Length; ++i)
            {
                var ch = text[i];
                if (ch == ')')
                {
                    urlEnd = i;
                    url = text.Substring(urlStart, urlEnd - urlStart);
                    fail = false;
                    break;
                }
            }

            cursor = urlEnd + 1;
            return !fail;
        }

        private VisualElement Element(params string[] classes)
        {
            return Element<VisualElement>(classes);
        }

        private VisualElement Element<T>(params string[] classes) where T : VisualElement, new()
        {
            return WithClasses(new T(), classes);
        }

        private VisualElement WithClasses(VisualElement element, params string[] classes)
        {
            foreach (var className in classes)
            {
                element.AddToClassList(className);
            }

            return element;
        }
    }
}