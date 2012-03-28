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
using System.Collections.Generic;

using ATGTestInput;
using System.Windows;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using CookComputing.XmlRpc;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Runtime.InteropServices;
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
        WindowList windowList;
        Thread backgroundThread;
        public bool debug = false;
        protected int objectTimeOut = 5;
        protected bool shiftKeyPressed = false;
        protected Stack logStack = new Stack(100);
        public Utils(WindowList windowList)
        {
            this.windowList = windowList;
            backgroundThread = new Thread(new ThreadStart(BackgroundThread));
            // Clean up window handles in different thread
            backgroundThread.Start();
        }
        ~Utils()
        {
            try
            {
                // Stop the cleanup thread
                backgroundThread.Interrupt();
                windowList = null;
                backgroundThread = null;
                Automation.RemoveAllEventHandlers();
            }
            catch (Exception ex)
            {
                if (debug)
                    Console.WriteLine(ex);
            }
        }
        /*
         * BackgroundThread: GC release
         */
        private void BackgroundThread()
        {
            while (true)
            {
                try
                {
                    // Wait 10 second before starting the next
                    // cleanup cycle
                    InternalWait(10);
                    // With GC collect,
                    // noticed very less memory being used all the time
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                }
            }
        }
        internal KeyInfo GetKey(string key)
        {
            try
            {
                switch (key.ToLower())
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
        internal KeyInfo[] GetKeyVal(string data)
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
            try
            {
                return keyList.ToArray(typeof(KeyInfo))
                    as KeyInfo[];
            }
            finally
            {
                keyList = null;
            }
        }
        public void LogMessage(Object o)
        {
            if (debug)
                Console.WriteLine(o);
            try
            {
                if (logStack.Count == 100)
                {
                    // Removes 1 log, if it has 100 instances
                    logStack.Pop();
                }
                // Add new log to the stack
                logStack.Push("INFO-" + o);
            }
            catch (Exception ex)
            {
                if (debug)
                    Console.WriteLine(ex);
            }
        }
        /*
         * InternalLaunchApp: Waits for the process to complete
         * and closes the handle, to avoid memory
         */
        internal void InternalLaunchApp(object data)
        {
            try
            {
                Process ps = data as Process;
                // Wait for the application to quit
                ps.WaitForExit();
                // Close the handle, so that we won't leak memory
                ps.Close();
                ps = null;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                throw;
            }
            // With GC collect,
            // noticed very less memory being used all the time
            GC.Collect();
        }
        private AutomationElement InternalGetWindowHandle(String windowName,
            ControlType[] type = null)
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
            InternalTreeWalker w = new InternalTreeWalker();
            try
            {
                foreach (AutomationElement e in windowList)
                {
                    try
                    {
                        currObjInfo = objInfo.GetObjectType(e,
                            e.Current.ControlType);
                        s = e.Current.Name;
                        if (s != null)
                            s = (new Regex(" ")).Replace(s, "");
                        if (s == null || s.Length == 0)
                        {
                            // txt0, txt1
                            actualString = currObjInfo.objType +
                                currObjInfo.objCount;
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
                            if (type == null)
                            {
                                LogMessage(windowName + " - Window found");
                                w = null;
                                rx = null;
                                objectList = null;
                                windowTmpList = null;
                                return e;
                            }
                            else
                            {
                                foreach (ControlType t in type)
                                {
                                    if (debug)
                                        LogMessage((t == e.Current.ControlType) +
                                            " : " + e.Current.ControlType.ProgrammaticName);
                                    if (t == e.Current.ControlType)
                                    {
                                        w = null;
                                        rx = null;
                                        objectList = null;
                                        windowTmpList = null;
                                        return e;
                                    }
                                }
                                LogMessage("type doesn't match !!!!!!!!!!!!!!");
                            }
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
                    try
                    {
                        // Remove element from the list
                        windowList.Remove(e);
                    }
                    catch (Exception ex)
                    {
                        LogMessage(ex);
                    }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            windowTmpList = null;
            objectList.Clear();
            objInfo = new ObjInfo(false);
            element = w.walker.GetFirstChild(AutomationElement.RootElement);
            try
            {
                while (null != element)
                {
                    currObjInfo = objInfo.GetObjectType(element,
                        element.Current.ControlType);
                    s = element.Current.Name;
                    if (s != null)
                        s = (new Regex(" ")).Replace(s, "");
                    if (s == null || s == "")
                    {
                        // txt0, txt1
                        actualString = currObjInfo.objType +
                            currObjInfo.objCount;
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
                        if (type == null)
                        {
                            LogMessage(windowName + " - Window found");
                            return element;
                        }
                        else
                        {
                            foreach (ControlType t in type)
                            {
                                if (debug)
                                    LogMessage((t == element.Current.ControlType) +
                                        " : " + element.Current.ControlType.ProgrammaticName);
                                if (t == element.Current.ControlType)
                                {
                                    return element;
                                }
                            }
                            LogMessage("type doesn't match !!!!!!!!!!!!!!");
                        }
                    }
                    element = w.walker.GetNextSibling(element);
                }
                element = w.walker.GetFirstChild(
                    AutomationElement.RootElement);
                // Reset object info
                objInfo = new ObjInfo(false);
                objectList.Clear();
                AutomationElement subChild;
                while (null != element)
                {
                    subChild = w.walker.GetFirstChild(
                        element);
                    while (subChild != null)
                    {
                        if (subChild.Current.Name != null)
                        {
                            currObjInfo = objInfo.GetObjectType(subChild,
                                subChild.Current.ControlType);
                            s = subChild.Current.Name;
                            if (s != null)
                                s = (new Regex(" ")).Replace(s, "");
                            if (s == null || s == "")
                            {
                                // txt0, txt1
                                actualString = currObjInfo.objType +
                                    currObjInfo.objCount;
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
                            if ((s != null && rx.Match(s).Success) ||
                                rx.Match(actualString).Success)
                            {
                                if (type == null)
                                {
                                    LogMessage(windowName + " - Window found");
                                    return subChild;
                                }
                                else
                                {
                                    foreach (ControlType t in type)
                                    {
                                        if (debug)
                                            LogMessage((t == subChild.Current.ControlType) +
                                                " : " + subChild.Current.ControlType.ProgrammaticName);
                                        if (t == subChild.Current.ControlType)
                                        {
                                            LogMessage(windowName + " - Window found");
                                            return subChild;
                                        }
                                    }
                                    LogMessage("type doesn't match !!!!!!!!!!!!!!");
                                }
                            }
                        }
                        subChild = w.walker.GetNextSibling(subChild);
                    }
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
                rx = null;
                objectList = null;
            }
            // Unable to find window
            return null;
        }
        internal AutomationElement GetWindowHandle(String windowName,
            bool waitForObj = true, ControlType[] type = null)
        {
            AutomationElement o = null;
            int retry = waitForObj ? objectTimeOut : 1;
            // For debugging use the following value
            //int retry = waitForObj ? 1 : 1;
            for (int i = 0; i < retry; i++)
            {
                Thread thread = new Thread(delegate()
                {
                    o = InternalGetWindowHandle(windowName, type);
                });
                thread.Start();
                // Wait 30 seconds (30 seconds * 1000 milli seconds)
                if (!thread.Join(30000))
                {
                    // Windows automation library hanged
                    LogMessage("WARNING: Thread aborting, as the program" +
                    " unable to find window within 30 seconds");
                    // This is an unsafe operation so use as a last resort.
                    // Aborting only the current thread
                    thread.Abort();
                }
                else
                {
                    // Collect all generations of memory.
                    GC.Collect();
                    if (o != null)
                    {
                        try
                        {
                            LogMessage("object is non null: " + o.Current.Name);
                        }
                        catch (System.Runtime.InteropServices.COMException ex)
                        {
                            // Noticed this with Notepad
                            LogMessage("Error HRESULT E_FAIL has been" +
                                " returned from a call to a COM component.");
                            LogMessage(ex.StackTrace);
                            continue;
                        }
                        return o;
                    }
                }
            }
            return o;
        }
        internal AutomationElement GetObjectHandle(AutomationElement e, String objName)
        {
            return GetObjectHandle(e, objName, null, true);
        }
        internal AutomationElement GetObjectHandle(AutomationElement e,
            String objName, ControlType[] type, bool waitForObj)
        {
            AutomationElement o = null;
            if (e == null)
            {
                LogMessage("GetObjectHandle: Child handle NULL");
                return null;
            }
            InternalTreeWalker w = new InternalTreeWalker();
            ObjInfo objInfo = new ObjInfo(false);
            int retry = waitForObj ? objectTimeOut : 1;
            // For debugging use the following value
            //int retry = waitForObj ? 1 : 1;
            ArrayList objectList = new ArrayList();
            for (int i = 0; i < retry; i++)
            {
                try
                {
                    o = InternalGetObjectHandle(w.walker.GetFirstChild(e),
                        objName, type, ref objectList);
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                    o = null;
                }
                finally
                {
                    objectList.Clear();
                    // Collect all generations of memory.
                    GC.Collect();
                }
                if (o != null)
                {
                    w = null;
                    objectList = null;
                    return o;
                }
                // Wait 1 second, rescan for object
                Thread.Sleep(1000);
            }
            w = null;
            objectList = null;
            return o;
        }
        private AutomationElement InternalGetObjectHandle(AutomationElement childHandle,
            String objName, ControlType[] type, ref ArrayList objectList)
        {
            if (childHandle == null)
            {
                LogMessage("InternalGetObjectHandle: Child handle NULL");
                return null;
            }
            int index;
            String s = null;
            AutomationElement element;
            CurrentObjInfo currObjInfo;
            String actualString = null;
            ObjInfo objInfo = new ObjInfo(false);

            InternalTreeWalker w = new InternalTreeWalker();
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
                    if (currObjInfo.objType != null)
                    {
                        if (s != null)
                            s = (new Regex(" ")).Replace(s, "");
                        if (s == null || s.Length == 0)
                        {
                            // txt0, txt1
                            actualString = currObjInfo.objType +
                                currObjInfo.objCount;
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
                        if (debug)
                        {
                            LogMessage(objName + " : " + actualString + " : " + s + " : " +
                                tmp);
                            LogMessage((s != null && rx.Match(s).Success) + " : " +
                                (rx.Match(actualString).Success));
                        }
                        objectList.Add(actualString);
                    }
                    if ((s != null && rx.Match(s).Success) ||
                        (actualString != null && rx.Match(actualString).Success))
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
                    AutomationElement subChild = InternalGetObjectHandle(
                        w.walker.GetFirstChild(element),
                        objName, type, ref objectList);
                    if (subChild != null)
                    {
                        // Object found, don't loop further
                        return subChild;
                    }
                    element = w.walker.GetNextSibling(element);
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
            finally
            {
                w = null;
                rx = null;
            }
            return null;
        }
        internal bool InternalGetObjectList(AutomationElement windowHandle,
            ref ArrayList objectList, ref Hashtable objectHT,
            ref string matchedKey, bool needAll = false,
            string objName = null, string parentName = null)
        {
            int index;
            Regex rx = null;
            String s = null;
            ObjInfo objInfo = new ObjInfo(false);
            String actualString = null;
            CurrentObjInfo currObjInfo;
            Hashtable propertyHT;
            // Trying to mimic python fnmatch.translate
            String tmp = null;

            InternalTreeWalker w = new InternalTreeWalker();
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
                    actualString = null;
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
                    objectList.Add(actualString);
                    if (objName != null || needAll)
                    {
                        //needAll - Required for GetChild
                        propertyHT = new Hashtable();
                        propertyHT.Add("key", actualString);
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
                            ((ArrayList)((Hashtable)objectHT[parentName])["children"]).Add(actualString);
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
                        objectHT.Add(actualString, propertyHT);
                        if (debug && rx != null)
                        {
                            LogMessage(objName + " : " + actualString + " : " + s
                                + " : " + tmp);
                            LogMessage((s != null && rx.Match(s).Success) + " : " +
                                (actualString != null && rx.Match(actualString).Success));
                        }
                        if ((s != null && rx != null && rx.Match(s).Success) ||
                        (actualString != null && rx != null && rx.Match(actualString).Success))
                        {
                            matchedKey = actualString;
                            LogMessage("String matched: " + needAll);
                            if (!needAll)
                                return true;
                        }
                    }

                    // If any subchild exist for the current element navigate to it
                    if (InternalGetObjectList(w.walker.GetFirstChild(element),
                        ref objectList, ref objectHT, ref matchedKey,
                        needAll, objName, actualString))
                        return true;
                    element = w.walker.GetNextSibling(element);
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
            finally
            {
                w = null;
                rx = null;
                propertyHT = null;
            }
            return false;
        }
        internal bool SelectListItem(AutomationElement element, String itemText,
            bool verify = false)
        {
            if (element == null || itemText == null || itemText.Length == 0)
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
                elementItem = GetObjectHandle(element, itemText);
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
                    if (elementItem.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                        out pattern))
                    {
                        LogMessage("SelectionItemPattern");
                        //((SelectionItemPattern)pattern).Select();
                        // NOTE: Work around, as the above doesn't seem to work
                        // with UIAComWrapper and UIAComWrapper is required
                        // to Edit value in Spin control
                        return InternalClick(elementItem);
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
        internal int InternalComboHandler(String windowName, String objName,
            String item, String actionType = "Select")
        {
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find window: " + windowName);
            }
            windowHandle.SetFocus();
            ControlType[] type = new ControlType[3] { ControlType.ComboBox,
                ControlType.ListItem, ControlType.List/*, ControlType.Text */ };
            AutomationElement childHandle = GetObjectHandle(windowHandle, objName,
                type, true);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123, "Unable to find Object: " + objName);
            }
            Object pattern = null;
            try
            {
                LogMessage("Handle name: " + childHandle.Current.Name +
                    " - " + childHandle.Current.ControlType.ProgrammaticName);
                if (!IsEnabled(childHandle))
                {
                    throw new XmlRpcFaultException(123, "Object state is disabled");
                }
                if (childHandle.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,
                    out pattern))
                {
                    LogMessage("ExpandCollapsePattern");
                    // Retry max 5 times
                    for (int i = 0; i < 5; i++)
                    {
                        switch (actionType)
                        {
                            case "Hide":
                                ((ExpandCollapsePattern)pattern).Collapse();
                                // Required to wait 1 second, before checking the state and retry collapsing
                                InternalWait(1);
                                if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                                    ExpandCollapseState.Collapsed)
                                {
                                    // Hiding same combobox multiple time consecutively
                                    // fails. Check for the state and retry to collapse
                                    LogMessage("Collapsed");
                                    return 1;
                                }
                                break;
                            case "Show":
                            case "Select":
                            case "Verify":
                                ((ExpandCollapsePattern)pattern).Expand();
                                // Required to wait 1 second, before checking the state and retry expanding
                                InternalWait(1);
                                if (((ExpandCollapsePattern)pattern).Current.ExpandCollapseState ==
                                    ExpandCollapseState.Expanded)
                                {
                                    // Selecting same combobox multiple time consecutively
                                    // fails. Check for the state and retry to expand
                                    LogMessage("Expaneded");
                                    if (actionType == "Show")
                                        return 1;
                                    else
                                    {
                                        childHandle.SetFocus();
                                        bool verify = actionType == "Verify" ? true : false;
                                        return SelectListItem(childHandle, item, verify) ? 1 : 0;
                                    }
                                }
                                break;
                        }
                    }
                }
                // Handle selectitem and verifyselect on list. Get ExpandCollapsePattern fails on list,
                // VM Library items are selected and verified correctly on Player with this fix
                else
                {
                    childHandle.SetFocus();
                    bool verify = actionType == "Verify" ? true : false;
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
                pattern = null;
                childHandle = null;
            }
            return 0;
        }
        internal bool IsEnabled(AutomationElement e)
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
            LogMessage("e.Current.IsEnabled: " + e.Current.IsEnabled);
            return false;
        }
        internal int InternalWait(object waitTime)
        {
            int time;
            try
            {
                time = Convert.ToInt32(waitTime, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                time = 5;
                LogMessage(ex);
            }
            if (time < 1)
                time = 1;
            Thread.Sleep(time * 1000);
            // Collect all generations of memory.
            GC.Collect();
            return 1;
        }
        internal int InternalWaitTillGuiExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            if (windowName == null || windowName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle, childHandle;
            try
            {
                int waitTime = 0;
                if (objName == null || objName.Length == 0)
                {
                    while (waitTime < guiTimeOut)
                    {
                        windowHandle = GetWindowHandle(windowName, false);
                        if (windowHandle != null)
                        {
                            return 1;
                        }
                        waitTime++;
                        InternalWait(1);
                    }
                }
                else
                {
                    while (waitTime < guiTimeOut)
                    {
                        windowHandle = GetWindowHandle(windowName,
                            false);
                        if (windowHandle != null &&
                            (childHandle = GetObjectHandle(windowHandle, objName,
                            null, false)) != null)
                        {
                            return 1;
                        }
                        waitTime++;
                        InternalWait(1);
                        windowHandle = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                windowHandle = childHandle = null;
            }
            return 0;
        }
        internal int InternalWaitTillGuiNotExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            if (windowName == null || windowName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle, childHandle;
            try
            {
                int waitTime = 0;
                if (objName == null || objName.Length == 0)
                {
                    while (waitTime < guiTimeOut)
                    {
                        windowHandle = GetWindowHandle(windowName, false);
                        if (windowHandle == null)
                            return 1;
                        waitTime++;
                        InternalWait(1);
                        windowHandle = null;
                    }
                }
                else
                {
                    while (waitTime < guiTimeOut)
                    {
                        windowHandle = GetWindowHandle(windowName, false);
                        if (windowHandle == null)
                            // If window doesn't exist, no Point in checking for object
                            // inside the window
                            return 1;
                        childHandle = GetObjectHandle(windowHandle, objName,
                            null, false);
                        if (childHandle == null)
                        {
                            windowHandle = null;
                            return 1;
                        }
                        waitTime++;
                        InternalWait(1);
                        childHandle = windowHandle = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        internal int InternalGuiExist(String windowName, String objName = null)
        {
            if (windowName == null || windowName.Length == 0)
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle, childHandle;
            try
            {
                windowHandle = GetWindowHandle(windowName, false);
                if (windowHandle == null)
                {
                    return 0;
                }
                windowHandle.SetFocus();
                if (objName != null && objName.Length > 0)
                {
                    childHandle = GetObjectHandle(windowHandle,
                        objName, null, false);
                    if (childHandle == null)
                    {
                        LogMessage("Unable to find Object: " + objName);
                        return 0;
                    }
                }
                return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                return 0;
            }
            finally
            {
                childHandle = windowHandle = null;
            }
        }
        internal bool InternalClick(AutomationElement element)
        {
            if (element == null)
                return false;
            Rect rect = element.Current.BoundingRectangle;
            Point pt = new Point(rect.X + rect.Width / 2,
                rect.Y + rect.Height / 2);
            Input.MoveToAndClick(pt);
            return true;
        }
        internal int IsMenuChecked(AutomationElement menuHandle)
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
                int ischecked;
                uint currentstate = ((LegacyIAccessiblePattern)pattern).Current.State;
                // Use fifth bit of current state to determine menu item is checked or not checked
                ischecked = (currentstate & 16) == 16 ? 1 : 0;
                LogMessage("IsMenuChecked: " + menuHandle.Current.Name + " : " + "Checked: " +
                    ischecked + " : " + "Current State: " + currentstate);
                pattern = null;
                return ischecked;
            }
            else
                LogMessage("Unable to get LegacyIAccessiblePattern");
                return 0;
        }
        internal int InternalMenuHandler(String windowName, String objName,
            ref ArrayList menuList, String actionType = "Select")
        {
            if (windowName == null || objName == null ||
                windowName.Length == 0 || objName.Length == 0)
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            String mainMenu = objName;
            String currObjName = null;
            Object invokePattern = null;
            AutomationElementCollection c = null;
            ControlType[] type = new ControlType[3] { ControlType.Menu,
                ControlType.MenuBar, ControlType.MenuItem };
            ControlType[] controlType = new ControlType[1] { ControlType.Menu };
            bool bContextNavigated = false;
            AutomationElement windowHandle, childHandle;
            AutomationElement prevObjHandle = null, firstObjHandle = null;

            InternalTreeWalker w = new InternalTreeWalker();
            try
            {
                windowHandle = GetWindowHandle(windowName);
                if (windowHandle == null)
                {
                    throw new XmlRpcFaultException(123,
                        "Unable to find window: " + windowName);
                }
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
                    LogMessage("childHandle: " + childHandle.Current.Name + " : " + currObjName + " : " +
                            childHandle.Current.ControlType.ProgrammaticName);
                    childHandle = GetObjectHandle(childHandle,
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
                        actionType == "VerifyCheck") && !IsEnabled(childHandle))
                    {
                        throw new XmlRpcFaultException(123,
                            "Object state is disabled");
                    }
                    childHandle.SetFocus();
                    if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                        out invokePattern))
                    {
                        if (actionType == "Select" || currObjName != objName ||
                             actionType == "SubMenu" || actionType == "VerifyCheck")
                        {
                            LogMessage("Invoking menu item: " + currObjName + " : " + objName +
                                " : " + childHandle.Current.ControlType.ProgrammaticName + " : "
                                + childHandle.Current.Name);
                            childHandle.SetFocus();
                            if (!(actionType == "VerifyCheck" && currObjName == objName))
                            {
                                InternalClick(childHandle);
                            }
                            try
                            {
                                // Invoke doesn't work for VMware Workstation
                                // But they work for Notepad
                                // MoveToAndClick works for VMware Workstation
                                // But not for Notepad (on first time)
                                // Requires 2 clicks !
                                //((InvokePattern)invokePattern).Invoke();
                                InternalWait(1);
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
                                        InternalClick(firstObjHandle);
                                    else
                                        // Check menu
                                        InternalClick(childHandle);
                                    return 1;
                                }
                                else if (actionType == "UnCheck")
                                {
                                    if (state == 0)
                                        // Already unchecked, just click back the main menu
                                        InternalClick(firstObjHandle);
                                    else
                                        // Uncheck menu
                                        InternalClick(childHandle);
                                    return 1;
                                }
                                break;
                            case "Exist":
                            case "Enabled":
                                state = IsEnabled(childHandle) == true ? 1 : 0;
                                LogMessage("IsEnabled(childHandle): " +
                                    childHandle.Current.Name + " : " + state);
                                LogMessage("IsEnabled(childHandle): " +
                                    childHandle.Current.ControlType.ProgrammaticName);
                                // Set it back to old state, else the menu selection left there
                                InternalClick(firstObjHandle);
                                // Don't process the last item
                                if (actionType == "Enabled")
                                    return state;
                                else if (actionType == "Exist")
                                    return 1;
                                break;
                            case "SubMenu":
                                string matchedKey = null;
                                Hashtable objectHT = new Hashtable();
                                ObjInfo objInfo = new ObjInfo(false);
                                menuList.Clear();
                                InternalGetObjectList(
                                    w.walker.GetFirstChild(childHandle),
                                    ref menuList, ref objectHT, ref matchedKey);
                                objectHT = null;
                                if (menuList.Count > 0)
                                {
                                    // Set it back to old state,
                                    // else the menu selection left there
                                    InternalClick(firstObjHandle);
                                    // Don't process the last item
                                    return 1;
                                }
                                else
                                    LogMessage("menuList.Count <= 0: " + menuList.Count);
                                break;
                            case "VerifyCheck":
                                state = IsMenuChecked(childHandle);
                                InternalClick(firstObjHandle);
                                return state;
                            default:
                                break;
                        }
                    }
                    else if (!bContextNavigated && InternalWaitTillGuiExist("Context",
                        null, 3) == 1)
                    {
                        LogMessage("Context");
                        AutomationElement tmpWindowHandle;
                        // Menu item under Menu are listed under Menu Window
                        if (actionType == "VerifyCheck")
                            tmpWindowHandle = GetWindowHandle("Context", true, controlType);
                        else
                            tmpWindowHandle = GetWindowHandle("Context");
                        if (tmpWindowHandle == null)
                        {
                            throw new XmlRpcFaultException(123,
                                "Unable to find window: Context");
                        }
                        // Find object from current handle, rather than navigating
                        // the complete window
                        childHandle = tmpWindowHandle;
                        bContextNavigated = true;
                        LogMessage("bContextNavigated: " + bContextNavigated);
                        if (actionType != "SubMenu")
                            continue;
                        else if (currObjName == objName)
                        {
                            switch (actionType)
                            {
                                case "SubMenu":
                                    string matchedKey = null;
                                    Hashtable objectHT = new Hashtable();
                                    ObjInfo objInfo = new ObjInfo(false);
                                    menuList.Clear();
                                    InternalGetObjectList(
                                        w.walker.GetFirstChild(childHandle),
                                        ref menuList, ref objectHT, ref matchedKey);
                                    if (menuList.Count > 0)
                                    {
                                        // Set it back to old state,
                                        // else the menu selection left there
                                        InternalClick(firstObjHandle);
                                        // Don't process the last item
                                        return 1;
                                    }
                                    else
                                        LogMessage("menuList.Count <= 0: " + menuList.Count);
                                    break;
                            }
                        }
                    }
                    else if (c != null && c.Count > 0)
                    {
                        LogMessage("c != null && c.Count > 0");
                        childHandle = windowHandle;
                        continue;
                    }
                    else if (InternalWaitTillGuiExist(prevObjHandle.Current.Name, null, 3) == 1)
                    {
                        LogMessage("prevObjHandle: " + prevObjHandle.Current.Name);
                        // Menu item under Menu are listed under Menu Window
                        LogMessage("Menu item under Menu are listed under Menu Window: " +
                            prevObjHandle.Current.Name);
                        AutomationElement tmpWindowHandle;
                        if (actionType == "VerifyCheck")
                            tmpWindowHandle = GetWindowHandle(prevObjHandle.Current.Name, true, controlType);
                        else
                            tmpWindowHandle = GetWindowHandle(prevObjHandle.Current.Name);
                        if (tmpWindowHandle != null)
                        {
                            // Find object from current handle, rather than navigating
                            // the complete window
                            LogMessage("Assigning tmpWindowHandle as childHandle");
                            childHandle = tmpWindowHandle;
                            LogMessage("childHandle: " + childHandle.Current.Name + " : " +
                                childHandle.Current.ControlType.ProgrammaticName);
                        }
                    }
                    // Required for Notepad like app
                    if ((c == null || c.Count == 0))
                    {
                        LogMessage("Work around for Windows application");
                        LogMessage(windowHandle.Current.Name + " : " + objName);
                        AutomationElement tmpChildHandle = GetObjectHandle(windowHandle, objName,
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
                                string matchedKey = null;
                                Hashtable objectHT = new Hashtable();
                                ObjInfo objInfo = new ObjInfo(false);
                                menuList.Clear();
                                InternalGetObjectList(w.walker.GetFirstChild(childHandle),
                                    ref menuList, ref objectHT, ref matchedKey);
                                // Set it back to old state,
                                // else the menu selection left there
                                InternalClick(firstObjHandle);
                                objectHT = null;
                                // Don't process the last item
                                return 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                if (firstObjHandle != null)
                {
                    // Set it back to old state, else the menu selection left there
                    InternalClick(firstObjHandle);
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
                windowHandle = childHandle = null;
                prevObjHandle = firstObjHandle = null;
            }
        }
    }
}
