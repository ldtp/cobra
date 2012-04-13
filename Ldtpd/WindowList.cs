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
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    public class WindowList : List<AutomationElement>
    {
        bool debug;
        Thread backgroundThread;
        ArrayList watchWindowList;
        internal ArrayList windowCallbackEvent;
        public WindowList(bool debug)
        {
            this.debug = debug;
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
        internal void InternalWait(int waitTime)
        {
            Thread.Sleep(waitTime * 1000);
            // Collect all generations of memory.
            GC.Collect();
        }
        public void LogMessage(Object o)
        {
            if (debug)
                Console.WriteLine(o);
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
                {
                    try
                    {
                        // Remove element from the list
                        this.Remove(el);
                    }
                    catch (Exception ex)
                    {
                        LogMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex);
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
                    InternalWait(10);
                    CleanUpWindowElements();
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
        private bool DoesWindowNameMatched(AutomationElement e, string windowName)
        {
            if (e == null || windowName == null || windowName.Length == 0)
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
            if (s1 == null || s1.Length == 0)
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
                        element.Current.Name != null &&
                        element.Current.Name.Length > 0)
                    {
                        // Add window handle that have name !
                        int[] rid = element.GetRuntimeId();
                        LogMessage("Added: " +
                            element.Current.ControlType.ProgrammaticName +
                            " : " + element.Current.Name + " : " + rid);
                        if (this.IndexOf(element) == -1)
                            this.Add(element);
                        LogMessage("Window list count: " +
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
                        LogMessage("Removed: " +
                            element.Current.ControlType.ProgrammaticName +
                            " : " + windowName + " : " + rid);
                        if (this.IndexOf(element) != -1)
                            this.Remove(element);
                        LogMessage("Removed - Window list count: " +
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
