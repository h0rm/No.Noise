// 
// InfoBox.cs
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
using Clutter;
using System.Collections.Generic;
using Cairo;
using System.Linq;
using System.Diagnostics;

namespace NoNoise.Visualization.Gui
{
    /// <summary>
    /// Gui element used to display information about songs.
    /// </summary>
    public class InfoBox : Clutter.Group
    {
        private CairoTexture texture;
        private StyleSheet style;
        private bool selection_info;

        public enum Size {Expanded, Collapsed};

        private Size size_mode;
        private List<String> title, subtile;

        public Size Mode {
            get { return size_mode; }
            set { size_mode = value; }
        }

        public new uint Width{
            get;
            private set;
        }

        public new uint Height {
            get;
            private set;

        }
        public InfoBox (StyleSheet style, uint width, uint height, bool selection)
        {
            Width = width;
            Height = height;
            selection_info = selection;
            size_mode = InfoBox.Size.Expanded;
            this.style = style;
            texture = new CairoTexture (width, height);
            texture.SetSize (width, height);
//            GenerateBackground ();
            Add (texture);
//            Update (new List<String>(new string[]{"Song 1", "Song 2", "blabal"}));
        }

        /// <summary>
        /// Clears the infobox.
        /// </summary>
        public void Clear ()
        {
            texture.Clear ();
            Cairo.Context cr = texture.Create ();
            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        /// <summary>
        /// Draws the background for the infobox into the texture.
        /// </summary>
        /// <param name="height">
        /// A <see cref="System.Int32"/>
        /// </param>
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

            cr.Color = selection_info ? style.Selection : style.Background;
            cr.FillPreserve ();

            cr.Color = selection_info ? style.SelectionBoarder : style.Border;
            cr.LineWidth = style.BorderSize;
            cr.Stroke ();

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();

            this.SetSize (Width, (float)h+10);
        }

        /// <summary>
        /// Helper class which encapsulates Subtitles and a count.
        /// </summary>
        private class CountedSubtitles {
            public String Name { get; set; }
            public int Count { get; set; }
        }

        /// <summary>
        /// Redraws the infobox.
        /// </summary>
        public void Update ()
        {
            Update (title, subtile);
        }


        /// <summary>
        /// Updates the text in the infobox.
        /// </summary>
        /// <param name="titles">
        /// A <see cref="List<String>"/>
        /// </param>
        /// <param name="subtitles">
        /// A <see cref="List<String>"/>
        /// </param>
        public void Update (List<String> titles, List<String> subtitles)
        {
            this.title = titles;
            this.subtile = subtitles;

            List<String> t, s;
            if (titles.Count > (Mode == InfoBox.Size.Expanded ? 5 : 2)) {

                Dictionary<String, CountedSubtitles> dict = new Dictionary<String, CountedSubtitles> ();


                foreach (String sub in subtitles) {

                    String lower = sub.ToLower ();
                    if (dict.ContainsKey (sub.ToLower ()))
                    {
                        dict[lower].Count++;
                    } else {
                        dict.Add (lower, new CountedSubtitles {
                            Name = sub,
                            Count = 1
                        });
                    }
                }


                var collection = from c in dict.Values
                                 orderby c.Count descending
                                 select c;

                t = new List<string> ();
                s = new List<string> ();

                // Show 5 artists with the most songs, and other
                int count = 0, num_count = 0;
                foreach (CountedSubtitles c in collection) {

                    if (count < 5) {
                        t.Add (c.Name);
                        s.Add (c.Count + " songs");
                    }

                    if (count == 5) {
                        t.Add ("Other");
                        s.Add ("");
                    }

                    if (count >= 5) {
                        num_count += c.Count;
                        s[s.Count-1] = num_count + " songs";
                    }
                    count ++;
                }

            } else {
                t = titles;
                s = subtitles;
            }

            GenerateBackground (t.Count);
            AddText (t, s);
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="titles">
        /// A <see cref="List<String>"/>
        /// </param>
        /// <param name="subtitles">
        /// A <see cref="List<String>"/>
        /// </param>
        public void AddText (List<String> titles, List<String> subtitles)
        {
            if (titles.Count != subtitles.Count)
                return;

            Cairo.Context cr = texture.Create ();
            double x = 5 + 10, y = 5 + 10 ;

            cr.Color = selection_info ? style.Background : style.Highlighted.Color;
            cr.SelectFontFace (style.Highlighted.Family, style.Highlighted.Slant, style.Highlighted.Weight);
            cr.SetFontSize (style.Highlighted.Size);
            TextExtents te_title;

            for (int i = 0; i < titles.Count; i++) {

//                Hyena.Log.Information (String.Format ("{0} - {1}", titles[i], subtitles[i]));

//                cr.Color = style.Highlighted.Color;
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

        /// <summary>
        /// Checks wether a string is to long.
        /// </summary>
        /// <param name="s">
        /// A <see cref="String"/>
        /// </param>
        /// <returns>
        /// A <see cref="String"/> which is shortened representation of the input string.
        /// </returns>
        public String CheckString (String s)
        {
            if (s.Length > 28) {
                s = s.Substring (0, 25) + "...";
            }
            return s;
        }

    }
}

