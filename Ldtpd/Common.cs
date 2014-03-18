/*
 * Cobra WinLDTP 4.0
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
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ldtpd
{
    public class Common
    {
        bool debug = false;
        System.IO.StreamWriter sw;
        StackTrace stackTrace = null;
        System.IO.FileStream fs = null;
        internal string writeToFile = null;

        public bool Debug
        {
            get { return debug; }
        }

        public Stack LogStack
        {
            get { return logStack; }
        }
        Stack logStack = new Stack(100);

        public Common(bool debug)
        {
            this.debug = debug;
            this.writeToFile = Environment.GetEnvironmentVariable("LDTP_DEBUG_FILE");
            if (writeToFile != null)
            {
                fs = new System.IO.FileStream(writeToFile,
                    System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);
                sw = new System.IO.StreamWriter(fs);
            }
            if (debug || writeToFile != null)
            {
                string tmpDebug = Environment.GetEnvironmentVariable("LDTP_DEBUG_LEVEL");
                if (tmpDebug != null && tmpDebug == "3")
                    stackTrace = new StackTrace(1, true);
            }
        }

        ~Common()
        {
            if (fs != null)
            {
                sw.Flush();
                fs.Close();
                fs = null;
            }
        }

        public void Wait(double waitTime)
        {
            Thread.Sleep((int)(waitTime * 1000));
            // Collect all generations of memory.
            GC.Collect();
        }

        public void LogProcessStat(Object o)
        {
            logStack.Push(o);
        }

        public void LogMessage(Object o)
        {
            if (debug)
            {
                string callerName = stackTrace == null ? "" : stackTrace.ToString();
                Console.WriteLine("{0}:{1}", callerName, o);
                try
                {
                    if (logStack.Count == 1000)
                    {
                        // Removes 1 log, if it has 1000 instances
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
            if (fs != null)
            {
                string callerName = stackTrace == null ? "" : stackTrace.ToString();
                // Write content to log file
                sw.WriteLine("{0}:{1}", callerName, o);
            }
        }
        public bool WildcardMatch(string stringToSearch, string searchPhrase)
        {
            if (searchPhrase.Contains("*") || searchPhrase.Contains("?"))
            {
                String tmp = Regex.Replace(searchPhrase, @"\*", @".*");
                tmp = Regex.Replace(tmp, @"\?", @".");
                tmp = @"\A(?ms)" + tmp + @"\Z(?ms)";
                Regex rx = new Regex(tmp, RegexOptions.Compiled |
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                    RegexOptions.CultureInvariant);
                return rx.IsMatch(stringToSearch);
            }
            else
                return stringToSearch == searchPhrase;
        }
    }
}
