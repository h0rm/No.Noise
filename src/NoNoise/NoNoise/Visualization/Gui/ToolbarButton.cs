// 
// ToolbarButton.cs
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
using Cairo;
using Clutter;

namespace NoNoise.Visualization.Gui
{
    /// <summary>
    /// Gui toolbar button element
    /// </summary>
    public class ToolbarButton : Button
    {
        public enum Border { None = 0, Left = 1, Right = 2}

        public Border Borders {
            get;
            protected set;
        }

        public String Text {
            get;
            protected set;
        }

        public ToolbarButton (String text, StyleSheet scheme, Border borders,
                              uint width, uint height) : base (scheme, width, height)
        {
            Text = text;
            Borders = borders;
            base.Initialize ();
        }

        /// <summary>
        /// Draws the button into the texture
        /// </summary>
        /// <param name="actor">
        /// A <see cref="CairoTexture"/>
        /// </param>
        protected void Draw (CairoTexture actor)
        {
            double x = 0.5, y = 0.5;
            double r = (texture_height - x - y) / 2;

            Cairo.Context cr = actor.Create ();

            if ((Borders & Border.Right) == Border.Right)
                cr.Arc (-x+texture_width-r, y+r, r, -Math.PI/2, Math.PI/2);
            else {
                cr.MoveTo (x+r,y);
                cr.LineTo (-x+texture_width, y);
                cr.LineTo (-x+texture_width, y+2*r);
            }

            if ((Borders & Border.Left) == Border.Left)
                cr.Arc (x+r, y+r, r, Math.PI/2, -Math.PI/2);
            else {
                cr.LineTo (x, y+2*r);
                cr.LineTo (x, y);
            }

            cr.ClosePath ();

            cr.Color = Style.Background;
            cr.FillPreserve();

            cr.Color = Style.Border;
            cr.LineWidth = Style.BorderSize;
            cr.Stroke ();

            cr.Color = Style.Standard.Color;

            cr.SelectFontFace (Style.Standard.Family, Style.Standard.Slant, Style.Standard.Weight);
            cr.SetFontSize (Style.Standard.Size);

            TextExtents te = cr.TextExtents (Text);

            cr.MoveTo ((texture_width-te.Width)/2,2*r-texture_height/4);
            cr.FontOptions.HintStyle = HintStyle.Full;
            cr.ShowText (Text);

            ((IDisposable) cr.Target).Dispose ();
            ((IDisposable) cr).Dispose ();
        }

        /// <summary>
        /// Generates the textures of the button.
        /// </summary>
        protected override void GenerateTextures ()
        {
            CairoTexture actor1 = new CairoTexture (texture_width, texture_height);
            Draw (actor1);
            textures.Add (actor1);
        }
    }
}

