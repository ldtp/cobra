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
