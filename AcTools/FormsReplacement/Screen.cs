namespace AcTools.FormsReplacement;

public static class Screen
{
    public class Rectangle
    {
        public int Width { get; }
        public int Height { get; }

        public Rectangle(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    public class ScreenInstance
    {
        public Rectangle Bounds { get; }

        public ScreenInstance(Rectangle bounds)
        {
            Bounds = bounds;
        }
    }

    public static ScreenInstance PrimaryScreen => throw new NotImplementedException();
}