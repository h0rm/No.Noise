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

        public NoNoiseHelpDialog ()
        {
            Notebook notebook = new Notebook ();
            VBox box1 = new VBox ();
            Label text1 = new Label ("NoNoise general help text. NoNoise general help text. NoNoise general help text.");
            box1.PackStart (text1, true, true, 5);
            notebook.AppendPage (box1, new Label ("General"));

            VBox box2 = new VBox ();
            Label text2 = new Label ("NoNoise specific help text. NoNoise specific help text. NoNoise specific help text.");
            box2.PackStart (text2, true, true, 5);
            notebook.AppendPage (box2, new Label ("Specific"));

            Title = "NoNoise Help";
            VBox.PackStart (notebook, true, true, 5);
            SetDefaultSize (240, 180);
            Resizable = false;

            ShowAll ();
        }
    }
}
