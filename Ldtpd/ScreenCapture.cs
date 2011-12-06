//------------------------------------//
//-----------------------------------//
//  By Farooq Azam
//  www.farooqazam.net
//  http://www.farooqazam.net/screen-capture-class-for-c-sharp/
//-----------------------------------//
//-----------------------------------//

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ldtpd
{
    class ScreenCapture
    {
        private Bitmap _screenshot = null;

        public Bitmap CaptureScreen()
        {
            return Capture(Rectangle.Empty, false);
        }

        public Bitmap CaptureScreen(string fileName)
        {
            Bitmap screenshot = Capture(Rectangle.Empty, false);
            saveImage(fileName, screenshot);
            return screenshot;
        }

        public void CopyToClipboard()
        {
            if (this._screenshot != null)
                Clipboard.SetImage(this._screenshot);
            else if (this._screenshot == null)
                MessageBox.Show("No screenshot found. Please take a screenshot first.", "Copy to Clipboard");
        }

        public Bitmap CaptureRectangle(Rectangle rect)
        {
            return Capture(rect, true);
        }

        public Bitmap CaptureRectangle(Rectangle rect, string fileName)
        {
            Bitmap screenshot = Capture(rect, true);
            saveImage(fileName, screenshot);
            return screenshot;
        }

        private Bitmap Capture(Rectangle rect, bool isRect)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            Bitmap screenshot = null;

            if (!isRect)
                screenshot = new Bitmap(screenWidth, screenHeight);
            else if (isRect)
                screenshot = new Bitmap(rect.Width, rect.Height);

            Graphics g = Graphics.FromImage(screenshot);
            if (!isRect)
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, screenshot.Size);
            }
            else if (isRect)
            {
                g.CopyFromScreen(new Point(rect.X, rect.Y), Point.Empty, rect.Size);
            }

            this._screenshot = screenshot;

            return screenshot;
        }

        private void saveImage(string fileName, Bitmap screenshot)
        {
            string ext = System.IO.Path.GetExtension(fileName); ;
            ext = ext.ToLower();

            if (ext == ".jpg" || ext.ToLower() == ".jpeg")
                screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            else if (ext == ".gif")
                screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Gif);
            else if (ext == ".png")
                screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
            else if (ext == ".bmp")
                screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Bmp);
            else if (ext == ".tiff")
                screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Tiff);
        }
    }
}
