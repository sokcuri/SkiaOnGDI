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
    }
}
