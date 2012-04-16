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
using System.Threading;
using System.Collections;
using CookComputing.XmlRpc;
using System.Windows.Forms;
using System.Windows.Automation;

namespace Ldtpd
{
    class Keyboard
    {
        Utils utils;
        public Keyboard(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        public int EnterString(string windowName, string objName = null,
            string data = null)
        {
            if (objName != null && objName.Length > 0)
            {
                AutomationElement windowHandle = utils.GetWindowHandle(windowName);
                if (windowHandle != null)
                {
                    AutomationElement childHandle = utils.GetObjectHandle(
                        windowHandle, objName);
                    windowHandle = null;
                    try
                    {
                        if (childHandle != null)
                        {
                            childHandle.SetFocus();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage(ex);
                        if (ex is XmlRpcFaultException)
                            throw;
                        else
                            throw new XmlRpcFaultException(123,
                                "Unhandled exception: " + ex.Message);
                    }
                    finally
                    {
                        childHandle = null;
                    }
                }
            }
            else
            {
                // Hack as Linux LDTPv1/v2
                data = windowName;
            }
            if (data == null || data.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            return GenerateKeyEvent(data);
        }
        public int GenerateKeyEvent(string data)
        {
            if (data == null || data.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            KeyInfo[] keys = utils.GetKeyVal(data);
            int index = 0;
            int lastIndex = 0;
            bool capsLock = false;
            foreach (KeyInfo key in keys)
            {
                try
                {
                    if (!capsLock &&
                        key.key == System.Windows.Input.Key.CapsLock)
                    {
                        // For the first time
                        // Set Caps Lock ON
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.CapsLock,
                            true);
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.CapsLock,
                            false);
                        capsLock = true;
                        continue;
                    }
                    if (capsLock && key.shift)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            true);
                    }
                    else if (!capsLock && key.shift)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            !utils.shiftKeyPressed);
                    }
                    else if (utils.shiftKeyPressed)
                    {
                        // Workaround: Release existing shift key
                        // As the default behavior fails when it finds capital letter
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            false);
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            true);
                    }
                    // Key press
                    ATGTestInput.Input.SendKeyboardInput(key.key, true);
                    if (!key.nonPrintKey)
                    {
                        // Key release
                        // Don't release nonPrintKey, it will be released later
                        ATGTestInput.Input.SendKeyboardInput(key.key, false);
                        for (int i = lastIndex; i < index; i++)
                        {
                            KeyInfo tmpKey = keys[i];
                            if (!tmpKey.nonPrintKey ||
                                tmpKey.key == System.Windows.Input.Key.CapsLock)
                                // Release only nonPrintKey
                                // Caps lock will be released later
                                break;
                            if (tmpKey.key == System.Windows.Input.Key.LeftShift ||
                                tmpKey.key == System.Windows.Input.Key.RightShift)
                            {
                                utils.shiftKeyPressed = false;
                            }
                            // Key release
                            ATGTestInput.Input.SendKeyboardInput(tmpKey.key, false);
                        }
                        // Update lastIndex with index
                        // the non_print_key that has been processed
                        lastIndex = index;
                    }
                    if (capsLock && key.shift)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            false);
                    }
                    else if (!capsLock && key.shift)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            !utils.shiftKeyPressed);
                    }
                    else if (utils.shiftKeyPressed)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            false);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(ex.StackTrace);
                    throw new XmlRpcFaultException(123, ex.Message);
                }
                Thread.Sleep(200);
                index++;
            }
            for (int i = lastIndex; i < index; i++)
            {
                KeyInfo tmpKey = keys[i];
                if (!tmpKey.nonPrintKey ||
                    tmpKey.key == System.Windows.Input.Key.CapsLock)
                    // Release only nonPrintKey
                    // Caps lock will be released later
                    break;
                if (utils.shiftKeyPressed)
                {
                    if (tmpKey.key == System.Windows.Input.Key.LeftShift ||
                        tmpKey.key == System.Windows.Input.Key.RightShift)
                    {
                        utils.shiftKeyPressed = false;
                    }
                }
                // Key release
                ATGTestInput.Input.SendKeyboardInput(tmpKey.key, false);
            }
            if (capsLock)
            {
                // Set Caps Lock OFF
                ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.CapsLock,
                    true);
                ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.CapsLock,
                    false);
            }
            return 1;
        }
        public int KeyPress(string data)
        {
            if (data == null || data.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            KeyInfo[] keys = utils.GetKeyVal(data);
            foreach (KeyInfo key in keys)
            {
                if (key.key == System.Windows.Input.Key.LeftShift ||
                    key.key == System.Windows.Input.Key.RightShift)
                {
                    utils.shiftKeyPressed = true;
                }
                try
                {
                    ATGTestInput.Input.SendKeyboardInput(key.key, true);
                }
                catch (Exception ex)
                {
                    LogMessage(ex.StackTrace);
                    throw new XmlRpcFaultException(123, ex.Message);
                }
                Thread.Sleep(200);
            }
            return 1;
        }
        public int KeyRelease(string data)
        {
            if (data == null || data.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            KeyInfo[] keys = utils.GetKeyVal(data);
            foreach (KeyInfo key in keys)
            {
                if (key.key == System.Windows.Input.Key.LeftShift ||
                    key.key == System.Windows.Input.Key.RightShift)
                {
                    utils.shiftKeyPressed = false;
                }
                try
                {
                    ATGTestInput.Input.SendKeyboardInput(key.key, false);
                }
                catch (Exception ex)
                {
                    LogMessage(ex.StackTrace);
                    throw new XmlRpcFaultException(123, ex.Message);
                }
                Thread.Sleep(200);
            }
            return 1;
        }
    }
}
