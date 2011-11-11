// 
// View.cs
// 
// Author:
//   Manuel Keglevic <manuel.keglevic@gmail.com>
//   Thomas Schulz <tjom@gmx.at>
//
// Copyright (c) 2011 Manuel Keglevic, Thomas Schulz
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
using System.Threading;
using Clutter;
using System.Collections.Generic;
using Banshee.NoNoise;
using NoNoise.Visualization.Gui;

namespace NoNoise.Visualization
{
    public class View : Clutter.Embed
    {
        private SongGroup point_group;
        private MainGui gui;
        private BansheeLibraryAnalyzer analyzer;

        public enum ScanStatus {Finished, Started, Rescan};

        public View () : base ()
        {
            Hyena.Log.Debug ("NoNoise/Vis - Clutter view initializing.");
            SetSizeRequest (100,100);
            Stage.Color = new Color (0.1,0.1,0.1,255);

            gui = new MainGui (Stage);
            Stage.Add (gui);

            point_group = new SongGroup (Stage);
            Stage.Add (point_group);
            point_group.LowerBottom ();

            gui.UpdateStatus ("Initializing visualization.", true);

            point_group.Init ();
            InitHandler ();
        }

        /// <summary>
        /// Initializes all Handlers.
        /// </summary>
        private void InitHandler ()
        {
            gui.ButtonClicked += HandleGuiButtonClicked;

            point_group.SongEntered += delegate (object source, SongInfoArgs args) {
                List<String> songs = new List<String> ();
                List<String> artists = new List<String> ();

                GetSongLists (args, ref songs, ref artists);
                gui.UpdateInfoText (songs, artists);
            };

            point_group.SongSelected += delegate(object source, SongInfoArgs args) {
                List<String> songs = new List<String> ();
                List<String> artists = new List<String> ();

                if(args.SongIDs.Count == 0) {
                    gui.ClearInfoSelection ();
                } else {
                    gui.SetResetButton (true);

                    GetSongLists (args, ref songs, ref artists);
//                    Hyena.Log.Information ("Retrieved song info");
                    gui.UpdateSelection (songs, artists);
//                    Hyena.Log.Information ("Updated song info");
                }
            };

            point_group.SelectionCleared += delegate {
                gui.ClearInfoSelection ();
                gui.SetResetButton (false);
            };

            point_group.SongStartPlaying += delegate (object source, SongInfoArgs args) {
                GeneratePlaylist (args.SongIDs, false);
            };

            point_group.LoadPcaFinished += delegate {
                gui.UpdateStatus ("Songs loaded. Double click to play.", false, 0);
            };

            gui.DebugButtonPressedEvent += HandleGuiDebugButtonPressedEvent;
        }


        /// <summary>
        /// Returns the titles and artists to the given <see cref="SongInfoArgs"/>.
        /// </summary>
        /// <param name="args">
        /// A <see cref="SongInfoArgs"/> which contains a list of song ids.
        /// </param>
        /// <param name="titles">
        /// A <see cref="List<String>"/>
        /// </param>
        /// <param name="artists">
        /// A <see cref="List<String>"/>
        /// </param>
        private void GetSongLists (SongInfoArgs args, ref List<String> titles, ref List<String> artists)
        {
            foreach (int i in args.SongIDs) {

                Banshee.Collection.TrackInfo track = analyzer.GetTrackInfoFor (i);

                if (track == null) {
                    Hyena.Log.Debug ("NoNoise/Vis - Warning, track not found");
                    continue;
                }

                titles.Add (String.Copy (track.TrackTitle == "" || track.TrackTitle == null
                                                            ? "Unknown Title" : track.TrackTitle));
                artists.Add (String.Copy (track.ArtistName == "" || track.ArtistName == null
                                                            ? "Unknown Artist" : track.ArtistName));
            }
        }

        /// <summary>
        /// Handler for button click events fired by the main gui.
        /// </summary>
        /// <param name="source">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="MainGui.ButtonClickedArgs"/>
        /// </param>
        void HandleGuiButtonClicked (object source, MainGui.ButtonClickedArgs args)
        {
            switch (args.ButtonClicked) {

            case MainGui.ButtonClickedArgs.Button.ZoomIn:
                point_group.ClusterOneStep (true);
                break;

            case MainGui.ButtonClickedArgs.Button.ZoomOut:
                point_group.ClusterOneStep (false);
                break;

            case MainGui.ButtonClickedArgs.Button.Select:
                point_group.ToggleSelection ();
                break;

            case MainGui.ButtonClickedArgs.Button.Remove:
                gui.UpdateStatus ("Selected songs removed.", false);
                point_group.RemoveSelected ();
                break;

            case MainGui.ButtonClickedArgs.Button.Reset:
                gui.UpdateStatus ("Visualization reset.", false);
                point_group.ResetRemovedPoints ();
                break;

            case MainGui.ButtonClickedArgs.Button.Playlist:
                gui.UpdateStatus ("Playlist NoNoise created.", false);
                GeneratePlaylist (point_group.GetSelectedSongIDs (), true);
                break;

            case MainGui.ButtonClickedArgs.Button.Clear:
                gui.UpdateStatus ("Selection cleared.", false);
                point_group.ClearSongSelection ();
                break;
            }
        }

        /// <summary>
        /// Adds the songs given by a list of ids to a new playlist.
        /// </summary>
        /// <param name="list">
        /// A <see cref="List<System.Int32>"/>
        /// </param>
        public void GeneratePlaylist (List<int> list, bool persistant)
        {
            if (add_to_playlist_event != null)
                    add_to_playlist_event (this, new AddToPlaylistEventArgs (list,persistant));
        }

        /// <summary>
        /// [Debug] Handler for debug button clicks.
        /// </summary>
        /// <param name="source">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="MainGui.DebugEventArgs"/>
        /// </param>
        void HandleGuiDebugButtonPressedEvent  (object source, MainGui.DebugEventArgs args)
        {
//            point_group.UpdateClipping ();
//            this.MyDispose ();
        }

        /// <summary>
        /// Retrieves the Pca-coordinates from the analyser and updates the visualization.
        /// </summary>
        public void GetPcaCoordinates ()
        {
            Hyena.Log.Debug ("NoNoise - updating vis");

            if (BansheeLibraryAnalyzer.Singleton == null)
                analyzer = BansheeLibraryAnalyzer.Init (null);  // this should not happen (missing callback)
            else
                analyzer = BansheeLibraryAnalyzer.Singleton;

            List<NoNoise.Data.DataEntry> data = analyzer.PcaCoordinates;

            gui.UpdateStatus ("Songs loading.", true, 0);


            if (point_group == null || !point_group.Initialized)
                return;

            point_group.LoadPcaData (data);
        }

        /// <summary>
        /// Applies the search filter on the visualization.
        /// </summary>
        /// <param name="not_hidden">
        /// A <see cref="List<System.Int32>"/> which specifies the songs which are not hidden.
        /// </param>
        public void UpdateHiddenSongs (List<int> not_hidden)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                point_group.UpdateHiddenSongs (not_hidden);
            });
        }

        public void UpdateStatus (ScanStatus status)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                switch (status) {
                case ScanStatus.Finished:
                    gui.UpdateStatus ("Scan finished.", false, 1);
                    break;
    
                case ScanStatus.Rescan:
                    gui.UpdateStatus ("Library changed. Please rescan (Tools > NoNoise).", false, 2);
                    break;
    
                case ScanStatus.Started:
                    gui.UpdateStatus ("Scanning library.", true, 2);
                    break;
                }
            });

        }

        public void UpdateScanProgress (int percent)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                gui.UpdateStatus (string.Format ("Scanning library. {0}% done...", percent), true, 2);
            });
        }

        /// <summary>
        /// Event fired when a new playlist is created.
        /// </summary>
        public event AddToPlaylistEvent OnAddToPlaylist {
            add { add_to_playlist_event += value; }
            remove { add_to_playlist_event -= value;}
        }

        public delegate void AddToPlaylistEvent (Object source, AddToPlaylistEventArgs args);

        private AddToPlaylistEvent add_to_playlist_event;

        /// <summary>
        /// Arguments for the <see cref="AddToPlaylistEvent"/> which contain a list of song ids.
        /// </summary>
        public class AddToPlaylistEventArgs
        {
            public Dictionary<int,int> SongIDs {
                get;
                private set;
            }

            public bool Persistent {
                get;
                private set;
            }

            public AddToPlaylistEventArgs (List<int> ids, bool persistent)
            {
                SongIDs = new Dictionary<int, int> ();
                foreach (int id in ids)
                    SongIDs.Add (id, id);

                Persistent = persistent;
            }

            public AddToPlaylistEventArgs (List<int> ids) : this (ids, false)
            {
            }
        }

        public override void Dispose ()
        {
            Stage.RemoveAll ();

            gui.Dispose ();
            point_group.Dispose ();
            gui.Destroy ();
            point_group.Destroy ();

            base.Dispose ();

            point_group = null;
            gui = null;
        }
    }
}

