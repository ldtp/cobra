/*
 * Cobra WinLDTP 4.0
 * 
 * Author: Nagappan Alagappan <nalagappan@vmware.com>
 * Author: John Yingjun Li <yjli@vmware.com>
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
using System.Collections;
using CookComputing.XmlRpc;
using System.Windows.Automation;
using System.Collections.Generic;

namespace Ldtpd
{
    class Menu
    {
        int processId = 0;
        Utils utils;
        public Menu(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        private int IsMenuChecked(AutomationElement menuHandle)
        {
            if (menuHandle == null)
            {
                LogMessage("Invalid menu handle");
                return 0;
            }
            Object pattern = null;
            if (menuHandle.TryGetCurrentPattern(LegacyIAccessiblePattern.Pattern,
                                        out pattern))
            {
                int isChecked;
                uint state = ((LegacyIAccessiblePattern)pattern).Current.State;
                // Use fifth bit of current state to determine menu item is checked or not checked
                isChecked = (state & 16) == 16 ? 1 : 0;
                LogMessage("IsMenuChecked: " + menuHandle.Current.Name + " : " + "Checked: " +
                    isChecked + " : " + "Current State: " + state);
                pattern = null;
                return isChecked;
            }
            else
                LogMessage("Unable to get LegacyIAccessiblePattern");
            return 0;
        }
        private int HandleSubMenu(AutomationElement childHandle,
            AutomationElement firstObjHandle, ref ArrayList menuList)
        {
            if (childHandle == null || firstObjHandle == null)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be null.");
            }
            string matchedKey = null;
            Hashtable objectHT = new Hashtable();
            try
            {
                menuList.Clear();
                utils.InternalGetObjectList(childHandle, ref menuList,
                    ref objectHT, ref matchedKey);
                if (menuList.Count > 0)
                {
                    // Set it back to old state,
                    // else the menu selection left there
                    utils.InternalClick(firstObjHandle);
                    // Don't process the last item
                    return 1;
                }
                else
                    LogMessage("menuList.Count <= 0: " + menuList.Count);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                objectHT = null;
            }
            return 0;
        }
        private int InternalMenuHandler(String windowName, String objName,
            ref ArrayList menuList, String actionType = "Select")
        {
            if (String.IsNullOrEmpty(windowName) ||
                String.IsNullOrEmpty(objName))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            Object pattern = null;
            String currObjName = null;
            AutomationElementCollection c = null;
            ControlType[] type = new ControlType[3] { ControlType.Menu,
                ControlType.MenuBar, ControlType.MenuItem };
            ControlType[] controlType = new ControlType[3] { ControlType.Menu,
                ControlType.MenuItem, ControlType.MenuBar };
            AutomationElement tmpContextHandle = null;
            AutomationElement windowHandle, childHandle;
            AutomationElement prevObjHandle = null, firstObjHandle = null;

            InternalTreeWalker w = new InternalTreeWalker();
            try
            {
                windowHandle = utils.GetWindowHandle(windowName);
                if (windowHandle == null)
                {
                    throw new XmlRpcFaultException(123,
                        "Unable to find window: " + windowName);
                }
                processId = windowHandle.Current.ProcessId;
                windowHandle.SetFocus();
                LogMessage("Window name: " + windowHandle + " : " +
                    windowHandle.Current.Name +
                    " : " + windowHandle.Current.ControlType.ProgrammaticName);
                childHandle = windowHandle;
                /*
                // element is an AutomationElement.
                AutomationPattern[] patterns = childHandle.GetSupportedPatterns();
                foreach (AutomationPattern pattern1 in patterns)
                {
                    Console.WriteLine("ProgrammaticName: " + pattern1.ProgrammaticName);
                    Console.WriteLine("PatternName: " + Automation.PatternName(pattern1));
                }
                /**/
                while (true)
                {
                    if (objName.Contains(";"))
                    {
                        int index = objName.IndexOf(";",
                            StringComparison.CurrentCulture);
                        currObjName = objName.Substring(0, index);
                        objName = objName.Substring(index + 1);
                    }
                    else
                    {
                        currObjName = objName;
                    }
                    LogMessage("childHandle: " + childHandle.Current.Name +
                        " : " + currObjName + " : " +
                        childHandle.Current.ControlType.ProgrammaticName);
                    childHandle = utils.GetObjectHandle(childHandle,
                        currObjName, type, false);
                    if (childHandle == null)
                    {
                        if (currObjName == objName)
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find Object: " + objName);
                        }
                        else
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find Object: " + currObjName);
                        }
                    }
                    // Store previous handle for later use
                    prevObjHandle = childHandle;
                    if (firstObjHandle == null)
                    {
                        // Save it for later use
                        firstObjHandle = childHandle;
                    }
                    if ((actionType == "Select" || actionType == "SubMenu" ||
                        actionType == "Check" || actionType == "UnCheck" ||
                        actionType == "VerifyCheck" || actionType == "Window") &&
                        !utils.IsEnabled(childHandle, false))
                    {
                        throw new XmlRpcFaultException(123,
                            "Object state is disabled");
                    }
                    try
                    {
                        if (actionType == "Window")
                        {
                            utils.InternalXYClick(childHandle);
                        }
                        else
                        {
                            // SetFocus() fails on Windows Explorer
                            childHandle.SetFocus();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage(ex);
                    }
                    if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                        out pattern) || childHandle.TryGetCurrentPattern(
                        ExpandCollapsePattern.Pattern, out pattern))
                    {
                        if (actionType == "Select" || currObjName != objName ||
                             actionType == "SubMenu" || actionType == "VerifyCheck" ||
                             actionType == "Window")
                        {
                            try
                            {
                                LogMessage("Invoking menu item: " + currObjName +
                                    " : " + objName + " : " +
                                    childHandle.Current.ControlType.ProgrammaticName +
                                    " : " + childHandle.Current.Name);
                            }
                            catch (Exception ex)
                            {
                                // Noticed with closewindow() to close Notepad
                                //    System.UnauthorizedAccessException: Access is denied
                                //       Exception from HRESULT: 0x80070005 (E_ACCESSDENIED)
                                LogMessage(ex);
                            }
                            if (actionType != "Window")
                            {
                                try
                                {
                                    // SetFocus() fails on Windows Explorer
                                    childHandle.SetFocus();
                                }
                                catch (Exception ex)
                                {
                                    LogMessage(ex);
                                }
                            }
                            if (!(actionType == "VerifyCheck" && currObjName == objName) &&
                                (actionType != "Window"))
                            {
                                utils.InternalClick(childHandle);
                            }
                            try
                            {
                                // Invoke doesn't work for VMware Workstation
                                // But they work for Notepad
                                // MoveToAndClick works for VMware Workstation
                                // But not for Notepad (on first time)
                                // Requires 2 clicks !
                                //((InvokePattern)pattern).Invoke();
                                utils.InternalWait(1);
                                c = childHandle.FindAll(TreeScope.Children,
                                    Condition.TrueCondition);
                            }
                            catch (System.NotImplementedException ex)
                            {
                                // Noticed with VMware Workstation
                                //    System.Runtime.InteropServices.COMException (0x80040200):
                                //       Exception from HRESULT: 0x80040200
                                LogMessage("NotImplementedException");
                                LogMessage(ex);
                            }
                            catch (System.Windows.Automation.ElementNotEnabledException ex)
                            {
                                // Noticed with VMware Workstation
                                //    System.Runtime.InteropServices.COMException (0x80040200):
                                //       Exception from HRESULT: 0x80040200
                                LogMessage("Element not enabled");
                                LogMessage(ex);
                            }
                            catch (Exception ex)
                            {
                                LogMessage(ex);
                            }
                        }
                    }
                    if (currObjName == objName && actionType != "SubMenu")
                    {
                        int state;
                        switch (actionType)
                        {
                            case "Select":
                            case "Window":
                                // No child menu item to be processed
                                return 1;
                            case "Check":
                            case "UnCheck":
                                state = IsMenuChecked(childHandle);
                                LogMessage("IsMenuChecked(childHandle): " +
                                    childHandle.Current.ControlType.ProgrammaticName);
                                LogMessage("actionType: " + actionType);
                                // Don't process the last item
                                if (actionType == "Check")
                                {
                                    if (state == 1)
                                        // Already checked, just click back the main menu
                                        utils.InternalClick(firstObjHandle);
                                    else
                                        // Check menu
                                        utils.InternalClick(childHandle);
                                    return 1;
                                }
                                else if (actionType == "UnCheck")
                                {
                                    if (state == 0)
                                        // Already unchecked, just click back the main menu
                                        utils.InternalClick(firstObjHandle);
                                    else
                                        // Uncheck menu
                                        utils.InternalClick(childHandle);
                                    return 1;
                                }
                                break;
                            case "Exist":
                            case "Enabled":
                                state = utils.IsEnabled(childHandle) == true ? 1 : 0;
                                LogMessage("IsEnabled(childHandle): " +
                                    childHandle.Current.Name + " : " + state);
                                LogMessage("IsEnabled(childHandle): " +
                                    childHandle.Current.ControlType.ProgrammaticName);
                                // Set it back to old state, else the menu selection left there
                                utils.InternalClick(firstObjHandle);
                                // Don't process the last item
                                if (actionType == "Enabled")
                                    return state;
                                else if (actionType == "Exist")
                                    return 1;
                                break;
                            case "SubMenu":
                                int status = HandleSubMenu(w.walker.GetFirstChild(childHandle),
                                    firstObjHandle, ref menuList);
                                if (status == 1)
                                    return 1;
                                break;
                            case "VerifyCheck":
                                state = IsMenuChecked(childHandle);
                                utils.InternalClick(firstObjHandle);
                                return state;
                            default:
                                break;
                        }
                    }
                    else if ((tmpContextHandle = utils.InternalWaitTillControlTypeExist(
                        ControlType.Menu, processId, 3)) != null)
                    {
                        LogMessage("InternalWaitTillControlTypeExist");
                        // Find object from current handle, rather than navigating
                        // the complete window
                        childHandle = tmpContextHandle;
                        if (actionType != "SubMenu")
                            continue;
                        else if (currObjName == objName)
                        {
                            switch (actionType)
                            {
                                case "SubMenu":
                                    int status = HandleSubMenu(w.walker.GetFirstChild(childHandle),
                                        firstObjHandle, ref menuList);
                                    if (status == 1)
                                        return 1;
                                    break;
                            }
                        }
                    }
                    else if (c != null && c.Count > 0)
                    {
                        if (currObjName == objName)
                        {
                            switch (actionType)
                            {
                                case "SubMenu":
                                    int status = HandleSubMenu(w.walker.GetFirstChild(childHandle),
                                        firstObjHandle, ref menuList);
                                    if (status == 1)
                                        return 1;
                                    break;
                            }
                        }
                        LogMessage("c != null && c.Count > 0");
                        childHandle = windowHandle;
                        continue;
                    }
                    // Required for Notepad like app
                    if ((c == null || c.Count == 0))
                    {
                        LogMessage("Work around for Windows application");
                        LogMessage(windowHandle.Current.Name + " : " + objName);
                        AutomationElement tmpChildHandle = utils.GetObjectHandle(
                            windowHandle, objName,
                            type, false);
                        // Work around for Notepad, as it doesn't find the menuitem
                        // on clicking any menu
                        if (tmpChildHandle != null)
                        {
                            LogMessage("Work around: tmpChildHandle != null");
                            if (actionType == "SubMenu" && currObjName == objName)
                                // Work around for Notepad like app
                                childHandle = tmpChildHandle;
                            else
                                // Work around for Notepad like app,
                                // but for actionType other than SubMenu
                                childHandle = windowHandle;
                        }
                    }
                    if (currObjName == objName)
                    {
                        switch (actionType)
                        {
                            case "SubMenu":
                                int status = HandleSubMenu(w.walker.GetFirstChild(childHandle),
                                    firstObjHandle, ref menuList);
                                if (status == 1)
                                    return 1;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                if (firstObjHandle != null && actionType != "Window")
                {
                    // Set it back to old state, else the menu selection left there
                    utils.InternalXYClick(firstObjHandle);
                }
                if (((ex is ElementNotAvailableException) ||
                    (ex is UnauthorizedAccessException)) &&
                    actionType == "Window")
                {
                    // API closewindow() can close Windows Explorer on XP, but:
                    // -----------------------------------------------------------
                    // if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                    //     out pattern) || childHandle.TryGetCurrentPattern(
                    //     ExpandCollapsePattern.Pattern, out pattern))
                    // -----------------------------------------------------------
                    // Sometimes above code will throw exception, sometimes not:
                    //    System.Runtime.InteropServices.COMException (0x80040201):
                    //       Exception from HRESULT: 0x80040201
                    //    System.UnauthorizedAccessException, Access is denied:
                    //       Exception from HRESULT: 0x80070005 (E_ACCESSDENIED))
                    // So use this if block as workaround
                    return 1;
                }
                if (ex is XmlRpcFaultException)
                    throw;
                else
                    throw new XmlRpcFaultException(123,
                        "Unhandled exception: " + ex.Message);
            }
            finally
            {
                c = null;
                w = null;
                pattern = null;
                windowHandle = childHandle = null;
                prevObjHandle = firstObjHandle = null;
            }
        }
        public int SelectMenuItem(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName,
                    ref menuList, "Select");
            }
            finally
            {
                menuList = null;
            }
        }
        public int MaximizeWindow(String windowName)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                String[] windowList = null;
                Generic generic = new Generic(this.utils);
                try
                {
                    windowList = generic.GetWindowList();
                    foreach (String window in windowList)
                    {
                        ArrayList menuList = new ArrayList();
                        try
                        {
                            if (utils.InternalGuiExist(window, "mnuSystem") == 1)
                            {
                                generic.GrabFocus(window);
                                InternalMenuHandler(window, "mnuSystem;mnuMaximize",
                                    ref menuList, "Window");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                        finally
                        {
                            menuList = null;
                        }
                    }
                    return 1;
                }
                finally
                {
                    generic = null;
                    windowList = null;
                }
            }
            else
            {
                ArrayList menuList = new ArrayList();
                try
                {
                    // FIXME: Verify this for i18n / l10n
                    return InternalMenuHandler(windowName, "mnuSystem;mnuMaximize",
                        ref menuList, "Window");
                }
                finally
                {
                    menuList = null;
                }
            }
        }
        public int MinimizeWindow(String windowName)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                String[] windowList = null;
                Generic generic = new Generic(this.utils);
                try
                {
                    windowList = generic.GetWindowList();
                    foreach (String window in windowList)
                    {
                        ArrayList menuList = new ArrayList();
                        try
                        {
                            if (utils.InternalGuiExist(window, "mnuSystem") == 1)
                            {
                                generic.GrabFocus(window);
                                InternalMenuHandler(window, "mnuSystem;mnuMinimize",
                                    ref menuList, "Window");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                        finally
                        {
                            menuList = null;
                        }
                    }
                    return 1;
                }
                finally
                {
                    generic = null;
                    windowList = null;
                }
            }
            else
            {
                ArrayList menuList = new ArrayList();
                try
                {
                    return InternalMenuHandler(windowName, "mnuSystem;mnuMinimize",
                        ref menuList, "Window");
                }
                finally
                {
                    menuList = null;
                }
            }
        }
        public int CloseWindow(String windowName)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                String[] windowList = null;
                Generic generic = new Generic(this.utils);
                try
                {
                    windowList = generic.GetWindowList();
                    foreach (String window in windowList)
                    {
                        ArrayList menuList = new ArrayList();
                        try
                        {
                            if (utils.InternalGuiExist(window, "mnuSystem") == 1)
                            {
                                generic.GrabFocus(window);
                                InternalMenuHandler(window, "mnuSystem;mnuClose",
                                    ref menuList, "Window");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                        finally
                        {
                            menuList = null;
                        }
                    }
                    return 1;
                }
                finally
                {
                    generic = null;
                    windowList = null;
                }
            }
            else
            {
                ArrayList menuList = new ArrayList();
                try
                {
                    return InternalMenuHandler(windowName, "mnuSystem;mnuClose",
                        ref menuList, "Window");
                }
                finally
                {
                    menuList = null;
                }
            }
        }
        public int MenuCheck(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName, ref menuList,
                    "Check");
            }
            finally
            {
                menuList = null;
            }
        }
        public int MenuUnCheck(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName, ref menuList,
                    "UnCheck");
            }
            finally
            {
                menuList = null;
            }
        }
        public int VerifyMenuCheck(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName, ref menuList, "VerifyCheck");
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return 0;
            }
            finally
            {
                menuList = null;
            }
        }
        public int VerifyMenuUnCheck(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName,
                    ref menuList, "VerifyCheck") == 1 ? 0 : 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return 0;
            }
            finally
            {
                menuList = null;
            }
        }
        public int MenuItemEnabled(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName,
                    ref menuList, "Enabled");
            }
            catch (XmlRpcFaultException ex)
            {
                LogMessage(ex);
                return 0;
            }
            finally
            {
                menuList = null;
            }
        }
        public int DoesSelectMenuItemExist(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName,
                    ref menuList, "Exist");
            }
            catch (XmlRpcFaultException ex)
            {
                LogMessage(ex);
                return 0;
            }
            finally
            {
                menuList = null;
            }
        }
        public String[] ListSubMenus(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                if (InternalMenuHandler(windowName, objName,
                    ref menuList, "SubMenu") == 1)
                {
                    return menuList.ToArray(typeof(string)) as string[];
                }
                else
                {
                    throw new XmlRpcFaultException(123, "Unable to get sub menuitem.");
                }
            }
            finally
            {
                menuList = null;
            }
        }
    }
}
