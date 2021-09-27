using System;
using System.Text;

namespace Md2Uitk
{
    internal struct Span
    {
        private readonly string stringBuffer;
        private readonly StringBuilder stringBuilderBuffer;

        public int Start;
        public int Length;
        public int End => Start + Length;

        private Span(string buffer) : this(buffer, 0, buffer.Length)
        {
        }

        private Span(string buffer, int start, int length)
        {
            stringBuffer = buffer;
            stringBuilderBuffer = null;
            Start = start;
            Length = length;
        }

        public Span(StringBuilder builder, int start, int length)
        {
            stringBuffer = null;
            stringBuilderBuffer = builder;
            Start = start;
            Length = length;
        }

        public char this[int index]
        {
            get
            {
                if (stringBuffer != null)
                {
                    return stringBuffer[Start + index];
                }

                if (stringBuilderBuffer != null)
                {
                    return stringBuilderBuffer[Start + index];
                }

                throw new InvalidOperationException();
            }
        }

        public bool ContainsAtIndex(Span other, int index)
        {
            if (Length < index + other.Length)
            {
                return false;
            }

            for (int i = 0; i < other.Length; ++i)
            {
                if (this[index + i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool StartsWith(Span other) => ContainsAtIndex(other, 0);
        public bool EndsWith(Span other) => ContainsAtIndex(other, Length - other.Length);

        public bool StartsWithAny(Span[] sequences)
        {
            foreach (var seq in sequences)
            {
                if (ContainsAtIndex(seq, 0))
                {
                    return true;
                }
            }

            return false;
        }

        public int Count(char ch, int index)
        {
            int count = 0;
            while (index < Length && this[index] == ch)
            {
                ++count;
                index++;
            }

            return count;
        }

        public int CountAny(char[] chars, int index)
        {
            int count = 0;
            while (index < Length && StringUtility.CharArrayContains(chars, this[index]))
            {
                ++count;
                index++;
            }

            return count;
        }

        public int CountSequence(Span sequence, int index)
        {
            int count = 0;
            while (ContainsAtIndex(sequence, index))
            {
                ++count;
                index += sequence.Length;
            }

            return count;
        }

        public Span TrimFront(int amount)
        {
            var other = this;
            if (other.Length < amount)
            {
                throw new IndexOutOfRangeException();
            }

            other.Start += amount;
            other.Length -= amount;
            return other;
        }

        public Span TrimFront(char[] chars)
        {
            var other = this;
            while (0 < other.Length && StringUtility.CharArrayContains(chars, other[0]))
            {
                other.Start++;
                other.Length--;
            }

            return other;
        }

        public Span TrimFront(Span sequence)
        {
            var other = this;
            if (other.StartsWith(sequence))
            {
                other = other.TrimFront(sequence.Length);
            }

            return other;
        }

        public Span TrimFront(Span[] sequences)
        {
            var other = this;
            while (true)
            {
                bool anyMatched = false;

                foreach (var seq in sequences)
                {
                    if (other.StartsWith(seq))
                    {
                        other = other.TrimFront(seq.Length);
                        anyMatched = true;
                    }
                }

                if (!anyMatched)
                {
                    break;
                }
            }


            return other;
        }

        public Span TrimEnd(char[] chars)
        {
            var other = this;
            while (0 < other.Length && StringUtility.CharArrayContains(chars, other[other.Length - 1]))
            {
                other.Length--;
            }

            return other;
        }

        public Span TrimFrontAndEnd(char[] chars)
        {
            return TrimFront(chars).TrimEnd(chars);
        }

        public bool IsWhitespace()
        {
            return TrimFrontAndEnd(Chars.Whitespace).Length == 0;
        }

        public Span Substring(int index, int inLength)
        {
            if (Length < index + inLength)
            {
                throw new ArgumentOutOfRangeException();
            }

            var other = this;
            other.Start += index;
            other.Length = inLength;
            return other;
        }

        public bool Equals(Span other)
        {
            if (other.Length != Length)
            {
                return false;
            }

            for (int i = 0; i < Length; ++i)
            {
                if (other[i] != this[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            if (stringBuffer != null)
            {
                return stringBuffer.Substring(Start, Length);
            }
            else if (stringBuilderBuffer != null)
            {
                return stringBuilderBuffer.ToString(Start, Length);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static implicit operator Span(string value) => new Span(value);
    }
}