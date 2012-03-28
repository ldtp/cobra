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
