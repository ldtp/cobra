/*
 * Cobra WinLDTP 4.0
 * 
 * Author: Nagappan Alagappan <nalagappan@vmware.com>
 * Copyright: Copyright (c) 2011-13 VMware, Inc. All Rights Reserved.
 * License: MIT license
 * 
 * http://ldtp.freedesktop.org
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
 * of the Software, and to permit persons to whom the Software is furnished to do
 * so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
*/
using System;
using System.Windows;
using System.Threading;
using System.Collections;
using CookComputing.XmlRpc;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    class Keyboard
    {
        Utils utils;
        bool shiftKeyPressed = false;
        public Keyboard(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        internal KeyInfo GetKey(string key)
        {
            try
            {
                switch (key.ToLower())
                {
                    case "ctrl":
                    case "ctrll":
                        return new KeyInfo(System.Windows.Input.Key.LeftCtrl, false, true);
                    case "ctrlr":
                        return new KeyInfo(System.Windows.Input.Key.RightCtrl, false, true);
                    case "caps":
                    case "capslock":
                        return new KeyInfo(System.Windows.Input.Key.CapsLock, false, true);
                    case "pgdown":
                        return new KeyInfo(System.Windows.Input.Key.PageDown, false);
                    case "alt":
                    case "altl":
                        return new KeyInfo(System.Windows.Input.Key.LeftAlt, false, true);
                    case "altr":
                        return new KeyInfo(System.Windows.Input.Key.RightAlt, false, true);
                    case "shift":
                    case "shiftl":
                        return new KeyInfo(System.Windows.Input.Key.LeftShift, true, true);
                    case "shiftr":
                        return new KeyInfo(System.Windows.Input.Key.RightShift, true, true);
                    case "menu":
                        return new KeyInfo(System.Windows.Input.Key.Apps, false, true);
                    case "esc":
                    case "escape":
                        return new KeyInfo(System.Windows.Input.Key.Escape, false);
                    case "bksp":
                    case "backspace":
                        return new KeyInfo(System.Windows.Input.Key.Back, false);
                    case "tab":
                        return new KeyInfo(System.Windows.Input.Key.Tab, false);
                    case "windowskey":
                    case "windowskeyl":
                        return new KeyInfo(System.Windows.Input.Key.LWin, false);
                    case "windowskeyr":
                        return new KeyInfo(System.Windows.Input.Key.RWin, false);
                    case "right":
                    case "arrowr":
                    case "arrowright":
                        return new KeyInfo(System.Windows.Input.Key.Right, false);
                    case "left":
                    case "arrowl":
                    case "arrowleft":
                        return new KeyInfo(System.Windows.Input.Key.Left, false);
                    case "up":
                    case "arrowu":
                    case "arrowup":
                        return new KeyInfo(System.Windows.Input.Key.Up, false);
                    case "down":
                    case "arrowd":
                    case "arrowdown":
                        return new KeyInfo(System.Windows.Input.Key.Down, false);
                    case " ":
                        return new KeyInfo(System.Windows.Input.Key.Space, false);
                    case "<":
                        return new KeyInfo(System.Windows.Input.Key.OemComma, true);
                    case ">":
                        return new KeyInfo(System.Windows.Input.Key.OemPeriod, true);
                    case ",":
                        return new KeyInfo(System.Windows.Input.Key.OemComma, false);
                    case ".":
                        return new KeyInfo(System.Windows.Input.Key.OemPeriod, false);
                    case "'":
                        return new KeyInfo(System.Windows.Input.Key.OemQuotes, false);
                    case "\"":
                        return new KeyInfo(System.Windows.Input.Key.OemQuotes, true);
                    case "!":
                        return new KeyInfo(System.Windows.Input.Key.D1, true);
                    case "@":
                        return new KeyInfo(System.Windows.Input.Key.D2, true);
                    case "#":
                        return new KeyInfo(System.Windows.Input.Key.D3, true);
                    case "$":
                        return new KeyInfo(System.Windows.Input.Key.D4, true);
                    case "%":
                        return new KeyInfo(System.Windows.Input.Key.D5, true);
                    case "^":
                        return new KeyInfo(System.Windows.Input.Key.D6, true);
                    case "&":
                        return new KeyInfo(System.Windows.Input.Key.D7, true);
                    case "*":
                        return new KeyInfo(System.Windows.Input.Key.D8, true);
                    case "(":
                        return new KeyInfo(System.Windows.Input.Key.D9, true);
                    case ")":
                        return new KeyInfo(System.Windows.Input.Key.D0, true);
                    case "_":
                        return new KeyInfo(System.Windows.Input.Key.OemMinus, true);
                    case "-":
                        return new KeyInfo(System.Windows.Input.Key.Subtract, false);
                    case "+":
                        return new KeyInfo(System.Windows.Input.Key.Add, true);
                    case "=":
                        return new KeyInfo(System.Windows.Input.Key.OemPlus, false);
                    case "?":
                        return new KeyInfo(System.Windows.Input.Key.OemQuestion, true);
                    case "/":
                        return new KeyInfo(System.Windows.Input.Key.OemQuestion, false);
                    case "|":
                        return new KeyInfo(System.Windows.Input.Key.OemPipe, true);
                    case "\\":
                        return new KeyInfo(System.Windows.Input.Key.OemPipe, false);
                    case "{":
                        return new KeyInfo(System.Windows.Input.Key.OemOpenBrackets, true);
                    case "[":
                        return new KeyInfo(System.Windows.Input.Key.OemOpenBrackets, false);
                    case "}":
                        return new KeyInfo(System.Windows.Input.Key.OemCloseBrackets, true);
                    case "]":
                        return new KeyInfo(System.Windows.Input.Key.OemCloseBrackets, false);
                    case ":":
                        return new KeyInfo(System.Windows.Input.Key.OemSemicolon, true);
                    case ";":
                        return new KeyInfo(System.Windows.Input.Key.OemSemicolon, false);
                    case "~":
                        return new KeyInfo(System.Windows.Input.Key.OemTilde, true);
                    case "`":
                        return new KeyInfo(System.Windows.Input.Key.OemTilde, false);
                    default:
                        bool shift = key.Length == 1 ?
                            Regex.Match(key, @"[A-Z]", RegexOptions.None).Success : false;
                        System.Windows.Input.KeyConverter k = new System.Windows.Input.KeyConverter();
                        System.Windows.Input.Key mykey = (System.Windows.Input.Key)k.ConvertFromString(key);
                        LogMessage(shift);
                        return new KeyInfo(mykey, shift);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            throw new XmlRpcFaultException(123, "Unsupported key type: " + key);
        }
        internal KeyInfo[] GetKeyVal(string data)
        {
            int index = 0;
            string token;
            int maxTokenSize = 15;
            ArrayList keyList = new ArrayList();
            while (index < data.Length)
            {
                token = "";
                if (data[index].ToString().Equals("<"))
                {
                    index++;
                    int i = 0;
                    while (!data[index].ToString().Equals(">") && i < maxTokenSize)
                    {
                        token += data[index++];
                        i++;
                    }
                    if (!data[index].ToString().Equals(">"))
                        // Premature end of string without an opening '<'
                        throw new XmlRpcFaultException(123,
                            "Premature end of string without an opening '<'.");
                    index++;
                }
                else
                {
                    token = data[index++].ToString();
                }
                LogMessage(token);
                keyList.Add(GetKey(token));
            }
            try
            {
                return keyList.ToArray(typeof(KeyInfo))
                    as KeyInfo[];
            }
            finally
            {
                keyList = null;
            }
        }
        public int EnterString(string windowName, string objName = null,
            string data = null)
        {
            if (!String.IsNullOrEmpty(objName))
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
            if (String.IsNullOrEmpty(data))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            KeyInfo[] keys = GetKeyVal(data);
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
                    if (key.key == System.Windows.Input.Key.LeftShift ||
                        key.key == System.Windows.Input.Key.RightShift)
                    {
                        shiftKeyPressed = true;
                    }
                    if (capsLock && key.shift)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            true);
                    }
                    else if (!capsLock && key.shift)
                    {
                        ATGTestInput.Input.SendKeyboardInput(System.Windows.Input.Key.LeftShift,
                            !shiftKeyPressed);
                    }
                    else if (shiftKeyPressed)
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
                            if (tmpKey.key == System.Windows.Input.Key.CapsLock)
                            {
                                // Release only nonPrintKey
                                // Caps lock will be released later
                                break;
                            }
                            if (tmpKey.key == System.Windows.Input.Key.LeftShift ||
                                tmpKey.key == System.Windows.Input.Key.RightShift)
                            {
                                shiftKeyPressed = false;
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
                            shiftKeyPressed);
                    }
                    else if (shiftKeyPressed)
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
                {
                    // Release only nonPrintKey
                    // Caps lock will be released later
                    break;
                }
                if (shiftKeyPressed)
                {
                    if (tmpKey.key == System.Windows.Input.Key.LeftShift ||
                        tmpKey.key == System.Windows.Input.Key.RightShift)
                    {
                        shiftKeyPressed = false;
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
            if (String.IsNullOrEmpty(data))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            KeyInfo[] keys = GetKeyVal(data);
            foreach (KeyInfo key in keys)
            {
                if (key.key == System.Windows.Input.Key.LeftShift ||
                    key.key == System.Windows.Input.Key.RightShift)
                {
                    shiftKeyPressed = true;
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
            if (String.IsNullOrEmpty(data))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            KeyInfo[] keys = GetKeyVal(data);
            foreach (KeyInfo key in keys)
            {
                if (key.key == System.Windows.Input.Key.LeftShift ||
                    key.key == System.Windows.Input.Key.RightShift)
                {
                    shiftKeyPressed = false;
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
