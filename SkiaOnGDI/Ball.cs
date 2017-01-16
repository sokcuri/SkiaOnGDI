using SkiaSharp;

namespace SkiaOnGDI
{
    class Ball
    {
        public SKPaint Paint { get; set; }

        public float X { get; set; }
        public float Y { get; set; }

        public float Radius { get; set; }

        public float SpeedX { get; set; } = 1f;
        public float SpeedY { get; set; } = 1f;

        public int DirectionX = 1;
        public int DirectionY = 1;

        public float Opacity { get; set; } = 1;
    }
}
