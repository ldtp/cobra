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
                        continue;
                    }
                    if (!appList.Contains(process.ProcessName))
                        // If added from the existing window list
                        // then ignore it
                        appList.Add(process.ProcessName);
                    element = w.walker.GetNextSibling(element);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                w = null;
                element = null;
            }
            return appList.ToArray(typeof(string)) as string[];
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
                List<AutomationElement> windowTmpList = new List<AutomationElement>();
                LogMessage("GetWindowList - Window list count: " +
                    utils.windowList.Count);
                try
                {
                    foreach (AutomationElement e in utils.windowList)
                    {
                        try
                        {
                            s = e.Current.Name;
                            LogMessage("Cached window name: " + s);
                            currObjInfo = objInfo.GetObjectType(e);
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
                        utils.windowList.Remove(e);
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                }
                // FIXME: Check whether resetting the ObjInfo is appropriate here
                objInfo = new ObjInfo(false);
                while (null != element)
                {
                    if (utils.windowList.IndexOf(element) != -1)
                    {
                        // As the window info already added to the windowArrayList
                        // let us not re-add it
                        LogMessage(element.Current.Name + " already in windowList");
                        element = w.walker.GetNextSibling(element);
                        continue;
                    }
                    s = element.Current.Name;
                    LogMessage("Window name: " + s);
                    currObjInfo = objInfo.GetObjectType(element);
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
                        c = element.FindAll(TreeScope.Children, condition);
                        foreach (AutomationElement e in c)
                        {
                            s = e.Current.Name;
                            currObjInfo = objInfo.GetObjectType(e);
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
            if (windowName == null || windowName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            string matchedKey = null;
            ObjInfo objInfo = new ObjInfo(false);
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
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
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
            AutomationElement childHandle;
            try
            {
                windowHandle.SetFocus();
                childHandle = utils.GetObjectHandle(windowHandle,
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
            finally
            {
                childHandle = windowHandle = null;
            }
            return 0;
        }
        public int StateEnabled(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement childHandle;
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                LogMessage("Unable to find window: " + windowName);
                return 0;
            }
            try
            {
                windowHandle.SetFocus();
                childHandle = utils.GetObjectHandle(windowHandle,
                    objName, null, false);
                if (childHandle == null)
                {
                    LogMessage("Unable to find Object: " + objName);
                    return 0;
                }
                if (utils.IsEnabled(childHandle))
                    return 1;
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
        public int HasState(String windowName, String objName,
           String state, int guiTimeOut = 0)
        {
            if (windowName == null || objName == null || windowName.Length == 0
                || objName.Length == 0 || state == null || state.Length == 0)
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
            windowHandle.SetFocus();
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle,
                objName, null, false);
            windowHandle = null;
            if (childHandle == null)
            {
                LogMessage("Unable to find Object: " + objName);
                return 0;
            }
            Object pattern;
            AutomationElementCollection c;
            try
            {
                if (!utils.IsEnabled(childHandle))
                {
                    // Let us not grab the focus which is enabled
                    // will be helpful during verification
                    LogMessage("childHandle.SetFocus");
                    childHandle.SetFocus();
                }
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
                            if (utils.IsEnabled(childHandle))
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
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle,
                objName, null, false);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            Object pattern;
            AutomationElementCollection c;
            ArrayList stateList = new ArrayList();
            try
            {
                if (utils.IsEnabled(childHandle))
                {
                    childHandle.SetFocus();
                }
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
                childHandle = null;
            }
        }
        public int Click(String windowName, String objName)
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
            windowHandle.SetFocus();
            ControlType[] type = new ControlType[9] { ControlType.Button,
                ControlType.CheckBox, ControlType.RadioButton,
                ControlType.SplitButton, ControlType.Menu, ControlType.ListItem,
                ControlType.MenuItem, ControlType.MenuBar, ControlType.Pane };
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
                windowHandle = null;
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
            finally
            {
                pattern = null;
                childHandle = null;
            }
            throw new XmlRpcFaultException(123, "Unable to perform action");
        }
        public int[] GetObjectSize(String windowName, String objName)
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            AutomationElement childHandle = utils.GetObjectHandle(windowHandle, objName);
            windowHandle = null;
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
            finally
            {
                childHandle = null;
            }
        }
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
            throw new XmlRpcFaultException(123, "Unable to find Object info: " + objName);
        }
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
        public string[] GetChild(String windowName, String childName = null,
            string role = null, string parentName = null)
        {
            if (windowName == null || windowName.Length == 0 ||
                (parentName == null && childName == null && role == null &&
                childName.Length == 0 && role.Length == 0 && parentName.Length == 0))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            Hashtable ht;
            string matchedKey = "";
            Hashtable objectHT = new Hashtable();
            ObjInfo objInfo = new ObjInfo(false);
            ArrayList childList = new ArrayList();
            ArrayList objectList = new ArrayList();
            InternalTreeWalker w;
            AutomationElement childHandle;
            AutomationElement windowHandle = utils.GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            w = new InternalTreeWalker();
            try
            {
                if (childName != null && childName.Length > 0 &&
                    role != null && role.Length > 0)
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
                        finally
                        {
                            rx = null;
                        }
                    }
                    return childList.ToArray(typeof(string)) as string[];
                }
                if (role != null && role.Length > 0)
                {
                    childHandle = utils.GetObjectHandle(windowHandle,
                        childName);
                    if (childHandle == null)
                    {
                        throw new XmlRpcFaultException(123,
                            "Unable to find child object: " + childName);
                    }
                    role = Regex.Replace(role, @" ", @"_");
                    utils.InternalGetObjectList(w.walker.GetFirstChild(windowHandle),
                        ref objectList, ref objectHT, ref matchedKey,
                        true, null, windowHandle.Current.Name);
                    foreach (string key in objectHT.Keys)
                    {
                        try
                        {
                            if (utils.debug)
                                LogMessage("Key: " + key);
                            ht = (Hashtable)objectHT[key];
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
                    childHandle = utils.GetObjectHandle(windowHandle,
                        childName);
                    if (childHandle == null)
                    {
                        throw new XmlRpcFaultException(123,
                            "Unable to find child object: " + childName);
                    }
                    utils.InternalGetObjectList(w.walker.GetFirstChild(childHandle),
                        ref objectList, ref objectHT, ref matchedKey,
                        true, null, windowHandle.Current.Name);
                    foreach (string key in objectHT.Keys)
                    {
                        try
                        {
                            if (utils.debug)
                                LogMessage("Key: " + key);
                            ht = (Hashtable)objectHT[key];
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
            if (windowName == null || windowName.Length == 0)
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
            if (objName == null || objName.Length == 0)
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
