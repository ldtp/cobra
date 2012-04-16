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
using ATGTestInput;
using System.Windows;
using System.Collections;
using CookComputing.XmlRpc;
using System.Windows.Forms;
using System.Windows.Automation;

namespace Ldtpd
{
    class Mouse
    {
        Utils utils;
        public Mouse(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        public int GenerateMouseEvent(int x, int y, String type = "b1p")
        {
            Point pt = new Point(x, y);
            switch (type)
            {
                case "b1p":
                    Input.SendMouseInput(x, y, 0, SendMouseInputFlags.LeftDown);
                    break;
                case "b1r":
                    Input.SendMouseInput(x, y, 0, SendMouseInputFlags.LeftUp);
                    break;
                case "b1c":
                    Input.MoveTo(pt);
                    Input.SendMouseInput(0, 0, 0, SendMouseInputFlags.LeftDown);
                    Input.SendMouseInput(0, 0, 0, SendMouseInputFlags.LeftUp);
                    break;
                case "b2p":
                    Input.SendMouseInput(x, y, 0, SendMouseInputFlags.MiddleDown);
                    break;
                case "b2r":
                    Input.SendMouseInput(x, y, 0, SendMouseInputFlags.MiddleUp);
                    break;
                case "b2c":
                    Input.MoveTo(pt);
                    Input.SendMouseInput(0, 0, 0, SendMouseInputFlags.MiddleDown);
                    Input.SendMouseInput(0, 0, 0, SendMouseInputFlags.MiddleUp);
                    break;
                case "b3p":
                    Input.SendMouseInput(x, y, 0, SendMouseInputFlags.RightDown);
                    break;
                case "b3r":
                    Input.SendMouseInput(x, y, 0, SendMouseInputFlags.RightUp);
                    break;
                case "b3c":
                    Input.MoveTo(pt);
                    Input.SendMouseInput(0, 0, 0, SendMouseInputFlags.RightDown);
                    Input.SendMouseInput(0, 0, 0, SendMouseInputFlags.RightUp);
                    break;
                case "abs":
                    Input.SendMouseInput(pt.X, pt.Y, 0,
                        SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
                    break;
                case "rel":
                    ATGTestInput.Input.SendMouseInput(pt.X, pt.Y, 0,
                        SendMouseInputFlags.Move);
                    break;
                default:
                    throw new XmlRpcFaultException(123,
                        "Unsupported mouse type: " + type);
            }
            return 1;
        }
    }
}
