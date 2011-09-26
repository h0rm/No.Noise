// 
// ZoomButton.cs
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

namespace NoNoise.Visualization.Gui
{
    public class ZoomButton : Button
    {
        private bool inward;

        public ZoomButton (bool inward) : base (30,30)
        {
            this.inward = inward;
            base.Initialize ();
        }

        protected override void GenerateTextures ()
        {
            double x = 5, y = 5;
            double r = (texture_width - x - y) / 2;

            CairoTexture actor = new CairoTexture (texture_width,texture_height);
            Cairo.Context context = actor.Create ();

            context.Arc (x+r, y+r, r, 0, 2*Math.PI);

            context.ClosePath ();

            context.Color = new Cairo.Color (0,0,0);
            context.FillPreserve ();

            context.LineWidth = 2.0;
            context.Color = new Cairo.Color (0.95,0.95,0.95);
            context.Stroke ();


            context.MoveTo (x+5,texture_height /2);
            context.LineTo (texture_width-5-x, texture_height /2);
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

