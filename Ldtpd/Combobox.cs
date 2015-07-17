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
using System.Windows.Forms;
using System.Windows.Automation;

namespace Ldtpd
{
    class Combobox
    {
        Utils utils;
        public Combobox(Utils utils)
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
            ControlType[] type = new ControlType[3] { ControlType.ComboBox,
                ControlType.ListItem, ControlType.List/*, ControlType.Text */ };
            try
            {
                return utils.GetObjectHandle(windowName, objName, type);
            }
            finally
            {
                type = null;
            }
        }
        private bool SelectListItem(AutomationElement element, String itemText,
            bool verify = false)
        {
            if (element == null || String.IsNullOrEmpty(itemText))
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be null or empty.");
            }
            LogMessage("SelectListItem Element: " + element.Current.Name +
                " - Type: " + element.Current.ControlType.ProgrammaticName);
            Object pattern = null;
            AutomationElement elementItem;
            try
            {
                utils.InternalWait(1);
                elementItem = utils.GetObjectHandle(element, itemText);
                if (elementItem != null)
                {
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    if (verify)
                    {
                        bool status = false;
                        if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                            out pattern))
                        {
                            status = ((SelectionItemPattern)pattern).Current.IsSelected;
                        }
                        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                            out pattern))
                        {
                            LogMessage("ExpandCollapsePattern");
                            element.SetFocus();
                            ((ExpandCollapsePattern)pattern).Collapse();
                        }
                        return status;
                    }
                    if (elementItem.TryGetCurrentPattern(ScrollItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("ScrollItemPattern");
                        ((ScrollItemPattern)pattern).ScrollIntoView();
                    }
                    if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        return utils.InternalClick(elementItem);
                    }
                    else if (elementItem.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                        out pattern))
                    {
                        LogMessage("ExpandCollapsePattern");
                        ((ExpandCollapsePattern)pattern).Expand();
                        element.SetFocus();
                        return true;
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
                elementItem = null;
            }
            throw new XmlRpcFaultException(123,
                "Unable to find item in the list: " + itemText);
        }
        private int InternalComboHandler(String windowName, String objName,
            String item, ref String selectedItem, String actionType = "Select",
            ArrayList childList = null)
        {
            bool verify = actionType == "Verify" ? true : false;
            ControlType[] comboTtype = new ControlType[3] { ControlType.ComboBox,
                ControlType.ListItem, ControlType.List/*, ControlType.Text */ };
            AutomationElement childHandle = utils.GetObjectHandle(windowName,
                objName, comboTtype, !verify);
            Object pattern = null;
            Object invokePattern = null;
            AutomationElement elementItem = null;
            ControlType[]  type = new ControlType[1] { ControlType.Button };
            try
            {
                LogMessage("Handle name: " + childHandle.Current.Name +
                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                if (!utils.IsEnabled(childHandle, !verify))
                {
                    throw new XmlRpcFaultException(123, "Object state is disabled");
                }
                elementItem = utils.GetObjectHandle(childHandle, "Open", type, true);
                LogMessage("elementItem: " + elementItem.Current.Name +
                                    " - " + elementItem.Current.ControlType.ProgrammaticName);
                if (childHandle.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                    out pattern) || childHandle.TryGetCurrentPattern(
                    InvokePattern.Pattern, out invokePattern) ||
                    (elementItem != null && elementItem.TryGetCurrentPattern(
                    InvokePattern.Pattern, out invokePattern)))
                {
                    LogMessage("ExpandCollapsePattern");
                    // Retry max 5 times
                    for (int i = 0; i < 5; i++)
                    {
                        switch (actionType)
                        {
                            case "Hide":
                                if (invokePattern != null)
                                {
                                    ((InvokePattern)invokePattern).Invoke();
                                    return 1;
                                }
                                else if (pattern != null)
                                {
                                    ((ExpandCollapsePattern)pattern).Collapse();
                                    // Required to wait 1 second,
                                    // before checking the state and retry collapsing
                                    utils.InternalWait(1);
                                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                                        ExpandCollapseState.Collapsed)
                                    {
                                        // Hiding same combobox multiple time consecutively
                                        // fails. Check for the state and retry to collapse
                                        LogMessage("Collapsed");
                                        return 1;
                                    }
                                }
                                break;
                            case "Show":
                            case "Select":
                            case "Verify":
                                elementItem = utils.GetObjectHandle(childHandle, "Open",
                                    type, !verify);
                                if (invokePattern != null || (elementItem != null &&
                                    elementItem.TryGetCurrentPattern(InvokePattern.Pattern,
                                    out invokePattern)))
                                {
                                    ((InvokePattern)invokePattern).Invoke();
                                }
                                else if (pattern != null)
                                    ((ExpandCollapsePattern)pattern).Expand();
                                // Required to wait 1 second,
                                // before checking the state and retry expanding
                                utils.InternalWait(1);
                                if (invokePattern != null || (pattern != null &&
                                    ((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                                    ExpandCollapseState.Expanded))
                                {
                                    // Selecting same combobox multiple time consecutively
                                    // fails. Check for the state and retry to expand
                                    LogMessage("Expaneded");
                                    if (actionType == "Show")
                                        return 1;
                                    else
                                    {
                                        return SelectListItem(childHandle, item, verify) ? 1 : 0;
                                    }
                                }
                                break;
                            case "GetComboValue":
                                Object selectionPattern = null;
                                LogMessage("GetComboValue");
                                elementItem = utils.GetObjectHandle(childHandle, "Open",
                                    type, true);
                                if (invokePattern != null || (elementItem != null &&
                                    elementItem.TryGetCurrentPattern(InvokePattern.Pattern,
                                    out invokePattern)))
                                {
                                    LogMessage("InvokePattern");
                                    childHandle.SetFocus();
                                    utils.InternalClick(elementItem);
                                    // InvokePattern doesn't work with Virtual Network
                                    // Editor of VMware Workstation, so used the above InternalClick
                                    //((InvokePattern)invokePattern).Invoke();
                                }
                                else if (pattern != null)
                                {
                                    LogMessage("ExpandCollapsePattern");
                                    ((ExpandCollapsePattern)pattern).Expand();
                                }
                                // Required to wait 1 second,
                                // before checking the state and retry expanding
                                utils.InternalWait(1);
                                LogMessage("Handle name: " + childHandle.Current.Name +
                                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                                bool typeExist = utils.InternalWaitTillChildControlTypeExist(childHandle, type);
                                LogMessage("Control type exist: " + typeExist);
                                AutomationElementCollection c = childHandle.FindAll(TreeScope.Subtree,
                                    Condition.TrueCondition);
                                LogMessage("AutomationElementCollection " + c.Count);
                                foreach (AutomationElement e in c)
                                {
                                    LogMessage(e.Current.Name + " : " + e.Current.ControlType.ProgrammaticName);
                                    bool status = false;
                                    if (e.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                                        out selectionPattern))
                                    {
                                        status = ((SelectionItemPattern)selectionPattern).Current.IsSelected;
                                        if (status)
                                        {
                                            LogMessage("Selected: " + e.Current.Name);
                                            selectedItem = e.Current.Name;
                                            ((ExpandCollapsePattern)pattern).Collapse();
                                            return 1;
                                        }
                                    }
                                }
                                LogMessage("Unable to find selected combo box value");
                                c = null;
                                selectionPattern = null;
                                if (invokePattern != null)
                                {
                                    ((InvokePattern)invokePattern).Invoke();
                                }
                                else if (pattern != null)
                                    ((ExpandCollapsePattern)pattern).Collapse();
                                return 0;
                            case "GetAllItem":
                                string matchedKey = null;
                                Hashtable objectHT = new Hashtable();
                                ArrayList tmpChildList = new ArrayList();
                                InternalTreeWalker w = new InternalTreeWalker();
                                elementItem = utils.GetObjectHandle(childHandle, "Open",
                                    type, true);
                                // Changes based on QT 5.0.2
                                if (invokePattern != null || (elementItem != null &&
                                    elementItem.TryGetCurrentPattern(InvokePattern.Pattern,
                                    out invokePattern)))
                                {
                                    ((InvokePattern)invokePattern).Invoke();
                                }
                                else if (pattern != null)
                                    ((ExpandCollapsePattern)pattern).Expand();
                                // Required to wait 1 second,
                                // before checking the state and retry expanding
                                utils.InternalWait(1);
                                utils.InternalGetObjectList(
                                    w.walker.GetFirstChild(childHandle),
                                    ref tmpChildList, ref objectHT, ref matchedKey,
                                    true, null, null, ControlType.ListItem);
                                if (invokePattern != null)
                                {
                                    ((InvokePattern)invokePattern).Invoke();
                                }
                                else if (pattern != null)
                                    ((ExpandCollapsePattern)pattern).Collapse();
                                // For Linux compatibility
                                Hashtable propertyHT;
                                foreach (String key in objectHT.Keys)
                                {
                                    propertyHT = (Hashtable)objectHT[key];
                                    string className = (string)propertyHT["class"];
                                    if (className != null &&
                                        className.Contains("list_item"))
                                        // Add only list items
                                        childList.Add(propertyHT["label"]);
                                }
                                w = null;
                                tmpChildList = null;
                                propertyHT = objectHT = null;
                                if (childList.Count > 0)
                                {
                                    // Don't process the last item
                                    return 1;
                                }
                                else
                                {
                                    LogMessage("childList.Count <= 0: " + childList.Count);
                                }
                                return 0;
                        }
                    }
                }
                // Handle selectitem and verifyselect on list.
                // Get ExpandCollapsePattern fails on list,
                // VM Library items are selected and
                // verified correctly on Player with this fix
                else
                {
                    LogMessage("SelectListItem");
                    childHandle.SetFocus();
                    return SelectListItem(childHandle, item, verify) ? 1 : 0;
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
                comboTtype = type = null;
                pattern = invokePattern = null;
                elementItem = childHandle = null;
            }
            return 0;
        }
        public int SelectIndex(String windowName, String objName, int index)
        {
            if (index == 0)
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            AutomationElement childHandle = GetObjectHandle(windowName,
                objName);
            LogMessage("Handle name: " + childHandle.Current.Name +
                " - " + childHandle.Current.ControlType.ProgrammaticName);
            if (!utils.IsEnabled(childHandle))
            {
                childHandle = null;
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            Object pattern;
            if (childHandle.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                    out pattern))
            {
                ((ExpandCollapsePattern)pattern).Expand();
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
            }
            if (element != null)
            {
                try
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
                }
            }
            throw new XmlRpcFaultException(123,
                "Unable to select item.");
        }
        public string[] GetAllItem(String windowName, String objName)
        {
            String selectedItem = null;
            ArrayList childList = new ArrayList();
            InternalComboHandler(windowName, objName, null, ref selectedItem,
                "GetAllItem", childList);
            return childList.ToArray(typeof(string)) as string[];
        }
        public int SelectItem(String windowName, String objName, String item)
        {
            return ComboSelect(windowName, objName, item);
        }
        public String GetComboValue(String windowName, String objName)
        {
            String selectedItem = null;
            if (InternalComboHandler(windowName, objName, null,
                ref selectedItem, "GetComboValue") == 1)
            {
                LogMessage("Item selected: " + selectedItem);
                return selectedItem;
            }
            else
                throw new XmlRpcFaultException(123, "Unable to get combobox value");
        }
        public int ShowList(String windowName, String objName)
        {
            String selectedItem = null;
            return InternalComboHandler(windowName, objName,
                null, ref selectedItem, "Show");
        }
        public int HideList(String windowName, String objName)
        {
            String selectedItem = null;
            return InternalComboHandler(windowName, objName, null,
                ref selectedItem, "Hide");
        }
        public int ComboSelect(String windowName, String objName, String item)
        {
            String selectedItem = null;
            return InternalComboHandler(windowName, objName, item,
                ref selectedItem, "Select");
        }
        public int VerifyDropDown(String windowName, String objName)
        {
            Object pattern = null;
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName,
                    objName);
                LogMessage("Handle name: " + childHandle.Current.Name +
                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                if (!utils.IsEnabled(childHandle))
                {
                    LogMessage("Object state is disabled");
                    return 0;
                }
                if (childHandle.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                    out pattern))
                {
                    LogMessage("ExpandCollapsePattern");
                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                        ExpandCollapseState.Expanded)
                    {
                        LogMessage("Expaneded");
                        return 1;
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
                childHandle = null;
            }
            return 0;
        }
        public int VerifyShowList(String windowName, String objName)
        {
            return VerifyDropDown(windowName, objName);
        }
        public int VerifyHideList(String windowName, String objName)
        {
            Object pattern = null;
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName,
                    objName);
                LogMessage("Handle name: " + childHandle.Current.Name +
                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                if (!utils.IsEnabled(childHandle))
                {
                    LogMessage("Object state is disabled");
                    return 0;
                }
                if (childHandle.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                    out pattern))
                {
                    LogMessage("ExpandCollapsePattern");
                    if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                        ExpandCollapseState.Collapsed)
                    {
                        LogMessage("Collapsed");
                        return 1;
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
                childHandle = null;
            }
            return 0;
        }
        public int VerifyComboSelect(String windowName, String objName, String item)
        {
            try
            {
                String selectedItem = null;
                return InternalComboHandler(windowName, objName, item,
                    ref selectedItem, "Verify");
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return 0;
            }
        }
    }
}
