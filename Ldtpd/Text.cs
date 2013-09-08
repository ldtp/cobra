/*
 * Cobra WinLDTP 3.5
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
using System.Threading;
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
        private AutomationElement GetObjectHandle(string windowName,
            string objName)
        {
            // Pane added for a bug in QT
            // Ref: https://cobra.codeplex.com/discussions/450296
            ControlType[] type = new ControlType[4] { ControlType.Edit,
                ControlType.Document, ControlType.ComboBox, ControlType.Pane };
            try
            {
                return utils.GetObjectHandle(windowName, objName, type);
            }
            finally
            {
                type = null;
            }
        }
        public int SetTextValue(String windowName, String objName, String value)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object valuePattern = null;
            try
            {
                if (childHandle.Current.ControlType == ControlType.ComboBox)
                {
                    AutomationElement o = null;
                    ArrayList objectList = new ArrayList();
                    ControlType[] type = new ControlType[1] { ControlType.Edit };
                    // NOTE: Using "*" for object name, which returns the first
                    // matching Edit control type
                    o = utils.InternalGetObjectHandle(childHandle,
                        "*", type, ref objectList);
                    if (o != null)
                        childHandle = o;
                    objectList = null;
                }
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
        public String GetTextValue(String windowName, String objName,
            int startPos = 0, int endPos = 0)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            String data = null;
            Object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(ValuePattern.Pattern,
                    out pattern))
                {
                    data = ((ValuePattern)pattern).Current.Value;
                }
                else if (childHandle.TryGetCurrentPattern(TextPattern.Pattern,
                    out pattern))
                {
                    data = ((TextPattern)pattern).DocumentRange.GetText(-1);
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
                if (startPos < 0)
                    startPos = 0;
                if (startPos > data.Length)
                    startPos = data.Length;
                if (endPos == 0 || endPos < startPos || endPos > data.Length)
                    endPos = data.Length;
                return data.Substring(startPos, endPos - startPos);
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
                data = null;
                pattern = null;
                childHandle = null;
            }
        }
        public int AppendText(String windowName,
            String objName, string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            string existingText = GetTextValue(windowName,
                objName);
            return SetTextValue(windowName, objName,
                existingText + value);
        }
        protected Thread clipboardThread = null;
        private void CopyToClipboard(object textData)
        {
            Clipboard.Clear();
            LogMessage(textData);
            Clipboard.SetText((string)textData);
        }
        public int CopyText(String windowName,
            String objName, int start, int end = -1)
        {
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
            string textData = existingText.Substring(start, end);
            // To workaround the exception
            // Current thread must be set to single thread apartment (STA)
            // mode before OLE calls can be made. Ensure that your Main
            // function has STAThreadAttribute marked on it.
            if (clipboardThread == null)
            {
                clipboardThread = new Thread(CopyToClipboard);
                clipboardThread.SetApartmentState(ApartmentState.STA);
                clipboardThread.IsBackground = false;
            }
            if (!clipboardThread.IsAlive)
            {
                clipboardThread.Start(textData);
            }
            clipboardThread.Join();
            return 1;
        }
        public int CutText(String windowName,
            String objName, int start, int end = -1)
        {
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
            string existingText = GetTextValue(windowName,
                objName);
            return existingText.Length;
        }
        public int InsertText(String windowName,
            String objName, int postion, string value)
        {
            if (String.IsNullOrEmpty(value))
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
            Object pattern;
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName,
                    objName);
                if (childHandle.TryGetCurrentPattern(ValuePattern.Pattern,
                    out pattern))
                {
                    if (((ValuePattern)pattern).Current.IsReadOnly)
                        return 0;
                    else
                        return 1;
                }
                else
                {
                    // Fallback to object state enabled
                    // if value pattern is not available
                    return utils.IsEnabled(childHandle, false) ? 1 : 0;
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                pattern = null;
                childHandle = null;
            }
            return 0;
        }
        public int PasteText(String windowName,
            String objName, int postion)
        {
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
            if (String.IsNullOrEmpty(value))
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
            if (String.IsNullOrEmpty(value))
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
