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
using System.Windows;
using System.Collections;
using CookComputing.XmlRpc;
using System.Windows.Forms;
using System.Windows.Automation;

namespace Ldtpd
{
    class Image
    {
        Utils utils;
        public Image(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        public string ImageCapture(string windowName = null,
            int x = 0, int y = 0, int width = -1, int height = -1)
        {
            System.Drawing.Bitmap b = null;
            ScreenShot ss = null;
            AutomationElement windowHandle;
            try
            {
                ss = new ScreenShot();
                // capture entire screen, and save it to a file
                string path = Path.GetTempPath() +
                    Path.GetRandomFileName() + ".png";
                if (windowName.Length > 0)
                {
                    windowHandle = utils.GetWindowHandle(windowName);
                    if (windowHandle == null)
                    {
                        throw new XmlRpcFaultException(123,
                            "Unable to find window: " + windowName);
                    }
                    windowHandle.SetFocus();
                    Rect rect = windowHandle.Current.BoundingRectangle;
                    System.Drawing.Rectangle rectangle = new 
                        System.Drawing.Rectangle((int)rect.X, (int)rect.Y,
                        (int)rect.Width, (int)rect.Height);
                    b = ss.CaptureSize(path, rectangle);
                }
                else if (width != -1 && height != -1)
                {
                    System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(
                        x, y, width, height);
                    b = ss.CaptureSize(path, rectangle);
                }
                else
                {
                    b = ss.Capture(path);
                }
                string encodedText = "";
                using (FileStream fs = File.Open(path, FileMode.Open,
                    FileAccess.Read))
                {
                    Byte[] bytesToEncode = new byte[fs.Length];
                    fs.Read(bytesToEncode, 0, (int)fs.Length);
                    encodedText = Convert.ToBase64String(bytesToEncode);
                    fs.Close();
                }
                LogMessage(path);
                File.Delete(path);
                return encodedText;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                if (ex is XmlRpcFaultException)
                    throw;
                throw new XmlRpcFaultException(123,
                    "Unhandled exception: " + ex.Message);
            }
            finally
            {
                b = null;
                ss = null;
                windowHandle = null;
            }
        }
    }
}
