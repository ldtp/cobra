/*
 * WinLDTP 1.0
 * 
 * Author: Nagappan Alagappan <nalagappan@vmware.com>
 * Copyright: Copyright (c) 2011-12 VMware, Inc. All Rights Reserved.
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
using System.Windows.Forms;
using System.Windows.Automation;

namespace Ldtpd
{
    class Tree
    {
        Utils utils;
        public Tree(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        private AutomationElement GetObjectHandle(string windowName,
            string objName, ControlType[] type = null, bool waitForObj = true)
        {
            if (type == null)
                type = new ControlType[2] { ControlType.Tree,
                    ControlType.List };
            try
            {
                return utils.GetObjectHandle(windowName,
                    objName, type, waitForObj);
            }
            finally
            {
                type = null;
            }
        }
        public int DoesRowExist(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            if (String.IsNullOrEmpty(text))
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            ControlType[] type;
            AutomationElement childHandle;
            AutomationElement elementItem;
            try
            {
                childHandle = GetObjectHandle(windowName,
                    objName, null, false);
                if (!utils.IsEnabled(childHandle))
                {
                    childHandle = null;
                    LogMessage("Object state is disabled");
                    return 0;
                }
                childHandle.SetFocus();
                type = new ControlType[2] { ControlType.TreeItem,
                    ControlType.ListItem };
                if (partialMatch)
                    text += "*";
                elementItem = utils.GetObjectHandle(childHandle,
                    text, type, false);
                if (elementItem != null)
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
                type = null;
                childHandle = elementItem = null;
            }
            return 0;
        }
        public int SelectRow(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            if (String.IsNullOrEmpty(text))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            Object pattern;
            ControlType[] type;
            AutomationElement elementItem;
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            try
            {
                childHandle.SetFocus();
                if (partialMatch)
                    text += "*";
                type = new ControlType[2] { ControlType.TreeItem,
                    ControlType.ListItem };
                elementItem = utils.GetObjectHandle(childHandle,
                    text, type, true);
                if (elementItem != null)
                {
                    elementItem.SetFocus();
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    if (elementItem.TryGetCurrentPattern(
                        SelectionItemPattern.Pattern, out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        utils.InternalClick(elementItem);
                        return 1;
                    }
                    else if (elementItem.TryGetCurrentPattern(
                        ExpandCollapsePattern.Pattern, out pattern))
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
                type = null;
                pattern = null;
                elementItem = childHandle = null;
            }
            throw new XmlRpcFaultException(123,
                "Unable to find the item in list: " + text);
        }
        public int SelectRowPartialMatch(String windowName, String objName,
            String text)
        {
            return SelectRow(windowName, objName, text, true);
        }
        public int SelectRowIndex(String windowName,
            String objName, int index)
        {
            Object pattern;
            AutomationElement element = null;
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            Condition prop1 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.ListItem);
            Condition prop2 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.TreeItem);
            Condition condition = new OrCondition(prop1, prop2);
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    condition);
                try
                {
                    element = c[index];
                    element.SetFocus();
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
                pattern = null;
                element = childHandle = null;
                prop1 = prop2 = condition = null;
            }
            throw new XmlRpcFaultException(123, "Unable to select item.");
        }
        public int ExpandTableCell(String windowName,
            String objName, int index)
        {
            Object pattern;
            AutomationElement element = null;
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            Condition prop1 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.ListItem);
            Condition prop2 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.TreeItem);
            Condition condition = new OrCondition(prop1, prop2);
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    condition);
                try
                {
                    element = c[index];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new XmlRpcFaultException(123, "Index out of range: " + index);
                }
                catch (ArgumentException)
                {
                    throw new XmlRpcFaultException(123, "Index out of range: " + index);
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                    throw new XmlRpcFaultException(123, "Index out of range: " + index);
                }
                finally
                {
                    c = null;
                    childHandle = null;
                    prop1 = prop2 = condition = null;
                }
                if (element != null)
                {
                    LogMessage(element.Current.Name + " : " +
                        element.Current.ControlType.ProgrammaticName);
                    if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                        out pattern))
                    {
                        LogMessage("ExpandCollapsePattern");
                        if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                            ExpandCollapseState.Expanded)
                            ((ExpandCollapsePattern)pattern).Collapse();
                        else if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                            ExpandCollapseState.Collapsed)
                            ((ExpandCollapsePattern)pattern).Expand();
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
            throw new XmlRpcFaultException(123, "Unable to expand item.");
        }
        public String GetCellValue(String windowName,
            String objName, int row, int column = 0)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            AutomationElement element = null;
            Condition prop1 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.ListItem);
            Condition prop2 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.TreeItem);
            Condition condition1 = new OrCondition(prop1, prop2);
            Condition condition2 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.Text);
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(
                    TreeScope.Children, condition1);
                element = c[row];
                c = element.FindAll(TreeScope.Children, condition2);
                element = c[column];
                c = null;
                if (element != null)
                    return element.Current.Name;
            }
            catch (IndexOutOfRangeException)
            {
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + "(" + row + ", " + column + ")");
            }
            catch (ArgumentException)
            {
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + "(" + row + ", " + column + ")");
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + "(" + row + ", " + column + ")");
            }
            finally
            {
                element = childHandle = null;
                prop1 = prop2 = condition1 = condition2 = null;
            }
            throw new XmlRpcFaultException(123,
                "Unable to get item value.");
        }
        public int GetRowCount(String windowName, String objName)
        {
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            AutomationElementCollection c;
            Condition prop1 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.ListItem);
            Condition prop2 = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.TreeItem);
            Condition condition = new OrCondition(prop1, prop2);
            try
            {
                childHandle.SetFocus();
                c = childHandle.FindAll(TreeScope.Children, condition);
                if (c == null)
                    throw new XmlRpcFaultException(123,
                        "Unable to get row count.");
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
                c = null;
                childHandle = null;
                prop1 = prop2 = condition = null;
            }
        }
    }
}
