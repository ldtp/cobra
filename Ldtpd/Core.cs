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
using System.IO;
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
using System.Windows.Automation.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

//[assembly: CLSCompliant(true)]
namespace Ldtpd
{
    public class Core : Utils
    {
        ProcessStats ps;
        public Core(WindowList windowList, Common common, bool debug = false)
            : base(windowList, common, debug)
        {
            ps = new ProcessStats(common);
        }
        [XmlRpcMethod("isalive", Description = "Client checks whether the server runs.")]
        public bool IsAlive()
        {
            return true;
        }
        [XmlRpcMethod("getlastlog", Description = "Get last log from the stack.")]
        public string GetLastLog()
        {
            if (common.LogStack.Count > 0)
                return (string)common.LogStack.Pop();
            return "";
        }
        [XmlRpcMethod("wait", Description = "Wait a given amount of seconds")]
        public int Wait(object waitTime)
        {
            int time;
            try
            {
                time = Convert.ToInt32(waitTime, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                time = 5;
                LogMessage(ex);
            }
            if (time < 1)
                time = 1;
            InternalWait(time);
            return 1;
        }
        [XmlRpcMethod("getobjectlist", Description = "Get object list")]
        public String[] GetObjectList(String windowName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetObjectList(windowName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getwindowlist", Description = "Get window list")]
        public String[] GetWindowList()
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetWindowList();
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("waittillguiexist",
            Description = "Wait till a window or component exists.")]
        public int WaitTillGuiExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            return InternalWaitTillGuiExist(windowName, objName, guiTimeOut);
        }
        [XmlRpcMethod("waittillguinotexist",
            Description = "Wait till a window or component does not exists.")]
        public int WaitTillGuiNotExist(String windowName,
            String objName = null, int guiTimeOut = 30)
        {
            return InternalWaitTillGuiNotExist(windowName, objName, guiTimeOut);
        }
        [XmlRpcMethod("guiexist", Description = "Checks whether a window or component exists.")]
        public int GuiExist(String windowName, String objName = null)
        {
            return InternalGuiExist(windowName, objName);
        }
        [XmlRpcMethod("objectexist", Description = "Checks whether a component exists.")]
        public int ObjectExist(String windowName, String objName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.ObjectExist(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("objtimeout ",
            Description = "Object timeout period, default 5 seconds.")]
        public int ObjectTimeOut(int objectTimeOut)
        {
            if (objectTimeOut <= 0)
                this.objectTimeOut = 5;
            else
                this.objectTimeOut = objectTimeOut;
            return 1;
        }
        [XmlRpcMethod("selectmenuitem",
            Description = "Select (click) a menuitem.")]
        public int SelectMenuItem(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.SelectMenuItem(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("maximizewindow",
            Description = "Maximize window.")]
        public int MaximizeWindow(String windowName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.MaximizeWindow(windowName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("minimizewindow",
            Description = "Minimize window.")]
        public int MinimizeWindow(String windowName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.MinimizeWindow(windowName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("closewindow",
            Description = "Close window.")]
        public int CloseWindow(String windowName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.CloseWindow(windowName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("menucheck",
            Description = "Check (click) a menuitem.")]
        public int MenuCheck(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.MenuCheck(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("menuuncheck",
            Description = "Uncheck (click) a menuitem.")]
        public int MenuUnCheck(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.MenuUnCheck(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("verifymenucheck",
            Description = "Verify a menuitem is unchecked.")]
        public int VerifyMenuCheck(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.VerifyMenuCheck(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("verifymenuuncheck",
            Description = "Verify a menuitem is unchecked.")]
        public int VerifyMenuUnCheck(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.VerifyMenuUnCheck(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("menuitemenabled",
            Description = "Verify a menuitem is enabled.")]
        public int MenuItemEnabled(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.MenuItemEnabled(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("doesmenuitemexist",
            Description = "Does a menu item exist.")]
        public int DoesSelectMenuItemExist(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.DoesSelectMenuItemExist(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("listsubmenus",
            Description = "List sub menu item.")]
        public String[] ListSubMenus(String windowName, String objName)
        {
            Menu menu = new Menu(this);
            try
            {
                return menu.ListSubMenus(windowName, objName);
            }
            finally
            {
                menu = null;
            }
        }
        [XmlRpcMethod("stateenabled",
            Description = "Checks whether an object state enabled.")]
        public int StateEnabled(String windowName, String objName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.StateEnabled(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("click", Description = "Click item.")]
        public int Click(String windowName, String objName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.Click(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("selectindex",
            Description = "Select combo box / layered pane item based on index.")]
        public int SelectIndex(String windowName, String objName, int index)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.SelectIndex(windowName, objName, index);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("getallitem",
            Description = "Get all combo box item based on name.")]
        public string[] GetAllItem(String windowName, String objName)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.GetAllItem(windowName, objName);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("selectitem",
            Description = "Select combo box item based on name.")]
        public int SelectItem(String windowName, String objName, String item)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.SelectItem(windowName, objName, item);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("showlist",
            Description = "Show combo box item based on name.")]
        public int ShowList(String windowName, String objName)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.ShowList(windowName, objName);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("hidelist",
            Description = "Hide combo box item based on name.")]
        public int HideList(String windowName, String objName)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.HideList(windowName, objName);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("comboselect",
            Description = "Select combo box / layered pane item based on name.")]
        public int ComboSelect(String windowName, String objName, String item)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.ComboSelect(windowName, objName, item);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("verifydropdown",
            Description = "Verify if combo box drop down list in the current dialog is visible.")]
        public int VerifyDropDown(String windowName, String objName)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.VerifyDropDown(windowName, objName);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("verifyshowlist",
            Description = "Verify if combo box drop down list in the current dialog is visible.")]
        public int VerifyShowList(String windowName, String objName)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.VerifyDropDown(windowName, objName);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("verifyhidelist",
            Description = "Verify if combo box drop down list in the current dialog is not visible.")]
        public int VerifyHideList(String windowName, String objName)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.VerifyHideList(windowName, objName);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("verifyselect",
            Description = "Select combo box / layered pane item based on name.")]
        public int VerifyComboSelect(String windowName, String objName, String item)
        {
            Combobox comboBox = new Combobox(this);
            try
            {
                return comboBox.VerifyComboSelect(windowName, objName, item);
            }
            finally
            {
                comboBox = null;
            }
        }
        [XmlRpcMethod("settextvalue",
            Description = "Type string sequence.")]
        public int SetTextValue(String windowName, String objName, String value)
        {
            Text text = new Text(this);
            try
            {
                return text.SetTextValue(windowName, objName, value);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("gettextvalue",
            Description = "Get text value")]
        public String GetTextValue(String windowName, String objName,
            int startPos = 0, int endPos = 0)
        {
            Text text = new Text(this);
            try
            {
                return text.GetTextValue(windowName, objName, startPos, endPos);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("appendtext",
            Description = "Append string with existing conent.")]
        public int AppendText(String windowName,
            String objName, string value)
        {
            Text text = new Text(this);
            try
            {
                return text.AppendText(windowName, objName, value);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("copytext",
            Description = "Copy text to clipboard.")]
        public int CopyText(String windowName,
            String objName, int start, int end = -1)
        {
            Text text = new Text(this);
            try
            {
                return text.CopyText(windowName, objName, start, end);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("cuttext",
            Description = "Cut text from existing text.")]
        public int CutText(String windowName,
            String objName, int start, int end = -1)
        {
            Text text = new Text(this);
            try
            {
                return text.CutText(windowName, objName, start, end);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("deletetext",
            Description = "Delete text from existing text.")]
        public int DeleteText(String windowName,
            String objName, int start, int end = -1)
        {
            Text text = new Text(this);
            try
            {
                return text.DeleteText(windowName, objName, start, end);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("getcharcount",
            Description = "Get character count.")]
        public int GetCharCount(String windowName,
            String objName)
        {
            Text text = new Text(this);
            try
            {
                return text.GetCharCount(windowName, objName);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("inserttext",
            Description = "Insert text in between existing text.")]
        public int InsertText(String windowName,
            String objName, int postion, string value)
        {
            Text text = new Text(this);
            try
            {
                return text.InsertText(windowName, objName, postion, value);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("istextstateenabled",
            Description = "Is text state enabled.")]
        public int IsTextStateEnabled(String windowName, String objName)
        {
            Text text = new Text(this);
            try
            {
                return text.IsTextStateEnabled(windowName, objName);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("pastetext",
            Description = "Paste text from clipboard.")]
        public int PasteText(String windowName,
            String objName, int postion)
        {
            Text text = new Text(this);
            try
            {
                return text.PasteText(windowName, objName, postion);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("verifysettext",
            Description = "Verify the text set is correct.")]
        public int VerifySetText(String windowName,
            String objName, string value)
        {
            Text text = new Text(this);
            try
            {
                return text.VerifySetText(windowName, objName, value);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("verifypartialmatch",
            Description = "Verify partial text set is correct.")]
        public int VerifyPartialText(String windowName,
            String objName, string value)
        {
            Text text = new Text(this);
            try
            {
                return text.VerifyPartialText(windowName, objName, value);
            }
            finally
            {
                text = null;
            }
        }
        [XmlRpcMethod("setvalue",
            Description = "Set value.")]
        public int SetValue(String windowName,
            String objName, double value)
        {
            Value v = new Value(this);
            try
            {
                return v.SetValue(windowName, objName, value);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getvalue", Description = "Get value")]
        public double GetValue(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetValue(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getslidervalue", Description = "Get value")]
        public double GetSliderValue(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetValue(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getminvalue", Description = "Get min value")]
        public double GetMinValue(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetMinValue(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getmaxvalue", Description = "Get max value")]
        public double GetMaxValue(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetMaxValue(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getmin", Description = "Get min value")]
        public double GetMin(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetMinValue(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getmax", Description = "Get max value")]
        public double GetMax(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetMaxValue(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("getminincrement", Description = "Get min increment value")]
        public double GetMinIncrement(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.GetMinIncrement(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("increase",
            Description = "Increment value by number of iterations")]
        public int Increase(String windowName, String objName, int iterations)
        {
            Value v = new Value(this);
            try
            {
                return v.Increase(windowName, objName, iterations);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("decrease",
            Description = "Decrement value by number of iterations")]
        public int Decrease(String windowName, String objName, int iterations)
        {
            Value v = new Value(this);
            try
            {
                return v.Decrease(windowName, objName, iterations);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("setmin", Description = "Set min value")]
        public int SetMin(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.SetMin(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("setmax", Description = "Set max value")]
        public int SetMax(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.SetMax(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("verifysetvalue",
            Description = "Verify that the set value is correct.")]
        public int VerifySetValue(String windowName,
            String objName, double value)
        {
            Value v = new Value(this);
            try
            {
                return v.VerifySetValue(windowName, objName, value);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("verifysliderhorizontal",
            Description = "Verify slider is horizontal.")]
        public int VerifySliderHorizontal(String windowName,String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.VerifySliderHorizontal(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("verifyslidervertical",
            Description = "Verify slider is vertical.")]
        public int VerifySliderVertical(String windowName, String objName)
        {
            Value v = new Value(this);
            try
            {
                return v.VerifySliderVertical(windowName, objName);
            }
            finally
            {
                v = null;
            }
        }
        [XmlRpcMethod("check", Description = "Check radio button / checkbox")]
        public int Check(String windowName, String objName)
        {
	    return InternalCheckObject(windowName, objName, "Check");
        }
        [XmlRpcMethod("uncheck", Description = "UnCheck radio button / checkbox")]
        public int UnCheck(String windowName, String objName)
        {
	    return InternalCheckObject(windowName, objName, "UnCheck");
        }
        [XmlRpcMethod("verifycheck",
            Description = "Verify radio button / checkbox is checked")]
        public int VerifyCheck(String windowName, String objName)
        {
            try
            {
                return InternalCheckObject(windowName, objName, "VerifyCheck");
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("verifyuncheck",
            Description = "Verify radio button / checkbox is unchecked")]
        public int VerifyUnCheck(String windowName, String objName)
        {
            try
            {
                return InternalCheckObject(windowName, objName, "VerifyUncheck");
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
            return 0;
        }
        [XmlRpcMethod("selecttab",
            Description = "Select tab based on name.")]
        public int SelectTab(String windowName,
            String objName, String tabName)
        {
            Tab tab = new Tab(this);
            try
            {
                return tab.SelectTab(windowName, objName, tabName);
            }
            finally
            {
                tab = null;
            }
        }
        [XmlRpcMethod("selecttabindex",
            Description = "Select tab based on index.")]
        public int SelectTabIndex(String windowName,
            String objName, int index)
        {
            Tab tab = new Tab(this);
            try
            {
                return tab.SelectTabIndex(windowName, objName, index);
            }
            finally
            {
                tab = null;
            }
        }
        [XmlRpcMethod("gettabname", Description = "Get tab based on index.")]
        public String GetTabName(String windowName,
            String objName, int index)
        {
            Tab tab = new Tab(this);
            try
            {
                return tab.GetTabName(windowName, objName, index);
            }
            finally
            {
                tab = null;
            }
        }
        [XmlRpcMethod("gettabcount", Description = "Get tab count.")]
        public int GetTabCount(String windowName, String objName)
        {
            Tab tab = new Tab(this);
            try
            {
                return tab.GetTabCount(windowName, objName);
            }
            finally
            {
                tab = null;
            }
        }
        [XmlRpcMethod("verifytabname",
            Description = "Verify tab name selected or not.")]
        public int VerifyTabName(String windowName,
            String objName, String tabName)
        {
            Tab tab = new Tab(this);
            try
            {
                return tab.VerifyTabName(windowName, objName, tabName);
            }
            finally
            {
                tab = null;
            }
        }
        [XmlRpcMethod("doesrowexist",
            Description = "Does the given row text exist in tree item or list item.")]
        public int DoesRowExist(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.DoesRowExist(windowName, objName, text,
                    partialMatch);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("selectrow",
            Description = "Select the given row in tree or list item.")]
        public int SelectRow(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.SelectRow(windowName, objName, text, partialMatch);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("verifyselectrow",
            Description = "Verify the given row in tree or list item is selected.")]
        public int VerifySelectRow(String windowName, String objName,
            String text, bool partialMatch = false)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.VerifySelectRow(windowName, objName, text, partialMatch);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("selectrowpartialmatch",
            Description = "Select the given row partial match in tree or list item.")]
        public int SelectRowPartialMatch(String windowName, String objName,
            String text)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.SelectRowPartialMatch(windowName, objName, text);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("selectrowindex",
            Description = "Select tab based on index.")]
        public int SelectRowIndex(String windowName,
            String objName, int index)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.SelectRowIndex(windowName, objName, index);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("expandtablecell",
            Description = "Expand or contract the tree table cell on the row index.")]
        public int ExpandTableCell(String windowName,
            String objName, int index)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.ExpandTableCell(windowName, objName, index);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("getcellvalue",
            Description = "Get tree table cell value on the row index.")]
        public String GetCellValue(String windowName,
            String objName, int row, int column = 0)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.GetCellValue(windowName, objName, row, column);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("gettablerowindex",
            Description = "Get the id of the row containing the given cell value")]
        public int GetTableRowIndex(String windowName,
            String objName, String cellValue)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.GetTableRowIndex(windowName, objName, cellValue);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("getrowcount",
            Description = "Get tree table cell row count.")]
        public int GetRowCount(String windowName, String objName)
        {
            Tree tree = new Tree(this);
            try
            {
                return tree.GetRowCount(windowName, objName);
            }
            finally
            {
                tree = null;
            }
        }
        [XmlRpcMethod("grabfocus",
            Description = "Grab focus of given element.")]
        public int GrabFocus(String windowName, String objName = null)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GrabFocus(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("handletablecell", Description = "Handle table cell.")]
        public int HandleTableCell()
        {
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("unhandletablecell", Description = "Unhandle table cell.")]
        public int UnHandleTableCell()
        {
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("remap", Description = "Remap window info.")]
        public int Remap(String windowName)
        {
            if (String.IsNullOrEmpty(windowName))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("activatetext", Description = "Activate text.")]
        public int ActivateText(String windowName, String objName)
        {
            if (String.IsNullOrEmpty(windowName) ||
                String.IsNullOrEmpty(objName))
            {
                throw new XmlRpcFaultException(123, "Argument cannot be empty.");
            }
            // For Linux compatibility
            return 1;
        }
        [XmlRpcMethod("generatemouseevent",
            Description = "Generate mouse event.")]
        public int GenerateMouseEvent(int x, int y, String type = "b1p")
        {
            Mouse mouse = new Mouse(this);
            try
            {
                return mouse.GenerateMouseEvent(x, y, type);
            }
            finally
            {
                mouse = null;
            }
        }
        [XmlRpcMethod("simulatemousemove",
            Description = "Simulate mouse move.")]
        public int SimulateMouseMove(int source_x, int source_y, int dest_x, int dest_y, double delay = 0.0)
        {
            Mouse mouse = new Mouse(this);
            try
            {
                return mouse.SimulateMouseMove(source_x, source_y, dest_x, dest_y, delay);
            }
            finally
            {
                mouse = null;
            }
        }
        [XmlRpcMethod("launchapp", Description = "Launch application.")]
        public int LaunchApp(string cmd, string[] args, int delay = 5,
            int env = 1, string lang = null)
        {
            Process ps = new Process();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();

                psi.FileName = cmd;

                if (args != null)
                {
                    // Space separated arguments
                    psi.Arguments = string.Join(" ", args);
                }

                psi.UseShellExecute = true;
                ps.StartInfo = psi;
                ps.Start();
                Thread thread = new Thread(new ParameterizedThreadStart(
                    InternalLaunchApp));
                // Clean up in different thread
                thread.Start(ps);
                Wait(delay);
                return 1;
            }
            catch (Exception ex)
            {
                LogMessage(ex);
                throw new XmlRpcFaultException(123,
                    "Unhandled exception: " + ex.Message);
            }
            finally
            {
                ps = null;
            }
        }
        [XmlRpcMethod("imagecapture", Description = "Launch application.")]
        /*
         * Return base64 encoded string, required for LDTPv2
         * */
        public string ImageCapture(string windowName = null,
            int x = 0, int y = 0, int width = -1, int height = -1)
        {
            Image image = new Image(this);
            try
            {
                return image.Capture(windowName, x, y, width, height);
            }
            finally
            {
                image = null;
            }
        }
        [XmlRpcMethod("hasstate",
            Description = "Verifies that the object has given state.")]
        public int HasState(String windowName, String objName,
            String state, int guiTimeOut = 0)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.HasState(windowName, objName, state, guiTimeOut);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getallstates",
            Description = "Get all the object states.")]
        public string[] GetAllStates(String windowName, String objName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetAllStates(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getobjectsize", Description = "Get object size.")]
        public int[] GetObjectSize(String windowName, String objName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetObjectSize(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getwindowsize", Description = "Get window size.")]
        public int[] GetWindowSize(String windowName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetWindowSize(windowName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getobjectinfo", Description = "Get object info.")]
        public string[] GetObjectInfo(String windowName, String objName)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetObjectInfo(windowName, objName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getobjectproperty", Description = "Get object property.")]
        public string GetObjectProperty(String windowName, String objName,
            string property)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetObjectProperty(windowName, objName, property);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("getchild", Description = "Get child.")]
        public string[] GetChild(String windowName, String childName = null,
            string role = null, string parentName = null)
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetChild(windowName, childName, role, parentName);
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("enterstring", Description = "Generate key event.")]
        public int EnterString(string windowName, string objName = null,
            string data = null)
        {
            Keyboard keyboard = new Keyboard(this);
            try
            {
                return keyboard.EnterString(windowName, objName, data);
            }
            finally
            {
                keyboard = null;
            }
        }
        [XmlRpcMethod("generatekeyevent", Description = "Generate key event.")]
        public int GenerateKeyEvent(string data)
        {
            Keyboard keyboard = new Keyboard(this);
            try
            {
                return keyboard.GenerateKeyEvent(data);
            }
            finally
            {
                keyboard = null;
            }
        }
        [XmlRpcMethod("keypress", Description = "Key press.")]
        public int KeyPress(string data)
        {
            Keyboard keyboard = new Keyboard(this);
            try
            {
                return keyboard.KeyPress(data);
            }
            finally
            {
                keyboard = null;
            }
        }
        [XmlRpcMethod("keyrelease", Description = "Key release.")]
        public int KeyRelease(string data)
        {
            Keyboard keyboard = new Keyboard(this);
            try
            {
                return keyboard.KeyRelease(data);
            }
            finally
            {
                keyboard = null;
            }
        }
        [XmlRpcMethod("mouseleftclick",
            Description = "Mouse left click on an object.")]
        public int MouseLeftClick(String windowName, String objName)
        {
            return Click(windowName, objName);
        }
        [XmlRpcMethod("getapplist",
            Description = "Get the current running application list.")]
        public string[] GetAppList()
        {
            Generic generic = new Generic(this);
            try
            {
                return generic.GetAppList();
            }
            finally
            {
                generic = null;
            }
        }
        [XmlRpcMethod("poll_events",
            Description = "poll events internal only.")]
        public string PollEvents()
        {
            if (windowList.windowCallbackEvent.Count > 0)
            {
                // Get first element
                string s = (string)windowList.windowCallbackEvent[0];
                // Remove first element, since the event is delivered
                windowList.windowCallbackEvent.RemoveAt(0);
                return s;
            }
            return "";
        }
        [XmlRpcMethod("onwindowcreate",
            Description = "On window create callback.")]
        public int OnWindowCreate(string windowName)
        {
            windowList.WatchWindow(windowName);
            return 1;
        }
        [XmlRpcMethod("removecallback",
            Description = "Remove callback.")]
        public int RemoveCallback(string windowName)
        {
            windowList.UnwatchWindow(windowName);
            return 1;
        }
        [XmlRpcMethod("getcpustat",
            Description = "Get CPU stat for the give process name.")]
        public double[] GetCpuStat(string processName)
        {
            ProcessStats ps = new ProcessStats(common);
            try
            {
                return ps.GetCpuUsage(processName);
            }
            finally
            {
                ps = null;
            }
        }
        [XmlRpcMethod("getmemorystat",
            Description = "Get memory stat.")]
        public long[] GetMemoryStat(string processName)
        {
            ProcessStats ps = new ProcessStats(common);
            try
            {
                return ps.GetPhysicalMemoryUsage(processName);
            }
            finally
            {
                ps = null;
            }
        }
        [XmlRpcMethod("startprocessmonitor",
            Description = "Start memory and CPU monitoring," +
            " with the time interval between each process scan.")]
        public int StartProcessMonitor(string processName, int interval = 2)
        {
            ps.StartProcessMonitor(processName, interval);
            return 1;
        }
        [XmlRpcMethod("stopprocessmonitor",
            Description = "Stop memory and CPU monitoring.")]
        public int StopProcessMonitor(string processName)
        {
            ps.StopProcessMonitor(processName);
            return 1;
        }
    }
}
