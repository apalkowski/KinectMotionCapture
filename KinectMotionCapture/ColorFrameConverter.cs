using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectMotionCapture
{
    internal static class ColorFrameConverter
    {
        private static WriteableBitmap _bitmap = null;
        private static int _width;
        private static int _height;
        private static byte[] _pixels = null;
        private static PixelFormat _pixelFormat = PixelFormats.Bgr32;

        public static BitmapSource CovertToBitmap(ColorImageFrame frame, double dpiX, double dpiY)
        {
            if (_bitmap == null)
            {
                _width = frame.Width;
                _height = frame.Height;
                _pixels = new byte[_width * _height * ((_pixelFormat.BitsPerPixel + 7) / 8)];
                _bitmap = new WriteableBitmap(_width, _height, dpiX, dpiY, _pixelFormat, null);
            }

            frame.CopyPixelDataTo(_pixels);

            _bitmap.Lock();

            Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));

            _bitmap.Unlock();

            return _bitmap;
        }
    }
}