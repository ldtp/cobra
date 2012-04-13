/*
WinLDTP 1.0

@author: Nagappan Alagappan <nalagappan@vmware.com>
@copyright: Copyright (c) 2011-12 VMware Inc.,
@license: LGPLv2

http://ldtp.freedesktop.org

This file may be distributed and/or modified under the terms of the GNU General
Public License version 2 as published by the Free Software Foundation. This file
is distributed without any warranty; without even the implied warranty of
merchantability or fitness for a particular purpose.

See 'README.txt' in the source distribution for more information.

Headers in this file shall remain intact.
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
