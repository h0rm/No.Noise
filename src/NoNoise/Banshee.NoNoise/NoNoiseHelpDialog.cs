// 
// NoNoiseHelpDialog.cs
// 
// Author:
//   thomas <${AuthorEmail}>
// 
// Copyright (c) 2011 thomas
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

using Gtk;

namespace Banshee.NoNoise
{
    public class NoNoiseHelpDialog : Gtk.Dialog
    {
        private readonly string general_help =
@"Help text!

This is going to be the help text!
NoNoise general help text. NoNoise general help text. NoNoise general help text. NoNoise general help text.
";
        private readonly string specific_help =
@"Help text!

This is going to be the specific help text!
NoNoise specific help text. NoNoise specific help text. NoNoise specific help text. NoNoise specific help text.
";

        public NoNoiseHelpDialog ()
        {
            Notebook notebook = new Notebook ();
            VBox box1 = new VBox ();
            TextView tv1 = new TextView ();
            tv1.Editable = false;
            tv1.CursorVisible = false;
//            tv.Sensitive = false;
            TextBuffer tb1 = new TextBuffer (new TextTagTable ());
            tb1.Text = general_help;
            tv1.Buffer = tb1;
            tv1.WrapMode = WrapMode.Word;
//            tv.SetSizeRequest (460, 380);
            ScrolledWindow sw1 = new ScrolledWindow ();
            sw1.AddWithViewport (tv1);
            sw1.SetSizeRequest (460, 380);
            box1.PackStart (sw1, true, true, 5);
            notebook.AppendPage (box1, new Label ("General"));

            VBox box2 = new VBox ();
            TextView tv2 = new TextView ();
            tv2.Editable = false;
            tv2.CursorVisible = false;
            TextBuffer tb2 = new TextBuffer (new TextTagTable ());
            tb2.Text = specific_help;
            tv2.Buffer = tb2;
            tv2.WrapMode = WrapMode.Word;
            ScrolledWindow sw2 = new ScrolledWindow ();
            sw2.AddWithViewport (tv2);
            sw2.SetSizeRequest (460, 380);
            box2.PackStart (sw2, true, true, 5);
            notebook.AppendPage (box2, new Label ("Specific"));

            Button close_button = new Button ();
            close_button.Label = "Close";
            close_button.Clicked += new EventHandler (OnCloseButtonClicked);

            Title = "NoNoise Help";
            VBox.PackStart (notebook, true, true, 5);
            AddActionWidget (close_button, 0);
            SetDefaultSize (460, 380);
            Resizable = false;

            ShowAll ();
        }

        private void OnCloseButtonClicked (object sender, EventArgs e)
        {
            Destroy ();
        }
    }
}
