/*
 * Cobra WinLDTP 3.0
 * 
 * Author: Nagappan Alagappan <nalagappan@vmware.com>
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
using System.IO;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace Ldtpd
{
    public class ProcessPerformanceCounter
    {
        // Reference:
        // http://pamelayang.blogspot.com/2010/06/per-process-cpu-usage-and-network-io.html
        Common common;
        public ProcessPerformanceCounter(Common common,
            string processName, int processId)
        {
            this.common = common;
            this.processId = processId;
            this.processName = processName;
            GetInstanceName(processId);
        }
        private int processId;
        private string instanceName, processName;
        PerformanceCounter cpuCounter; 
        public string InstanceName {
            get { return instanceName; }
            set { instanceName = value; }
        }
        private string GetInstanceName(int processId)
        {
            instanceName = "";
            if (!string.IsNullOrEmpty(processName))
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    int i = 0;
                    foreach (Process p in processes)
                    {
                        instanceName = FormatInstanceName(p.ProcessName, i);
                        if (PerformanceCounterCategory.CounterExists("ID Process",
                            "Process"))
                        {
                            PerformanceCounter counter = new PerformanceCounter(
                                "Process", "ID Process", instanceName);
                            if (processId == counter.RawValue)
                            {
                                if (common.Debug)
                                    common.LogMessage(instanceName +
                                        " : " + processId);
                                cpuCounter = new PerformanceCounter("Process",
                                    "% Processor Time", instanceName);
                                break;
                            }
                            instanceName = "";
                        }
                        i++;
                    }
                }
            }
            return instanceName;
        }
        private string FormatInstanceName(string processName, int count)
        {
            string instanceName = string.Empty;
            if (count == 0)
                instanceName = processName;
            else
                instanceName = string.Format("{0}#{1}", processName, count);
            return instanceName;
        }
        public float NextValue()
        {
            float firstValue = 0, secondValue = 0;
            try
            {
                // firstValue always returns 0
                firstValue = cpuCounter.NextValue();
                // Need to wait 1 second to get actual CPU usage
                // Refer above blog post
                common.Wait(1);
                // Call once again NextValue to get the actual CPU usage
                secondValue = cpuCounter.NextValue();
                if (common.Debug)
                {
                    common.LogMessage("?First value: " + firstValue);
                    common.LogMessage("?Second value: " + secondValue);
                }
            }
            catch (Exception ex)
            {
                // Process doesn't exist
                common.LogMessage(ex);
                // adjust instance name based on process id
                GetInstanceName(processId);
                //cpuCounter.InstanceName = instanceName;
                common.Wait(1);
                secondValue = cpuCounter.NextValue();
                if (common.Debug)
                {
                    common.LogMessage("#First value: " + firstValue);
                    common.LogMessage("#Second value: " + secondValue);
                }
            }
            return secondValue;
        }
    }
    class ProcessStats
    {
        Common common;
        bool monitorProcess;
        ArrayList processList;
        Thread processMonitorThread;
        public ProcessStats(Common common)
        {
            this.common = common;
            monitorProcess = false;
            processMonitorThread = null;
            processList = new ArrayList();
        }
        private string GetProcessName(string processName)
        {
            if (String.IsNullOrEmpty(processName))
                return "";
            if (Path.GetExtension(processName) == String.Empty)
                return processName;
            return processName.Substring(0, processName.LastIndexOf('.'));
        }
        public long[] GetPhysicalMemoryUsage(string processName, bool monitor = false)
        {
            ArrayList memoryUsage = new ArrayList();
            try
            {
                processName = GetProcessName(processName);
                common.LogMessage(processName);
                Process[] ps = Process.GetProcessesByName(processName);
                foreach (Process p in ps)
                {
                    // Unable to get PrivateWorkingSet64 as displayed in
                    // Task Manager, so getting the closest one WorkingSet64
                    common.LogMessage("Memory used: {0}." +
                        p.WorkingSet64 / (1024 * 1024));
                    if (monitor)
                        common.LogProcessStat(string.Format("MEMINFO-{0}({1}) - {2}",
                            p.ProcessName, p.Id, p.WorkingSet64 / (1024 * 1024)));
                    // Output in MB (Linux compatible output)
                    // Working memory will be in bytes, to convert it to MB
                    // divide it by 1024*1024
                    memoryUsage.Add((long)(p.WorkingSet64 / (1024 * 1024)));
                }
                if (common.Debug)
                {
                    foreach (long value in memoryUsage)
                        common.LogMessage(value);
                }
                return memoryUsage.ToArray(typeof(long)) as long[];
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
                return memoryUsage.ToArray(typeof(long)) as long[];
            }
            finally
            {
                memoryUsage = null;
            }
        }
        private class ProcessInfo
        {
            // To pass multiple arguments to thread
            public bool monitor;
            public Process process;
            public ArrayList cpuUsage;
        }
        private void GetCurrentCpuUsage(object data)
        {
            bool monitor = ((ProcessInfo)data).monitor;
            Process process = ((ProcessInfo)data).process;
            ArrayList cpuUsage = ((ProcessInfo)data).cpuUsage;
            try
            {
                ProcessPerformanceCounter perf = new ProcessPerformanceCounter(
                            common, process.ProcessName, process.Id);
                double value = perf.NextValue();
                value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                common.LogMessage("value: " + value + " : " + process.Id);
                if (monitor)
                    common.LogProcessStat(string.Format("CPUINFO-{0}({1}) - {2}",
                        process.ProcessName, process.Id, value));
                cpuUsage.Add(value);
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
            }
        }
        public double[] GetCpuUsage(string processName, bool monitor = false)
        {
            ArrayList cpuUsage = new ArrayList();
            ArrayList threadPool = new ArrayList();
            try
            {
                processName = GetProcessName(processName);
                common.LogMessage(processName);
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process p in processes)
                {
                    try
                    {
                        // Need to call this once, before calling GetCurrentCpuUsage
                        ProcessPerformanceCounter perf = new ProcessPerformanceCounter(
                            common, processName, p.Id);
                        if (perf != null)
                            perf = null;
                        ProcessInfo processInfo = new ProcessInfo();
                        processInfo.process = p;
                        processInfo.monitor = monitor;
                        processInfo.cpuUsage = cpuUsage;
                        if (processes.Length > 1)
                        {
                            // Increased the performance of output
                            // else for running 10+ instance of an app
                            // takes lot of time
                            Thread thread = new Thread(new ParameterizedThreadStart(
                                GetCurrentCpuUsage));
                            thread.Start(processInfo);
                            threadPool.Add(thread);
                        }
                        else
                            GetCurrentCpuUsage(processInfo);
                    }
                    catch (Exception ex)
                    {
                        common.LogMessage(ex);
                    }
                }
                foreach (Thread thread in threadPool)
                {
                    // Wait for each thread to complete its job
                    thread.Join();
                }
                if (common.Debug)
                {
                    foreach (double value in cpuUsage)
                        common.LogMessage(value);
                }
                return cpuUsage.ToArray(typeof(double)) as double[];
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
                return cpuUsage.ToArray(typeof(double)) as double[];
            }
            finally
            {
                cpuUsage = null;
                threadPool = null;
            }
        }
        private void MonitorProcess(object interval)
        {
            while (monitorProcess)
            {
                try
                {
                    foreach (string processName in processList)
                    {
                        GetPhysicalMemoryUsage(processName,
                            true);
                        GetCpuUsage(processName, true);
                    }
                }
                catch (Exception ex)
                {
                    common.LogMessage(ex);
                }
                common.Wait((int)interval);
            }
        }
        public void StartProcessMonitor(string processName, int interval)
        {
            try
            {
                processName = GetProcessName(processName);
                common.LogMessage(processName);
                if (processList.IndexOf(processName) == -1)
                    processList.Add(processName);
                if (!monitorProcess)
                {
                    // Start monitoring
                    monitorProcess = true;
                    processMonitorThread = new Thread(new ParameterizedThreadStart(
                                    MonitorProcess));
                    if (interval <= 0)
                        // Watch every 2 seconds if invalid argument were present
                        interval = 2;
                    processMonitorThread.Start(interval);
                }
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
            }
        }
        public void StopProcessMonitor(string processName)
        {
            try
            {
                processName = GetProcessName(processName);
                common.LogMessage(processName);
                monitorProcess = false;
                if (processList.Count <= 0 && processMonitorThread != null)
                    processMonitorThread.Join();
                if (processList.IndexOf(processName) != -1)
                    processList.Remove(processName);
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
            }
        }
    }
}
