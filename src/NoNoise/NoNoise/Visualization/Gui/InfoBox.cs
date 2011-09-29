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
using System.Linq;

namespace NoNoise.Visualization.Gui
{
    public class InfoBox : Clutter.Group
    {
        private List<String> info_strings;
        private CairoTexture texture;
        private StyleSheet style;

        public new uint Width{
            get;
            private set;
        }

        public new uint Height {
            get;
            private set;

        }
        public InfoBox (StyleSheet style, uint width, uint height)
        {
            Width = width;
            Height = height;

            this.style = style;
            info_strings = new List<string> ();
            texture = new CairoTexture (width, height);
            texture.SetSize (width, height);
//            GenerateBackground ();
            Add (texture);
//            Update (new List<String>(new string[]{"Song 1", "Song 2", "blabal"}));
        }

        public void Clear ()
        {
            texture.Clear ();
            Cairo.Context cr = texture.Create ();
            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }
        private void GenerateBackground (int height)
        {
            double x = 5+0.5, y = 5+0.5;
            double w = Width - 10;
            double h = Height -10;

//            Hyena.Log.Information ("Infobox background");
            texture.Clear ();
            Cairo.Context cr = texture.Create ();

            cr.Color = style.Highlighted.Color;
            cr.SelectFontFace (style.Highlighted.Family, style.Highlighted.Slant, style.Highlighted.Weight);
            cr.SetFontSize (style.Highlighted.Size);
            TextExtents te_title = cr.TextExtents ("Song List");

            h = height * (2 * te_title.Height + te_title.Height /2)+30;

            double r = 10;

            cr.NewSubPath ();

            cr.Arc (x+w-r, y+h-r, r, 0, Math.PI/2);
            cr.Arc (x+r, y+h-r, r, Math.PI/2, Math.PI);
            cr.Arc (x+r, y+r, r, Math.PI, -Math.PI/2);
            cr.Arc (x-r+w, y+r, r, -Math.PI/2, 0);

            cr.ClosePath ();

            cr.Color = style.Background;
            cr.FillPreserve ();

            cr.Color = style.Border;
            cr.LineWidth = style.BorderSize;
            cr.Stroke ();

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        private class CountedSubtitles {
            public String Name { get; set; }
            public int Count { get; set; }
        }

        public void Update (List<String> titles, List<String> subtitles)
        {
            List<String> t, s;
            if (titles.Count > 5) {
                List<CountedSubtitles> cs = new List<CountedSubtitles> ();

                foreach (String a in subtitles) {

                    if (cs.Find (delegate (CountedSubtitles i) { return i.Name == a; }) == null) {

                        cs.Add (new CountedSubtitles
                          { Name = a,
                            Count = subtitles.FindAll (
                                     delegate (String i) { return i == a; }).Count
                          });
                    }
                }

                var collection = from c in cs
                                 orderby c.Count descending
                                 select c;

                t = new List<string> ();
                s = new List<string> ();

                foreach (CountedSubtitles c in collection) {
                    t.Add (c.Name);
                    s.Add (c.Count + " songs");
                }

            } else {
                t = titles;
                s = subtitles;
            }

            GenerateBackground (t.Count);
            AddText (t, s);
        }

        public void AddText (List<String> titles, List<String> subtitles)
        {
            if (titles.Count != subtitles.Count)
                return;

            Cairo.Context cr = texture.Create ();
            double x = 5 + 10, y = 5 + 10 ;

            cr.Color = style.Highlighted.Color;
            cr.SelectFontFace (style.Highlighted.Family, style.Highlighted.Slant, style.Highlighted.Weight);
            cr.SetFontSize (style.Highlighted.Size);
            TextExtents te_title;

            TextExtents te_subtitle;

            for (int i = 0; i < titles.Count; i++) {

                Hyena.Log.Information (String.Format ("{0} - {1}", titles[i], subtitles[i]));

                cr.Color = style.Highlighted.Color;
                cr.SelectFontFace (style.Highlighted.Family, style.Highlighted.Slant, style.Highlighted.Weight);
                cr.SetFontSize (style.Highlighted.Size);
                te_title = cr.TextExtents ("Song List");

                y += te_title.Height + te_title.Height / 2;
                cr.MoveTo (x, y);

                cr.ShowText (CheckString(titles[i]));

                cr.SetFontSize (10);

                y += te_title.Height;
                cr.MoveTo (x,y);
                cr.ShowText (subtitles[i]);
            }

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        public String CheckString (String s)
        {
            if (s.Length > 28) {
                s = s.Substring (0, 25) + "...";
            }
            return s;
        }

    }
}

