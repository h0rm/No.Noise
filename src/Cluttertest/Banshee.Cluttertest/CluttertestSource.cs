//
// CluttertestSource.cs
//
// Authors:
//   Cool Extension Author <cool.extension@author.com>
//
// Copyright (C) 2011 Cool Extension Author
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Addins;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Clutter;
using Gtk;
using GLib;
using System.Threading;

namespace Banshee.Cluttertest
{

    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
    public class CluttertestSource : Banshee.Sources.Source
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;

        public CluttertestSource () : base (AddinManager.CurrentLocalizer.GetString ("Cluttertest"),
                                               AddinManager.CurrentLocalizer.GetString ("Cluttertest"),
		                                       sort_order,
		                                       "extension-unique-id")
        {
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new CustomView ());

            Hyena.Log.Information ("Testing!  Cluttertest source has been instantiated!");
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { return 0; }
        }

        private class CustomView : ISourceContents
        {
            //Gtk.Label label = new Gtk.Label ("Cluttertest extension is working!");
            //ClutterView cv;
            View view;

            public CustomView ()
            {
                //Gtk.Box box = new Gtk.HBox(true,0);
                if (!GLib.Thread.Supported) GLib.Thread.Init();
                Clutter.Threads.Init();
                ClutterHelper.Init();

                Hyena.Log.Information ("ClutterView creation");

                //cv = new ClutterView();
                //cv.Init();

                view = new View();
                view.TestGenerateData();
                //GLib.Thread thread = new GLib.Thread(cv.Init);

                this.Widget.Shown += delegate{
                    Hyena.Log.Information("Widget shown");
                    //(w as ClutterView).GenerateOverview();
                };
            }

            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get { return view; } }
            public ISource Source { get { return null; } }

        }

    }
}
