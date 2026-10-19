using System;
using System.Globalization;
using Avalonia;

namespace System.Windows {
    public struct Rect : IEquatable<Rect> {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public static Rect Parse(string s) {
            var p = s.Split(',');
            return new Rect(double.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), double.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture));
        }

        public string ToString(IFormatProvider provider) {
            return string.Format(provider, "{0},{1},{2},{3}", X, Y, Width, Height);
        }

        public Rect(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height) { }

        public Rect(Size size) : this(0d, 0d, size.Width, size.Height) { }

        public Rect(Point a, Point b) : this(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) { }

        public static Rect Empty => new Rect(double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);

        public bool IsEmpty => Width < 0d;
        public double Left => X;
        public double Top => Y;
        public double Right => IsEmpty ? double.NegativeInfinity : X + Width;
        public double Bottom => IsEmpty ? double.NegativeInfinity : Y + Height;
        public Point TopLeft => new Point(X, Y);
        public Point TopRight => new Point(Right, Y);
        public Point BottomLeft => new Point(X, Bottom);
        public Point BottomRight => new Point(Right, Bottom);

        public Point Location {
            get => new Point(X, Y);
            set {
                X = value.X;
                Y = value.Y;
            }
        }

        public Size Size {
            get => new Size(Width, Height);
            set {
                Width = value.Width;
                Height = value.Height;
            }
        }

        public static implicit operator Rect(Avalonia.Rect r) => new Rect(r.X, r.Y, r.Width, r.Height);
        public static implicit operator Avalonia.Rect(Rect r) => new Avalonia.Rect(r.X, r.Y, Math.Max(0d, r.Width), Math.Max(0d, r.Height));

        public bool Contains(Point p) => Contains(p.X, p.Y);
        public bool Contains(double x, double y) => !IsEmpty && x >= X && x <= X + Width && y >= Y && y <= Y + Height;
        public bool Contains(Rect r) => !IsEmpty && !r.IsEmpty && X <= r.X && Y <= r.Y && X + Width >= r.X + r.Width && Y + Height >= r.Y + r.Height;

        public bool IntersectsWith(Rect r) {
            return !IsEmpty && !r.IsEmpty && r.Left <= Right && r.Right >= Left && r.Top <= Bottom && r.Bottom >= Top;
        }

        public void Intersect(Rect r) {
            if (!IntersectsWith(r)) {
                this = Empty;
                return;
            }

            var left = Math.Max(Left, r.Left);
            var top = Math.Max(Top, r.Top);
            Width = Math.Max(Math.Min(Right, r.Right) - left, 0d);
            Height = Math.Max(Math.Min(Bottom, r.Bottom) - top, 0d);
            X = left;
            Y = top;
        }

        public static Rect Intersect(Rect a, Rect b) {
            a.Intersect(b);
            return a;
        }

        public void Union(Rect r) {
            if (IsEmpty) {
                this = r;
            } else if (!r.IsEmpty) {
                var left = Math.Min(Left, r.Left);
                var top = Math.Min(Top, r.Top);
                Width = Math.Max(Right, r.Right) - left;
                Height = Math.Max(Bottom, r.Bottom) - top;
                X = left;
                Y = top;
            }
        }

        public static Rect Union(Rect a, Rect b) {
            a.Union(b);
            return a;
        }

        public void Union(Point p) => Union(new Rect(p, p));

        public void Offset(double dx, double dy) {
            X += dx;
            Y += dy;
        }

        public static Rect Offset(Rect r, double dx, double dy) {
            r.Offset(dx, dy);
            return r;
        }

        public void Inflate(double w, double h) {
            X -= w;
            Y -= h;
            Width = Math.Max(Width + w * 2d, 0d);
            Height = Math.Max(Height + h * 2d, 0d);
        }

        public static Rect Inflate(Rect r, double w, double h) {
            r.Inflate(w, h);
            return r;
        }

        public static bool operator ==(Rect a, Rect b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        public static bool operator !=(Rect a, Rect b) => !(a == b);

        public bool Equals(Rect other) => this == other;
        public override bool Equals(object obj) => obj is Rect r && this == r;
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", X, Y, Width, Height);
    }
}
