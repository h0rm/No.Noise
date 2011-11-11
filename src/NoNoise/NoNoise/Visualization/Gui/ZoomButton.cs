// 
// ZoomButton.cs
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

namespace NoNoise.Visualization.Gui
{
    /// <summary>
    /// Gui element for the zoom in / out buttons
    /// </summary>
    public class ZoomButton : Button
    {
        private bool inward;

        public ZoomButton (StyleSheet style, bool inward) : base (style,30,30)
        {
            this.inward = inward;
            base.Initialize ();
        }

        /// <summary>
        /// Generates the undlying texture
        /// </summary>
        protected override void GenerateTextures ()
        {
            double x = 5+0.5, y = 5+0.5;
            double r = (texture_width - x - y) / 2;

            CairoTexture actor = new CairoTexture (texture_width,texture_height);
            Cairo.Context context = actor.Create ();

            context.Arc (x+r, y+r, r, 0, 2*Math.PI);

            context.ClosePath ();

            context.Color = Style.Background;
            context.FillPreserve ();

            context.Color = Style.Border;
            context.LineWidth = Style.BorderSize;
            context.Stroke ();

            context.Color = Style.Foreground;

            context.MoveTo (x+5,texture_height /2);
            context.LineTo (texture_width-5-x, texture_height /2);
            context.LineWidth = 2.0;
            context.Stroke ();

            if (inward)
            {
                context.MoveTo (texture_width /2,y+5);
                context.LineTo (texture_width /2,texture_height-5-y);
                context.Stroke ();
            }

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();

            textures.Add (actor);
        }

    }
}

