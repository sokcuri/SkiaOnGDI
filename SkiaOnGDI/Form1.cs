using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkiaOnGDI
{
    public unsafe partial class Form1 : Form
    {
        FPSCounter counter;
        SKBitmap buffer;
        SKCanvas bufferContext;

        // resources
        Random r = new Random();
        List<Ball> balls = new List<Ball>();
        Queue<Ball> lazyAddBalls = new Queue<Ball>();
        Queue<Ball> lazyRemoveBalls = new Queue<Ball>();

        IntPtr mHandle;

        public Form1()
        {
            InitializeComponent();

            mHandle = this.Handle;

            var screen = Screen.PrimaryScreen;

            this.Width = screen.WorkingArea.Width;
            this.Height = screen.WorkingArea.Height;
            this.Location = Point.Empty;

            counter = new FPSCounter();

            Task.Factory.StartNew(() =>
            {
                buffer = new SKBitmap(screen.WorkingArea.Width, screen.WorkingArea.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
                bufferContext = new SKCanvas(buffer);

                //bufferContext.RotateDegrees(1f);

                CreateBalls();

                while (true)
                {
                    PipeLine();
                }
            });

            return;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000;
                cp.ExStyle |= 0x00000020;
                return cp;
            }
        }

        void CreateBalls()
        {
            for (int i = 0; i < 128; i++)
                lazyAddBalls.Enqueue(new Ball()
                {
                    X = this.Width / 2,
                    Y = this.Height / 2,
                    SpeedX = r.Next(12) + 1,
                    SpeedY = r.Next(12) + 1,
                    Radius = r.Next(2, 30),
                    DirectionX = new[] { -1, 1 }[r.Next(2)],
                    DirectionY = new[] { -1, 1 }[r.Next(2)],
                    Paint = new SKPaint()
                    {
                        Color = Color.FromArgb(
                            r.Next(256),
                            r.Next(256),
                            r.Next(256)
                            ).ToSKColor(),
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 2,
                        IsAntialias = true
                    }
                });
        }

        IntPtr hBitmap = IntPtr.Zero;
        IntPtr scan0 = IntPtr.Zero;

        public void UpdateFormDisplay()
        {
            IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
            IntPtr memDc = Win32.CreateCompatibleDC(screenDc);

            IntPtr oldBitmap = IntPtr.Zero;

            if (hBitmap == IntPtr.Zero)
            {
                var bmh = new Win32.BITMAPV5HEADER();
                bmh.bV5Size = (uint)Marshal.SizeOf<Win32.BITMAPV5HEADER>();
                bmh.bV5Width = buffer.Width;
                bmh.bV5Height = -buffer.Height;
                bmh.bV5Planes = 1;
                bmh.bV5BitCount = 32;
                bmh.bV5Compression = Win32.BitmapCompressionMode.BI_RGB;
                bmh.bV5AlphaMask = 0xFF000000;
                bmh.bV5RedMask = 0x00FF0000;
                bmh.bV5GreenMask = 0x0000FF00;
                bmh.bV5BlueMask = 0x000000FF;

                hBitmap = Win32.CreateDIBSection(screenDc, ref bmh, 0, out scan0, IntPtr.Zero, 0);
            }
            int stride = 4 * ((buffer.Width * buffer.BytesPerPixel + 3) / 4);
            int totBytes = stride * buffer.Height;
            
            buffer.CopyPixelsTo(scan0, buffer.ByteCount);
            oldBitmap = Win32.SelectObject(memDc, hBitmap);

            Size size = new Size(buffer.Width, buffer.Height);
            Point pointSource = new Point(0, 0);
            Point topPos = Point.Empty;

            Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION();
            blend.BlendOp = Win32.AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = 255;
            blend.AlphaFormat = Win32.AC_SRC_ALPHA;

            Win32.UpdateLayeredWindow(mHandle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32.ULW_ALPHA);

            Win32.ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero)
            {
                Win32.SelectObject(memDc, oldBitmap);
            }
            Win32.DeleteDC(memDc);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void PipeLine()
        {
            Render(bufferContext);

            UpdateFormDisplay();
        }

        private void Render(SKCanvas c)
        {
            c.Clear();
            
            while (lazyAddBalls.Count > 0)
                balls.Add(lazyAddBalls.Dequeue());

            while (lazyRemoveBalls.Count > 0)
                balls.Remove(lazyRemoveBalls.Dequeue());

            foreach (Ball ball in balls)
            {
                ball.X += ball.SpeedX * ball.DirectionX;
                ball.Y += ball.SpeedY * ball.DirectionY;

                if (ball.X < ball.Radius || ball.X > Width - ball.Radius)
                    ball.DirectionX *= -1;

                if (ball.Y < ball.Radius || ball.Y > Height - ball.Radius)
                    ball.DirectionY *= -1;

                ball.X = Math.Min(Math.Max(ball.X, ball.Radius), Width - ball.Radius);
                ball.Y = Math.Min(Math.Max(ball.Y, ball.Radius), Height - ball.Radius);

                ball.Opacity = Math.Max(ball.Opacity, 0);
                ball.Paint.Color = ball.Paint.Color.WithAlpha((byte)(255 * ball.Opacity));

                if (ball.Opacity < float.Epsilon)
                    lazyRemoveBalls.Enqueue(ball);

                c.DrawOval(ball.X, ball.Y, ball.Radius, ball.Radius, ball.Paint);
            }

            counter.Update();

            using (var paint = new SKPaint()
            {
                Color = SKColors.Red,
                IsAntialias = true,
                TextAlign = SKTextAlign.Right,
                TextSize = 20
            })
            {
                Console.WriteLine(counter.FPS);
                c.DrawText($"FPS: {counter.FPS}",
                    (int)(Width - 10),
                    (int)(Height - 10),
                    paint);

                c.DrawText($"Object: {balls.Count}",
                    (int)(Width - 10),
                    (int)(Height - 10 - paint.TextSize * 1.5),
                    paint);
            }
            //c.RotateDegrees(0.5f, buffer.Width / 2, buffer.Height / 2);

        }
    }
}
