// 
// InfoBox.cs
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
using Cairo;

namespace NoNoise
{
    public class InfoBox : Clutter.Group
    {
        private List<String> info_strings;
        private CairoTexture texture;

        public uint Width{
            get;
            private set;
        }

        public uint Height {
            get;
            private set;

        }
        public InfoBox (uint width, uint height)
        {
            Width = width;
            Height = height;

            info_strings = new List<string> ();
            texture = new CairoTexture (width, height);
            texture.SetSize (width, height);
            GenerateBackground ();
            Add (texture);
//            Update (new List<String>(new string[]{"Song 1", "Song 2", "blabal"}));
        }

        private void GenerateBackground ()
        {
            double x = 5, y = 5;
            double w = Width - 10;
            double h = Height -10;

            Hyena.Log.Information ("Infobox background");
            texture.Clear ();
            Cairo.Context cr = texture.Create ();

            double r = 10;

            cr.NewSubPath ();

            cr.Arc (x+w-r, y+h-r, r, 0, Math.PI/2);
            cr.Arc (x+r, y+h-r, r, Math.PI/2, Math.PI);
            cr.Arc (x+r, y+r, r, Math.PI, -Math.PI/2);
            cr.Arc (x-r+w, y+r, r, -Math.PI/2, 0);

            cr.ClosePath ();

            cr.Color = new Cairo.Color (0,0,0);
            cr.FillPreserve ();

            cr.Color = new Cairo.Color (0.95,0.95,0.95);
            cr.LineWidth = 2.0;
            cr.Stroke ();

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        public void Update (List<String> lines)
        {
            GenerateBackground ();
            AddText (lines);
        }

        public void AddText (List<String> lines)
        {
            Cairo.Context cr = texture.Create ();
            double x = 5 + 10, y = 5 + 10 ;

            cr.Color = new Cairo.Color (0.95,0.95,0.0);
            cr.SelectFontFace ("Verdana", Cairo.FontSlant.Normal, Cairo.FontWeight.Bold);
            cr.SetFontSize (14);

            TextExtents te = cr.TextExtents ("Song List");

            foreach (String s in lines) {

                y += te.Height + te.Height /3;
                cr.MoveTo (x, y);
                cr.ShowText (s);
//                Hyena.Log.Information ("Line "+ s);
            }
            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

    }
}

