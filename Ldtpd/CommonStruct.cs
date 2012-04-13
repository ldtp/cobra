using System;
using System.Windows.Automation;

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
        public CurrentObjInfo GetObjectType(AutomationElement e)
        {
            ControlType type = e.Current.ControlType;
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
}
