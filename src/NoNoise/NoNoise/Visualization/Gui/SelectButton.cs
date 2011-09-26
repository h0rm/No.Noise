// 
// SelectButton.cs
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

namespace NoNoise.Visualization.Gui
{
    public class SelectButton : ToggleButton
    {
        public SelectButton () : base (70, 30)
        {
            Initialize ();
        }

        private void Draw (CairoTexture actor, bool black)
        {

            double x = 5, y = 5;
            double r = (texture_height - x - y) / 2;

            Cairo.Context cr = actor.Create ();

            cr.Arc (-x+texture_width-r, y+r, r, -Math.PI/2, Math.PI/2);
            cr.Arc (x+r, y+r, r, Math.PI/2, -Math.PI/2);

            cr.ClosePath ();

            if (black)
                cr.Color = new Cairo.Color (0,0,0);
            else
                cr.Color = new Cairo.Color (0.95,0.95,0.95);

            cr.FillPreserve ();

            cr.Color = new Cairo.Color (0.95,0.95,0.95);
            cr.LineWidth = 2.0;
            cr.Stroke ();

            if (!black)
                cr.Color = new Cairo.Color (0,0,0);

            cr.SelectFontFace ("Verdana", Cairo.FontSlant.Normal, Cairo.FontWeight.Bold);
            cr.SetFontSize (12);

            TextExtents te = cr.TextExtents ("select");
            cr.MoveTo ((texture_width-te.Width)/2,y+r+te.Height/2);
            cr.ShowText ("select");

//            cr.Rectangle (11.5,11.5,8,8);
//
//            if (black)
//                cr.Color = new Cairo.Color (0.95,0.95,0.95);
//            else
//                cr.Color = new Cairo.Color (0,0,0);
//
//            cr.LineWidth = 1.0;
//            cr.SetDash (new double[]{3,2},0);
//            cr.Stroke ();

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        protected override void GenerateTextures ()
        {

            CairoTexture actor1 = new CairoTexture (texture_width, texture_height);
            Draw (actor1, true);
            textures.Add (actor1);

            CairoTexture actor2 = new CairoTexture (texture_width, texture_height);
            Draw (actor2, false);
            textures.Add (actor2);
        }

        protected override void OnStateChanged ()
        {
//            HideAll ();
            textures[0].Hide ();
            textures[1].Hide ();


            if (state == ToggleButton.State.Off)
                textures[0].Show ();
            else
                textures[1].Show ();

            Hyena.Log.Information ("State changed");
        }
    }
}

