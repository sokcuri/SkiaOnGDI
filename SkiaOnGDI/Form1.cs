using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkiaOnGDI
{
    public partial class Form1 : Form
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
                buffer = new SKBitmap(screen.WorkingArea.Width, screen.WorkingArea.Height);
                bufferContext = new SKCanvas(buffer);

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
                var bih = new Win32.BITMAPINFOHEADER();
                bih.biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>();
                bih.biWidth = buffer.Width;
                bih.biHeight = buffer.Height;
                bih.biPlanes = 1;
                bih.biBitCount = 32;
                bih.biCompression = Win32.BitmapCompressionMode.BI_RGB;
                bih.biSizeImage = 0;
                bih.biXPelsPerMeter = 0;
                bih.biYPelsPerMeter = 0;
                bih.biClrUsed = 0;
                bih.biClrImportant = 0;
                
                var bi = new Win32.BITMAPINFO();
                bi.bmiHeader = bih;
                hBitmap = Win32.CreateDIBSection(screenDc, ref bi, 0, out scan0, IntPtr.Zero, 0);
            }
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
        }
    }
}
