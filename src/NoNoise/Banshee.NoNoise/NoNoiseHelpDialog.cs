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

using Mono.Addins;

using Gtk;

namespace Banshee.NoNoise
{
    public class NoNoiseHelpDialog : Gtk.Dialog
    {
        private readonly string getting_started = "While the NoNoise plug-in is activated, you can start the " +
            "NoNoise view via View > NoNoise Visualization. This will show a visualization of your music library " +
            "instead of the normal browser view.\n\nBefore you can start using the visualization, you have to scan " +
            "your library via Tools > NoNoise > Start NoNoise scan. Depending on the size of your library, this will " +
            "take some time (approximately 2.5s per song or 1500 songs per hour).\nYou can pause/resume the scan at " +
            "any time using the same menu entry.\n\nOnce your library has been completely scanned, you will see a " +
            "point cloud which represents your music library.\n";
        private readonly string general_help = "Once your library has been scanned, you can use the NoNoise plug-in " +
            "to create playlists.\n\nQuick Guide:\n* press the 'select' button\n* select tracks using the left mouse " +
            "button\n(* remove selected tracks from the visualization using the 'remove' button)\n* create a playlist " +
            "with the selected tracks by pressing the 'playlist' button\n\nUser Interface:\nIn the upper left corner " +
            "you have two buttons to zoom in and out. In the middle of the upper part of the view you have the panel " +
            "with all the other buttons. When you hover over a point in the visualization you see an info box in the " +
            "upper right corner showing information about the point. When you select (a) point(s) you see a similar " +
            "box in the lower right corner showing information about the selected point(s). In the bottom left corner " +
            "you see the status bar which tells you when you have to rescan the library and other useful information.\n\n" +
            "Buttons:\n* '+'/'-': The plus and minus buttons can be used to zoom in and out. Depending on the number of " +
            "songs in your library and the current zoom level this will also change the clustering level, i.e. clusters " +
            "will be split up when you zoom in and merged together when you zoom out. By hovering over the points you " +
            "see the current cluster size in the info box.\n" +
            "* 'select': This button toggles the selection mode. While in selection mode you can mark points by pressing " +
            "the left mouse button and drawing a line around the area you want to select. The endpoint of the line is " +
            "automatically connected to its starting point. Selected points remain selected if you leave the selection mode " +
            "so you can move the view and select more points. Selected points are shown in light red while semi-selected " +
            "points (i.e. clusters where some songs are selected but not all) are dark red.\n" +
            "* 'reset'/'clear': Pressing the reset button restores previously removed points. When points are selected " +
            "this button can be used to unselect the points again.\n" +
            "* 'remove': This button removes the selected points from the visualization.\n" +
            "* 'playlist': This button creates a new playlist with all selected songs.\n\n" +
            "Navigation:\n* move view: While not in selection mode you can move the view by holding the left mouse button.\n" +
            "* double click: A double click on a point will start playing the song(s) contained in this point.\n" +
            "* search function: Typing something into the search field, in the upper right corner, will filter the " +
            "visualization in the same way as it filters the normal music browser.\n\n" +
            "Colors:\n* points: Unselected points are white, selected points are light red and semi-selected points are dark " +
            "red.\n* background: The area which contains the points is black, the surrounding area is shown in gray.\n";
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
        private readonly string credits = "This plug-in was to a large extent inspired by Anita Lillie's " +
            "Masters thesis 'MusicBox'.\nhttp://thesis.flyingpudding.com/\n\n" +
            "ClutterFlow\nHelpful to see how to write Banshee plug-ins with clutter integration\n" +
            "Mathijs Dumon <mathijsken@hotmail.com>\n\n\n" +
            "NoNoise uses the following libraries/pieces of code:\n\n" +
            "Math.NET Iridium, part of the Math.NET Project\n" +
            "http://mathnet.opensourcedotnet.info\n" +
            "Copyright (c) 2002-2008, Christoph Rüegg,  http://christoph.ruegg.name\n" +
            "\t\t\t\t\tJoannes Vermorel, http://www.vermorel.com\n" +
            "GNU Lesser General Public License\nVersion 2.1, February 1999\n\n" +
            "Mirage\n" +
            "http://hop.at/mirage\n" +
            "Dominik Schnitzer <dominik@schnitzer.at>\n" +
            "GPL License Version 2\n\n" +
            "Quadtree Datastructure\nModified version of http://quadtree.svn.sourceforge.net/\n" +
            "John McDonald\nGary Texmo\n\n" +
            "Selection Algorithm (Winding number)\nhttp://softsurfer.com/Archive/algorithm_0103/algorithm_0103.htm\n" +
            "Dan Sunday" +
            "\n";

        public NoNoiseHelpDialog ()
        {
            Notebook notebook = new Notebook ();

            AddHelpPage (notebook, "Getting started", getting_started);
            AddHelpPage (notebook, "General", general_help);
            AddHelpPage (notebook, "Settings", settings_help);
            AddHelpPage (notebook, "Credits", credits);

            Button close_button = new Button ();
            close_button.Label = "Close";
            close_button.Clicked += new EventHandler (OnCloseButtonClicked);

            Title = "NoNoise Help";
            VBox.PackStart (notebook, true, true, 5);
            AddActionWidget (close_button, 0);
            SetDefaultSize (520, 380);
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
            tb.Text = AddinManager.CurrentLocalizer.GetString (text);
            tv.Buffer = tb;
            tv.WrapMode = WrapMode.Word;
            ScrolledWindow sw = new ScrolledWindow ();
            sw.AddWithViewport (tv);
            sw.SetSizeRequest (520, 380);
            box.PackStart (sw, true, true, 5);
            notebook.AppendPage (box, new Label (AddinManager.CurrentLocalizer.GetString (title)));
        }

        private void OnCloseButtonClicked (object sender, EventArgs e)
        {
            Destroy ();
        }
    }
}
