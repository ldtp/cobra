/*
WinLDTP 1.0

@author: Nagappan Alagappan <nalagappan@vmware.com>
@copyright: Copyright (c) 2011-12 VMware Inc.,
@license: LGPLv2

http://ldtp.freedesktop.org

This file may be distributed and/or modified under the terms of the GNU General
Public License version 2 as published by the Free Software Foundation. This file
is distributed without any warranty; without even the implied warranty of
merchantability or fitness for a particular purpose.

See 'README.txt' in the source distribution for more information.

Headers in this file shall remain intact.
*/
using System;
using System.Collections;

// Additional namespace
using System.IO;
using System.Text;
using ATGTestInput;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using CookComputing.XmlRpc;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Windows.Automation.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

//[assembly: CLSCompliant(true)]
namespace Ldtpd
{
    public class Core : Utils
    {
        //Hashtable objectHT = new Hashtable();
        public Core(bool debug = false)
        {
            this.debug = debug;
        }
        [XmlRpcMethod("getlastlog", Description = "Get last log from the stack.")]
        public string GetLastLog()
        {
            if (logStack.Count > 0)
                return (string)logStack.Pop();
            return "";
        }
        [XmlRpcMethod("wait", Description = "Wait a given amount of seconds")]
        public int Wait(object waitTime)
        {
            return InternalWait(waitTime);
        }
        [XmlRpcMethod("getobjectlist", Description = "Get object list")]
        public String[] GetObjectList(String windowName)
        {
            if (windowName == null || windowName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            ArrayList objectList = new ArrayList();
            Hashtable objectHT = new Hashtable();
            string matchedKey = null;
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            InternalGetObjectList(walker.GetFirstChild(windowHandle),
                ref objectList, ref objectHT, ref matchedKey,
                true, null, windowHandle.Current.Name);
            if (debug)
            {
                LogMessage(objectList.Count);
                foreach (string key in objectHT.Keys)
                {
                    LogMessage("Key: " + ((Hashtable)objectHT[key])["key"]);
                    LogMessage("Parent: " + ((Hashtable)objectHT[key])["parent"]);
                    LogMessage("Obj index: " + ((Hashtable)objectHT[key])["obj_index"]);
                    LogMessage("Class: " + ((Hashtable)objectHT[key])["class"]);
                    foreach (string child in (ArrayList)((Hashtable)objectHT[key])["children"])
                        LogMessage("Children: " + child);
                }
            }
            objectHT = null;
            matchedKey = null;
            return objectList.ToArray(typeof(string)) as string[];
        }
        [XmlRpcMethod("getwindowlist", Description = "Get window list")]
        public String[] GetWindowList()
        {
            int index;
            String s, actualString;
            ArrayList windowArrayList = new ArrayList();
            CurrentObjInfo currObjInfo;
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement element;
            element = walker.GetFirstChild(AutomationElement.RootElement);
            Condition condition1 = new PropertyCondition(AutomationElement.ControlTypeProperty,
                ControlType.Window);
            try
            {
                AutomationElementCollection c;
                List<AutomationElement> windowTmpList = new List<AutomationElement>();
                LogMessage("GetWindowList - Window list count: " + this.windowList.Count);
                try
                {
                    foreach (AutomationElement e in windowList)
                    {
                        try
                        {
                            s = e.Current.Name;
                            LogMessage("Cached window name: " + s);
                            currObjInfo = objInfo.GetObjectType(e, e.Current.ControlType);
                            actualString = currObjInfo.objType + s;
                            index = 1;
                            while (true)
                            {
                                if (windowArrayList.IndexOf(actualString) < 0)
                                    break;
                                actualString = currObjInfo.objType + s + index;
                                index++;
                            }
                            windowArrayList.Add(actualString);
                        }
                        catch (ElementNotAvailableException ex)
                        {
                            // If window doesn't exist, remove it from list
                            windowTmpList.Add(e);
                            LogMessage(ex);
                        }
                        catch (Exception ex)
                        {
                            // Capture any unhandled exception,
                            // so that the framework won't crash
                            windowTmpList.Add(e);
                            LogMessage(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Since window list is added / removed in different thread
                    // values of windowList might be altered and an exception is thrown
                    // Just handle the global exception
                    LogMessage(ex);
                }
                try
                {
                    foreach (AutomationElement e in windowTmpList)
                        windowList.Remove(e);
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                }
                // FIXME: Check whether resetting the ObjInfo is appropriate here
                objInfo = new ObjInfo(false);
                while (null != element)
                {
                    if (windowList.IndexOf(element) != -1)
                    {
                        // As the window info already added to the windowArrayList
                        // let us not re-add it
                        LogMessage(element.Current.Name + " already in windowList");
                        element = walker.GetNextSibling(element);
                        continue;
                    }
                    s = element.Current.Name;
                    LogMessage("Window name: " + s);
                    currObjInfo = objInfo.GetObjectType(element, element.Current.ControlType);
                    actualString = currObjInfo.objType + s;
                    index = 1;
                    while (true)
                    {
                        if (windowArrayList.IndexOf(actualString) < 0)
                            break;
                        actualString = currObjInfo.objType + s + index;
                        index++;
                    }
                    windowArrayList.Add(actualString);
                    c = null;
                    try
                    {
                        c = element.FindAll(TreeScope.Children, condition1);
                        foreach (AutomationElement e in c)
                        {
                            s = e.Current.Name;
                            currObjInfo = objInfo.GetObjectType(e, e.Current.ControlType);
                            if (s == null || s == "")
                                actualString = currObjInfo.objType + currObjInfo.objCount;
                            else
                            {
                                actualString = currObjInfo.objType + s;
                                index = 1;
                                while (true)
                                {
                                    if (windowArrayList.IndexOf(actualString) < 0)
                                        break;
                                    actualString = currObjInfo.objType + s + index;
                                    index++;
                                }
                            }
                            windowArrayList.Add(actualString);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage(ex);
                    }
                    element = walker.GetNextSibling(element);
                }
                return windowArrayList.ToArray(typeof(string)) as string[];
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            // Unable to find window
            return null;
        }
        [XmlRpcMethod("waittillguiexist",
            Description = "Wait till a window or component exists.")]
        public int WaitTillGuiExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            return InternalWaitTillGuiExist(windowName, objName, guiTimeOut);
        }
        [XmlRpcMethod("waittillguinotexist",
            Description = "Wait till a window or component does not exists.")]
        public int WaitTillGuiNotExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            return InternalWaitTillGuiNotExist(windowName, objName, guiTimeOut);
        }
        [XmlRpcMethod("guiexist", Description = "Checks whether a window or component exists.")]
        public int GuiExist(String windowName, String objName = null)
        {
            return InternalGuiExist(windowName, objName);
        }
        [XmlRpcMethod("objectexist", Description = "Checks whether a component exists.")]
        public int ObjectExist(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            try
            {
                windowHandle.SetFocus();
                AutomationElement childHandle = GetObjectHandle(windowHandle,
                    objName, null, false);
                if (childHandle != null)
                    return 1;
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("objtimeout ",
            Description = "Object timeout period, default 5 seconds.")]
        public int ObjectTimeOut(int objectTimeOut)
        {
            if (objectTimeOut <= 0)
                this.objectTimeOut = 5;
            else
                this.objectTimeOut = objectTimeOut;
            return 1;
        }
        [XmlRpcMethod("selectmenuitem",
            Description = "Select (click) a menuitem.")]
        public int SelectMenuItem(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            return InternalMenuHandler(windowName, objName, ref menuList, "Select");
        }
        [XmlRpcMethod("maximizewindow",
            Description = "Maximize window.")]
        public int MaximizeWindow(String windowName)
        {
            ArrayList menuList = new ArrayList();
            // Need to see how this is going to be for i18n / l10n
            return InternalMenuHandler(windowName, "mnuSystem;mnuMaximize",
                ref menuList, "Select");
        }
        [XmlRpcMethod("minimizewindow",
            Description = "Minimize window.")]
        public int MinimizeWindow(String windowName)
        {
            ArrayList menuList = new ArrayList();
            // Need to see how this is going to be for i18n / l10n
            return InternalMenuHandler(windowName, "mnuSystem;mnuMinimize",
                ref menuList, "Select");
        }
        [XmlRpcMethod("closewindow",
            Description = "Close window.")]
        public int CloseWindow(String windowName)
        {
            ArrayList menuList = new ArrayList();
            // Need to see how this is going to be for i18n / l10n
            return InternalMenuHandler(windowName, "mnuSystem;mnuClose",
                ref menuList, "Select");
        }
        [XmlRpcMethod("menucheck",
            Description = "Check (click) a menuitem.")]
        public int MenuCheck(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            // Works for "Notepad" like app,
            //  but fails for "VMware Workstation" like app
            return InternalMenuHandler(windowName, objName, ref menuList, "Check");
        }
        [XmlRpcMethod("menuuncheck",
            Description = "Uncheck (click) a menuitem.")]
        public int MenuUnCheck(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            // Works for "Notepad" like app,
            //  but fails for "VMware Workstation" like app
            return InternalMenuHandler(windowName, objName, ref menuList, "UnCheck");
        }
        [XmlRpcMethod("verifymenucheck",
            Description = "Verify a menuitem is unchecked.")]
        public int VerifyMenuCheck(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            String currObjName = null;
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            windowHandle.SetFocus();
            LogMessage("Window name: " + windowHandle + " : " +
                windowHandle.Current.Name +
                " : " + windowHandle.Current.ControlType.ProgrammaticName);
            bool bContextNavigated = false;
            AutomationElement firstObjHandle = null;
            AutomationElement prevObjHandle = null;
            AutomationElement childHandle = windowHandle;
            while (true)
            {
                try
                {
                    if (objName.Contains(";"))
                    {
                        int index = objName.IndexOf(";", StringComparison.CurrentCulture);
                        currObjName = objName.Substring(0, index);
                        objName = objName.Substring(index + 1);
                    }
                    else
                    {
                        currObjName = objName;
                    }
                    childHandle = GetObjectHandle(childHandle,
                        currObjName, null, false);
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
                        // Save it for later use
                        firstObjHandle = childHandle;
                    if (!IsEnabled(childHandle))
                    {
                        throw new XmlRpcFaultException(123,
                            "Object state is disabled");
                    }
                    if (childHandle.Current.ControlType != ControlType.MenuItem)
                    {
                        throw new XmlRpcFaultException(123,
                            "Object type should be menu item");
                    }
                    childHandle.SetFocus();
                    Object pattern = null;
                    AutomationElementCollection c = null;
                    if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern,
                        out pattern))
                    {
                        if (((TogglePattern)pattern).Current.ToggleState == ToggleState.Off)
                        {
                            LogMessage("Invoking menu item: " + currObjName + " : " + objName +
                                " : " + childHandle.Current.ControlType.ProgrammaticName);
                            Rect rect = childHandle.Current.BoundingRectangle;
                            Point pt = new Point(rect.X + rect.Width / 2,
                                rect.Y + rect.Height / 2);
                            Input.MoveToAndClick(pt);
                        }
                        else
                        {
                            Rect rect = firstObjHandle.Current.BoundingRectangle;
                            Point pt = new Point(rect.X + rect.Width / 2,
                                rect.Y + rect.Height / 2);
                            Input.MoveToAndClick(pt);
                            LogMessage("Menu item already checked");
                        }
                    }
                    else if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                        out pattern))
                    {
                        LogMessage("Invoking menu item: " + currObjName + " : " + objName +
                            " : " + childHandle.Current.ControlType.ProgrammaticName);
                        Rect rect = childHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                        try
                        {
                            ((InvokePattern)pattern).Invoke();
                            Wait(1);
                            c = childHandle.FindAll(TreeScope.Children,
                                Condition.TrueCondition);
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                    }
                    if (currObjName == objName)
                    {
                        // Set it back to old state, else the menu selection left there
                        Rect rect = firstObjHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                        // Don't process the last item
                        return 1;
                    }
                    else if (!bContextNavigated && WaitTillGuiExist("Context",
                        null, 3) == 1)
                    {
                        // Menu item under Menu are listed under Menu Window
                        windowHandle = GetWindowHandle("Context");
                        if (windowHandle == null)
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find window: Context");
                        }
                        // Find object from current handle, rather than navigating
                        // the complete window
                        childHandle = windowHandle;
                        bContextNavigated = true;
                    }
                    else if (c != null && c.Count > 0)
                    {
                        childHandle = windowHandle;
                    }
                    else if (WaitTillGuiExist(prevObjHandle.Current.Name,
                        null, 3) == 1)
                    {
                        // Menu item under Menu are listed under Menu Window
                        windowHandle = GetWindowHandle(prevObjHandle.Current.Name);
                        if (windowHandle == null)
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find window: " + prevObjHandle.Current.Name);
                        }
                        // Find object from current handle, rather than navigating
                        // the complete window
                        childHandle = windowHandle;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                    if (firstObjHandle != null)
                    {
                        // Set the state back to old state
                        Rect rect = firstObjHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                    }
                    return 0;
                }
            }
        }
        [XmlRpcMethod("verifymenuuncheck",
            Description = "Verify a menuitem is checked.")]
        public int VerifyMenuUnCheck(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            String currObjName = null;
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            windowHandle.SetFocus();
            LogMessage("Window name: " + windowHandle + " : " +
                windowHandle.Current.Name +
                " : " + windowHandle.Current.ControlType.ProgrammaticName);
            bool bContextNavigated = false;
            AutomationElement firstObjHandle = null;
            AutomationElement prevObjHandle = null;
            AutomationElement childHandle = windowHandle;
            while (true)
            {
                try
                {
                    if (objName.Contains(";"))
                    {
                        int index = objName.IndexOf(";", StringComparison.CurrentCulture);
                        currObjName = objName.Substring(0, index);
                        objName = objName.Substring(index + 1);
                    }
                    else
                    {
                        currObjName = objName;
                    }
                    childHandle = GetObjectHandle(childHandle,
                        currObjName, null, false);
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
                        // Save it for later use
                        firstObjHandle = childHandle;
                    if (!IsEnabled(childHandle))
                    {
                        throw new XmlRpcFaultException(123,
                            "Object state is disabled");
                    }
                    if (childHandle.Current.ControlType != ControlType.MenuItem)
                    {
                        throw new XmlRpcFaultException(123,
                            "Object type should be menu item");
                    }
                    childHandle.SetFocus();
                    Object pattern = null;
                    AutomationElementCollection c = null;
                    if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern,
                        out pattern))
                    {
                        if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                        {
                            LogMessage("Invoking menu item: " + currObjName + " : " + objName +
                                " : " + childHandle.Current.ControlType.ProgrammaticName);
                            Rect rect = childHandle.Current.BoundingRectangle;
                            Point pt = new Point(rect.X + rect.Width / 2,
                                rect.Y + rect.Height / 2);
                            Input.MoveToAndClick(pt);
                        }
                        else
                        {
                            Rect rect = firstObjHandle.Current.BoundingRectangle;
                            Point pt = new Point(rect.X + rect.Width / 2,
                                rect.Y + rect.Height / 2);
                            Input.MoveToAndClick(pt);
                            LogMessage("Menu item already unchecked");
                        }
                    }
                    else if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                        out pattern))
                    {
                        LogMessage("Invoking menu item: " + currObjName + " : " + objName +
                            " : " + childHandle.Current.ControlType.ProgrammaticName);
                        Rect rect = childHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                        try
                        {
                            ((InvokePattern)pattern).Invoke();
                            Wait(1);
                            c = childHandle.FindAll(TreeScope.Children,
                                Condition.TrueCondition);
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                    }
                    if (currObjName == objName)
                    {
                        // Set it back to old state, else the menu selection left there
                        Rect rect = firstObjHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                        // Don't process the last item
                        return 1;
                    }
                    else if (!bContextNavigated && WaitTillGuiExist("Context",
                        null, 3) == 1)
                    {
                        // Menu item under Menu are listed under Menu Window
                        windowHandle = GetWindowHandle("Context");
                        if (windowHandle == null)
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find window: Context");
                        }
                        // Find object from current handle, rather than navigating
                        // the complete window
                        childHandle = windowHandle;
                        bContextNavigated = true;
                    }
                    else if (c != null && c.Count > 0)
                    {
                        childHandle = windowHandle;
                    }
                    else if (WaitTillGuiExist(prevObjHandle.Current.Name,
                        null, 3) == 1)
                    {
                        // Menu item under Menu are listed under Menu Window
                        windowHandle = GetWindowHandle(prevObjHandle.Current.Name);
                        if (windowHandle == null)
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find window: " + prevObjHandle.Current.Name);
                        }
                        // Find object from current handle, rather than navigating
                        // the complete window
                        childHandle = windowHandle;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                    if (firstObjHandle != null)
                    {
                        // Set the state back to old state
                        Rect rect = firstObjHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                    }
                    return 0;
                }
            }
        }
        [XmlRpcMethod("menuitemenabled",
            Description = "Verify a menuitem is enabled.")]
        public int MenuItemEnabled(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName, ref menuList, "Enabled");
            }
            catch (XmlRpcFaultException ex)
            {
                LogMessage(ex);
                return 0;
            }
        }
        [XmlRpcMethod("doesmenuitemexist",
            Description = "Does a menu item exist.")]
        public int DoesSelectMenuItemExist(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            try
            {
                return InternalMenuHandler(windowName, objName, ref menuList, "Exist");
            }
            catch (XmlRpcFaultException ex)
            {
                LogMessage(ex);
                return 0;
            }
        }
        [XmlRpcMethod("listsubmenus",
            Description = "List sub menu item.")]
        public String[] ListSubMenus(String windowName, String objName)
        {
            ArrayList menuList = new ArrayList();
            if (InternalMenuHandler(windowName, objName, ref menuList, "SubMenu") == 1)
                return menuList.ToArray(typeof(string)) as string[];
            else
                throw new XmlRpcFaultException(123, "Unable to get sub menuitem.");
        }
        [XmlRpcMethod("stateenabled",
            Description = "Checks whether an object state enabled.")]
        public int StateEnabled(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            try
            {
                windowHandle.SetFocus();
                AutomationElement childHandle = GetObjectHandle(windowHandle,
                    objName, null, false);
                if (childHandle == null)
                {
                    LogMessage("Unable to find Object: " + objName);
                    return 0;
                }
                if (IsEnabled(childHandle))
                    return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("click", Description = "Click item.")]
        public int Click(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            windowHandle.SetFocus();
            ControlType[] type = new ControlType[9] { ControlType.Button,
                ControlType.CheckBox, ControlType.RadioButton,
                ControlType.SplitButton, ControlType.Menu, ControlType.ListItem,
                ControlType.MenuItem, ControlType.MenuBar, ControlType.Pane };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            Object pattern = null;
            try
            {
                childHandle.SetFocus();
                if (childHandle.Current.ControlType == ControlType.Pane)
                {
                    // NOTE: Work around, as the pane doesn't seem to work
                    // with any actions. Noticed this window, when Windows
                    // Security Warning dialog pop's up
                    Rect rect = childHandle.Current.BoundingRectangle;
                    Point pt = new Point(rect.X + rect.Width / 2,
                        rect.Y + rect.Height / 2);
                    Input.MoveToAndClick(pt);
                    return 1;
                }
                else if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                    out pattern))
                {
                    if (childHandle.Current.ControlType == ControlType.Menu ||
                        childHandle.Current.ControlType == ControlType.MenuBar ||
                        childHandle.Current.ControlType == ControlType.MenuItem ||
                        childHandle.Current.ControlType == ControlType.ListItem)
                    {
                        //((InvokePattern)invokePattern).Invoke();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        Rect rect = childHandle.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
                    }
                    else
                    {
                        ((InvokePattern)pattern).Invoke();
                    }
                    return 1;
                }
                else if (childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                    out pattern))
                {
                    ((SelectionItemPattern)pattern).Select();
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
            throw new XmlRpcFaultException(123, "Unable to perform action");
        }
        [XmlRpcMethod("selectindex",
            Description = "Select combo box / layered pane item based on index.")]
        public int SelectIndex(String windowName, String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            if (index == 0)
                throw new XmlRpcFaultException(123,
                    "Index out of range: " + index);
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            windowHandle.SetFocus();
            ControlType[] type = new ControlType[3] { ControlType.ComboBox,
                ControlType.ListItem, ControlType.List };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            LogMessage("Handle name: " + childHandle.Current.Name +
                " - " + childHandle.Current.ControlType.ProgrammaticName);
            if (!IsEnabled(childHandle))
            {
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
                        Rect rect = element.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
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
            }
            throw new XmlRpcFaultException(123,
                "Unable to select item.");
        }
        [XmlRpcMethod("selectitem",
            Description = "Select combo box / layered pane item based on name.")]
        public int SelectItem(String windowName, String objName, String item)
        {
            return ComboSelect(windowName, objName, item);
        }
        [XmlRpcMethod("showlist",
            Description = "Show combo box item based on name.")]
        public int ShowList(String windowName, String objName)
        {
            return InternalComboHandler(windowName, objName, null, "Show");
        }
        [XmlRpcMethod("hidelist",
            Description = "Hide combo box item based on name.")]
        public int HideList(String windowName, String objName)
        {
            return InternalComboHandler(windowName, objName, null, "Hide");
        }
        [XmlRpcMethod("comboselect",
            Description = "Select combo box / layered pane item based on name.")]
        public int ComboSelect(String windowName, String objName, String item)
        {
            return InternalComboHandler(windowName, objName, item, "Select");
        }
        [XmlRpcMethod("verifydropdown",
            Description = "Verify if combo box drop down list in the current dialog is visible.")]
        public int VerifyDropDown(String windowName, String objName)
        {
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            windowHandle.SetFocus();
            ControlType[] type = new ControlType[3] { ControlType.ComboBox,
                ControlType.ListItem, ControlType.List };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName,
                type, true);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            try
            {
                LogMessage("Handle name: " + childHandle.Current.Name +
                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                if (!IsEnabled(childHandle))
                {
                    LogMessage("Object state is disabled");
                    return 0;
                }
                Object pattern = null;
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
            return 0;
        }
        [XmlRpcMethod("verifyshowlist",
            Description = "Verify if combo box drop down list in the current dialog is visible.")]
        public int VerifyShowList(String windowName, String objName)
        {
            return VerifyDropDown(windowName, objName);
        }
        [XmlRpcMethod("verifyhidelist",
            Description = "Verify if combo box drop down list in the current dialog is not visible.")]
        public int VerifyHideList(String windowName, String objName)
        {
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            windowHandle.SetFocus();
            ControlType[] type = new ControlType[3] { ControlType.ComboBox,
                ControlType.ListItem, ControlType.List };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName,
                type, true);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            try
            {
                LogMessage("Handle name: " + childHandle.Current.Name +
                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                if (!IsEnabled(childHandle))
                {
                    LogMessage("Object state is disabled");
                    return 0;
                }
                Object pattern = null;
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
            return 0;
        }
        [XmlRpcMethod("verifyselect",
            Description = "Select combo box / layered pane item based on name.")]
        public int VerifyComboSelect(String windowName, String objName, String item)
        {
            try
            {
                return InternalComboHandler(windowName, objName, item, "Verify");
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return 0;
            }
        }
        [XmlRpcMethod("settextvalue",
            Description = "Type string sequence.")]
        public int SetTextValue(String windowName, String objName, String value)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Edit };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
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
            return 1;
        }
        [XmlRpcMethod("gettextvalue",
            Description = "Get text value")]
        public String GetTextValue(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Edit };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, false);
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
                    return ((RangeValuePattern)pattern).Current.Value.ToString(CultureInfo.CurrentCulture);
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
        }
        [XmlRpcMethod("setvalue",
            Description = "Type string sequence.")]
        public int SetValue(String windowName,
            String objName, double value)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Slider,
                ControlType.Spinner };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            try
            {
                if (!IsEnabled(childHandle))
                {
                    throw new XmlRpcFaultException(123,
                        "Object state is disabled");
                }
                object valuePattern = null;
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern,
                    out valuePattern))
                {
                    ((RangeValuePattern)valuePattern).SetValue(value);
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
            throw new XmlRpcFaultException(123, "Unable to set value");
        }
        [XmlRpcMethod("getvalue", Description = "Get object value")]
        public double GetValue(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Slider, ControlType.Spinner };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            Object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(RangeValuePattern.Pattern, out pattern))
                {
                    return ((RangeValuePattern)pattern).Current.Value;
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
            throw new XmlRpcFaultException(123, "Unable to get value");
        }
        [XmlRpcMethod("check", Description = "Check radio button / checkbox")]
        public int Check(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.CheckBox, ControlType.RadioButton };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            try
            {
                childHandle.SetFocus();
                Object pattern = null;
                if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    if (((TogglePattern)pattern).Current.ToggleState == ToggleState.Off)
                    {
                        Object invoke = null;
                        if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern, out invoke))
                            ((InvokePattern)invoke).Invoke();
                        else
                            ((TogglePattern)pattern).Toggle();
                    }
                    else
                        LogMessage("Checkbox / Radio button already checked");
                    return 1;
                }
                else if (childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                {
                    ((SelectionItemPattern)pattern).Select();
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
            LogMessage("Unsupported pattern to perform action");
            throw new XmlRpcFaultException(123, "Unsupported pattern to perform action");
        }
        [XmlRpcMethod("uncheck", Description = "UnCheck radio button / checkbox")]
        public int UnCheck(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.CheckBox };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            try
            {
                childHandle.SetFocus();
                Object pattern = null;
                if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                    {
                        Object invoke = null;
                        if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern, out invoke))
                            ((InvokePattern)invoke).Invoke();
                        else
                            ((TogglePattern)pattern).Toggle();
                    }
                    else
                        LogMessage("Checkbox already unchecked");
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
            throw new XmlRpcFaultException(123, "Unsupported pattern to perform action");
        }
        [XmlRpcMethod("verifycheck",
            Description = "Verify radio button / checkbox is checked")]
        public int VerifyCheck(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            ControlType[] type = new ControlType[2] { ControlType.CheckBox, ControlType.RadioButton };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName, type, true);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            try
            {
                childHandle.SetFocus();
                Object pattern = null;
                if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                        return 1;
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("verifyuncheck",
            Description = "Verify radio button / checkbox is unchecked")]
        public int VerifyUnCheck(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            ControlType[] type = new ControlType[2] { ControlType.CheckBox, ControlType.RadioButton };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName, type, true);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            try
            {
                childHandle.SetFocus();
                Object pattern = null;
                if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    if (((TogglePattern)pattern).Current.ToggleState == ToggleState.Off)
                        return 1;
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("selecttab",
            Description = "Select tab based on name.")]
        public int SelectTab(String windowName,
            String objName, String tabName)
        {
            if (windowName == null || objName == null || windowName.Length == 0 ||
                objName.Length == 0 || tabName == null || tabName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            try
            {
                childHandle.SetFocus();
                AutomationElement elementItem = GetObjectHandle(childHandle,
                    tabName);
                if (elementItem != null)
                {
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    Object pattern;
                    if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        Rect rect = elementItem.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
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
            throw new XmlRpcFaultException(123,
                "Unable to find the item in tab list: " + tabName);
        }
        [XmlRpcMethod("selecttabindex",
            Description = "Select tab based on index.")]
        public int SelectTabIndex(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            childHandle.SetFocus();
            AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                Condition.TrueCondition);
            Object pattern;
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
                        Rect rect = element.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
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
            throw new XmlRpcFaultException(123, "Unable to select item.");
        }
        [XmlRpcMethod("gettabname", Description = "Get tab based on index.")]
        public String GetTabName(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            childHandle.SetFocus();
            AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                Condition.TrueCondition);
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
            if (element != null)
            {
                return element.Current.Name;
            }
            throw new XmlRpcFaultException(123,
                "Unable to find item.");
        }
        [XmlRpcMethod("gettabcount", Description = "Get tab count.")]
        public int GetTabCount(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
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
        }
        [XmlRpcMethod("verifytabname",
            Description = "Verify tab name selected or not.")]
        public int VerifyTabName(String windowName,
            String objName, String tabName)
        {
            if (windowName == null || objName == null || windowName.Length == 0 ||
                objName.Length == 0 || tabName == null || tabName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            ControlType[] type = new ControlType[1] { ControlType.Tab };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            if (!IsEnabled(childHandle))
            {
                LogMessage("Object state is disabled");
                return 0;
            }
            try
            {
                childHandle.SetFocus();
                AutomationElement elementItem = GetObjectHandle(childHandle,
                    tabName);
                if (elementItem != null)
                {
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    Object pattern;
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
                return 0;
            }
            LogMessage("Unable to find the item in tab list: " + tabName);
            return 0;
        }
        [XmlRpcMethod("doesrowexist",
            Description = "Does the given row text exist in tree item or list item.")]
        public int DoesRowExist(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            if (windowName == null || objName == null || windowName.Length == 0 ||
                objName.Length == 0 || text == null || text.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            ControlType[] type = new ControlType[2] { ControlType.Tree,
                ControlType.List };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            if (!IsEnabled(childHandle))
            {
                LogMessage("Object state is disabled");
                return 0;
            }
            try
            {
                childHandle.SetFocus();
                type = new ControlType[2] { ControlType.TreeItem,
                ControlType.ListItem };
                if (partialMatch)
                    text += "*";
                AutomationElement elementItem = GetObjectHandle(childHandle,
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
            return 0;
        }
        [XmlRpcMethod("selectrow",
            Description = "Select the given row in tree or list item.")]
        public int SelectRow(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            if (windowName == null || objName == null || windowName.Length == 0
                || objName.Length == 0 || text == null || text.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Tree,
                ControlType.List };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            try
            {
                childHandle.SetFocus();
                if (partialMatch)
                    text += "*";
                AutomationElement elementItem = GetObjectHandle(childHandle,
                    text);
                if (elementItem != null)
                {
                    elementItem.SetFocus();
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    Object pattern;
                    if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        Rect rect = elementItem.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
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
            throw new XmlRpcFaultException(123, "Unable to find the item in list: " + text);
        }
        [XmlRpcMethod("selectrowpartialmatch",
            Description = "Select the given row partial match in tree or list item.")]
        public int SelectRowPartialMatch(String windowName, String objName,
            String text)
        {
            return SelectRow(windowName, objName, text, true);
        }
        [XmlRpcMethod("selectrowindex",
            Description = "Select tab based on index.")]
        public int SelectRowIndex(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Tree,
                ControlType.List };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                Object pattern;
                AutomationElement element = null;
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
                        Rect rect = element.Current.BoundingRectangle;
                        Point pt = new Point(rect.X + rect.Width / 2,
                            rect.Y + rect.Height / 2);
                        Input.MoveToAndClick(pt);
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
            throw new XmlRpcFaultException(123, "Unable to select item.");
        }
        [XmlRpcMethod("expandtablecell",
            Description = "Expand or contract the tree table cell on the row index.")]
        public int ExpandTableCell(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Tree,
                ControlType.TreeItem };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                Object pattern;
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
            throw new XmlRpcFaultException(123, "Unable to expand item.");
        }
        [XmlRpcMethod("getcellvalue",
            Description = "Get tree table cell value on the row index.")]
        public String GetCellValue(String windowName,
            String objName, int index)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Tree, 
                ControlType.TreeItem };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            if (!IsEnabled(childHandle))
            {
                throw new XmlRpcFaultException(123,
                    "Object state is disabled");
            }
            try
            {
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                AutomationElement element = null;
                element = c[index];
                if (element != null)
                    return element.Current.Name;
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
            throw new XmlRpcFaultException(123, "Unable to get item value.");
        }
        [XmlRpcMethod("getrowcount",
            Description = "Get tree table cell row count.")]
        public int GetRowCount(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.Tree,
                ControlType.TreeItem };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            try
            {
                if (!IsEnabled(childHandle))
                {
                    throw new XmlRpcFaultException(123,
                        "Object state is disabled");
                }
                childHandle.SetFocus();
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
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
        }
        [XmlRpcMethod("grabfocus",
            Description = "Grab focus of given element.")]
        public int GrabFocus(String windowName, String objName = null)
        {
            if (windowName == null || windowName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            if (objName == null || objName.Length == 0)
            {
                // If objName is not provided, just grab window focus
                windowHandle.SetFocus();
                return 1;
            }
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            try
            {
                childHandle.SetFocus();
                return 1;
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
        }
        [XmlRpcMethod("handletablecell", Description = "Handle table cell.")]
        public int HandleTableCell()
        {
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("unhandletablecell", Description = "Unhandle table cell.")]
        public int UnHandleTableCell()
        {
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("remap", Description = "Remap window info.")]
        public int Remap(String windowName)
        {
            if (windowName == null || windowName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("activatetext", Description = "Activate text.")]
        public int ActivateText(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("generatemouseevent",
            Description = "Generate mouse event.")]
        public int GenerateMouseEvent(int x, int y, String type = "b1p")
        {
            Point pt = new Point(x, y);
            switch (type)
            {
                case "b1p":
                    ATGTestInput.Input.SendMouseInput(x, y, 0, ATGTestInput.SendMouseInputFlags.LeftDown);
                    break;
                case "b1r":
                    ATGTestInput.Input.SendMouseInput(x, y, 0, ATGTestInput.SendMouseInputFlags.LeftUp);
                    break;
                case "b1c":
                    ATGTestInput.Input.MoveTo(pt);
                    ATGTestInput.Input.SendMouseInput(0, 0, 0, ATGTestInput.SendMouseInputFlags.LeftDown);
                    ATGTestInput.Input.SendMouseInput(0, 0, 0, ATGTestInput.SendMouseInputFlags.LeftUp);
                    break;
                case "b2p":
                    ATGTestInput.Input.SendMouseInput(x, y, 0, ATGTestInput.SendMouseInputFlags.MiddleDown);
                    break;
                case "b2r":
                    ATGTestInput.Input.SendMouseInput(x, y, 0, ATGTestInput.SendMouseInputFlags.MiddleUp);
                    break;
                case "b2c":
                    ATGTestInput.Input.MoveTo(pt);
                    ATGTestInput.Input.SendMouseInput(0, 0, 0, ATGTestInput.SendMouseInputFlags.MiddleDown);
                    ATGTestInput.Input.SendMouseInput(0, 0, 0, ATGTestInput.SendMouseInputFlags.MiddleUp);
                    break;
                case "b3p":
                    ATGTestInput.Input.SendMouseInput(x, y, 0, ATGTestInput.SendMouseInputFlags.RightDown);
                    break;
                case "b3r":
                    ATGTestInput.Input.SendMouseInput(x, y, 0, ATGTestInput.SendMouseInputFlags.RightUp);
                    break;
                case "b3c":
                    ATGTestInput.Input.MoveTo(pt);
                    ATGTestInput.Input.SendMouseInput(0, 0, 0, ATGTestInput.SendMouseInputFlags.RightDown);
                    ATGTestInput.Input.SendMouseInput(0, 0, 0, ATGTestInput.SendMouseInputFlags.RightUp);
                    break;
                case "abs":
                    ATGTestInput.Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
                    break;
                case "rel":
                    ATGTestInput.Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move);
                    break;
                default:
                    throw new XmlRpcFaultException(123, "Unsupported mouse type: " + type);
            }
            return 1;
        }
        [XmlRpcMethod("launchapp", Description = "Launch application.")]
        public int LaunchApp(string cmd, string[] args, int delay = 5,
            int env = 1, string lang = null)
        {
            try
            {
                Process ps = new Process();
                ProcessStartInfo psi = new ProcessStartInfo();

                psi.FileName = cmd;

                if (args != null)
                {
                    // Space separated arguments
                    psi.Arguments = string.Join(" ", args);
                }

                psi.UseShellExecute = true;
                ps.StartInfo = psi;
                ps.Start();
                Thread thread = new Thread(new ParameterizedThreadStart(InternalLaunchApp));
                // Clean up in different thread
                thread.Start(ps);
                Wait(delay);
                ps = null;
                return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                throw new XmlRpcFaultException(123,
                    "Unhandled exception: " + ex.Message);
            }
        }
        [XmlRpcMethod("imagecapture", Description = "Launch application.")]
        /*
         * Return base64 encoded string, required for LDTPv2
         * */
        public string ImageCapture(string windowName = null,
            int x = 0, int y = 0, int width = -1, int height = -1)
        {
            System.Drawing.Bitmap b = null;
            ScreenCapture sc = null;
            try
            {
                sc = new ScreenCapture();
                // capture entire screen, and save it to a file
                string path = Path.GetTempPath() + Path.GetRandomFileName() + ".png";
                if (windowName.Length > 0)
                {
                    AutomationElement windowHandle = GetWindowHandle(windowName);
                    if (windowHandle == null)
                    {
                        throw new XmlRpcFaultException(123,
                            "Unable to find window: " + windowName);
                    }
                    windowHandle.SetFocus();
                    Rect rect = windowHandle.Current.BoundingRectangle;
                    System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(
                        (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                    b = sc.CaptureRectangle(rectangle, path);
                }
                else if (width != -1 && height != -1)
                {
                    System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(
                        x, y, width, height);
                    b = sc.CaptureRectangle(rectangle, path);
                }
                else
                {
                    b = sc.CaptureScreen(path);
                }
                string encodedText = "";
                using (FileStream fs = File.Open(path, FileMode.Open,
                    FileAccess.Read))
                {
                    Byte[] bytesToEncode = new byte[fs.Length];
                    fs.Read(bytesToEncode, 0, (int)fs.Length);
                    encodedText = Convert.ToBase64String(bytesToEncode);
                    fs.Close();
                }
                LogMessage(path);
                File.Delete(path);
                return encodedText;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                if (ex is XmlRpcFaultException)
                    throw;
                throw new XmlRpcFaultException(123,
                    "Unhandled exception: " + ex.Message);
            }
            finally
            {
                if (b != null)
                    b = null;
                if (sc != null)
                    sc = null;
            }
        }
        [XmlRpcMethod("hasstate",
            Description = "Verifies that the object has given state.")]
        public int HasState(String windowName, String objName,
            String state, int guiTimeOut = 0)
        {
            if (windowName == null || objName == null || windowName.Length == 0
                || objName.Length == 0 || state == null || state.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            windowHandle.SetFocus();
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, null, false);
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            try
            {
                if (!IsEnabled(childHandle))
                {
                    // Let us not grab the focus which is enabled
                    // will be helpful during verification
                    LogMessage("childHandle.SetFocus");
                    childHandle.SetFocus();
                }
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                if (c == null)
                {
                    LogMessage("Unable to get row count.");
                    return 0;
                }
                Object pattern;
                do
                {
                    LogMessage("State: " + state);
                    switch (state.ToLower(CultureInfo.CurrentCulture))
                    {
                        case "visible":
                        case "showing":
                            if (childHandle.Current.IsOffscreen == false)
                                return 1;
                            break;
                        case "enabled":
                            if (IsEnabled(childHandle))
                                return 1;
                            break;
                        case "focused":
                            LogMessage("childHandle.Current.HasKeyboardFocus: " +
                                childHandle.Current.HasKeyboardFocus);
                            if (childHandle.Current.HasKeyboardFocus)
                                return 1;
                            break;
                        case "checked":
                            if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern,
                                out pattern))
                            {
                                if (((TogglePattern)pattern).Current.ToggleState ==
                                    ToggleState.On)
                                {
                                    return 1;
                                }
                            }
                            break;
                        case "selected":
                            if (childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                                out pattern))
                            {
                                if (((SelectionItemPattern)pattern).Current.IsSelected)
                                    return 1;
                            }
                            break;
                        case "selectable":
                            if (IsEnabled(childHandle) &&
                                childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                                out pattern))
                            {
                                // Assuming, if its enabled and has selection item pattern
                                // then its selectable
                                return 1;
                            }
                            break;
                    }
                    if (guiTimeOut > 0)
                    {
                        // Wait a second and retry checking the state
                        Wait(1);
                        guiTimeOut--;
                    }
                } while (guiTimeOut > 0);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("getallstates",
            Description = "Get all the object states.")]
        public string[] GetAllStates(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, null, false);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            try
            {
                if (IsEnabled(childHandle))
                {
                    childHandle.SetFocus();
                }
                AutomationElementCollection c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                if (c == null)
                    throw new XmlRpcFaultException(123,
                        "Unable to get row count.");
                Object pattern;
                ArrayList stateList = new ArrayList();
                if (childHandle.Current.IsOffscreen == false)
                {
                    stateList.Add("visible");
                    stateList.Add("showing");
                }
                if (IsEnabled(childHandle))
                    stateList.Add("enabled");
                if (childHandle.Current.HasKeyboardFocus)
                    stateList.Add("focused");
                if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern,
                    out pattern))
                {
                    if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                    {
                        stateList.Add("checked");
                    }
                }
                if (childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                            out pattern))
                {
                    if (((SelectionItemPattern)pattern).Current.IsSelected)
                    {
                        stateList.Add("selected");
                        //stateList.Add("checked");
                    }
                }
                if (IsEnabled(childHandle) &&
                    childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                            out pattern))
                {
                    stateList.Add("selectable");
                }
                return stateList.ToArray(typeof(string)) as string[];
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
        }
        [XmlRpcMethod("getobjectsize", Description = "Get object size.")]
        public int[] GetObjectSize(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName);
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            try
            {
                childHandle.SetFocus();
                Rect rect = childHandle.Current.BoundingRectangle;
                return new int[] { (int)rect.X, (int)rect.Y,
                (int)rect.Width, (int)rect.Height };
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
        }
        [XmlRpcMethod("getobjectinfo", Description = "Get object info.")]
        public string[] GetObjectInfo(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            string matchedKey = "";
            ArrayList objectList = new ArrayList();
            Hashtable objectHT = new Hashtable();
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            try
            {
                if (InternalGetObjectList(walker.GetFirstChild(windowHandle),
                    ref objectList, ref objectHT, ref matchedKey,
                    false, objName, windowHandle.Current.Name))
                {
                    LogMessage(objectHT.Count + " : " + objectList.Count);
                    LogMessage(objectList[objectList.Count - 1]);
                    Hashtable ht = (Hashtable)objectHT[matchedKey];
                    if (ht != null)
                    {
                        ArrayList keyList = new ArrayList();
                        foreach (string key in ht.Keys)
                        {
                            LogMessage(key);
                            keyList.Add(key);
                        }
                        return keyList.ToArray(typeof(string)) as string[];
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
                objectHT = null;
                objectList = null;
            }
            throw new XmlRpcFaultException(123, "Unable to find Object info: " + objName);
        }
        [XmlRpcMethod("getobjectproperty", Description = "Get object property.")]
        public string GetObjectProperty(String windowName, String objName,
            string property)
        {
            if (windowName == null || objName == null || property == null ||
                windowName.Length == 0 || objName.Length == 0 || property.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            bool flag;
            string matchedKey = "";
            ArrayList objectList = new ArrayList();
            Hashtable objectHT = new Hashtable();
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            try
            {
                if (property == "children")
                    flag = true;
                else
                    flag = false;
                if (InternalGetObjectList(walker.GetFirstChild(windowHandle),
                    ref objectList, ref objectHT, ref matchedKey,
                    flag, objName, windowHandle.Current.Name) || flag)
                {
                    if (debug)
                    {
                        LogMessage(objectList.Count);
                        foreach (string key in objectHT.Keys)
                        {
                            LogMessage("Key: " +
                                ((Hashtable)objectHT[key])["key"]);
                            LogMessage("Parent: " +
                                ((Hashtable)objectHT[key])["parent"]);
                            LogMessage("Obj index: " +
                                ((Hashtable)objectHT[key])["obj_index"]);
                            LogMessage("Class: " +
                                ((Hashtable)objectHT[key])["class"]);
                            foreach (string child in
                                (ArrayList)((Hashtable)objectHT[key])["children"])
                                LogMessage("Children: " + child);
                        }
                    }
                    LogMessage(objectHT.Count + " : " + objectList.Count);
                    LogMessage(objectList[objectList.Count - 1]);
                    LogMessage("matchedKey: " + matchedKey + " : " + flag);
                    Hashtable ht = (Hashtable)objectHT[matchedKey];
                    if (ht != null)
                    {
                        foreach (string key in ht.Keys)
                        {
                            LogMessage(key);
                            if (key == property)
                            {
                                if (property == "children")
                                {
                                    ArrayList childrenList = (ArrayList)ht[key];
                                    LogMessage("Count: " + childrenList.Count);
                                    string value = "";
                                    foreach (string child in childrenList)
                                    {
                                        if (value == "")
                                            value = child;
                                        else
                                            value += ", " + child;
                                    }
                                    return value;
                                }
                                else
                                    return (string)ht[key];
                            }
                        }
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
                objectHT = null;
                objectList = null;
            }
            throw new XmlRpcFaultException(123, "Unable to find Object property: " +
                property + " of object: " + objName);
        }
        [XmlRpcMethod("getchild", Description = "Get child.")]
        public string[] GetChild(String windowName, String childName = null,
            string role = null, string parentName = null)
        {
            if (windowName == null || windowName.Length == 0 ||
                (parentName == null && childName == null && role == null &&
                childName.Length == 0 && role.Length == 0 && parentName.Length == 0))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            string matchedKey = "";
            ArrayList childList = new ArrayList();
            ArrayList objectList = new ArrayList();
            Hashtable objectHT = new Hashtable();
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            try
            {
                if (childName != null && childName.Length > 0 &&
                    role != null && role.Length > 0)
                {
                    InternalGetObjectList(walker.GetFirstChild(windowHandle),
                        ref objectList, ref objectHT, ref matchedKey,
                        true, childName, windowHandle.Current.Name);
                    foreach (string key in objectHT.Keys)
                    {
                        try
                        {
                            if (debug)
                                LogMessage("Key: " + key);
                            Hashtable ht = (Hashtable)objectHT[key];
                            String tmp = Regex.Replace(childName, @"\*", @".*");
                            tmp = Regex.Replace(tmp, " ", "");
                            tmp = Regex.Replace(tmp, @"\(", @"\(");
                            tmp = Regex.Replace(tmp, @"\)", @"\)");
                            Regex rx = new Regex(tmp, RegexOptions.Compiled |
                                RegexOptions.IgnorePatternWhitespace |
                                RegexOptions.Multiline |
                                RegexOptions.CultureInvariant);
                            if (debug)
                            {
                                LogMessage("Role matched: " +
                                    (string)ht["class"] == role);
                                if (ht.ContainsKey("label") &&
                                    (string)ht["label"] != null)
                                    LogMessage("Label matched: " +
                                        rx.Match((string)ht["label"]).Success);
                            }
                            if ((string)ht["class"] == role &&
                                ((ht.ContainsKey("label") &&
                                (string)ht["label"] != null &&
                                rx.Match((string)ht["label"]).Success) ||
                                ((ht.ContainsKey("key") &&
                                (string)ht["key"] != null &&
                                rx.Match((string)ht["key"]).Success))))
                            {
                                childList.Add(ht["key"]);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                    }
                    return childList.ToArray(typeof(string)) as string[];
                }
                if (role != null && role.Length > 0)
                {
                    AutomationElement childHandle = GetObjectHandle(windowHandle,
                        childName);
                    if (childHandle == null)
                    {
                        throw new XmlRpcFaultException(123,
                            "Unable to find child object: " + childName);
                    }
                    role = Regex.Replace(role, @" ", @"_");
                    InternalGetObjectList(walker.GetFirstChild(windowHandle),
                        ref objectList, ref objectHT, ref matchedKey,
                        true, null, windowHandle.Current.Name);
                    foreach (string key in objectHT.Keys)
                    {
                        try
                        {
                            if (debug)
                                LogMessage("Key: " + key);
                            Hashtable ht = (Hashtable)objectHT[key];
                            if ((string)ht["class"] == role)
                                childList.Add(ht["key"]);
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                    }
                    return childList.ToArray(typeof(string)) as string[];
                }
                if (childName != null && childName.Length > 0)
                {
                    AutomationElement childHandle = GetObjectHandle(windowHandle,
                        childName);
                    if (childHandle == null)
                    {
                        throw new XmlRpcFaultException(123,
                            "Unable to find child object: " + childName);
                    }
                    InternalGetObjectList(walker.GetFirstChild(childHandle),
                        ref objectList, ref objectHT, ref matchedKey,
                        true, null, windowHandle.Current.Name);
                    foreach (string key in objectHT.Keys)
                    {
                        try
                        {
                            if (debug)
                                LogMessage("Key: " + key);
                            Hashtable ht = (Hashtable)objectHT[key];
                            childList.Add(ht["key"]);
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                    }
                    return childList.ToArray(typeof(string)) as string[];
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
                objectHT = null;
                objectList = null;
            }
            throw new XmlRpcFaultException(123, "Unsupported parameter type passed");
        }
        [XmlRpcMethod("enterstring", Description = "Generate key event.")]
        public int EnterString(string windowName, string objName = null,
            string data = null)
        {
            if (objName != null && objName.Length > 0)
            {
                AutomationElement windowHandle = GetWindowHandle(windowName);
                if (windowHandle != null)
                {
                    AutomationElement childHandle = GetObjectHandle(windowHandle, objName);
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
        [XmlRpcMethod("generatekeyevent", Description = "Generate key event.")]
        public int GenerateKeyEvent(string data)
        {
            if (data == null || data.Length == 0)
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
                            if (!tmpKey.nonPrintKey ||
                                tmpKey.key == System.Windows.Input.Key.CapsLock)
                                // Release only nonPrintKey
                                // Caps lock will be released later
                                break;
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
                            !shiftKeyPressed);
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
                    // Release only nonPrintKey
                    // Caps lock will be released later
                    break;
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
        [XmlRpcMethod("keypress", Description = "Key press.")]
        public int KeyPress(string data)
        {
            if (data == null || data.Length == 0)
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
        [XmlRpcMethod("keyrelease", Description = "Key release.")]
        public int KeyRelease(string data)
        {
            if (data == null || data.Length == 0)
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
        [XmlRpcMethod("mouseleftclick", Description = "Mouse left click on an object.")]
        public int MouseLeftClick(String windowName, String objName)
        {
            return Click(windowName, objName);
        }
    }
}
