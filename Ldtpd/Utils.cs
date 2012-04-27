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
using ATGTestInput;
using System.Windows;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using CookComputing.XmlRpc;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    public class Utils : XmlRpcListenerService
    {
        Thread backgroundThread;
        public bool debug = false;
        internal Common common;
        internal WindowList windowList;
        protected int objectTimeOut = 5;
        public Utils(WindowList windowList, Common common, bool debug)
        {
            this.debug = debug;
            this.windowList = windowList;
            this.common = common;
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
        public void LogMessage(Object o)
        {
            common.LogMessage(o);
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
            tmp = Regex.Replace(tmp, @"\\", @"\\");
            tmp = Regex.Replace(tmp, "( |\r|\n)", "");
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
                        currObjInfo = objInfo.GetObjectType(e);
                        s = e.Current.Name;
                        if (s != null)
                            s = Regex.Replace(s, "( |\r|\n)", "");
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
                    currObjInfo = objInfo.GetObjectType(element);
                    s = element.Current.Name;
                    if (s != null)
                        s = Regex.Replace(s, "( |\r|\n)", "");
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
                            currObjInfo = objInfo.GetObjectType(subChild);
                            s = subChild.Current.Name;
                            if (s != null)
                                s = Regex.Replace(s, "( |\r|\n)", "");
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
            if (String.IsNullOrEmpty(windowName))
            {
                LogMessage("Invalid window name");
                return o;
            }
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
            if (e == null || String.IsNullOrEmpty(objName))
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
        internal AutomationElement GetObjectHandle(string windowName,
            string objName, ControlType[] type = null, bool waitForObj = true)
        {
            if (String.IsNullOrEmpty(windowName) ||
                String.IsNullOrEmpty(objName))
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
                objName, type, true);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            return childHandle;
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
            String tmp = Regex.Replace(objName, @"\*", @".*") + "$";
            tmp = Regex.Replace(tmp, @"( |:|\.|_|\r|\n)", "");
            tmp = Regex.Replace(tmp, @"\\", @"\\");
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
                    currObjInfo = objInfo.GetObjectType(element);
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
                        if (element.Current.ControlType == ControlType.MenuItem)
                        { // Do this only for menuitem type
                            // Split keyboard shortcut, as that might not be
                            // part of user provided object name
                            // Pattern anything has Ctrl+ || Function key
                            string[] tmpStrArray = Regex.Split(s,
                                @"(Ctrl\+|F\d)");
                            LogMessage("Menuitem shortcut length: " +
                                tmpStrArray.Length);
                            if (tmpStrArray.Length > 1)
                                // Keyboard shortcut found,
                                // just take first element from array
                                s = tmpStrArray[0];
                            tmpStrArray = null;
                        }
                    }
                    if (currObjInfo.objType != null)
                    {
                        if (s != null)
                            s = Regex.Replace(s, @"( |\t|:|\.|_|\r|\n)", "");
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
            string objName = null, string parentName = null,
            ControlType type = null)
        {
            if (windowHandle == null)
            {
                LogMessage("Invalid window handle");
                return false;
            }
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
                    currObjInfo = objInfo.GetObjectType(element);
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
                        s = Regex.Replace(s, " ", "");
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
                    if (type == null || type == element.Current.ControlType)
                        // Add if ControlType is null
                        // or only matching type, if available
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
        internal bool IsEnabled(AutomationElement e, bool wait = true)
        {
            if (e == null)
                return false;
            if (objectTimeOut <= 0)
                return e.Current.IsEnabled;
            int waitTimeOut = wait ? objectTimeOut : 1;
            for (int i = 0; i < waitTimeOut; i++)
            {
                if (e.Current.IsEnabled)
                    return true;
                // Wait 1 second before retrying
                Thread.Sleep(1000);
            }
            LogMessage("e.Current.IsEnabled: " + e.Current.IsEnabled);
            return false;
        }
        internal void InternalWait(int time)
        {
            common.Wait(time);
        }
        internal int InternalWaitTillGuiExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            if (String.IsNullOrEmpty(windowName))
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
            if (String.IsNullOrEmpty(windowName))
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle, childHandle;
            try
            {
                int waitTime = 0;
                if (String.IsNullOrEmpty(objName))
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
            if (String.IsNullOrEmpty(windowName))
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
                if (!String.IsNullOrEmpty(objName))
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
        internal int InternalCheckObject(string windowName, string objName,
					 string actionType)
        {
            if (String.IsNullOrEmpty(windowName) ||
                String.IsNullOrEmpty(objName) ||
                String.IsNullOrEmpty(actionType))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            AutomationElement windowHandle = GetWindowHandle(windowName);
            if (windowHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find window: " + windowName);
            }
            ControlType[] type = new ControlType[2] { ControlType.CheckBox,
                ControlType.RadioButton };
            AutomationElement childHandle = GetObjectHandle(windowHandle,
                objName, type, true);
            windowHandle = null;
            if (childHandle == null)
            {
                throw new XmlRpcFaultException(123,
                    "Unable to find Object: " + objName);
            }
            AutomationPattern[] patterns = childHandle.GetSupportedPatterns();
            Object pattern = null;
            try
            {
                if (childHandle.TryGetCurrentPattern(TogglePattern.Pattern,
                    out pattern))
                {
                    switch (actionType)
                    {
                        case "VerifyCheck":
                            if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                                return 1;
                            return 0;
                        case "VerifyUncheck":
                            if (((TogglePattern)pattern).Current.ToggleState == ToggleState.Off)
                                return 1;
                            return 0;
                        case "Check":
                            if (((TogglePattern)pattern).Current.ToggleState == ToggleState.On)
                            {
                                LogMessage("Checkbox / Radio button already checked");
                                return 1;
                            }
                            break;
                        case "UnCheck":
                            if (((TogglePattern)pattern).Current.ToggleState == ToggleState.Off)
                            {
                                LogMessage("Checkbox / Radio button already unchecked");
                                return 1;
                            }
                            break;
                        default:
                            throw new XmlRpcFaultException(123, "Unsupported actionType");
                    }
                    Object invoke = null;
                    childHandle.SetFocus();
                    if (childHandle.TryGetCurrentPattern(InvokePattern.Pattern,
                                     out invoke))
                        ((InvokePattern)invoke).Invoke();
                    else
                        ((TogglePattern)pattern).Toggle();
                    invoke = null;
                    return 1;
                }
                else if (childHandle.TryGetCurrentPattern(SelectionItemPattern.Pattern,
                    out pattern))
                {
                    switch (actionType)
                    {
                        case "VerifyCheck":
                            if (((SelectionItemPattern)pattern).Current.IsSelected)
                                return 1;
                            return 0;
                        case "VerifyUncheck":
                            if (((SelectionItemPattern)pattern).Current.IsSelected == false)
                                return 1;
                            return 0;
                        case "Check":
                            childHandle.SetFocus();
                            ((SelectionItemPattern)pattern).Select();
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
                childHandle = null;
            }
            LogMessage("Unsupported pattern to perform action");
            throw new XmlRpcFaultException(123,
                "Unsupported pattern to perform action");
        }
    }
}
