using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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

        public Form1()
        {
            //OpenTK.Toolkit.Init();
            InitializeComponent();

            var screen = Screen.PrimaryScreen;
            buffer = new SKBitmap(screen.WorkingArea.Width, screen.WorkingArea.Height);
            bufferContext = new SKCanvas(buffer);

            this.Width = buffer.Width;
            this.Height = buffer.Height;

            this.Location = Point.Empty;

            counter = new FPSCounter();

            var t = new System.Windows.Forms.Timer()
            {
                Interval = 1
            };

            t.Tick += (s, e) =>
            {
                PipeLine();
            };

            t.Start();
            //var renderer = new Thread(() =>
            //{
            //    while (true)
            //    {
            //        this.BeginInvoke(new Action(() =>
            //        {
            //            Render();
            //        }));
            //    }
            //});

            //renderer.IsBackground = true;
            //renderer.Start();
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
            for (int i = 0; i < 256; i++)
                lazyAddBalls.Enqueue(new Ball()
                {
                    X = this.Width / 2,
                    Y = this.Height /2,
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
        public void UpdateFormDisplay(Image backgroundImage)
        {
            IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
            IntPtr memDc = Win32.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                //Display-image
                Bitmap bmp = new Bitmap(backgroundImage);
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));  //Set the fact that background is transparent
                oldBitmap = Win32.SelectObject(memDc, hBitmap);

                //Display-rectangle
                Size size = bmp.Size;
                Point pointSource = new Point(0, 0);
                Point topPos = new Point(this.Left, this.Top);

                //Set up blending options
                Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION();
                blend.BlendOp = Win32.AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = Win32.AC_SRC_ALPHA;

                Win32.UpdateLayeredWindow(this.Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32.ULW_ALPHA);

                //Clean-up
                bmp.Dispose();
                Win32.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    Win32.SelectObject(memDc, oldBitmap);
                    Win32.DeleteObject(hBitmap);
                }
                Win32.DeleteDC(memDc);
            }
            catch (Exception)
            {
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateBalls();
            PipeLine();
        }

        private void PipeLine()
        {
            Render(bufferContext);
            
            byte[] bytes = buffer.Bytes;

            var bmp = new Bitmap(buffer.Width, buffer.Height);
            var bmpLock = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(bytes, 0, bmpLock.Scan0, bytes.Length);

            bmp.UnlockBits(bmpLock);

            UpdateFormDisplay(bmp);
            
            bmp.Dispose();
        }

        private void Render(SKCanvas c)
        {
            c.Clear();
            this.Location = Point.Empty;

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
                ball.Opacity = 0.5f;
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

                c.DrawText($"한글이지롱rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrㅁ나어리ㅏㅁㄴ어리먼이러민어리먼이ㅏㅓ미ㅓㅇ닐머니아ㅓ리ㅏㅁ넝리ㅏ머ㅣㅇ나ㅓㄹ미ㅏ넝리ㅏ멍ㄴ", (int)(Width - 10), 20, paint);
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
