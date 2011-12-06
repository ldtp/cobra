/*
WinLDTP 1.0

@author: Nagappan Alagappan <nalagappan@vmware.com>
@copyright: Copyright (c) 2011 VMware Inc.,
@license: LGPL

http://ldtp.freedesktop.org

This file may be distributed and/or modified under the terms of the GNU General
Public License version 2 as published by the Free Software Foundation. This file
is distributed without any warranty; without even the implied warranty of
merchantability or fitness for a particular purpose.

See 'README.txt' in the source distribution for more information.

Headers in this file shall remain intact.
*/
using System;
using System.Collections.Generic;

using ATGTestInput;
using System.Windows;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using CookComputing.XmlRpc;
using System.Globalization;
using System.Windows.Forms;using System.Windows.Automation;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    public struct CurrentObjInfo
    {
        public string objType;
        public int objCount;
        public CurrentObjInfo(String objType, int objCount)
        {
            this.objType = objType;
            this.objCount = objCount;
        }
    }
    public struct KeyInfo
    {
        public System.Windows.Input.Key key;
        public bool shift;
        public bool nonPrintKey;
        public KeyInfo(System.Windows.Input.Key key, bool shift,
            bool nonPrintKey = false)
        {
            this.key = key;
            this.shift = shift;
            this.nonPrintKey = nonPrintKey;
        }
    }
    public struct ObjInfo
    {
        public int cbo, txt, btn, rbtn, chk, mnu;
        public int lbl, slider, ukn, lst, frm, header, headeritem, dlg;
        public int tab, tabitem, tbar, tree, tblc, tbl;
        public ObjInfo(bool dummyValue)
        {
            cbo = txt = btn = rbtn = chk = mnu = 0;
            lbl = slider = ukn = lst = frm = header = headeritem = 0;
            tab = tabitem = tbar = tree = tblc = tbl = dlg = 0;
        }
        public CurrentObjInfo GetObjectType(AutomationElement e, ControlType type)
        {
            if (type == ControlType.Edit ||
                type == ControlType.Document)
                return new CurrentObjInfo("txt", txt++);
            else if (type == ControlType.Text)
                return new CurrentObjInfo("lbl", lbl++);
            else if (type == ControlType.ComboBox)
                return new CurrentObjInfo("cbo", cbo++);
            else if (type == ControlType.Button)
                return new CurrentObjInfo("btn", btn++);
            else if (type == ControlType.RadioButton)
                return new CurrentObjInfo("rbtn", rbtn++);
            else if (type == ControlType.CheckBox)
                return new CurrentObjInfo("chk", chk++);
            else if (type == ControlType.Slider ||
                type == ControlType.Spinner)
                return new CurrentObjInfo("sldr", slider++);
            else if (type == ControlType.Menu || type == ControlType.MenuBar ||
                type == ControlType.MenuItem)
                return new CurrentObjInfo("mnu", mnu++);
            else if (type == ControlType.List || type == ControlType.ListItem)
                return new CurrentObjInfo("lst", lst++);
            else if (type == ControlType.Window)
            {
                if (e.Current.LocalizedControlType == "dialog")
                    // Might need a fix for other languages: Ex: French / Germany
                    // as the localized control name could be different than dialog
                    return new CurrentObjInfo("dlg", dlg++);
                else
                    return new CurrentObjInfo("frm", frm++);
            }
            else if (type == ControlType.Header)
                return new CurrentObjInfo("hdr", header++);
            else if (type == ControlType.HeaderItem)
                return new CurrentObjInfo("hdri", headeritem++);
            else if (type == ControlType.ToolBar)
                return new CurrentObjInfo("tbar", tbar++);
            else if (type == ControlType.Tree)
                return new CurrentObjInfo("tree", tree++);
            else if (type == ControlType.TreeItem)
                // For Linux compatibility
                return new CurrentObjInfo("tblc", tblc++);
            else if (type == ControlType.Tab)
                // For Linux compatibility
                return new CurrentObjInfo("ptl", tab++);
            else if (type == ControlType.TabItem)
                // For Linux compatibility
                return new CurrentObjInfo("ptab", tabitem++);
            else if (type == ControlType.Table)
                // For Linux compatibility
                return new CurrentObjInfo("tbl", tbl++);
            return new CurrentObjInfo("ukn", ukn++);
        }
    }
    public class Utils : XmlRpcListenerService
    {
        public bool debug = false;
        protected int objectTimeOut = 5;
        protected TreeWalker walker = null;
        protected bool shiftKeyPressed = false;
        protected Stack logStack = new Stack();
        protected List<AutomationElement> windowList;
        public Utils()
        {
            // Ignore Ldtpd from list of applications
            Condition condition1 = new PropertyCondition(AutomationElement.ProcessIdProperty,
                Process.GetCurrentProcess().Id);
            Condition condition2 = new AndCondition(new Condition[] {
                System.Windows.Automation.Automation.ControlViewCondition,
                new NotCondition(condition1)});
            walker = new TreeWalker(condition2);
            windowList = new List<AutomationElement>();
            /*
            http://stackoverflow.com/questions/3144751/why-is-this-net-uiautomation-app-leaking-pooling
            Automation.AddStructureChangedEventHandler(AutomationElement.RootElement,
                TreeScope.Subtree,
                new StructureChangedEventHandler(OnStructureChanged));
             * Let us not use this, as its leaking memory, tested on Windows XP SP3
             * Windows 7 SP1
            /* */
            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent,
                AutomationElement.RootElement, TreeScope.Subtree,
                new AutomationEventHandler(ref this.OnWindowCreate));
            Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent,
                AutomationElement.RootElement, TreeScope.Subtree,
                new AutomationEventHandler(ref this.OnWindowDelete));
        }
        ~Utils()
        {
            Automation.RemoveAllEventHandlers();
        }
        protected KeyInfo GetKey(string key)
        {
            try
            {
                switch (key)
                {
                    case "ctrl":
                    case "ctrll":
                        return new KeyInfo(System.Windows.Input.Key.LeftCtrl, false, true);
                    case "ctrlr":
                        return new KeyInfo(System.Windows.Input.Key.RightCtrl, false, true);
                    case "caps":
                    case "capslock":
                        return new KeyInfo(System.Windows.Input.Key.CapsLock, false, true);
                    case "pgdown":
                        return new KeyInfo(System.Windows.Input.Key.PageDown, false);
                    case "alt":
                    case "altl":
                        return new KeyInfo(System.Windows.Input.Key.LeftAlt, false, true);
                    case "altr":
                        return new KeyInfo(System.Windows.Input.Key.RightAlt, false, true);
                    case "shift":
                    case "shiftl":
                        shiftKeyPressed = true;
                        return new KeyInfo(System.Windows.Input.Key.LeftShift, true, true);
                    case "shiftr":
                        shiftKeyPressed = true;
                        return new KeyInfo(System.Windows.Input.Key.RightShift, true, true);
                    case "esc":
                    case "escape":
                        return new KeyInfo(System.Windows.Input.Key.Escape, false);
                    case "bksp":
                    case "backspace":
                        return new KeyInfo(System.Windows.Input.Key.Back, false);
                    case "tab":
                        return new KeyInfo(System.Windows.Input.Key.Tab, false);
                    case "windowskey":
                    case "windowskeyl":
                        return new KeyInfo(System.Windows.Input.Key.LWin, false);
                    case "windowskeyr":
                        return new KeyInfo(System.Windows.Input.Key.RWin, false);
                    case " ":
                        return new KeyInfo(System.Windows.Input.Key.Space, false);
                    case "<":
                        return new KeyInfo(System.Windows.Input.Key.OemComma, true);
                    case ">":
                        return new KeyInfo(System.Windows.Input.Key.OemPeriod, true);
                    case "'":
                        return new KeyInfo(System.Windows.Input.Key.OemQuotes, false);
                    case "\"":
                        return new KeyInfo(System.Windows.Input.Key.OemQuotes, true);
                    case "!":
                        return new KeyInfo(System.Windows.Input.Key.D1, true);
                    case "@":
                        return new KeyInfo(System.Windows.Input.Key.D2, true);
                    case "#":
                        return new KeyInfo(System.Windows.Input.Key.D3, true);
                    case "$":
                        return new KeyInfo(System.Windows.Input.Key.D4, true);
                    case "%":
                        return new KeyInfo(System.Windows.Input.Key.D5, true);
                    case "^":
                        return new KeyInfo(System.Windows.Input.Key.D6, true);
                    case "&":
                        return new KeyInfo(System.Windows.Input.Key.D7, true);
                    case "*":
                        return new KeyInfo(System.Windows.Input.Key.D8, true);
                    case "(":
                        return new KeyInfo(System.Windows.Input.Key.D9, true);
                    case ")":
                        return new KeyInfo(System.Windows.Input.Key.D0, true);
                    case "_":
                        return new KeyInfo(System.Windows.Input.Key.Subtract, true);
                    case "+":
                        return new KeyInfo(System.Windows.Input.Key.Add, true);
                    default:
                        bool shift = key.Length == 1 ?
                            Regex.Match(key, @"[A-Z]", RegexOptions.None).Success : false;
                        System.Windows.Input.KeyConverter k = new System.Windows.Input.KeyConverter();
                        System.Windows.Input.Key mykey = (System.Windows.Input.Key)k.ConvertFromString(key);
                        LogMessage(shift);
                        return new KeyInfo(mykey, shift);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            throw new XmlRpcFaultException(123, "Unsupported key type: " + key);
        }
        protected KeyInfo[] GetKeyVal(string data)
        {
            int index = 0;
            string token;
            int maxTokenSize = 15;
            ArrayList keyList = new ArrayList();
            while (index < data.Length)
            {
                token = "";
                if (data[index].ToString().Equals("<"))
                {
                    index++;
                    int i = 0;
                    while (!data[index].ToString().Equals(">") && i < maxTokenSize)
                    {
                        token += data[index++];
                        i++;
                    }
                    if (!data[index].ToString().Equals(">"))
                        // Premature end of string without an opening '<'
                        throw new XmlRpcFaultException(123,
                            "Premature end of string without an opening '<'.");
                    index++;
                }
                else
                {
                    token = data[index++].ToString();
                }
                LogMessage(token);
                keyList.Add(GetKey(token));
            }
            return keyList.ToArray(typeof(KeyInfo))
                as KeyInfo[];
        }
        public void LogMessage(Object o)
        {
            if (debug)
                Console.WriteLine(o);
            logStack.Push("INFO-" + o);
        }
        private void CleanUpWindowElements()
        {
            /*
             * Clean up handles that no longer exist
             * */
            List<AutomationElement> windowTmpList = new List<AutomationElement>();
            try
            {
                foreach (AutomationElement el in windowList)
                {
                    try
                    {
                        LogMessage(el.Current.Name);
                    }
                    catch (ElementNotAvailableException ex)
                    {
                        // Don't alter the current list, remove it later
                        windowTmpList.Add(el);
                        LogMessage(ex);
                    }
                    catch (Exception ex)
                    {
                        // Don't alter the current list, remove it later
                        windowTmpList.Add(el);
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
                foreach (AutomationElement el in windowTmpList)
                    // Remove element from the list
                    windowList.Remove(el);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            // With GC collect, noticed very less memory being used all the time
            GC.Collect();
        }
        private void OnWindowCreate(object sender, AutomationEventArgs e)
        {
            /*
             * Add all newly created window handle to the list on window create event
             * */
            try
            {
                AutomationElement element;
                element = sender as AutomationElement;
                if (e.EventId == WindowPattern.WindowOpenedEvent)
                {
                    if (element != null &&
                        element.Current.Name != null &&
                        element.Current.Name.Length > 0)
                    {
                        // Add window handle that have name !
                        int[] rid = element.GetRuntimeId();
                        LogMessage("Added: " +
                            element.Current.ControlType.ProgrammaticName +
                            " : " + element.Current.Name + " : " + rid);
                        if (windowList.IndexOf(element) == -1)
                            windowList.Add(element);
                        LogMessage("Window list count: " +
                            this.windowList.Count);
                    }
                }
            }
            catch (Exception)
            {
                // Do nothing
            }
            Thread thread = new Thread(new ThreadStart(CleanUpWindowElements));
            // Clean up in different thread
            thread.Start();
        }
        private void OnWindowDelete(object sender, AutomationEventArgs e)
        {
            /*
             * Delete window handle that exist in the list on window close event
             * */
            try
            {
                AutomationElement element;
                element = sender as AutomationElement;
                if (e.EventId == WindowPattern.WindowClosedEvent)
                {
                    if (element != null &&
                        element.Current.Name != null)
                    {
                        int[] rid = element.GetRuntimeId();
                        LogMessage("Removed: " +
                            element.Current.ControlType.ProgrammaticName +
                            " : " + element.Current.Name + " : " + rid);
                        if (windowList.IndexOf(element) != -1)
                            this.windowList.Remove(element);
                        LogMessage("Removed - Window list count: " +
                            this.windowList.Count);
                    }
                }
            }
            catch (Exception)
            {
                // Since window list is added / removed in different thread
                // values of windowList might be altered and an exception is thrown
                // Just handle the global exception
            }
            Thread thread = new Thread(new ThreadStart(CleanUpWindowElements));
            // Clean up in different thread
            thread.Start();
        }
        private AutomationElement InternalGetWindowHandle(String windowName)
        {
            String s;
            int index;
            String actualString;
            AutomationElement element;
            CurrentObjInfo currObjInfo;
            ObjInfo objInfo = new ObjInfo(false);
            ArrayList objectList = new ArrayList();
            // Trying to mimic python fnmatch.translate
            String tmp = Regex.Replace(windowName, @"\*", @".*");
            tmp = Regex.Replace(tmp, " ", "");
            //tmp += @"\Z(?ms)";
            Regex rx = new Regex(tmp, RegexOptions.Compiled |
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                RegexOptions.CultureInvariant);
            List<AutomationElement> windowTmpList = new List<AutomationElement>();
            try
            {
                foreach (AutomationElement e in windowList)
                {
                    try
                    {
                        currObjInfo = objInfo.GetObjectType(e, e.Current.ControlType);
                        s = e.Current.Name;
                        if (s != null)
                            s = (new Regex(" ")).Replace(s, "");
                        if (s == null || s.Length == 0)
                        {
                            // txt0, txt1
                            actualString = currObjInfo.objType + currObjInfo.objCount;
                        }
                        else
                        {
                            // txtName, txtPassword
                            actualString = currObjInfo.objType + s;
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
                        LogMessage("Window: " + actualString + " : " + tmp);
                        objectList.Add(actualString);
                        // FIXME: Handle dlg0 as in Linux
                        if ((s != null && rx.Match(s).Success) ||
                            rx.Match(actualString).Success)
                        {
                            LogMessage(windowName + " - Window found");
                            return e;
                        }
                    }
                    catch (ElementNotAvailableException ex)
                    {
                        // Don't alter the current list, remove it later
                        windowTmpList.Add(e);
                        LogMessage(ex);
                    }
                    catch (Exception ex)
                    {
                        // Don't alter the current list, remove it later
                        windowTmpList.Add(e);
                        LogMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            try
            {
                foreach (AutomationElement e in windowTmpList)
                    // Remove element from the list
                    windowList.Remove(e);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            objInfo = new ObjInfo(false);
            element = walker.GetFirstChild(AutomationElement.RootElement);
            try
            {
                while (null != element)
                {
                    currObjInfo = objInfo.GetObjectType(element, element.Current.ControlType);
                    s = element.Current.Name;
                    if (s != null)
                        s = (new Regex(" ")).Replace(s, "");
                    if (s == null || s == "")
                    {
                        // txt0, txt1
                        actualString = currObjInfo.objType + currObjInfo.objCount;
                    }
                    else
                    {
                        // txtName, txtPassword
                        actualString = currObjInfo.objType + s;
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
                    LogMessage("Window: " + actualString + " : " + tmp);
                    objectList.Add(actualString);
                    // FIXME: Handle dlg0 as in Linux
                    if ((s != null && rx.Match(s).Success) || rx.Match(actualString).Success)
                    {
                        LogMessage(windowName + " - Window found");
                        return element;
                    }
                    element = walker.GetNextSibling(element);
                }
                element = walker.GetFirstChild(AutomationElement.RootElement);
                // Reset object info
                objInfo = new ObjInfo(false);
                objectList.Clear();
                while (null != element)
                {
                    AutomationElement subChild = walker.GetFirstChild(element);
                    while (subChild != null)
                    {
                        if (subChild.Current.Name != null)
                        {
                            currObjInfo = objInfo.GetObjectType(subChild, subChild.Current.ControlType);
                            s = subChild.Current.Name;
                            if (s != null)
                                s = (new Regex(" ")).Replace(s, "");
                            if (s == null || s == "")
                            {
                                // txt0, txt1
                                actualString = currObjInfo.objType + currObjInfo.objCount;
                            }
                            else
                            {
                                // txtName, txtPassword
                                actualString = currObjInfo.objType + s;
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
                            LogMessage("SubWindow: " + actualString + " : " + tmp);
                            if ((s != null && rx.Match(s).Success) || rx.Match(actualString).Success)
                            {
                                LogMessage(windowName + " - Window found");
                                return subChild;
                            }
                        }
                        subChild = walker.GetNextSibling(subChild);
                    }
                    element = walker.GetNextSibling(element);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            // Unable to find window
            return null;
        }
        protected AutomationElement GetWindowHandle(String windowName,
            bool waitForObj = true)
        {
            AutomationElement o = null;
            int retry = waitForObj ? objectTimeOut : 1;
            // For debugging use the following value
            //int retry = waitForObj ? 1 : 1;
            for (int i = 0; i < retry; i++)
            {
                o = InternalGetWindowHandle(windowName);
                if (o != null)
                    return o;
            }
            return o;
        }
        protected AutomationElement GetObjectHandle(AutomationElement e, String objName)
        {
            return GetObjectHandle(e, objName, null, true);
        }
        protected AutomationElement GetObjectHandle(AutomationElement e,
            String objName, ControlType[] type, bool waitForObj)
        {
            AutomationElement o = null;
            if (e == null)
            {
                LogMessage("Child handle NULL");
                return null;
            }
            ObjInfo objInfo = new ObjInfo(false);
            int retry = waitForObj ? objectTimeOut : 1;
            // For debugging use the following value
            //int retry = waitForObj ? 1 : 1;
            for (int i = 0; i < retry; i++)
            {
                ArrayList objectList = new ArrayList();
                try
                {
                    o = InternalGetObjectHandle(walker.GetFirstChild(e), objName,
                        type, ref objectList);
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                    o = null;
                }
                if (o != null)
                    return o;
                // Wait 1 second, rescan for object
                Thread.Sleep(1000);
            }
            return o;
        }
        private AutomationElement InternalGetObjectHandle(AutomationElement childHandle,
            String objName, ControlType[] type, ref ArrayList objectList)
        {
            if (childHandle == null)
            {
                LogMessage("Child handle NULL");
                return null;
            }
            int index;
            String s = null;
            String actualString1 = null;
            ObjInfo objInfo = new ObjInfo(false);
            AutomationElement element;
            CurrentObjInfo currObjInfo;
            /*
            Condition condition1 = new PropertyCondition(AutomationElement.ProcessIdProperty,
                Process.GetCurrentProcess().Id);
            Condition condition2 = new OrCondition(new Condition[] { Automation.ControlViewCondition,
                Automation.ContentViewCondition, new NotCondition(condition1) });
            /**/
            // Trying to mimic python fnmatch.translate
            String tmp = Regex.Replace(objName, @"\*", @".*");
            tmp = Regex.Replace(tmp, " ", "");
            tmp = Regex.Replace(tmp, @"\(", @"\(");
            tmp = Regex.Replace(tmp, @"\)", @"\)");
            // This fails for some reason, commenting out for now
            //tmp += @"\Z(?ms)";
            Regex rx = new Regex(tmp, RegexOptions.Compiled |
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                RegexOptions.CultureInvariant);
            try
            {
                element = childHandle;
                while (null != element)
                {
                    s = element.Current.Name;
                    currObjInfo = objInfo.GetObjectType(element, element.Current.ControlType);
                    if (s == null)
                    {
                        LogMessage("Current object Name is null");
                    }
                    else
                    {
                        s = s.ToString();
                        if (debug)
                            LogMessage("Obj name: " + s + " : " +
                                element.Current.ControlType.ProgrammaticName);
                    }
                    actualString1 = null;
                    if (currObjInfo.objType != null)
                    {
                        if (s != null)
                            s = (new Regex(" ")).Replace(s, "");
                        if (s == null || s.Length == 0)
                        {
                            // txt0, txt1
                            actualString1 = currObjInfo.objType + currObjInfo.objCount;
                        }
                        else
                        {
                            // txtName, txtPassword
                            actualString1 = currObjInfo.objType + s;
                            index = 1;
                            while (true)
                            {
                                if (objectList.IndexOf(actualString1) < 0)
                                {
                                    // Object doesn't exist, assume this is the first
                                    // element with the name and type
                                    break;
                                }
                                actualString1 = currObjInfo.objType + s + index;
                                index++;
                            }
                        }
                        if (debug)
                        {
                            LogMessage(objName + " : " + actualString1 + " : " + s + " : " +
                                tmp);
                            LogMessage((s != null && rx.Match(s).Success) + " : " +
                                (actualString1 != null && rx.Match(actualString1).Success));
                        }
                        objectList.Add(actualString1);
                    }
                    if ((s != null && rx.Match(s).Success) ||
                        (actualString1 != null && rx.Match(actualString1).Success))
                    {
                        if (type == null)
                            return element;
                        else
                        {
                            foreach (ControlType t in type)
                            {
                                if (debug)
                                    LogMessage((t == element.Current.ControlType) +
                                        " : " + element.Current.ControlType.ProgrammaticName);
                                if (t == element.Current.ControlType)
                                    return element;
                            }
                            LogMessage("type doesn't match !!!!!!!!!!!!!!");
                        }
                    }
                    // If any subchild exist for the current element navigate to it
                    AutomationElement subChild = InternalGetObjectHandle(walker.GetFirstChild(element),
                        objName, type, ref objectList);
                    if (subChild != null)
                    {
                        // Object found, don't loop further
                        return subChild;
                    }
                    element = walker.GetNextSibling(element);
                }
            }
            catch (ElementNotAvailableException ex)
            {
                LogMessage(ex);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return null;
        }
        protected bool InternalGetObjectList(AutomationElement windowHandle,
            ref ArrayList objectList, ref Hashtable objectHT,
            ref string matchedKey, bool needAll = false,
            string objName = null, string parentName = null)
        {
            int index;
            Regex rx = null;
            String s = null;
            ObjInfo objInfo = new ObjInfo(false);
            String actualString1 = null;
            CurrentObjInfo currObjInfo;
            Hashtable propertyHT;
            // Trying to mimic python fnmatch.translate
            String tmp = null;
            if (objName != null)
            {
                tmp = Regex.Replace(objName, @"\*", @".*");
                tmp = Regex.Replace(tmp, " ", "");
                tmp = Regex.Replace(tmp, @"\(", @"\(");
                tmp = Regex.Replace(tmp, @"\)", @"\)");
                //tmp += @"\Z(?ms)";
                rx = new Regex(tmp, RegexOptions.Compiled |
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                    RegexOptions.CultureInvariant);
            }
            try
            {
                AutomationElement element = windowHandle;
                while (null != element)
                {
                    s = element.Current.Name;
                    currObjInfo = objInfo.GetObjectType(element, element.Current.ControlType);
                    if (s == null)
                    {
                        LogMessage("Current object Name is null");
                    }
                    else
                    {
                        s = s.ToString();
                        if (debug)
                            LogMessage("Obj name: " + s + " : " +
                                element.Current.ControlType.ProgrammaticName);
                    }
                    actualString1 = null;
                    if (s != null)
                        s = (new Regex(" ")).Replace(s, "");
                    if (s == null || s.Length == 0)
                    {
                        // txt0, txt1
                        actualString1 = currObjInfo.objType + currObjInfo.objCount;
                    }
                    else
                    {
                        // txtName, txtPassword
                        actualString1 = currObjInfo.objType + s;
                        index = 1;
                        while (true)
                        {
                            if (objectList.IndexOf(actualString1) < 0)
                            {
                                // Object doesn't exist, assume this is the first
                                // element with the name and type
                                break;
                            }
                            actualString1 = currObjInfo.objType + s + index;
                            index++;
                        }
                    }
                    objectList.Add(actualString1);
                    if (objName != null || needAll)
                    {
                        //needAll - Required for GetChild
                        propertyHT = new Hashtable();
                        propertyHT.Add("key", actualString1);
                        try
                        {
                            LogMessage(element.Current.LocalizedControlType);
                            string className = element.Current.LocalizedControlType;
                            if (element.Current.ControlType == ControlType.Button)
                                // For LDTP compatibility
                                className = "push_button";
                            propertyHT.Add("class",
                                Regex.Replace(className, " ", "_"));
                        }
                        catch (Exception ex)
                        {
                            LogMessage(ex);
                            propertyHT.Add("class",
                                element.Current.ControlType.ProgrammaticName);
                        }
                        propertyHT.Add("parent", parentName);
                        //propertyHT.Add("child_index", element.Current);
                        ArrayList childrenList = new ArrayList();
                        propertyHT.Add("children", childrenList);
                        // Check if parent exists
                        if (parentName != null && parentName.Length > 0 &&
                            objectHT[parentName] != null)
                        {
                            LogMessage("parentName NOT NULL");
                            // Add current child to the parent
                            ((ArrayList)((Hashtable)objectHT[parentName])["children"]).Add(actualString1);
                        }
                        else
                            LogMessage("parentName NULL");
                        propertyHT.Add("obj_index",
                            currObjInfo.objType + "#" + currObjInfo.objCount);
                        if (s != null)
                            propertyHT.Add("label", element.Current.Name);
                        // Following 2 properties exist in Linux
                        //propertyHT.Add("label_by", s == null ? "" : s);
                        //propertyHT.Add("description", element.Current.DescribedBy);
                        if (element.Current.AcceleratorKey != "")
                            propertyHT.Add("key_binding",
                                element.Current.AcceleratorKey);
                        // Add individual property to object property
                        objectHT.Add(actualString1, propertyHT);
                        if (debug && rx != null)
                        {
                            LogMessage(objName + " : " + actualString1 + " : " + s
                                + " : " + tmp);
                            LogMessage((s != null && rx.Match(s).Success) + " : " +
                                (actualString1 != null && rx.Match(actualString1).Success));
                        }
                        if ((s != null && rx != null && rx.Match(s).Success) ||
                        (actualString1 != null && rx != null && rx.Match(actualString1).Success))
                        {
                            matchedKey = actualString1;
                            LogMessage("String matched: " + needAll);
                            if (!needAll)
                                return true;
                        }
                    }

                    // If any subchild exist for the current element navigate to it
                    if (InternalGetObjectList(walker.GetFirstChild(element),
                        ref objectList, ref objectHT, ref matchedKey,
                        needAll, objName, actualString1))
                        return true;
                    element = walker.GetNextSibling(element);
                }
            }
            catch (ElementNotAvailableException ex)
            {
                LogMessage("Exception: " + ex);
            }
            catch (Exception ex)
            {
                LogMessage("Exception: " + ex);
            }
            return false;
        }
        protected bool SelectListItem(AutomationElement element, String itemText)
        {
            if (element == null || itemText == null || itemText.Length == 0)
            {
                throw new XmlRpcFaultException(123,
                    "Argument cannot be null or empty.");
            }
            element.SetFocus();
            AutomationElement elementItem;
            try
            {
                elementItem = GetObjectHandle(element.FindFirst(TreeScope.Children,
                    Condition.TrueCondition), itemText);
                if (elementItem != null)
                {
                    LogMessage(elementItem.Current.Name + " : " +
                        elementItem.Current.ControlType.ProgrammaticName);
                    Object pattern = null;
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
                        return true;
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
            throw new XmlRpcFaultException(123,
                "Unable to find item in the list: " + itemText);
        }
        protected bool IsEnabled(AutomationElement e)
        {
            if (e == null)
                return false;
            if (objectTimeOut <= 0)
                return e.Current.IsEnabled;
            for (int i = 0; i < objectTimeOut; i++)
            {
                if (e.Current.IsEnabled)
                    return true;
                // Wait 1 second before retrying
                Thread.Sleep(1000);
            }
            return false;
        }
    }
}
