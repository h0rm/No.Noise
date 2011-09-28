// 
// View.cs
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
using Clutter;
using System.Collections.Generic;
using Banshee.NoNoise;
using NoNoise.Visualization.Gui;

namespace NoNoise.Visualization
{
    public class View : Clutter.Embed
    {
        SongGroup point_group;
        MainGui gui;
        Banshee.NoNoise.BansheeLibraryAnalyzer analyzer;

        public View () : base ()
        {
            Hyena.Log.Information ("View Start");
            SetSizeRequest (100,100);
            Stage.Color = new Color (0,0,0,255);
            point_group = new SongGroup (Stage);
            gui = new MainGui (Stage);


            Stage.Add (point_group);
            Stage.Add (gui);

                      point_group.LowerBottom ();
//            Stage.SetClip (0,0,Stage.Width, Stage.Height);

            InitHandler ();
        }

        private void InitHandler ()
        {
            gui.ButtonClicked += HandleGuiButtonClicked;

            Stage.AllocationChanged += delegate {
                Stage.SetClip (0,0,Stage.Width, Stage.Height);
                Hyena.Log.Information ("Clutter stage allocation changed to " + Stage.Width + "x" + Stage.Height);
                point_group.QueueRelayout ();
            };

            point_group.SongEntered += delegate (object source, SongHighlightArgs args) {
                List<String> songs = new List<String> ();

                foreach (int i in args.SongIDs)
                    songs.Add (i.ToString());

                gui.UpdateInfoText (songs);
            };

            gui.DebugButtonPressedEvent += HandleGuiDebugButtonPressedEvent;
        }

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
                point_group.RemoveSelected ();
                break;

            case MainGui.ButtonClickedArgs.Button.Reset:
                point_group.ResetRemovedPoints ();
                break;

            case MainGui.ButtonClickedArgs.Button.Playlist:
                GeneratePlaylist (point_group.GetSelectedSongIDs ());
                break;
            }
        }

        public void GeneratePlaylist (List<int> list)
        {
            if (add_to_playlist_event != null)
                    add_to_playlist_event (this, new AddToPlaylistEventArgs (list));
        }

        public void FinishedInit ()
        {
            point_group.UpdateClipping ();
        }
        void HandleGuiDebugButtonPressedEvent  (object source, MainGui.DebugEventArgs args)
        {
            point_group.UpdateClipping ();
        }

        public void GetPcaCoordinates ()
        {
            if (BansheeLibraryAnalyzer.Singleton == null)
                analyzer = BansheeLibraryAnalyzer.Init (null);  // TODO this should not happen (missing callback)
            else
                analyzer = BansheeLibraryAnalyzer.Singleton;
            point_group.LoadPcaData (analyzer.PcaCoordinates);
        }

        public void TestGenerateData ()
        {
            //point_group.TestGenerateCircles(5000,5000,2000);
            point_group.ParseTextFile ("../../airport_locations.tsv", 8000);
        }

        public void UpdateHiddenSongs (List<int> not_hidden)
        {
            point_group.UpdateHiddenSongs (not_hidden);
            Hyena.Log.Information ("Update hidden songs. Not hidden: " + not_hidden.Count);
        }

        public event AddToPlaylistEvent OnAddToPlaylist {
            add { add_to_playlist_event += value; }
            remove { add_to_playlist_event -= value;}
        }

        public delegate void AddToPlaylistEvent (Object source, AddToPlaylistEventArgs args);

        private AddToPlaylistEvent add_to_playlist_event;

        public struct AddToPlaylistEventArgs
        {
            public List<int> SongIDs {
                get;
                private set;
            }

            public AddToPlaylistEventArgs (List<int> ids)
            {
                SongIDs = ids;
            }
        }
    }
}

