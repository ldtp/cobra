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
using System.Diagnostics;
using System.Windows.Automation;

namespace Ldtpd
{
    internal class InternalTreeWalker
    {
        public TreeWalker walker;
        public InternalTreeWalker()
        {
            // TreeWalker instance as a global variable started consuming more
            // memory over a period of time, moving the code to individual methods
            // kept the memory usage low
            // Ignore Ldtpd from list of applications
            Condition condition1 = new PropertyCondition(AutomationElement.ProcessIdProperty,
                Process.GetCurrentProcess().Id);
            Condition condition2 = new AndCondition(new Condition[] {
                System.Windows.Automation.Automation.ControlViewCondition,
                new NotCondition(condition1)});
            walker = new TreeWalker(condition2);
        }
        ~InternalTreeWalker()
        {
            walker = null;
        }
    }
}
