// Core/Types/UiPadding.cs
namespace DuelLedger.Vision
{
    /// <summary>
    /// Windows.Forms に依存しない軽量 Padding 互換実装
    /// </summary>
    public readonly struct UiPadding
    {
        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }

        public int Horizontal => Left + Right;
        public int Vertical   => Top + Bottom;

        public static readonly UiPadding Empty = new UiPadding(0);

        public UiPadding(int all) : this(all, all, all, all) { }

        public UiPadding(int left, int top, int right, int bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }

        public override string ToString()
            => $"{{Left={Left},Top={Top},Right={Right},Bottom={Bottom}}}";

        public override bool Equals(object? obj)
            => obj is UiPadding p && p.Left == Left && p.Top == Top && p.Right == Right && p.Bottom == Bottom;

        public override int GetHashCode()
            => System.HashCode.Combine(Left, Top, Right, Bottom);

        public static bool operator ==(UiPadding a, UiPadding b) => a.Equals(b);
        public static bool operator !=(UiPadding a, UiPadding b) => !a.Equals(b);
    }
}
