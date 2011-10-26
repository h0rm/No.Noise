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
    public class NoNoiseClutterSourceContents : ISourceContents, IDisposable
    {
        private MusicLibrarySource source;
        private View view;
        private PlaylistSource playing;

        public delegate void ScanFinishedEvent (object source, ScanFinishedEventArgs args);
        private ScanFinishedEvent scan_event;
        public delegate void ToggleScannableEvent (object source, ToggleScannableEventArgs args);
        private ToggleScannableEvent scannable_event;

        public NoNoiseClutterSourceContents (bool pcadata)
        {
            //Gtk.Box box = new Gtk.HBox(true,0);
            if (!GLib.Thread.Supported)
                GLib.Thread.Init();

            Clutter.Threads.Init();
            Clutter.Application.InitForToolkit ();

            //cv = new ClutterView();
            //cv.Init();
            Clutter.Threads.Enter ();
            view = new View();
//
            if (pcadata) {
                BansheeLibraryAnalyzer.Init (this, true);
                view.GetPcaCoordinates ();
            } else
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

        public void Scan (bool start)
        {
            Hyena.Log.Information ("NoNoise - Scan " + (start ? "started." : "paused."));
            BansheeLibraryAnalyzer.Singleton.Scan (start);
        }

        public void ScanFinished ()
        {
            Hyena.Log.Information ("NoNoise - Scan finished.");
            scan_event (this, new ScanFinishedEventArgs ("supi"));
        }

        public void ScannableChanged (bool scannable)
        {
            Hyena.Log.Debug ("NoNoise - Scannable changed to: " + scannable);
            scannable_event (this, new ToggleScannableEventArgs (scannable));
        }

        public void PcaCoordinatesUpdated ()
        {
            Clutter.Threads.Enter ();
            Hyena.Log.Debug ("NoNoise - updating vis");
            view.GetPcaCoordinates ();
            Clutter.Threads.Leave ();
        }

        void HandleViewOnAddToPlaylist (object sender, View.AddToPlaylistEventArgs args)
        {
            if (args.SongIDs.Count == 0)
                return;
            
            ITrackModelSource trackmodel = (ITrackModelSource)source;

            for (int i = 0; i < trackmodel.TrackModel.Count; i++) {
                DatabaseTrackInfo track_info = (trackmodel.TrackModel [i] as DatabaseTrackInfo);
                if (args.SongIDs.ContainsKey (track_info.TrackId))
                    trackmodel.TrackModel.Selection.Select (i);
            }

            PlaylistSource playlist;

            if (args.Persistant) {
                playlist = new PlaylistSource (Catalog.GetString ("NoNoise"), source);

                playlist.Save ();
                playlist.PrimarySource.AddChildSource (playlist);

            } else {

                if (playing != null) {
//                    playing.Deactivate ();
                    playing.Unmap ();
                    playing = null;
                }

                playing = new PlaylistSource (Catalog.GetString ("playing NoNoise"), source);
                playing.Save ();

//                playing.PrimarySource.AddChildSource (playing);

                playlist = playing;
            }

            playlist.AddSelectedTracks (source);

            trackmodel.TrackModel.Selection.Clear ();
            playlist.NotifyUser ();

            ServiceManager.PlaybackController.Source = playlist;

            if (!ServiceManager.PlayerEngine.IsPlaying ())
                ServiceManager.PlayerEngine.Play ();
            else
                ServiceManager.PlaybackController.First ();
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

            List<int> lst = new List<int> ();

            foreach (TrackInfo t in trackmodel.TrackModel.SelectedItems) {
                if (t == null)
                    continue;
                
                DatabaseTrackInfo track_info = (t as DatabaseTrackInfo);

                lst.Add (track_info.TrackId);
            }

            trackmodel.TrackModel.Selection.Clear ();
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
            Clutter.Threads.Leave ();

            if (playing != null)
                playing.Unmap ();
            playing = null;


//            if (view != null) {
//                view.Dispose ();
//                view = null;
//            }

        }
        public void ResetSource () { }
        public Widget Widget { get { return view; } }
        public ISource Source { get { return source; } }

        public class ScanFinishedEventArgs
        {
            public ScanFinishedEventArgs (string info)
            {
                Info = info;
            }

            public string Info {
                get;
                private set;
            }
        }

        //Event Handler which is called when the scan has finished
        public event ScanFinishedEvent OnScanFinished {
            add { scan_event += value; }
            remove { scan_event -= value; }
        }

        public class ToggleScannableEventArgs
        {
            public ToggleScannableEventArgs (bool scannable)
            {
                Scannable = scannable;
            }

            public bool Scannable {
                get;
                private set;
            }
        }

        //Event Handler which is called when the scannable state has changed
        public event ToggleScannableEvent OnToggleScannable {
            add { scannable_event += value; }
            remove { scannable_event -= value; }
        }
    }
}

