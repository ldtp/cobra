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
using System.Collections;
using CookComputing.XmlRpc;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Collections.Generic;

namespace Ldtpd
{
    class Value
    {
        Utils utils;
        public Value(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
            LogMessage(o);
        }
        private AutomationElement GetObjectHandle(string windowName,
            string objName)
        {
            ControlType[] type = new ControlType[2] { ControlType.Slider,
                ControlType.Spinner };
            try
            {
                return utils.GetObjectHandle(windowName, objName, type);
            }
            finally
            {
                type = null;
            }
        }
        public int SetValue(String windowName,
            String objName, double value)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            object pattern = null;
            try
            {
                if (!utils.IsEnabled(childHandle))
                {
                    throw new XmlRpcFaultException(123,
                        "Object state is disabled");
                }
                childHandle.SetFocus();
                if (childHandle.TryGetCurrentPattern(LegacyIAccessiblePattern.Pattern,
                    out pattern))
                {
/*                    
                    if (((LegacyIAccessiblePattern)pattern).Current.IsReadOnly)
                    {
                        throw new XmlRpcFaultException(123,
                            "Control is read-only.");
                    }
*/
                    ((LegacyIAccessiblePattern)pattern).SetValue(Convert.ToString(value, CultureInfo.InvariantCulture));
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
                childHandle = null;
            }
            throw new XmlRpcFaultException(123, "Unable to set value");
        }
        public double GetValue(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            Object pattern = null;
            try
            {
                childHandle.SetFocus();
                if (childHandle.TryGetCurrentPattern(LegacyIAccessiblePattern.Pattern,
                    out pattern))
                {
/*                    
                    if (((LegacyIAccessiblePattern)pattern).Current.IsReadOnly)
                    {
                        throw new XmlRpcFaultException(123,
                            "Control is read-only.");
                    }
*/
                    return Convert.ToDouble(((LegacyIAccessiblePattern)pattern).Current.Value, CultureInfo.InvariantCulture);
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
            throw new XmlRpcFaultException(123, "Unable to get value");
        }
        public int VerifySetValue(String windowName, String objName, double value)
        {
            try
            {
                double v = GetValue(windowName, objName);
                if (v == value)
                    return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        public double GetMinValue(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            Object pattern = null;
            try
            {
                childHandle.SetFocus();
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    if (((RangeValuePattern)pattern).Current.IsReadOnly)
                    {
                        throw new XmlRpcFaultException(123,
                            "Control is read-only.");
                    }
                    return ((RangeValuePattern)pattern).Current.Minimum;
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
            throw new XmlRpcFaultException(123, "Unable to get value");
        }
        public double GetMaxValue(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            Object pattern = null;
            try
            {
                childHandle.SetFocus();
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    if (((RangeValuePattern)pattern).Current.IsReadOnly)
                    {
                        throw new XmlRpcFaultException(123,
                            "Control is read-only.");
                    }
                    return ((RangeValuePattern)pattern).Current.Maximum;
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
            throw new XmlRpcFaultException(123, "Unable to get value");
        }
        public int VerifySliderHorizontal(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName, objName);
                OrientationType orientationType = (OrientationType)
                    childHandle.GetCurrentPropertyValue(
                    AutomationElement.OrientationProperty);
                if (orientationType == OrientationType.Horizontal)
                    return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                childHandle = null;
            }
            return 0;
        }
        public int VerifySliderVertical(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName, objName);
                OrientationType orientationType = (OrientationType)
                    childHandle.GetCurrentPropertyValue(
                    AutomationElement.OrientationProperty);
                if (orientationType == OrientationType.Vertical)
                    return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                childHandle = null;
            }
            return 0;
        }
        public double GetMinIncrement(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            Object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out pattern))
                {
                    if (((RangeValuePattern)pattern).Current.IsReadOnly)
                    {
                        throw new XmlRpcFaultException(123,
                            "Control is read-only.");
                    }
                    return ((RangeValuePattern)pattern).Current.SmallChange;
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
            throw new XmlRpcFaultException(123, "Unable to get value");
        }
        public int Increase(string windowName, string objName, int iterations)
        {
            double max = Double.MaxValue;//GetMaxValue(windowName, objName);
            double value = GetValue(windowName, objName);
            bool flag = false;
            for (int i = 0; i < iterations; i++)
            {
                if (value < max)
                {
                    flag = true;
                    value += 1.0;
                    SetValue(windowName, objName, value);
                }
                else
                    break;
            }
            if (flag)
                return 1;
            return 0;
        }
        public int Decrease(string windowName, string objName, int iterations)
        {
            double min = Double.MinValue;//GetMinValue(windowName, objName);
            double value = GetValue(windowName, objName);
            bool flag = false;
            for (int i = 0; i < iterations; i++)
            {
                if (min < value)
                {
                    flag = true;
                    value -= 1.0;
                    SetValue(windowName, objName, value);
                }
                else
                    break;
            }
            if (flag)
                return 1;
            return 0;
        }
        public int SetMin(string windowName, string objName)
        {
            double min = GetMinValue(windowName, objName);
            return SetValue(windowName, objName, min);
        }
        public int SetMax(string windowName, string objName)
        {
            double max = GetMaxValue(windowName, objName);
            return SetValue(windowName, objName, max);
        }
    }
}
