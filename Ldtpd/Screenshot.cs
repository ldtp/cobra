/*
WinLDTP 1.0

@author: Nagappan Alagappan <nalagappan@vmware.com>
@copyright: Copyright (c) 2011-12 VMware Inc.,
@license: MIT license

http://ldtp.freedesktop.org

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace Ldtpd
{
    class ScreenShot
    {
        public Bitmap Capture()
        {
            return ScreenCapture(Rectangle.Empty);
        }

        public Bitmap Capture(string fileName)
        {
            Bitmap screenshot = ScreenCapture(Rectangle.Empty);
            saveImage2File(screenshot, fileName);
            return screenshot;
        }

        public Bitmap CaptureSize(Rectangle rect)
        {
            return ScreenCapture(rect, true);
        }

        public Bitmap CaptureSize(string fileName, Rectangle rect)
        {
            Bitmap screenshot = ScreenCapture(rect, true);
            saveImage2File(screenshot, fileName);
            return screenshot;
        }

        private void saveImage2File(Bitmap screenshot, string fileName)
        {
            string ext = Path.GetExtension(fileName);
            switch (ext.ToLower())
            {
                case ".png":
                    screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case ".jpg":
                case ".jpeg":
                    screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case ".tiff":
                    screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case ".bmp":
                    screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case ".gif":
                    screenshot.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
            }
        }

        private Bitmap ScreenCapture(Rectangle rect, bool isRect = false)
        {
            int width = 0;
            int height = 0;
            Bitmap screenshot;

            // Get the size of multi-monitor
            foreach (Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                int x = screen.Bounds.X + screen.Bounds.Width;
                if (x > width)
                    width = x;
                int y = screen.Bounds.Y + screen.Bounds.Height;
                if (y > height)
                    height = y;
            }

            if (isRect)
                screenshot = new Bitmap(rect.Width, rect.Height);
            else
                screenshot = new Bitmap(width, height);

            Graphics g = Graphics.FromImage(screenshot);
            if (isRect)
            {
                // Copy from given x, y, width and height
                g.CopyFromScreen(new Point(rect.X, rect.Y),
                    Point.Empty, rect.Size);
            }
            else
            {
                // Copy complete screen
                g.CopyFromScreen(Point.Empty,
                    Point.Empty, screenshot.Size);
            }
            return screenshot;
        }
    }
}
