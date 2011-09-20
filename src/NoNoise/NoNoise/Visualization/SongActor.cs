// 
// SongActor.cs
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
using System.Diagnostics;

namespace NoNoise.Visualization
{
    /// <summary>
    /// This class is the visible representation of a song.
    /// </summary>
    public class SongActor : Clutter.Texture
    {
        #region Static stuff
        static private List<CairoTexture> prototype_list = new List<CairoTexture> ();
        static private uint circle_size = 50;
        static private int max_prototypes = 4;

        public enum Color {Green, Red, Blue, White};

        static public uint CircleSize {
            get { return circle_size; }
        }
        /// <summary>
        /// Generate the prototypes which are used for drawing.
        /// </summary>
        static public void GeneratePrototypes ()
        {
            prototype_list.Clear ();

            for (int i=0; i < max_prototypes; i++)
            {
                CairoTexture texture = new CairoTexture (circle_size,circle_size);
                prototype_list.Add (texture);
                UpdatePrototypeWithColor (texture, i);
            }
        }

        /// <summary>
        /// Updates the prototype whith the given color.
        /// </summary>
        /// <param name="actor">
        /// A <see cref="CairoTexture"/>
        /// </param>
        /// <param name="color_index">
        /// A <see cref="System.Int32"/>
        /// </param>
        static private void UpdatePrototypeWithColor (CairoTexture actor, int color_index)
        {
            color_index = color_index % max_prototypes;

            Hyena.Log.Debug ("Color changed to "+color_index);

            switch (color_index)
            {
            case (int)Color.Green:
                UpdatePrototype (actor,0.0, 1.0, 0.0);
                break;

            case (int)Color.Red:
                UpdatePrototype (actor,1.0, 0.0, 0.0);
                break;

            case (int)Color.Blue:
                UpdatePrototype (actor,0.0, 0.0, 1.0);
                break;

            case (int)Color.White:
                UpdatePrototype (actor,1.0, 1.0, 1.0);
                break;
            }
        }

        static private void UpdatePrototype (CairoTexture actor, double r, double g, double b)
        {
            UpdatePrototype (actor,r,g,b,0.55,0,0,0,0);
        }

        /// <summary>
        /// Redraws the circle_prototype which is used for all circles as template.
        /// This means a new circle is drawn with cairo and stored in a texture.
        /// </summary>
        static private void UpdatePrototype (CairoTexture actor, double r, double g, double b, double a,
                                                   double arc, double a_r, double a_g, double a_b)
        {
            double size = (double)circle_size;

            Hyena.Log.Debug ("Color : " + r + " " + g + " " + b + " ");
            actor.Clear();
            Cairo.Context context = actor.Create();


            Cairo.Gradient pattern = new Cairo.RadialGradient(size/2.0,size/2.0,size/3.0,
                                                            size/2.0,size/2.0,size/2.0);


            //Cairo.Gradient pattern = new Cairo.LinearGradient(0,0,circle_size,circle_size);
            pattern.AddColorStop(0,new Cairo.Color (r,g,b,a));
            pattern.AddColorStop(1.0,new Cairo.Color (r,g,b,0.1));

            context.LineWidth = (double)size/5.0;
            context.Arc (size/2.0, size/2.0,
                         size/2.0-context.LineWidth/2.0,0,2*Math.PI);

            context.Save();

            context.Pattern = pattern;
            //context.Color = new Cairo.Color (r,g,b,0.3);
            context.Fill();

            //context.Restore ();

            if (arc != 0) {

                context.LineWidth = (double)size/10.0;
                context.Arc (size/2.0, size/2.0,
                         size/2.0-context.LineWidth/2.0,-Math.PI/2.0,2*Math.PI*arc/100.0-Math.PI/2.0);

                Hyena.Log.Debug ("Arc prototype "+ arc);
                context.Color = new Cairo.Color (a_r,a_g,a_b,0.5);
                context.Stroke ();
            }
            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        #endregion
        private CairoTexture prototype;

        public CairoTexture Prototype
        {
            set
            {
                prototype = value;
                this.CoglTexture = prototype.CoglTexture;
            }

            get { return prototype; }
        }

        public void SetPrototypeByColor (Color color)
        {
            Debug.Assert ((int)color < prototype_list.Count);

            Prototype = prototype_list[(int)color];
        }

        public SongPoint Owner {
            get;
            set;
        }
    }
}
