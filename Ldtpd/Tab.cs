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
using System.Windows;
using System.Collections;
using CookComputing.XmlRpc;
using System.Windows.Forms;
using System.Windows.Automation;

namespace Ldtpd
{
    class Tab
    {
        Utils utils;
        public Tab(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        public int SelectTab(String windowName,
            String objName, String tabName)
        {
            if (windowName == null || objName == null || windowName.Length == 0 ||
                objName.Length == 0 || tabName == null || tabName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement elementItem;
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
            Object pattern;
            try
            {
                childHandle.SetFocus();
                elementItem = utils.GetObjectHandle(childHandle,
                    tabName);
                if (elementItem != null)
                {
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        utils.InternalClick(elementItem);
                        return 1;
                    }
                    else if (elementItem.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                        out pattern))
                    {
                        LogMessage("ExpandCollapsePattern");
                        ((ExpandCollapsePattern)pattern).Expand();
                        return 1;
                    }
                    else
                    {
                        throw new XmlRpcFaultException(123,
                            "Unsupported pattern.");
                    }
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
            throw new XmlRpcFaultException(123,
                "Unable to find the item in tab list: " + tabName);
        }
        public int SelectTabIndex(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
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
            childHandle.SetFocus();
            AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                Condition.TrueCondition);
            childHandle = null;
            AutomationElement element = null;
            try
            {
                element = c[index];
            }
            catch (IndexOutOfRangeException)
            {
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            }
            catch (ArgumentException)
            {
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                throw new XmlRpcFaultException(123, "Index out of range: " + index);
            }
            finally
            {
                c = null;
            }
            Object pattern;
            try
            {
                if (element != null)
                {
                    LogMessage(element.Current.Name + " : " +
                        element.Current.ControlType.ProgrammaticName);
                    if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        element.SetFocus();
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        utils.InternalClick(element);
                        return 1;
                    }
                    else if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                        out pattern))
                    {
                        LogMessage("ExpandCollapsePattern");
                        ((ExpandCollapsePattern)pattern).Expand();
                        element.SetFocus();
                        return 1;
                    }
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
                element = null;
                pattern = null;
            }
            throw new XmlRpcFaultException(123, "Unable to select item.");
        }
        public String GetTabName(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
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
            childHandle.SetFocus();
            AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                Condition.TrueCondition);
            childHandle = null;
            AutomationElement element = null;
            try
            {
                element = c[index];
            }
            catch (IndexOutOfRangeException)
            {
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            }
            catch (ArgumentException)
            {
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            }
            finally
            {
                c = null;
            }
            if (element != null)
            {
                string s = element.Current.Name;
                element = null;
                return s;
            }
            throw new XmlRpcFaultException(123,
                "Unable to find item.");
        }
        public int GetTabCount(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
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
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                return c.Count;
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
        public int VerifyTabName(String windowName,
            String objName, String tabName)
        {
            if (windowName == null || objName == null || windowName.Length == 0 ||
                objName.Length == 0 || tabName == null || tabName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle,
                objName, type, true);
            windowHandle = null;
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                LogMessage("Object state is disabled");
                return 0;
            }
            Object pattern;
            AutomationElement elementItem;
            try
            {
                childHandle.SetFocus();
                elementItem = utils.GetObjectHandle(childHandle, tabName);
                if (elementItem != null)
                {
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        return ((SelectionItemPattern)pattern).Current.IsSelected ? 1 : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                pattern = null;
                childHandle = elementItem = null;
            }
            LogMessage("Unable to find the item in tab list: " + tabName);
            return 0;
        }
    }
}
