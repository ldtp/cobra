/*
 * Cobra WinLDTP 3.0
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
    class Scrollbar
    {
        Utils utils;
        public Scrollbar(Utils utils)
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
            ControlType[] type = new ControlType[1] { ControlType.ScrollBar };
            try
            {
                return utils.GetObjectHandle(windowName, objName, type);
            }
            finally
            {
                type = null;
            }
        }
        public int OneDown(String windowName, String objName, int iterations)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    for (int i = 0; i < iterations; i++)
                    {
                        double value = ((RangeValuePattern)pattern).Current.Value;
                        // Value < 3 doesn't work, tried 1, 2
                        if ((value + 3) < ((RangeValuePattern)pattern).Current.Maximum)
                            ((RangeValuePattern)pattern).SetValue(value + 3);
                    }
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to one down");
        }
        public int OneUp(String windowName, String objName, int iterations)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    for (int i = 0; i < iterations; i++)
                    {
                        double value = ((RangeValuePattern)pattern).Current.Value;
                        // Value < 3 doesn't work, tried 1, 2
                        if ((value - 3) > ((RangeValuePattern)pattern).Current.Minimum)
                            ((RangeValuePattern)pattern).SetValue(value - 3);
                    }
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to one up");
        }
        public int OneLeft(String windowName, String objName, int iterations)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    for (int i = 0; i < iterations; i++)
                    {
                        double value = ((RangeValuePattern)pattern).Current.Value;
                        // Value < 3 doesn't work, tried 1, 2
                        if ((value - 3) > ((RangeValuePattern)pattern).Current.Minimum)
                            ((RangeValuePattern)pattern).SetValue(value - 3);
                    }
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to one left");
        }
        public int OneRight(String windowName, String objName, int iterations)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    for (int i = 0; i < iterations; i++)
                    {
                        double value = ((RangeValuePattern)pattern).Current.Value;
                        if ((value + 3) < ((RangeValuePattern)pattern).Current.Maximum)
                            // Value < 3 doesn't work, tried 1, 2
                            ((RangeValuePattern)pattern).SetValue(value + 3);
                    }
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to one right");
        }
        public int ScrollDown(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    double value = ((RangeValuePattern)pattern).Current.Maximum;
                    ((RangeValuePattern)pattern).SetValue(value);
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to scroll down");
        }
        public int ScrollUp(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    double value = ((RangeValuePattern)pattern).Current.Minimum;
                    ((RangeValuePattern)pattern).SetValue(value);
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to scroll up");
        }
        public int ScrollLeft(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    double value = ((RangeValuePattern)pattern).Current.Minimum;
                    ((RangeValuePattern)pattern).SetValue(value);
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to scroll left");
        }
        public int ScrollRight(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    // Since we could not identify whether the object is vertical
                    // or horizontal, let us do the same for down and right
                    double value = ((RangeValuePattern)pattern).Current.Maximum;
                    ((RangeValuePattern)pattern).SetValue(value);
                    return 1;
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
            }
            throw new XmlRpcFaultException(123, "Unable to scroll right");
        }
        public int VerifyScrollBar(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName,
                    objName);
                return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return 0;
            }
            finally
            {
                childHandle = null;
            }
        }
        public int VerifyScrollBarHorizontal(String windowName, String objName)
        {
            // Unsupported on Windows, as there is no direct way to determine
            // whether its a vertical or horizontal scroll bar other than the name
            // which is not unique across application
            return 0;
        }
        public int VerifyScrollBarVertical(String windowName, String objName)
        {
            // Unsupported on Windows, as there is no direct way to determine
            // whether its a vertical or horizontal scroll bar other than the name
            // which is not unique across application
            return 0;
        }
    }
}
