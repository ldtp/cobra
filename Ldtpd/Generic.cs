/*
 * Cobra WinLDTP 3.0
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
using System.Windows;
using System.Collections;
using System.Diagnostics;
using CookComputing.XmlRpc;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    class Generic
    {
        Utils utils;
        public Generic(Utils utils)
        {
            this.utils = utils;
        }
        private void LogMessage(Object o)
        {
            utils.LogMessage(o);
        }
        private AutomationElement GetObjectHandle(string windowName,
            string objName, ControlType[] type)
        {
            return utils.GetObjectHandle(windowName, objName, type);
        }
        public string[] GetAppList()
        {
            Process process;
            ArrayList appList = new ArrayList();
            foreach (AutomationElement e in utils.windowList)
            {
                // Get a process using the process id.
                try
                {
                    process = Process.GetProcessById(e.Current.ProcessId);
                }
                catch
                {
                    continue;
                }
                appList.Add(process.ProcessName);
            }
            InternalTreeWalker w = new InternalTreeWalker();
            AutomationElement element = w.walker.GetFirstChild(AutomationElement.RootElement);
            try
            {
                while (null != element)
                {

                    // Get a process using the process id.
                    try
                    {
                        process = Process.GetProcessById(element.Current.ProcessId);
                    }
                    catch
                    {
                        process = null;
                        continue;
                    }
                    if (!appList.Contains(process.ProcessName))
                        // If added from the existing window list
                        // then ignore it
                        appList.Add(process.ProcessName);
                    process = null;
                    element = w.walker.GetNextSibling(element);
                }
                return appList.ToArray(typeof(string)) as string[];
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return appList.ToArray(typeof(string)) as string[];
            }
            finally
            {
                w = null;
                appList = null;
                element = null;
            }
        }
        public String[] GetWindowList()
        {
            int index;
            String s, actualString;
            ArrayList windowArrayList = new ArrayList();
            CurrentObjInfo currObjInfo;
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement element;
            InternalTreeWalker w = new InternalTreeWalker();
            element = w.walker.GetFirstChild(AutomationElement.RootElement);
            Condition condition = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Window);
            try
            {
                AutomationElementCollection c;
                // FIXME: Check whether resetting the ObjInfo is appropriate here
                objInfo = new ObjInfo(false);
                while (null != element)
                {
                    if (utils.windowList.IndexOf(element) == -1)
                        utils.windowList.Add(element);
                    s = element.Current.Name;
                    LogMessage("Window name: " + s);
                    currObjInfo = objInfo.GetObjectType(element);
                    if (String.IsNullOrEmpty(s))
                        actualString = currObjInfo.objType + currObjInfo.objCount;
                    else
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
                    try
                    {
                        c = element.FindAll(TreeScope.Subtree, condition);
                        foreach (AutomationElement e in c)
                        {
                            if (utils.windowList.IndexOf(e) == -1)
                                utils.windowList.Add(e);
                            s = e.Current.Name;
                            currObjInfo = objInfo.GetObjectType(e);
                            if (String.IsNullOrEmpty(s))
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
                    element = w.walker.GetNextSibling(element);
                }
                return windowArrayList.ToArray(typeof(string)) as string[];
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                w = null;
                windowArrayList = null;
            }
            // Unable to find window
            return null;
        }
        public String[] GetObjectList(String windowName)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            string matchedKey = null;
            Hashtable objectHT = new Hashtable();
            ArrayList objectList = new ArrayList();
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " +
                    windowName);
            }
            InternalTreeWalker walker = new InternalTreeWalker();
            utils.InternalGetObjectList(walker.walker.GetFirstChild(windowHandle),
                ref objectList, ref objectHT, ref matchedKey,
                true, null, windowHandle.Current.Name);
            if (utils.debug)
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
            walker = null;
            objectHT = null;
            windowHandle = null;
            try
            {
                return objectList.ToArray(typeof(string)) as string[];
            }
            finally
            {
                objectList = null;
            }
        }
        public int ObjectExist(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName, objName, null);
                if (childHandle != null)
                    return 1;
                LogMessage("Unable to find Object: " + objName);
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
        public int StateEnabled(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName, objName, null);
                if (utils.IsEnabled(childHandle))
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
        public int HasState(String windowName, String objName,
           String state, int guiTimeOut = 0)
        {
            Object pattern;
            AutomationElement childHandle;
            AutomationElementCollection c;
            try
            {
                childHandle = GetObjectHandle(windowName, objName, null);
                c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                if (c == null)
                {
                    LogMessage("Unable to get row count.");
                    return 0;
                }
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
                            if (utils.IsEnabled(childHandle, false))
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
                            if (utils.IsEnabled(childHandle) &&
                                childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                                out pattern))
                            {
                                // Assuming, if its enabled and has selection item pattern
                                // then its selectable
                                return 1;
                            }
                            break;
                        case "editable":
                            if (childHandle.TryGetCurrentPattern(ValuePattern.Pattern,
                                out pattern))
                            {
                                if (((ValuePattern)pattern).Current.IsReadOnly)
                                    return 0;
                                else
                                    return 1;
                            }
                            break;
                    }
                    if (guiTimeOut > 0)
                    {
                        // Wait a second and retry checking the state
                        utils.InternalWait(1);
                        guiTimeOut--;
                    }
                } while (guiTimeOut > 0);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                c = null;
                pattern = null;
                childHandle = null;
            }
            return 0;
        }
        public string[] GetAllStates(String windowName, String objName)
        {
            Object pattern;
            AutomationElement childHandle;
            AutomationElementCollection c;
            ArrayList stateList = new ArrayList();
            try
            {
                childHandle = GetObjectHandle(windowName, objName, null);
                c = childHandle.FindAll(TreeScope.Children,
                    Condition.TrueCondition);
                if (c == null)
                    throw new XmlRpcFaultException(123,
                        "Unable to get row count.");
                if (childHandle.Current.IsOffscreen == false)
                {
                    stateList.Add("visible");
                    stateList.Add("showing");
                }
                if (utils.IsEnabled(childHandle))
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
                        // FIXME:
                        //stateList.Add("checked");
                    }
                }
                if (utils.IsEnabled(childHandle) &&
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
            finally
            {
                c = null;
                pattern = null;
                stateList = null;
                childHandle = null;
            }
        }
        public int Click(String windowName, String objName)
        {
            ControlType[] type = new ControlType[11] { ControlType.Button,
                ControlType.CheckBox, ControlType.RadioButton,
                ControlType.SplitButton, ControlType.Menu, ControlType.ListItem,
                ControlType.MenuItem, ControlType.MenuBar, ControlType.Pane,
                ControlType.Hyperlink, ControlType.ToolBar };
            Object pattern = null;
            AutomationElement childHandle;
            try
            {
                childHandle = GetObjectHandle(windowName, objName, type);
                if (!utils.IsEnabled(childHandle))
                {
                    throw new XmlRpcFaultException(123,
                        "Object state is disabled");
                }
                try
                {
                    childHandle.SetFocus();
                }
                catch (Exception ex)
                {
                    // Have noticed exception with
                    // maximize / minimize button
                    LogMessage(ex);
                }
                if (childHandle.Current.ControlType == ControlType.Pane)
                {
                    // NOTE: Work around, as the pane doesn't seem to work
                    // with any actions. Noticed this window, when Windows
                    // Security Warning dialog pop's up
                    utils.InternalClick(childHandle);
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
                        utils.InternalClick(childHandle);
                    }
                    else
                    {
                        try
                        {
                            ((InvokePattern)pattern).Invoke();
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                            // Have noticed exception with
                            // maximize / minimize button
                            utils.InternalClick(childHandle);
                        }
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
            finally
            {
                type = null;
                pattern = null;
                childHandle = null;
            }
            throw new XmlRpcFaultException(123, "Unable to perform action");
        }
        public int[] GetObjectSize(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = utils.GetObjectHandle(windowName, objName);
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
            finally
            {
                childHandle = null;
            }
        }
        public String GetAccessKey(String windowName, String objName)
        {
            AutomationElement childHandle;
            try
            {
                childHandle = utils.GetObjectHandle(windowName, objName);
                if (String.IsNullOrEmpty(childHandle.Current.AccessKey))
                    throw new XmlRpcFaultException(123, "No access key associated");
                return childHandle.Current.AccessKey;
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
        public int[] GetWindowSize(String windowName)
        {
            AutomationElement windowHandle;
            try
            {
                windowHandle = utils.GetWindowHandle(windowName);
                if (windowHandle == null)
                {
                    throw new XmlRpcFaultException(123,
                        "Unable to find window: " + windowName);
                }
                Rect rect = windowHandle.Current.BoundingRectangle;
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
            finally
            {
                windowHandle = null;
            }
        }
        public string[] GetObjectInfo(String windowName, String objName)
        {
            if (String.IsNullOrEmpty(windowName) ||
                String.IsNullOrEmpty(objName))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            string matchedKey = "";
            ArrayList objectList = new ArrayList();
            Hashtable objectHT = new Hashtable();
            InternalTreeWalker w;
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            w = new InternalTreeWalker();
            Hashtable ht;
            ArrayList keyList = new ArrayList();
            try
            {
                if (utils.InternalGetObjectList(w.walker.GetFirstChild(windowHandle),
                    ref objectList, ref objectHT, ref matchedKey,
                    false, objName, windowHandle.Current.Name))
                {
                    LogMessage(objectHT.Count + " : " + objectList.Count);
                    LogMessage(objectList[objectList.Count - 1]);
                    ht = (Hashtable)objectHT[matchedKey];
                    if (ht != null)
                    {
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
                w = null;
                ht = null;
                keyList = null;
                windowHandle = null;
            }
            throw new XmlRpcFaultException(123,
                "Unable to find Object info: " + objName);
        }
        public string GetObjectProperty(String windowName, String objName,
            string property)
        {
            if (String.IsNullOrEmpty(windowName) ||
                String.IsNullOrEmpty(objName) ||
                String.IsNullOrEmpty(property))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            bool flag;
            string matchedKey = "";
            ArrayList objectList = new ArrayList();
            Hashtable objectHT = new Hashtable();
            InternalTreeWalker w;
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            w = new InternalTreeWalker();
            Hashtable ht;
            ArrayList childrenList;
            try
            {
                if (property == "children")
                    flag = true;
                else
                    flag = false;
                if (utils.InternalGetObjectList(w.walker.GetFirstChild(windowHandle),
                    ref objectList, ref objectHT, ref matchedKey,
                    flag, objName, windowHandle.Current.Name) || flag)
                {
                    if (utils.debug)
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
                    ht = (Hashtable)objectHT[matchedKey];
                    if (ht != null)
                    {
                        foreach (string key in ht.Keys)
                        {
                            LogMessage(key);
                            if (key == property)
                            {
                                if (property == "children")
                                {
                                    childrenList = (ArrayList)ht[key];
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
                w = null;
                ht = null;
                windowHandle = null;
            }
            throw new XmlRpcFaultException(123, "Unable to find Object property: " +
                property + " of object: " + objName);
        }
        public string[] GetObjectNameAtCoords(int waitTime = 0)
        {
            ObjInfo objInfo = new ObjInfo(false);
            CurrentObjInfo currObjInfo;
            ArrayList objectList = new ArrayList();
            String parentWindow = null;
            String objectString = null;
            System.Drawing.Point mouse;
            AutomationElement aimElement, parentElement, firstElement;
            InternalTreeWalker w = new InternalTreeWalker();
			utils.InternalWait(waitTime);
            try
            {
                mouse = System.Windows.Forms.Cursor.Position;
                aimElement = AutomationElement.FromPoint(new System.Windows.
                    Point(mouse.X, mouse.Y));
                if (aimElement == null ||
                    aimElement.Current.ClassName == "SysListView32" ||
                    aimElement.Current.ClassName == "Shell_TrayWnd" ||
                    aimElement.Current.ClassName == "MSTaskListWClass" ||
                    aimElement.Current.ControlType == ControlType.Window ||
                    aimElement.Current.ControlType == ControlType.TitleBar ||
                    w.walker.GetParent(aimElement).Current.Name == "Context")
                {
                    // If mouse is directly on desktop, taskbar, window, titlebar
                    // and context menu, no other controls, then return None
                    return new string[] { "None", "None" };
                }
                parentElement = aimElement;
                while (true)
                {
                    if (parentElement.Current.ControlType == ControlType.Window ||
                        parentElement.Current.ClassName == "Progman" ||
                        parentElement.Current.ClassName == "Shell_TrayWnd")
                    {
                        // Use window, desktop and taskbar as parent window
                        break;
                    }
                    parentElement = w.walker.GetParent(parentElement);
                }
                currObjInfo = objInfo.GetObjectType(parentElement);
                parentWindow = parentElement.Current.Name;
                if (parentWindow != null)
                    parentWindow = Regex.Replace(parentWindow, "( |\r|\n)", "");
                if (String.IsNullOrEmpty(parentWindow))
                {
                    // txt0, txt1
                    parentWindow = currObjInfo.objType + currObjInfo.objCount;
                }
                else
                {
                    // txtName, txtPassword
                    parentWindow = currObjInfo.objType + parentWindow;
                }
                firstElement = w.walker.GetFirstChild(parentElement);
                if (firstElement == null)
                {
                    LogMessage("Can not get object on window");
                    return new string[] { "None", "None" };
                }
                GetObjectName(firstElement, ref objectList, aimElement, ref objectString);
                return new string[] { parentWindow, objectString };
            }
            catch (ElementNotAvailableException ex)
            {
                LogMessage("Exception: " + ex);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                w = null;
                objectList = null;
            }
            throw new XmlRpcFaultException(123, "Unable to get object name");
        }
        public bool GetObjectName(AutomationElement firstElement, ref ArrayList objectList,
            AutomationElement aimElement, ref string actualString)
        {
            if (firstElement == null)
            {
                LogMessage("Invalid object handle");
                return false;
            }
            CurrentObjInfo currObjInfo;
            ObjInfo objInfo = new ObjInfo(false);
            InternalTreeWalker w = new InternalTreeWalker();
            int index;
            string s = null;
            AutomationElement childElement;
            try
            {
                childElement = firstElement;
                while (childElement != null)
                {
                    s = childElement.Current.Name;
                    currObjInfo = objInfo.GetObjectType(childElement);
                    if (s == null)
                    {
                        LogMessage("Current object Name is null");
                    }
                    else
                    {
                        s = s.ToString();
                        if (true)
                            LogMessage("Obj name: " + s + " : " +
                                childElement.Current.ControlType.ProgrammaticName);
                    }
                    if (currObjInfo.objType != null)
                    {
                        if (s != null)
                            s = Regex.Replace(s, @"( |\t|:|\.|_|\r|\n|<|>)", "");
                        if (String.IsNullOrEmpty(s))
                        {
                            // txt0, txt1
                            actualString = currObjInfo.objType +
                                currObjInfo.objCount;
                        }
                        else
                        {
                            // txtName, txtPassword
                            actualString = currObjInfo.objType + s;
                            LogMessage("###" + actualString + "###");
                            index = 1;
                            while (true)
                            {
                                if (objectList.IndexOf(actualString) < 0)
                                {
                                    // Object doesn't exist, assume this is the first
                                    // element with the name and type
                                    break;
                                }
                                actualString = currObjInfo.objType + s + index;
                                index++;
                            }
                        }
                        objectList.Add(actualString);
                    }
                    if (aimElement == childElement && aimElement.Current.BoundingRectangle ==
                        childElement.Current.BoundingRectangle)
                    {
                        // Different controls on toolbar have same handle, probably it is a
                        // underlying bug with toolbar, so also use BoundingRectangle here
                        return true;
                    }
                    // If any subchild exist for the current element navigate to it
                    AutomationElement subChild = w.walker.GetFirstChild(childElement);
                    if (subChild != null)
                    {
                        if (GetObjectName(subChild, ref objectList, aimElement,
                            ref actualString))
                            return true;
                    }
                    childElement = w.walker.GetNextSibling(childElement);
                }
                // If aimElement is not found, set actualString as None
                actualString = "None";
            }
            catch (ElementNotAvailableException ex)
            {
                LogMessage("Exception: " + ex);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                w = null;
            }
            return false;
        }
        public string[] GetChild(String windowName, String childName = null,
            string role = null, string parentName = null)
        {
            if (String.IsNullOrEmpty(windowName) ||
                (String.IsNullOrEmpty(parentName) &&
                String.IsNullOrEmpty(childName) &&
                String.IsNullOrEmpty(role)))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            AutomationElement childHandle;
            if (!String.IsNullOrEmpty(childName))
            {
                childHandle = utils.GetObjectHandle(windowHandle, childName);
                if (childHandle == null)
                {
                    throw new XmlRpcFaultException(123,
                        "Unable to find child object: " + childName);
                }
            }
            if (!String.IsNullOrEmpty(role))
                role = Regex.Replace(role, @" ", @"_");
            Hashtable ht;
            string matchedKey = "";
            Hashtable objectHT = new Hashtable();
            ArrayList childList = new ArrayList();
            ArrayList objectList = new ArrayList();
            InternalTreeWalker w = new InternalTreeWalker();
            try
            {
                if (!String.IsNullOrEmpty(childName) ||
                    !String.IsNullOrEmpty(role))
                {
                    utils.InternalGetObjectList(w.walker.GetFirstChild(windowHandle),
                        ref objectList, ref objectHT, ref matchedKey,
                        true, childName, windowHandle.Current.Name);
                    Regex rx;
                    foreach (string key in objectHT.Keys)
                    {
                        try
                        {
                            if (utils.debug)
                                LogMessage("Key: " + key);
                            ht = (Hashtable)objectHT[key];
                            String tmp = Regex.Replace(childName, @"\*", @".*");
                            tmp = Regex.Replace(tmp, " ", "");
                            tmp = Regex.Replace(tmp, @"\(", @"\(");
                            tmp = Regex.Replace(tmp, @"\)", @"\)");
                            rx = new Regex(tmp, RegexOptions.Compiled |
                                RegexOptions.IgnorePatternWhitespace |
                                RegexOptions.Multiline |
                                RegexOptions.CultureInvariant);
                            if (utils.debug)
                            {
                                LogMessage("Role matched: " +
                                    (string)ht["class"] == role);
                                if (ht.ContainsKey("label") &&
                                    (string)ht["label"] != null)
                                    LogMessage("Label matched: " +
                                        rx.Match((string)ht["label"]).Success);
                            }
                            if ((String.IsNullOrEmpty(role) ||
                                (!String.IsNullOrEmpty(role) &&
                                (string)ht["class"] == role)) &&
                                ((ht.ContainsKey("label") &&
                                (string)ht["label"] != null &&
                                rx.Match((string)ht["label"]).Success) ||
                                (ht.ContainsKey("key") &&
                                (string)ht["key"] != null &&
                                rx.Match((string)ht["key"]).Success)))
                            {
                                childList.Add(ht["key"]);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                        }
                        finally
                        {
                            rx = null;
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
                w = null;
                ht = objectHT = null;
                childHandle = windowHandle = null;
                childList = objectList = null;
            }
            throw new XmlRpcFaultException(123,
                "Unsupported parameter type passed");
        }
        public int GrabFocus(String windowName, String objName = null)
        {
            if (String.IsNullOrEmpty(windowName))
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
            if (String.IsNullOrEmpty(objName))
            {
                // If objName is not provided, just grab window focus
                windowHandle.SetFocus();
                return 1;
            }
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle,
                objName);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
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
            finally
            {
                childHandle = null;
            }
        }
    }
}
