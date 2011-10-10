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
        private readonly string settings_help = "While the NoNoise plug-in is activated, you " +
            "can access its settings via Edit > Preferences and switching to the NoNoise tab.\n\n\nPCA:\n" +
            "In this section you can adjust the features for the Principal Component Analysis (PCA). If you are not " +
            "familiar with PCA, a simplified explanation could be: This is used to convert multidimensional song data " +
            "into 2D data (i.e. coordinates) for the visualization.\n\nMFCC Vector:\n" +
            "Mel-Frequency Cepstral Coefficients (MFCCs) are mathematical coefficients for sound modeling. For each " +
            "song, a large matrix with MFCCs is computed using the Banshee Mirage plug-in. You can try different " +
            "vectors of this matrix to enhance the visualization of your music library.\n\n" +
            "* Mean: A vector containing the mean value of each row of the matrix\n" +
            "* SquaredMean: A vector containing the square root of the sum of squared values of each row of the matrix\n" +
            "* Median: A vector containing the median value of each row of the matrix\n" +
            "* Minimum: A vector containing the smallest value of each row of the matrix\n" +
            "* Maximum: A vector containing the largest value of each row of the matrix\n\n" +
            "Song Duration:\nIf this is checked, the song duration is also taken as feature for the PCA.\n";

        public NoNoiseHelpDialog ()
        {
            Notebook notebook = new Notebook ();

            AddHelpPage (notebook, "General", general_help);
            AddHelpPage (notebook, "Settings", settings_help);

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

        private void AddHelpPage (Notebook notebook, string title, string text)
        {
            VBox box = new VBox ();
            TextView tv = new TextView ();
            tv.Editable = false;
            tv.CursorVisible = false;
            TextBuffer tb = new TextBuffer (new TextTagTable ());
            tb.Text = text;
            tv.Buffer = tb;
            tv.WrapMode = WrapMode.Word;
            ScrolledWindow sw = new ScrolledWindow ();
            sw.AddWithViewport (tv);
            sw.SetSizeRequest (460, 380);
            box.PackStart (sw, true, true, 5);
            notebook.AppendPage (box, new Label (title));
        }

        private void OnCloseButtonClicked (object sender, EventArgs e)
        {
            Destroy ();
        }
    }
}
