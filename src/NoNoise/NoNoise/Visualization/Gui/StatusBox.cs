// 
// StatusBox.cs
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
using Cairo;
using System.Collections.Generic;
using System.Timers;

namespace NoNoise.Visualization.Gui
{
    public class StatusBox : Group, IDisposable
    {
        private CairoTexture texture;
        private List<CairoTexture> spinner;
        private Group spinner_actor;

        private StyleSheet style;
        private uint height;
        private Timer spinner_timer;

        private int frame = 0;

        public String Text {
            get;
            private set;
        }
        public StatusBox (StyleSheet style, uint width, uint height)
        {
            this.style = style;
            this.height = height;

            Text = "test";
            spinner = new List<CairoTexture> ();

            texture = new CairoTexture (width, height);
            spinner_actor = new Group ();
            GenerateSpinners ();

            spinner_actor.SetPosition (5, 0 );

            Add (texture);
            Add (spinner_actor);

            spinner_timer = new Timer ();
            spinner_timer.Interval = 150;
            spinner_timer.Elapsed += HandleSpinnerNextFrame;

//            Update ("",true);
        }

        private void GenerateSpinners ()
        {
            spinner = new List<CairoTexture> ();

            for (int i=0; i < 8; i++) {
                CairoTexture current = new CairoTexture (height, height);
                DrawSpinner (current, i);
                spinner.Add (current);
                spinner_actor.Add (current);
                current.Hide ();
            }
        }

        void HandleSpinnerNextFrame (object sender, ElapsedEventArgs e)
        {
            lock (spinner) {

//                Hyena.Log.Debug ("Timer " + frame);
                frame = (++frame) %8;

                foreach (Actor a in spinner)
                    a.Hide ();

                spinner[frame].Show ();
            }
        }

        public void Clear ()
        {
            foreach (Actor a in spinner)
                    a.Hide ();

            texture.Clear ();
            Cairo.Context cr = texture.Create ();
            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        public void Update (String text, bool waiting)
        {
            Clear ();

            Text = text;

            lock (spinner) {
                spinner_timer.Stop ();

                GenerateBackground (waiting);

                if (waiting) {
                    spinner[0].Show ();
                    spinner_timer.Start ();
                }
            }
        }

        public void DrawSpinner (CairoTexture tex, int count)
        {
            Cairo.Context cr = tex.Create ();

            cr.Translate (height /2, height /2);
            cr.LineWidth = 3;

            for (int i = 0; i < 8; i ++) {
                cr.Color = new Cairo.Color (1,1,1,1-(double)((count+i)%8)/7);

                cr.MoveTo (0, -height/2.5+3);
                cr.LineTo (0, -height/2.5);
                cr.Rotate (-Math.PI / 4);
                cr.Stroke ();
            }

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        /// <summary>
        /// Draws the background for the infobox into the texture.
        /// </summary>
        /// <param name="height">
        /// A <see cref="System.Int32"/>
        /// </param>
        private void GenerateBackground (bool offset)
        {
            Cairo.Context cr = texture.Create ();
            cr.SelectFontFace (style.Standard.Family, style.Standard.Slant, style.Standard.Weight);
            cr.SetFontSize (style.Standard.Size);
            cr.FontOptions.HintStyle = HintStyle.Full;

            TextExtents te = cr.TextExtents (Text);

            double x = 0.5, y = 0.5;
            double r = (height - x - y) / 2;

            double width = te.XAdvance + (offset?spinner_actor.Width+4:0) + 1.5*r;


            cr.Arc (-x+width-r, y+r, r, -Math.PI/2, 0);

            cr.LineTo (-x+width, y+2*r);
            cr.LineTo (x, y+2*r);
            cr.LineTo (x, y);

            cr.ClosePath ();

            cr.Color = new Cairo.Color (0.0, 0, 0, 0.9);
            cr.FillPreserve();

            cr.Color = new Cairo.Color (0.0, 0, 0, 0.9);
            cr.LineWidth = style.BorderSize;
            cr.Stroke ();

            cr.Color = style.Background;

            cr.MoveTo (5 + (offset?spinner_actor.Width+4:0),2*r-height/4);
            cr.ShowText (Text);

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }


        public override void Dispose ()
        {
            spinner_timer.Stop ();
            spinner_timer = null;
        }
    }
}

