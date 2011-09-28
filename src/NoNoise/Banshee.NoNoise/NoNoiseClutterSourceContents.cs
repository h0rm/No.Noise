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
using System.Collections.Generic;
using NoNoise.Visualization.Util;
using NoNoise.Visualization;
using Banshee.Sources.Gui;
using Banshee.Sources;
using Gtk;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Playlist;
using Mono.Unix;

namespace Banshee.NoNoise
{
    public class NoNoiseClutterSourceContents : ISourceContents
    {

        MusicLibrarySource source;
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
            Clutter.Threads.Enter ();
            view = new View();
//
            if (pcadata)
                view.GetPcaCoordinates ();
            else
                view.TestGenerateData();

            Clutter.Threads.Leave ();
            //GLib.Thread thread = new GLib.Thread(cv.Init);

            view.OnAddToPlaylist += HandleViewOnAddToPlaylist;

            Banshee.ServiceStack.Application.ClientStarted += delegate {
                Clutter.Threads.Enter ();
                view.FinishedInit ();
                Clutter.Threads.Leave ();
            };

        }

        void HandleViewOnAddToPlaylist (object sender, View.AddToPlaylistEventArgs args)
        {

            if (args.SongIDs.Count == 0)
                return;
            
            ITrackModelSource trackmodel = (ITrackModelSource)source;

            trackmodel.TrackModel.Selection.SelectAll ();

            foreach (TrackInfo t in trackmodel.TrackModel.SelectedItems) {
                DatabaseTrackInfo track_info = (t as DatabaseTrackInfo);

                if (track_info == null)
                    continue;

                if (!args.SongIDs.Contains (track_info.TrackId)) {

                    trackmodel.TrackModel.Selection.Unselect (trackmodel.TrackModel.IndexOf (t));

                } else {

                    Hyena.Log.Information (String.Format ("Added {0}: {1} - {2}",track_info.TrackId,
                                                      track_info.ArtistName, track_info.TrackTitle));
                }
            }

            PlaylistSource playlist = new PlaylistSource (Catalog.GetString ("NoNoise"), source);
            playlist.Save ();
            playlist.PrimarySource.AddChildSource (playlist);

            playlist.AddSelectedTracks (source);
            playlist.NotifyUser ();
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

            this.source = source as MusicLibrarySource;

            this.source.TrackModel.Reloaded += delegate {
                UpdateView ();
            };
            return true;
        }

        void UpdateView ()
        {

            ITrackModelSource trackmodel = (ITrackModelSource)source;

            trackmodel.TrackModel.Selection.SelectAll ();

            Hyena.Log.Information ("Count= "+trackmodel.TrackModel.Count);

            List<int> lst = new List<int> ();

            foreach (TrackInfo t in trackmodel.TrackModel.SelectedItems) {
                if (t == null)
                    continue;
                
                DatabaseTrackInfo track_info = (t as DatabaseTrackInfo);

                lst.Add (track_info.TrackId);
            }

            view.UpdateHiddenSongs (lst);
        }

        void HandleSourcehandleTracksDeleted (Source sender, TrackEventArgs args)
        {
            UpdateView ();
        }

        void HandleSourcehandleTracksAdded (Source sender, TrackEventArgs args)
        {
            UpdateView ();
        }

        public void Dispose ()
        {
            Clutter.Threads.Enter ();
            view = null;
//            if (view != null) {
//                view.Dispose ();
//                view = null;
//            }


            Clutter.Threads.Leave ();

        }
        public void ResetSource () { }
        public Widget Widget { get { return view; } }
        public ISource Source { get { return source; } }
    }
}

