using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
        Bitmap bmp;

        // resources
        Random r = new Random();
        List<Ball> balls = new List<Ball>();
        Queue<Ball> lazyAddBalls = new Queue<Ball>();
        Queue<Ball> lazyRemoveBalls = new Queue<Ball>();

        IntPtr mHandle;

        public Form1()
        {
            //OpenTK.Toolkit.Init();
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
                bmp = new Bitmap(buffer.Width, buffer.Height);

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

        //Updates the Form's display using API calls
        public void UpdateFormDisplay(Bitmap bmp)
        {
            IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
            IntPtr memDc = Win32.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            //Display-image
            hBitmap = bmp.GetHbitmap(Color.FromArgb(0));  //Set the fact that background is transparent
            oldBitmap = Win32.SelectObject(memDc, hBitmap);

            //Display-rectangle
            Size size = bmp.Size;
            Point pointSource = new Point(0, 0);
            Point topPos = Point.Empty;

            //Set up blending options
            Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION();
            blend.BlendOp = Win32.AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = 255;
            blend.AlphaFormat = Win32.AC_SRC_ALPHA;

            Win32.UpdateLayeredWindow(mHandle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32.ULW_ALPHA);

            //Clean-up
            //bmp.Dispose();
            Win32.ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero)
            {
                Win32.SelectObject(memDc, oldBitmap);
                Win32.DeleteObject(hBitmap);
            }
            Win32.DeleteDC(memDc);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void PipeLine()
        {
            Render(bufferContext);

            var bmpLock = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            buffer.CopyPixelsTo(bmpLock.Scan0, buffer.ByteCount);

            bmp.UnlockBits(bmpLock);

            UpdateFormDisplay(bmp);
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

                //ball.Opacity -= 0.05f;
                //ball.Opacity = 0.5f;
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
