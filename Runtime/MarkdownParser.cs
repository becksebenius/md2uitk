using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Md2Uitk
{
    public class MarkdownParser
    {
        public ILinkHandler LinkHandler { get; set; } = new DebugLogLinkHandler();
        public IImageDownloader ImageDownloader { get; set; } = new WebRequestImageDownloader();

        public VisualElement Parse(string markdown)
        {
            var data = (Span) markdown;

            var lines = new List<Span>();
            for (int cursor = 0; cursor < data.Length;)
            {
                var line = ReadLine(data, cursor);

                cursor += line.Length;
                cursor++;
                lines.Add(line);
            }

            var elementFactory = new ElementFactory(LinkHandler, ImageDownloader);
            return new Block(lines, elementFactory).Process();
        }

        Span ReadLine(Span span, int cursor)
        {
            int start = cursor;
            while (true)
            {
                int index = span.Start + cursor;
                if (span.End <= index)
                {
                    break;
                }

                var ch = span[index];
                if (ch == '\n')
                {
                    break;
                }

                cursor++;
            }

            return span.Substring(start, cursor - start);
        }
    }
}