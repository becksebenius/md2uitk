namespace Md2Uitk
{
    internal static class Chars
    {
        public static readonly char[] Whitespace =
        {
            (char) 32, (char) 9, (char) 10, (char) 11, (char) 12, (char) 13
        };

        public static readonly Span[] CodeBlockIndentPrefixes =
        {
            "    ", "\t"
        };

        public static readonly Span[] CodeBlockIndentWithinListPrefixes =
        {
            "        ", "\t\t"
        };

        public static readonly Span CodeBlockCapture = "```";

        public static readonly Span[] UnorderedListBullets =
        {
            "+ ", "* ", "- "
        };

        public static readonly Span HrSequence = "---";

        public static readonly Span H = "#";

        public static readonly Span ForcedLinebreakTail = "  ";

        public static readonly Span Blockquote = ">";

        public static readonly char[] Numbers =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
        };

        public static readonly Span OrderedListNumberSuffix = ". ";

        public static readonly Span StrongTag1 = "**";
        public static readonly Span StrongTag2 = "__";
        public static readonly Span EmTag1 = "*";
        public static readonly Span EmTag2 = "_";
        public static readonly char InlineCodeChar = '`';
    }
}