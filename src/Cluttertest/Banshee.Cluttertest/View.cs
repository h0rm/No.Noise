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

namespace Banshee.Cluttertest
{
    public class View : Clutter.Embed
    {
        PointGroup point_group;
        Gui gui;

        public View () : base ()
        {
            Hyena.Log.Information ("View Start");
            SetSizeRequest (100,100);
            Stage.Color = new Color (0,0,0,255);
            point_group = new PointGroup ();
            gui = new Gui ();

            Stage.Add (point_group);
            Stage.Add (gui);

            point_group.SongEntered += delegate (object source, SongHighlightArgs args) {
                //Hyena.Log.Information ("Entered "+args.X+":"+args.Y+" | "+args.ID);
            };

            point_group.SongLeft += delegate (object source, SongHighlightArgs args) {
                //Hyena.Log.Information ("Left "+args.X+":"+args.Y+" | "+args.ID);
            };

            //Event Handler to handle zoom
            gui.ZoomChangedEvent += HandleGuiZoomChangedEvent;

            gui.DebugButtonPressedEvent += HandleGuiDebugButtonPressedEvent;
        }

        void HandleGuiDebugButtonPressedEvent (object source, Gui.DebugEventArgs args)
        {
            point_group.ClusterOneStep (args.Value == 1);
        }

        void HandleGuiZoomChangedEvent (object source, Gui.ZoomLevelArgs args)
        {
            point_group.ZoomOnCenter (args.Inwards);
        }

        public void TestGenerateData ()
        {
            //point_group.TestGenerateCircles(5000,5000,2000);
            point_group.ParseTextFile ("/home/horm/Downloads/16255/airport_locations.tsv");
        }
    }
}

