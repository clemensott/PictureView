using System;

namespace PictureView
{
    internal struct IntSize : IEquatable<IntSize>
    {
        public static readonly IntSize Empty = new IntSize();

        public int Width { get; set; }

        public int Height { get; set; }

        public IntSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(IntSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IntSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        public static bool operator ==(IntSize s1, IntSize s2)
        {
            return s1.Width == s2.Width && s1.Height == s2.Height;
        }

        public static bool operator !=(IntSize s1, IntSize s2)
        {
            return s1.Width != s2.Width || s1.Height != s2.Height;
        }
    }
}
