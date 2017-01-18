using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using SkiaSharp;

namespace SkiaOnGDI
{
    public static unsafe class BitmapEx
    {
        private static FieldInfo nativeImageFieldInfo = 
            typeof(Bitmap).GetField("nativeImage", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic);

        public static IntPtr GetNativeImage(this Bitmap bmp)
        {
            return (IntPtr)nativeImageFieldInfo.GetValue(bmp);
        }

        public static IntPtr GetHBitmap2(this Bitmap bmp, Color c)
        {
            IntPtr native = bmp.GetNativeImage();
            IntPtr hBitmap;

            Win32.GdipCreateHBITMAPFromBitmap(new HandleRef(bmp, native), out hBitmap,
                ColorTranslator.ToWin32(Color.FromArgb(0)));
            Console.WriteLine(hBitmap);
            return hBitmap;
        }

        public static IntPtr GetHBitmap3(this Bitmap bmp, IntPtr dc)
        {
            IntPtr native = bmp.GetNativeImage();
            IntPtr hBitmap;

            var lockeddata = new BitmapData();
            var bih = new Win32.BITMAPINFOHEADER();
            bih.biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>();
            bih.biWidth = bmp.Width;
            bih.biHeight = bmp.Height;
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
            IntPtr scan0;
            hBitmap = Win32.CreateDIBSection(dc, ref bi, 0, out scan0, IntPtr.Zero, 0);
            
            var rect = new Win32.GPRECT()
            {
                Width = bmp.Width,
                Height = bmp.Height
            };

            lockeddata.Stride = -bmp.Width * 4;
            lockeddata.Scan0 = scan0 + (bmp.Width * 4 * (bmp.Height - 1));

            Win32.GdipBitmapLockBits(new HandleRef(bmp, native), ref rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb, lockeddata);
            Win32.GdipBitmapUnlockBits(new HandleRef(bmp, native), lockeddata);

            int* ptr;
            uint i;

            for (ptr = (int*)scan0, i = 0; i < bmp.Width * bmp.Height; ptr++, i++)
            {
                if ((*ptr & 0xff000000) == 0xff000000) continue;
                *ptr = blend_argb_no_bkgnd_alpha(*ptr, 0);
            }

            return hBitmap;
        }

        public static bool ConvHBitmap(this SKBitmap bmp, ref IntPtr hBitmap, ref IntPtr scan0)
        {
            if (hBitmap == IntPtr.Zero)
            {
                IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
                var bih = new Win32.BITMAPINFOHEADER();
                bih.biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>();
                bih.biWidth = bmp.Width;
                bih.biHeight = bmp.Height;
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
            return true;
        }
        
        static int blend_argb_no_bkgnd_alpha(int src, int bkgnd)
        {
            byte b = (byte)src;
            byte g = (byte)(src >> 8);
            byte r = (byte)(src >> 16);
            byte alpha = (byte)(src >> 24);

            return ((b + ((byte)bkgnd * (255 - alpha) + 127) / 255) |
                    (g + ((byte)(bkgnd >> 8) * (255 - alpha) + 127) / 255) << 8 |
                    (r + ((byte)(bkgnd >> 16) * (255 - alpha) + 127) / 255) << 16 |
                    (alpha << 24));
        }
    }
}
