/*
 * Cobra WinLDTP 3.5
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
using System.Text;
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
using System.Text.RegularExpressions;

namespace Ldtpd
{
    public class Utils : XmlRpcListenerService
    {
        Thread backgroundThread;
        public bool debug = false;
        internal Common common;
        internal WindowList windowList;
        protected int windowRetry = 3;
        protected int objectTimeOut = 5;
        protected String appUnderTest = null;
        internal string writeToFile;
        public Utils(WindowList windowList, Common common, bool debug)
        {
            this.debug = debug;
            this.windowList = windowList;
            this.common = common;
            writeToFile = common.writeToFile;
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
                if (debug || writeToFile != null)
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
            ControlType[] type = null, bool waitTillGuiNotExist = false)
        {
            String s;
            int index;
            String actualString;
            AutomationElement element;
            CurrentObjInfo currObjInfo;
            AutomationElementCollection c;
            ObjInfo objInfo = new ObjInfo(false);
            ArrayList objectList = new ArrayList();
            // Trying to mimic python fnmatch.translate
            String tmp = Regex.Replace(windowName, @"\*", @".*");
            tmp = Regex.Replace(tmp, @"\?", @".");
            tmp = Regex.Replace(tmp, @"\\", @"\\");
            tmp = Regex.Replace(tmp, "( |\r|\n)", "");
            tmp = @"\A(?ms)" + tmp + @"\Z(?ms)";
            //tmp += @"\Z(?ms)";
            Regex rx = new Regex(tmp, RegexOptions.Compiled |
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                RegexOptions.CultureInvariant);
            List<AutomationElement> windowTmpList = new List<AutomationElement>();
            InternalTreeWalker w;
            Condition condition;
            try
            {
                foreach (AutomationElement e in windowList)
                {
                    try
                    {
                        Rect rect = e.Current.BoundingRectangle;
                        if (rect.Width == 0 && rect.Height == 0)
                        {
                            // Window no longer exist
                            windowTmpList.Add(e);
                            continue;
                        }
                        currObjInfo = objInfo.GetObjectType(e);
                        s = e.Current.Name;
                        if (s != null)
                            s = Regex.Replace(s, "( |\r|\n)", "");
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
                                rx = null;
                                objectList = null;
                                return e;
                            }
                            else
                            {
                                foreach (ControlType t in type)
                                {
                                    if (debug || writeToFile != null)
                                        LogMessage((t == e.Current.ControlType) +
                                            " : " + e.Current.ControlType.ProgrammaticName);
                                    if (t == e.Current.ControlType)
                                    {
                                        rx = null;
                                        objectList = null;
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
            finally
            {
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
                    windowTmpList = null;
                }
                catch (Exception ex)
                {
                    LogMessage(ex);
                }
            }
            if (waitTillGuiNotExist)
                // If window doesn't exist for waitTillGuiNotExist
                // return here as we don't need to find the window handle
                return null;
            condition = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Window);
            w = new InternalTreeWalker();
            windowTmpList = null;
            objectList.Clear();
            objInfo = new ObjInfo(false);
            element = w.walker.GetFirstChild(AutomationElement.RootElement);
            try
            {
                while (null != element)
                {
                    if (windowList.IndexOf(element) == -1)
                        // Add parent window handle,
                        // if it doesn't exist
                        windowList.Add(element);
                    if (!String.IsNullOrEmpty(appUnderTest))
                    {
                        // If app under test doesn't match
                        // continue searching next app
                        Process process;
                        // Get a process using the process id.
                        try
                        {
                            process = Process.GetProcessById(element.Current.ProcessId);
                            if (process.ProcessName != appUnderTest)
                            {
                                // app name doesn't match
                                element = w.walker.GetNextSibling(element);
                                continue;
                            }
                        }
                        catch
                        {
                            // Something went wrong, since app name
                            // is provided, search for next app
                            element = w.walker.GetNextSibling(element);
                            continue;
                        }
                    }
                    c = element.FindAll(TreeScope.Subtree, condition);
                    foreach (AutomationElement e in c)
                    {
                        if (windowList.IndexOf(e) == -1)
                            // Add sub window handle, if it doesn't
                            // exist
                            windowList.Add(e);
                        currObjInfo = objInfo.GetObjectType(e);
                        s = e.Current.Name;
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
                        LogMessage("Window dynamic: " + actualString + " : " + tmp);
                        objectList.Add(actualString);
                        // FIXME: Handle dlg0 as in Linux
                        if ((s != null && rx.Match(s).Success) ||
                            rx.Match(actualString).Success)
                        {
                            if (type == null)
                            {
                                LogMessage(windowName + " - Window found");
                                return e;
                            }
                            else
                            {
                                foreach (ControlType t in type)
                                {
                                    if (debug || writeToFile != null)
                                        LogMessage((t == e.Current.ControlType) +
                                            " : " + e.Current.ControlType.ProgrammaticName);
                                    if (t == e.Current.ControlType)
                                    {
                                        return e;
                                    }
                                }
                                LogMessage("type doesn't match !!!!!!!!!!!!!!");
                            }
                        }
                    }
                    // Get next parent window handle in the list
                    element = w.walker.GetNextSibling(element);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            finally
            {
                c = null;
                w = null;
                rx = null;
                condition = null;
                objectList = null;
            }
            // Unable to find window
            return null;
        }
        internal AutomationElement GetWindowHandle(String windowName,
            bool waitForObj = true, ControlType[] type = null,
            bool waitTillGuiNotExist = false)
        {
            AutomationElement o = null;
            String tmpAppUnderTest = null;
            if (String.IsNullOrEmpty(windowName))
            {
                LogMessage("Invalid window name");
                return o;
            }
            int retry = waitForObj ? windowRetry : 1;
            // For debugging use the following value
            //int retry = waitForObj ? 1 : 1;
            for (int i = 0; i < retry; i++)
            {
                Thread thread = new Thread(delegate()
                {
                    o = InternalGetWindowHandle(windowName, type);
                    if (!String.IsNullOrEmpty(tmpAppUnderTest))
                    {
                        // For alternate lookup, change
                        // appUnderTest, so all the apps are looked
                        appUnderTest = tmpAppUnderTest;
                        tmpAppUnderTest = null;
                    }
                    else if (String.IsNullOrEmpty(tmpAppUnderTest) &&
                      !String.IsNullOrEmpty(appUnderTest))
                    {
                        // For alternate lookup, change
                        // appUnderTest, so all the apps are looked
                        tmpAppUnderTest = appUnderTest;
                        appUnderTest = null;
                    }
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
                        finally
                        {
                            if (!String.IsNullOrEmpty(tmpAppUnderTest))
                                // Reset appUnderTest with the value back
                                appUnderTest = tmpAppUnderTest;
                        }
                        return o;
                    }
                }
            }
            if (!String.IsNullOrEmpty(tmpAppUnderTest))
                // Reset appUnderTest with the value back
                appUnderTest = tmpAppUnderTest;
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
        internal AutomationElement InternalGetObjectHandle(
            AutomationElement childHandle, String objName,
            ControlType[] type, ref ArrayList objectList,
            ObjInfo objInfo = null)
        {
            if (childHandle == null || String.IsNullOrEmpty(objName))
            {
                LogMessage("InternalGetObjectHandle: Child handle NULL");
                return null;
            }
            int index;
            String s = null;
            String objIndex;
            bool isObjIndex = false;
            AutomationElement element;
            CurrentObjInfo currObjInfo;
            String actualString = null;
            String automationId = null;
            bool isAutomationId = false;
            if (objInfo == null)
                objInfo = new ObjInfo(false);

            InternalTreeWalker w = new InternalTreeWalker();
            if (Regex.IsMatch(objName, @"^#"))
            {
                isAutomationId = true;
                // Object id format: #AutomationId
                automationId = objName.Split(new Char[] { '#' })[1];
            }
            else if (Regex.IsMatch(objName, @"#"))
            {
                isObjIndex = true;
            }
            // Trying to mimic python fnmatch.translate
            String tmp = Regex.Replace(objName, @"( |:|\.|_|\r|\n|<|>)", "");
            tmp = Regex.Replace(tmp, @"\*", @".*");
            tmp = Regex.Replace(tmp, @"\?", @".");
            tmp = Regex.Replace(tmp, @"\\", @"\\");
            tmp = Regex.Replace(tmp, @"\(", @"\(");
            tmp = Regex.Replace(tmp, @"\)", @"\)");
            tmp = Regex.Replace(tmp, @"\+", @"\+");
            tmp = Regex.Replace(tmp, @"\#", @"\#");
            tmp = @"\A(?ms)" + tmp + @"\Z(?ms)";
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
                    objIndex = null;
                    s = element.Current.Name;
                    currObjInfo = objInfo.GetObjectType(element);
                    if (String.IsNullOrEmpty(s))
                    {
                        LogMessage("Current object Name is null");
                    }
                    else
                    {
                        s = s.ToString();
                        if (debug || writeToFile != null)
                            LogMessage("Obj name: " + s + " : " +
                                element.Current.ControlType.ProgrammaticName);
                        if (element.Current.ControlType == ControlType.MenuItem)
                        { // Do this only for menuitem type
                            // Split keyboard shortcut, as that might not be
                            // part of user provided object name
                            string[] tmpStrArray = Regex.Split(s, @"\t");
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
                        if (debug || writeToFile != null)
                        {
                            LogMessage(objName + " : " + actualString + " : " + s + " : " +
                                tmp);
                            LogMessage((s != null && rx.Match(s).Success) + " : " +
                                (rx.Match(actualString).Success));
                        }
                        objectList.Add(actualString);
                        if (isObjIndex)
                            objIndex = currObjInfo.objType + "#" + currObjInfo.objCount;
                    }
                    if ((isAutomationId && automationId == element.Current.AutomationId) ||
                        (isObjIndex && !String.IsNullOrEmpty(objIndex) && rx.Match(objIndex).Success) ||
                        (!isAutomationId && !isObjIndex &&
                        (s != null && rx.Match(s).Success) ||
                        (actualString != null && rx.Match(actualString).Success)))
                    {
                        if (type == null)
                            return element;
                        else
                        {
                            foreach (ControlType t in type)
                            {
                                if (debug || writeToFile != null)
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
                        objName, type, ref objectList, objInfo);
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
            ControlType type = null, ObjInfo objInfo = null)
        {
            if (windowHandle == null)
            {
                LogMessage("Invalid window handle");
                return false;
            }
            int index;
            Regex rx = null;
            String s = null;
            if (objInfo == null)
                objInfo = new ObjInfo(false);
            string objIndex;
            // Trying to mimic python fnmatch.translate
            string tmp = null;
            string utf8 = null;
            Hashtable propertyHT;
            bool isObjIndex = false;
            byte[] utf8Bytes = null;
            String actualString = null;
            String automationId = null;
            CurrentObjInfo currObjInfo;
            bool isAutomationId = false;

            InternalTreeWalker w = new InternalTreeWalker();
            if (objName != null)
            {
                if (Regex.IsMatch(objName, @"^#"))
                {
                    isAutomationId = true;
                    // Object id format: #AutomationId
                    automationId = objName.Split(new Char[] { '#' })[1];
                }
                else if (Regex.IsMatch(objName, @"#"))
                {
                    isObjIndex = true;
                }
                tmp = Regex.Replace(objName, @"( |:|\.|_|\r|\n|<|>)", "");
                tmp = Regex.Replace(tmp, @"\*", @".*");
                tmp = Regex.Replace(tmp, @"\?", @".");
                tmp = Regex.Replace(tmp, @"\\", @"\\");
                tmp = Regex.Replace(tmp, @"\(", @"\(");
                tmp = Regex.Replace(tmp, @"\)", @"\)");
                tmp = Regex.Replace(tmp, @"\+", @"\+");
                tmp = Regex.Replace(tmp, @"\#", @"\#");
                tmp = @"\A(?ms)" + tmp + @"\Z(?ms)";
                rx = new Regex(tmp, RegexOptions.Compiled |
                       RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                       RegexOptions.CultureInvariant);
            }
            try
            {
                AutomationElement element = windowHandle;
                while (null != element)
                {
                    objIndex = null;
                    s = element.Current.Name;
                    currObjInfo = objInfo.GetObjectType(element);
                    if (s == null)
                    {
                        LogMessage("Current object Name is null");
                    }
                    else
                    {
                        s = s.ToString();
                        if (debug || writeToFile != null)
                            LogMessage("Obj name: " + s + " : " +
                                element.Current.ControlType.ProgrammaticName);
                    }
                    actualString = null;
                    if (s != null)
                        s = Regex.Replace(s, @"( |\t|:|\.|_|\r|\n|<|>)", "");
                    if (String.IsNullOrEmpty(s))
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
                    // Convert utf16 to utf8
                    // VMW bug#1054336
                    utf8Bytes = Encoding.UTF8.GetBytes(actualString);
                    utf8 = Encoding.UTF8.GetString(utf8Bytes);
                    if (type == null || type == element.Current.ControlType)
                    {
                        // Add if ControlType is null
                        // or only matching type, if available
                        objectList.Add(utf8);
                    }
                    if (objName != null || needAll)
                    {
                        //needAll - Required for GetChild
                        propertyHT = new Hashtable();
                        propertyHT.Add("key", utf8);
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
                            ((ArrayList)((Hashtable)objectHT[parentName])["children"]).Add(utf8);
                        }
                        else
                            LogMessage("parentName NULL");
                        propertyHT.Add("obj_index",
                            currObjInfo.objType + "#" + currObjInfo.objCount);
                        if (s != null)
                        {
                            // Convert utf16 to utf8
                            // VMW bug#1054336
                            byte[] labelUtf8Bytes = Encoding.UTF8.GetBytes(element.Current.Name);
                            string label = Encoding.UTF8.GetString(labelUtf8Bytes);
                            propertyHT.Add("label", label);
                        }
                        // Following 2 properties exist in Linux
                        //propertyHT.Add("label_by", s == null ? "" : s);
                        //propertyHT.Add("description", element.Current.DescribedBy);
                        if (element.Current.AcceleratorKey != "")
                            propertyHT.Add("key_binding",
                                element.Current.AcceleratorKey);
                        if (!String.IsNullOrEmpty(element.Current.AutomationId))
                            propertyHT.Add("window_id",
                                element.Current.AutomationId);
                        // Add individual property to object property
                        objectHT.Add(Encoding.UTF8.GetString(utf8Bytes), propertyHT);
                        if (isObjIndex)
                            objIndex = currObjInfo.objType + "#" + currObjInfo.objCount;
                        if ((debug || writeToFile != null) && rx != null)
                        {
                            LogMessage(objName + " : " + utf8 + " : " + s
                                + " : " + tmp);
                            LogMessage((s != null && rx.Match(s).Success) + " : " +
                                (utf8 != null && rx.Match(utf8).Success));
                        }
                        if ((isAutomationId && !String.IsNullOrEmpty(automationId) &&
                            automationId == element.Current.AutomationId) ||
                            (isObjIndex && !String.IsNullOrEmpty(objIndex) && rx.Match(objIndex).Success) ||
                            (!isAutomationId && !isObjIndex &&
                            (s != null && rx != null && rx.Match(s).Success) ||
                            (utf8 != null && rx != null && rx.Match(utf8).Success)))
                        {
                            matchedKey = utf8;
                            LogMessage("String matched: " + needAll);
                            if (!needAll)
                                return true;
                        }
                    }

                    // If any subchild exist for the current element navigate to it
                    if (InternalGetObjectList(w.walker.GetFirstChild(element),
                        ref objectList, ref objectHT, ref matchedKey,
                        needAll, objName, utf8, type, objInfo))
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
                // Wait 1 second before retrying when wait is true
                if (wait)
                    Thread.Sleep(1000);
            }
            LogMessage("e.Current.IsEnabled: " + e.Current.IsEnabled);
            return false;
        }
        internal void InternalWait(int time)
        {
            common.Wait(time);
        }
        internal void InternalWait(double time)
        {
            common.Wait(time);
        }
        internal AutomationElement InternalWaitTillControlTypeExist(ControlType type,
            int processId, int guiTimeOut = 30)
        {
            InternalTreeWalker w;
            AutomationElementCollection c;
            Condition condition = new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Menu);
            w = new InternalTreeWalker();
            try
            {
                int waitTime = 0;
                while (waitTime < guiTimeOut)
                {
                    AutomationElement element = w.walker.GetFirstChild(AutomationElement.RootElement);
                    while (null != element)
                    {
                        if (windowList.IndexOf(element) == -1)
                            // Add parent window handle,
                            // if it doesn't exist
                            windowList.Add(element);
                        try
                        {
                            if (element.Current.ProcessId != processId)
                            {
                                // app name doesn't match
                                element = w.walker.GetNextSibling(element);
                                continue;
                            }
                            if (element.Current.ControlType == type)
                                return element;
                        }
                        catch
                        {
                            // Something went wrong, since app name
                            // is provided, search for next app
                            element = w.walker.GetNextSibling(element);
                            continue;
                        }
                        c = element.FindAll(TreeScope.Subtree, condition);
                        foreach (AutomationElement e in c)
                        {
                            try
                            {
                                if (e.Current.ControlType == type)
                                    return e;
                            }
                            catch
                            {
                                // Something went wrong, since app name
                                // is provided, search for next app
                                element = w.walker.GetNextSibling(element);
                                continue;
                            }
                        }
                        element = w.walker.GetNextSibling(element);
                        waitTime++;
                        //InternalWait(1);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return null;
        }
        internal int InternalWaitTillGuiExist(String windowName,
            String objName = null, int guiTimeOut = 30, String state = null)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle, childHandle = null;
            try
            {
                int waitTime = 0;
                if (String.IsNullOrEmpty(objName))
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
                if (childHandle != null) // To avoid compilation warning
                    childHandle = null;
                windowHandle = null;
            }
            return 0;
        }
        internal int InternalWaitTillGuiNotExist(String windowName,
            String objName = null, int guiTimeOut = 30, String state = null)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                LogMessage("Argument cannot be empty.");
                return 0;
            }
            AutomationElement windowHandle, childHandle = null;
            try
            {
                int waitTime = 0;
                if (String.IsNullOrEmpty(objName))
                {
                    while (waitTime < guiTimeOut)
                    {
                        windowHandle = GetWindowHandle(windowName, false,
                            null, true);
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
                        windowHandle = GetWindowHandle(windowName, false,
                            null, true);
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
            finally
            {
                if (childHandle != null) // To avoid compilation warning
                    childHandle = null;
                windowHandle = null;
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
        internal bool InternalXYClick(AutomationElement element)
        {
            // Click just on X and Y co-ordinates,
            // required for Google Chrome (mnuSystem;mnuMinimize)
            // or minimizewindow('*Chrome*')
            if (element == null)
                return false;
            Rect rect = element.Current.BoundingRectangle;
            Point pt = new Point(rect.X, rect.Y);
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
