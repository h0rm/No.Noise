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
            gui = new MainGui ();


            Stage.Add (point_group);
            Stage.Add (gui);
            point_group.LowerBottom ();
//            Stage.SetClip (0,0,Stage.Width, Stage.Height);

            Stage.AllocationChanged += delegate {
                Stage.SetClip (0,0,Stage.Width, Stage.Height);
            };

            point_group.SongEntered += delegate (object source, SongHighlightArgs args) {
                List<String> songs = new List<String> ();

                foreach (int i in args.SongIDs)
                    songs.Add (i.ToString());

                gui.UpdateInfoText (songs);
//                Hyena.Log.Information ("Entered "+args.ID + "\n" + ids);
            };

            point_group.SongLeft += delegate (object source, SongHighlightArgs args) {
                Hyena.Log.Information ("Left");
            };


            //Event Handler to handle zoom
            gui.ZoomChangedEvent += HandleGuiZoomChangedEvent;

            gui.DebugButtonPressedEvent += HandleGuiDebugButtonPressedEvent;
        }

        void HandleGuiDebugButtonPressedEvent  (object source, MainGui.DebugEventArgs args)
        {
            point_group.ClusterOneStep (args.Value == 1);
        }

        void HandleGuiZoomChangedEvent (object source, MainGui.ZoomLevelArgs args)
        {
            point_group.ZoomOnCenter (args.Inwards);
        }

        public void GetPcaCoordinates ()
        {
            analyzer = Banshee.NoNoise.BansheeLibraryAnalyzer.Init (null);
            point_group.LoadPcaData (analyzer.PcaCoordinates);
        }

        public void TestGenerateData ()
        {
            //point_group.TestGenerateCircles(5000,5000,2000);
            point_group.ParseTextFile ("../../airport_locations.tsv", 8000);
        }
    }
}

