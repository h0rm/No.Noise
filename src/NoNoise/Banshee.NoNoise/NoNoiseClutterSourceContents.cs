// 
// NoNoiseClutterSourceContents.cs
// 
// Author:
//   horm <${AuthorEmail}>
// 
// Copyright (c) 2011 horm
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
using NoNoise.Visualization.Util;
using NoNoise.Visualization;
using Banshee.Sources.Gui;
using Banshee.Sources;
using Gtk;
using Banshee.Library;
using Clutter;
using GLib;

namespace Banshee.NoNoise
{
    public class NoNoiseClutterSourceContents : ISourceContents
    {

        View view;

        public NoNoiseClutterSourceContents (bool pcadata)
        {
            //Gtk.Box box = new Gtk.HBox(true,0);
            if (!GLib.Thread.Supported)
                GLib.Thread.Init();

            Clutter.Threads.Init();
            Clutter.Application.InitForToolkit ();

            Hyena.Log.Information ("ClutterView creation");

            //cv = new ClutterView();
            //cv.Init();

            view = new View();
//
            if (pcadata)
                view.GetPcaCoordinates ();
            else
                view.TestGenerateData();
            
            //GLib.Thread thread = new GLib.Thread(cv.Init);

            this.Widget.Shown += delegate{
                Hyena.Log.Information("Widget shown");
                //(w as ClutterView).GenerateOverview();
            };
        }

        ~ NoNoiseClutterSourceContents ()
        {
            Dispose ();
        }

        public bool SetSource (ISource source)
        {

            if ((source as MusicLibrarySource) == null)
                return false;

            if ((source as MusicLibrarySource) == this.Source)
                return true;

            return true;

        }

        public void Dispose ()
        {

            if (view != null) {
                view.Dispose ();
                view = null;
            }

            Clutter.Application.Quit ();
        }
        public void ResetSource () { }
        public Widget Widget { get { return view; } }
        public ISource Source { get { return null; } }
    }
}

