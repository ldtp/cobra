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
    public class ObjInfo
    {
        public int cbo, txt, btn, rbtn, chk, mnu, pane, hlnk;
        public int lbl, slider, ukn, lst, frm, header, headeritem, dlg;
        public int tab, tabitem, tbar, tree, tblc, tbl, scbr;
        public ObjInfo(bool dummyValue)
        {
            cbo = txt = btn = rbtn = chk = mnu = pane = hlnk = 0;
            lbl = slider = ukn = lst = frm = header = headeritem = 0;
            tab = tabitem = tbar = tree = tblc = tbl = dlg = scbr = 0;
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
                // For Linux compatibility
                return new CurrentObjInfo("ttbl", tree++);
            else if (type == ControlType.TreeItem)
                // For Linux compatibility
                return new CurrentObjInfo("tblc", tblc++);
            else if (type == ControlType.DataItem)
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
            else if (type == ControlType.Pane)
                return new CurrentObjInfo("pane", pane++);
            else if (type == ControlType.Hyperlink)
                return new CurrentObjInfo("hlnk", hlnk++);
            else if (type == ControlType.ScrollBar)
                return new CurrentObjInfo("scbr", scbr++);
            return new CurrentObjInfo("ukn", ukn++);
        }
    }
}
