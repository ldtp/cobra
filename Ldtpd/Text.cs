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
using System.Collections;
using CookComputing.XmlRpc;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Collections.Generic;

namespace Ldtpd
{
    class Text
    {
        Utils utils;
        public Text(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        public int SetTextValue(String windowName, String objName, String value)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0 ||
                value == null || value.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Edit };
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle,
                objName, type, true);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object valuePattern = null;
            try
            {
                // Reference: http://msdn.microsoft.com/en-us/library/ms750582.aspx
                if (!childHandle.TryGetCurrentPattern(ValuePattern.Pattern,
                    out valuePattern))
                {
                    childHandle.SetFocus();
                    SendKeys.SendWait(value);
                }
                else
                    ((ValuePattern)valuePattern).SetValue(value);
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
                valuePattern = null;
            }
            return 1;
        }
        public String GetTextValue(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Edit };
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle,
                objName, type, false);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            Object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(ValuePattern.Pattern,
                    out pattern))
                {
                    return ((ValuePattern)pattern).Current.Value;
                }
                else if (childHandle.TryGetCurrentPattern(TextPattern.Pattern,
                    out pattern))
                {
                    return ((TextPattern)pattern).DocumentRange.GetText(-1);
                }
                else if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    return ((RangeValuePattern)pattern).Current.Value.ToString(
                        CultureInfo.CurrentCulture);
                }
                else
                {
                    throw new XmlRpcFaultException(123, "Unable to get text");
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
                pattern = null;
                childHandle = null;
            }
        }
        public int AppendText(String windowName,
            String objName, string value)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0 ||
                value == null || value.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            return SetTextValue(windowName, objName,
                existingText + value);
        }
        public int CopyText(String windowName,
            String objName, int start, int end = -1)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            if (start < 0 || end != -1 && (start > end || end < start))
            {
                throw new XmlRpcFaultException(123,
                    "Invalid argument.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            if (end > existingText.Length)
                end = existingText.Length;
            if (start > existingText.Length)
                start = existingText.Length;
            if (end == -1 || end == existingText.Length)
                end = existingText.Length - start;
            else
                end = existingText.Length - end;
            LogMessage(existingText.Substring(start, end));
            Clipboard.SetText(existingText.Substring(start, end));
            return 1;
        }
        public int CutText(String windowName,
            String objName, int start, int end = -1)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            if (start < 0 || end != -1 && (start > end || end < start))
            {
                throw new XmlRpcFaultException(123,
                    "Invalid argument.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            if (end > existingText.Length)
                end = existingText.Length;
            if (start > existingText.Length)
                start = existingText.Length;
            int clipboaredEnd = 0;
            if (end == -1 || end == existingText.Length)
            {
                clipboaredEnd = existingText.Length - start;
                end = existingText.Length;
            }
            else if (end - start >= 0)
            {
                clipboaredEnd = end - start;
            }
            string clipboard = existingText.Substring(start, clipboaredEnd);
            LogMessage(clipboard);
            if (clipboard != "")
                // Copy to clipboard
                Clipboard.SetText(clipboard);
            // Set back remaining text
            string newString = existingText.Substring(0,
                start) + existingText.Substring(end,
                 existingText.Length - end);
            LogMessage(newString);
            return SetTextValue(windowName, objName,
                newString);
        }
        public int DeleteText(String windowName,
            String objName, int start, int end = -1)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            if (start < 0 || end != -1 && (start > end || end < start))
            {
                throw new XmlRpcFaultException(123,
                    "Invalid argument.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            if (end > existingText.Length)
                end = existingText.Length;
            if (start > existingText.Length)
                start = existingText.Length;
            if (end == -1 || end == existingText.Length)
                end = existingText.Length;
            // Set back remaining text
            string newString = existingText.Substring(0,
                start) + existingText.Substring(end,
                existingText.Length - end);
            return SetTextValue(windowName, objName,
                newString);
        }
        public int GetCharCount(String windowName,
            String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            return existingText.Length;
        }
        public int InsertText(String windowName,
            String objName, int postion, string value)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0 ||
                value == null || value.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            if (postion < 0)
                postion = 0;
            if (postion >= existingText.Length)
            {
                postion = existingText.Length;
            }
            LogMessage(postion + " : " + existingText.Length);
            // Set new text
            string newString = existingText.Substring(0, postion) +
                value + existingText.Substring(postion,
                existingText.Length - postion);
            return SetTextValue(windowName, objName,
                newString);
        }
        public int IsTextStateEnabled(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                return 0;
            }
            AutomationElement windowHandle, childHandle;
            try
            {
                windowHandle = utils.GetWindowHandle(windowName);
                if (windowHandle == null)
                {
                    return 0;
                }
                ControlType[] type = new ControlType[1] { ControlType.Edit };
                childHandle = utils.GetObjectHandle(windowHandle,
                    objName, type, false);
                windowHandle = null;
                if (childHandle == null)
                {
                    return 0;
                }
                if (utils.IsEnabled(childHandle, false))
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                childHandle = windowHandle = null;
            }
            return 0;
        }
        public int PasteText(String windowName,
            String objName, int postion)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            if (postion < 0)
                postion = 0;
            if (postion >= existingText.Length)
            {
                postion = existingText.Length;
            }
            LogMessage(postion + " : " + existingText.Length);
            // Copy text from clipboard
            string value = Clipboard.GetText();
            // Set new text
            string newString = existingText.Substring(0, postion) +
                value + existingText.Substring(postion,
                existingText.Length - postion);
            return SetTextValue(windowName, objName,
                newString);
        }
        public int VerifySetText(String windowName,
            String objName, string value)
        {
            if (value == null || value.Length == 0)
                return 0;
            try
            {
                string existingText = GetTextValue(windowName,
                    objName);
                if (existingText.Equals(value))
                    return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        public int VerifyPartialText(String windowName,
            String objName, string value)
        {
            if (value == null || value.Length == 0)
                return 0;
            try
            {
                string existingText = GetTextValue(windowName,
                    objName);
                if (existingText.Contains(value))
                    return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
    }
}
