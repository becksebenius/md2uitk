using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Md2Uitk
{
    internal struct Block
    {
        private ElementFactory elementFactory;
        private List<Span> lines;
        private int start;
        private int length;
        private BlockType type;
        private int orderedListNumber;

        private Span this[int index]
        {
            get => lines[start + index];
            set => lines[start + index] = value;
        }

        public Block(List<Span> lines, ElementFactory elementFactory)
        {
            this.elementFactory = elementFactory;
            this.lines = lines;
            start = 0;
            length = lines.Count;
            type = BlockType.Root;
            orderedListNumber = default;
        }

        private Block SubBlock(int subBlockStart, int subBlockLength, BlockType subBlockType)
            => new Block
            {
                elementFactory = elementFactory,
                lines = lines,
                start = subBlockStart,
                length = subBlockLength,
                type = subBlockType
            };

        private bool IsListItemBlock => type == BlockType.OrderedListItem || type == BlockType.UnorderedListItem;

        public VisualElement Process()
        {
            // Preprocess
            VisualElement rootElement;
            VisualElement contentElement;
            if (type == BlockType.Root)
            {
                rootElement = elementFactory.Root();
                contentElement = rootElement;
            }
            else if (type == BlockType.Blockquote)
            {
                rootElement = elementFactory.Blockquote();
                contentElement = rootElement;

                for (int i = 0; i < length; ++i)
                {
                    var line = this[i];
                    var lineWithoutWhitespace = line.TrimFront(Chars.Whitespace);
                    if (lineWithoutWhitespace.StartsWith(Chars.Blockquote))
                    {
                        line = lineWithoutWhitespace.TrimFront(1);
                    }

                    this[i] = line;
                }
            }
            else if (type == BlockType.List)
            {
                rootElement = elementFactory.List();
                contentElement = rootElement;
            }
            else if (type == BlockType.UnorderedListItem)
            {
                var firstLine = this[0];
                int numIndents = firstLine.CountAny(Chars.Whitespace, 0) / 4;
                this[0] = firstLine.TrimFront(Chars.Whitespace).TrimFront(Chars.UnorderedListBullets);

                rootElement = elementFactory.UnorderedListItem();
                for (int i = 0; i < numIndents; ++i)
                {
                    rootElement.Add(elementFactory.Indent());
                }

                rootElement.Add(elementFactory.Bullet());
                rootElement.Add(contentElement = elementFactory.UnorderedListContents());
            }
            else if (type == BlockType.OrderedListItem)
            {
                var firstLine = this[0];
                int numIndents = firstLine.CountAny(Chars.Whitespace, 0) / 4;
                this[0] = firstLine
                    .TrimFront(Chars.Whitespace)
                    .TrimFront(Chars.Numbers)
                    .TrimFront(Chars.OrderedListNumberSuffix);

                rootElement = elementFactory.OrderedListItem();
                for (int i = 0; i < numIndents; ++i)
                {
                    rootElement.Add(elementFactory.Indent());
                }

                rootElement.Add(elementFactory.OrderedListNumber(orderedListNumber));
                rootElement.Add(contentElement = elementFactory.OrderedListContents());
            }
            else if (type == BlockType.IndentCode)
            {
                rootElement = elementFactory.Code();
                contentElement = rootElement;

                for (int i = 0; i < length; ++i)
                {
                    foreach (var indent in Chars.CodeBlockIndentPrefixes)
                    {
                        if (this[i].StartsWith(indent))
                        {
                            this[i] = this[i].TrimFront(indent.Length);
                            break;
                        }
                    }
                }
            }
            else if (type == BlockType.BacktickCode)
            {
                rootElement = elementFactory.Code();
                contentElement = rootElement;

                // Cull out the backticks
                this[0] = this[0].TrimFront(Chars.Whitespace).TrimFront(Chars.CodeBlockCapture);
                if (this[0].IsWhitespace())
                {
                    start++;
                    length--;
                }

                this[length - 1] = this[length - 1].TrimFront(Chars.Whitespace).TrimFront(Chars.CodeBlockCapture);
                if (this[length - 1].IsWhitespace())
                {
                    length--;
                }
            }
            else
            {
                throw new NotImplementedException(type.ToString());
            }

            int nextOrderedListNumber = -1;
            int nextOrderedListNumberLine = -1;

            // Line processing
            if (type == BlockType.BacktickCode || type == BlockType.IndentCode)
            {
                var stringBuilder = StringUtility.GrabTempStringBuilder();
                for (int lineId = 0; lineId < length; lineId++)
                {
                    stringBuilder.AppendLine(this[lineId].ToString());
                }

                contentElement.Add(elementFactory.CodeContents(stringBuilder.ToString()));
            }
            else
            {
                for (int lineId = 0; lineId < length;)
                {
                    var line = this[lineId];
                    var lineWithoutWhitespace = line.TrimFront(Chars.Whitespace);

                    // Whitespace Lines
                    // (should only happen at the beginning of root block)
                    {
                        if (lineWithoutWhitespace.TrimEnd(Chars.Whitespace).Length == 0)
                        {
                            ++lineId;
                            continue;
                        }
                    }

                    // Headers
                    {
                        int numHeaderIndicators = lineWithoutWhitespace.CountSequence(Chars.H, 0);
                        if (0 < numHeaderIndicators)
                        {
                            var text = lineWithoutWhitespace.TrimFront(Chars.H.Length * numHeaderIndicators)
                                .TrimFront(Chars.Whitespace);
                            contentElement.Add(elementFactory.H(text, numHeaderIndicators));
                            ++lineId;
                            continue;
                        }
                    }

                    // Horizontal Rule
                    {
                        if (line.StartsWith(Chars.HrSequence))
                        {
                            contentElement.Add(elementFactory.Hr());
                            ++lineId;
                            continue;
                        }
                    }

                    // List Processing
                    {
                        // List
                        if (type != BlockType.List)
                        {
                            if (IsListItemLine(lineWithoutWhitespace))
                            {
                                int numLines = 0;
                                do
                                {
                                    int numIndents = line.CountAny(Chars.Whitespace, 0) / 4;
                                    int blockLength = GetNumLinesInListBlock(lineId + numLines, numIndents);
                                    numLines += blockLength;
                                } while (lineId + numLines < length && IsListItemLine(this[lineId + numLines].TrimFront(Chars.Whitespace)));

                                contentElement.Add(
                                    SubBlock(
                                            start + lineId,
                                            numLines,
                                            BlockType.List)
                                        .Process());
                                lineId += numLines;
                                continue;
                            }
                        }

                        if (type == BlockType.List)
                        {
                            // Unordered List Item
                            {
                                if (lineWithoutWhitespace.StartsWithAny(Chars.UnorderedListBullets))
                                {
                                    int numIndents = line.CountAny(Chars.Whitespace, 0) / 4;
                                    int blockLength = GetNumLinesInListBlock(lineId, numIndents);

                                    contentElement.Add(
                                        SubBlock(
                                                start + lineId,
                                                blockLength,
                                                BlockType.UnorderedListItem)
                                            .Process());
                                    lineId += blockLength;
                                    continue;
                                }
                            }

                            // Ordered List Item
                            {
                                int numNumberChars = lineWithoutWhitespace.CountAny(Chars.Numbers, 0);
                                if (0 < numNumberChars)
                                {
                                    if (lineWithoutWhitespace.ContainsAtIndex(Chars.OrderedListNumberSuffix,
                                        numNumberChars))
                                    {
                                        var currentNumber =
                                            nextOrderedListNumberLine == lineId
                                                ? nextOrderedListNumber
                                                : int.Parse(lineWithoutWhitespace.Substring(0, numNumberChars)
                                                    .ToString());

                                        int numIndents = line.CountAny(Chars.Whitespace, 0) / 4;
                                        int blockLength = GetNumLinesInListBlock(lineId, numIndents);

                                        var subBlock =
                                            SubBlock(
                                                start + lineId,
                                                blockLength,
                                                BlockType.OrderedListItem);
                                        subBlock.orderedListNumber = currentNumber;
                                        contentElement.Add(subBlock.Process());
                                        lineId += blockLength;

                                        // Save off the ordered list number in case the next line can follow a sequence
                                        nextOrderedListNumberLine = lineId;
                                        nextOrderedListNumber = currentNumber + 1;

                                        continue;
                                    }
                                }
                            }
                        }
                    }

                    // Blockquote
                    {
                        if (lineWithoutWhitespace.StartsWith(Chars.Blockquote))
                        {
                            int numLines = ReadNumLinesInBlock(lineId);

                            contentElement.Add(
                                SubBlock(
                                        start + lineId,
                                        numLines,
                                        BlockType.Blockquote)
                                    .Process());
                            lineId += numLines;
                            continue;
                        }
                    }

                    // Code
                    {
                        var indentPrefixes =
                            IsListItemBlock
                                ? Chars.CodeBlockIndentWithinListPrefixes
                                : Chars.CodeBlockIndentPrefixes;
                        if (line.StartsWithAny(indentPrefixes))
                        {
                            int numLines = 0;
                            do
                            {
                                numLines++;
                            } while (
                                lineId + numLines < length
                                && this[lineId + numLines]
                                    .StartsWithAny(indentPrefixes));

                            contentElement.Add(
                                SubBlock(
                                        start + lineId,
                                        numLines,
                                        BlockType.IndentCode)
                                    .Process());
                            lineId += numLines;
                            continue;
                        }

                        if (lineWithoutWhitespace.StartsWith(Chars.CodeBlockCapture))
                        {
                            int numLines = 0;
                            do
                            {
                                numLines++;
                            } while (
                                lineId + numLines < length
                                && !this[lineId + numLines]
                                    .TrimEnd(Chars.Whitespace)
                                    .EndsWith(Chars.CodeBlockCapture));

                            numLines++; // Include the closing line

                            if (lineId + numLines < length)
                            {
                                contentElement.Add(
                                    SubBlock(
                                            start + lineId,
                                            numLines,
                                            BlockType.BacktickCode)
                                        .Process());
                                lineId += numLines;
                                continue;
                            }
                        }
                    }

                    // Paragraph
                    {
                        var stringBuilder = StringUtility.GrabTempStringBuilder();
                        stringBuilder.Append(line.TrimFrontAndEnd(Chars.Whitespace).ToString());

                        int numLines = ReadNumLinesInBlock(lineId);
                        for (int j = 1; j < numLines; ++j)
                        {
                            var blockLine = this[lineId + j];
                            stringBuilder.Append(" ");
                            stringBuilder.Append(blockLine.TrimFrontAndEnd(Chars.Whitespace).ToString());
                        }

                        lineId += numLines;
                        contentElement.Add(elementFactory.P(stringBuilder.ToString()));
                        continue;
                    }
                }
            }

            return rootElement;
        }

        private bool IsListItemLine(Span line)
        {
            // Next line starts with a bullet list
            if (line.StartsWithAny(Chars.UnorderedListBullets))
            {
                return true;
            }

            // Next line starts with a number list
            int numNumberChars = line.CountAny(Chars.Numbers, 0);
            if (0 < numNumberChars)
            {
                if (line.ContainsAtIndex(Chars.OrderedListNumberSuffix, numNumberChars))
                {
                    return true;
                }
            }

            return false;
        }

        private int GetNumLinesInListBlock(int startingLineId, int indentLevel)
        {
            int blockEnd = startingLineId;
            while (true)
            {
                int numLines = ReadNumLinesInBlock(blockEnd);
                blockEnd += numLines;

                // Check for multiple paragraphs
                if (blockEnd < length && this[blockEnd].IsWhitespace())
                {
                    if (blockEnd + 1 < length
                        && this[blockEnd + 1].CountAny(Chars.Whitespace, 0) / 4 >= indentLevel + 1)
                    {
                        continue;
                    }
                }

                break;
            }

            return blockEnd - startingLineId;
        }

        private int ReadNumLinesInBlock(int startingLine)
        {
            int i = startingLine + 1;
            for (; i < length; ++i)
            {
                if (IsNewBlock(i))
                {
                    break;
                }
            }

            return i - startingLine;
        }

        private bool IsNewBlock(int lineId)
        {
            if (0 < start + lineId)
            {
                // Previous line ends with 2 or more space characters
                if (this[lineId - 1].EndsWith(Chars.ForcedLinebreakTail))
                {
                    return true;
                }
            }

            var line = this[lineId];
            line = line.TrimFront(Chars.Whitespace);

            // Next line is part of a list
            if (IsListItemLine(line))
            {
                return true;
            }

            // Next line is a horizontal rule
            if (line.StartsWith(Chars.HrSequence))
            {
                return true;
            }

            // Next line is empty
            if (line.TrimEnd(Chars.Whitespace).Length == 0)
            {
                return true;
            }
            
            // Line is a code block
            if (line.StartsWith(Chars.CodeBlockCapture))
            {
                return true;
            }

            return false;
        }
    }
}