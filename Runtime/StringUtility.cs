using System.Text;

namespace Md2Uitk
{
    internal static class StringUtility
    {
        private static readonly StringBuilder TempStringBuilder = new StringBuilder();
        
        public static bool CharArrayContains(char[] array, char value)
        {
            foreach (var ch in array)
            {
                if (ch == value)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static StringBuilder GrabTempStringBuilder()
        {
            TempStringBuilder.Clear();
            return TempStringBuilder;
        }
    }
}