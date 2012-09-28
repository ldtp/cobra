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
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    public class WindowList : List<AutomationElement>
    {
        Common common;
        Thread backgroundThread;
        ArrayList watchWindowList;
        internal ArrayList windowCallbackEvent;
        public WindowList(Common common)
        {
            this.common = common;
            watchWindowList = new ArrayList();
            windowCallbackEvent = new ArrayList();
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
            backgroundThread = new Thread(new ThreadStart(BackgroundThread));
            // Clean up window handles in different thread
            backgroundThread.Start();
        }
        public void WatchWindow(string windowName)
        {
            watchWindowList.Add(windowName);
        }
        public void UnwatchWindow(string windowName)
        {
            if (watchWindowList.Contains(windowName))
                watchWindowList.Remove(windowName);
        }
        private void CleanUpWindowElements()
        {
            /*
             * Clean up handles that no longer exist
             * */
            List<AutomationElement> windowTmpList = new List<AutomationElement>();
            try
            {
                foreach (AutomationElement el in this)
                {
                    try
                    {
                        common.LogMessage(el.Current.Name);
                        Rect rect = el.Current.BoundingRectangle;
                        if (rect.X == 0 && rect.Y == 0 &&
                            rect.Width == 0 && rect.Height == 0)
                            // Window no longer exist
                            windowTmpList.Add(el);
                    }
                    catch (ElementNotAvailableException ex)
                    {
                        // Don't alter the current list, remove it later
                        windowTmpList.Add(el);
                        common.LogMessage(ex);
                    }
                    catch (Exception ex)
                    {
                        // Don't alter the current list, remove it later
                        windowTmpList.Add(el);
                        common.LogMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // Since window list is added / removed in different thread
                // values of windowList might be altered and an exception is thrown
                // Just handle the global exception
                common.LogMessage(ex);
            }
            try
            {
                foreach (AutomationElement el in windowTmpList)
                {
                    try
                    {
                        // Remove element from the list
                        this.Remove(el);
                    }
                    catch (Exception ex)
                    {
                        common.LogMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
            }
            windowTmpList = null;
            // With GC collect, noticed very less memory being used all the time
            GC.Collect();
        }
        /*
         * BackgroundThread: Cleanup window handle that are not current valid
         */
        private void BackgroundThread()
        {
            while (true)
            {
                try
                {
                    // Wait 10 second before starting the next
                    // cleanup cycle
                    common.Wait(10);
                    CleanUpWindowElements();
                    // With GC collect,
                    // noticed very less memory being used all the time
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    common.LogMessage(ex);
                }
            }
        }
        private bool DoesWindowNameMatched(AutomationElement e,
            string windowName)
        {
            if (e == null || String.IsNullOrEmpty(windowName))
                return false;
            String s1, s2;
            CurrentObjInfo currObjInfo;
            // Trying to mimic python fnmatch.translate
            String tmp = Regex.Replace(windowName, @"\*", @".*");
            tmp = Regex.Replace(tmp, " ", "");
            //tmp += @"\Z(?ms)";
            Regex rx = new Regex(tmp, RegexOptions.Compiled |
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                RegexOptions.CultureInvariant);
            s1 = e.Current.Name;
            if (s1 != null)
                s1 = (new Regex(" ")).Replace(s1, "");
            if (String.IsNullOrEmpty(s1))
            {
                return false;
            }
            ObjInfo objInfo = new ObjInfo(false);
            currObjInfo = objInfo.GetObjectType(e);
            // LDTP format object name
            s2 = currObjInfo.objType + s1;
            if (rx.Match(s1).Success || rx.Match(s2).Success)
                return true;
            return false;
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
                        !String.IsNullOrEmpty(element.Current.Name))
                    {
                        // Add window handle that have name !
                        int[] rid = element.GetRuntimeId();
                        common.LogMessage("Added: " +
                            element.Current.ControlType.ProgrammaticName +
                            " : " + element.Current.Name + " : " + rid);
                        if (this.IndexOf(element) == -1)
                            this.Add(element);
                        common.LogMessage("Window list count: " +
                            this.Count);
                        foreach (string windowName in watchWindowList)
                        {
                            if (DoesWindowNameMatched(element, windowName))
                            {
                                windowCallbackEvent.Add("onwindowcreate-" + windowName);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Do nothing
            }
        }
        private void OnWindowDelete(object sender, AutomationEventArgs e)
        {
            /*
             * Delete window handle that exist in the list on window close event
             * */
            try
            {
                AutomationElement element = sender as AutomationElement;
                if (e.EventId == WindowPattern.WindowClosedEvent)
                {
                    if (element != null)
                    {
                        string windowName = element.Current.Name;
                        int[] rid = element.GetRuntimeId();
                        common.LogMessage("Removed: " +
                            element.Current.ControlType.ProgrammaticName +
                            " : " + windowName + " : " + rid);
                        if (this.IndexOf(element) != -1)
                            this.Remove(element);
                        common.LogMessage("Removed - Window list count: " +
                            this.Count);
                    }
                }
            }
            catch
            {
                // Since window list is added / removed in different thread
                // values of windowList might be altered and an exception is thrown
                // Just handle the global exception
            }
        }
    }
}
