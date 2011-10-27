// 
// SelectionActor.cs
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
using System.Collections.Generic;
using NoNoise.Visualization.Util;
using Hyena;

namespace NoNoise.Visualization
{
    /// <summary>
    /// This actor is used for the selection of points.
    /// </summary>
    public class SelectionActor : Clutter.Group
    {
        private double old_x, old_y, segment_x, segment_y, shift_x, shift_y, scale;
        private int count = 0;
        private Clutter.CairoTexture texture;
        private double start_x, start_y;

        private List<Point> vertices;

        public SelectionActor (uint width, uint height, Cairo.Color color)
        {
            Color = color;
            texture = new Clutter.CairoTexture (width, height);
            texture.SetSize (width, height);
            this.Add (texture);
            vertices = new List<Point> ();
        }

        public new void SetSize (float width, float height)
        {
            texture.SetSurfaceSize ((uint)Math.Ceiling(width), (uint)Math.Ceiling (height));
            texture.SetSize ((float)Math.Ceiling(width), (float)Math.Ceiling (height));
//            base.SetSize (width, height);
        }

        /// <summary>
        /// Color of the selection polygon.
        /// </summary>
        public Cairo.Color Color {
            get;
            set;
        }

        /// <summary>
        /// Clears the selection polygon.
        /// </summary>
        public void Clear ()
        {

            texture.Clear ();
            Cairo.Context context = texture.Create();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        /// <summary>
        /// Resets the selection.
        /// </summary>
        public void Reset ()
        {
//            Hyena.Log.Debug ("Reset");
            Clear ();

            vertices.Clear ();

            count = 0;
        }

        /// <summary>
        /// Starts a new selection.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/> which specifies the untransformed x mouse-coordinate.
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/> which specifies the untransformed y mouse-coordinate.
        /// </param>
        /// <param name="scale">
        /// A <see cref="System.Double"/> which specifies the current scale.
        /// </param>
        /// <param name="shift_x">
        /// A <see cref="System.Double"/> which specifies the transformed x-coordinate of the <see cref="PointGroup"/>.
        /// </param>
        /// <param name="shift_y">
        /// A <see cref="System.Double"/> which specifies the transformed y-coordinate of the <see cref="PointGroup"/>.
        /// </param>
        public void Start (double x, double y, double scale, double shift_x, double shift_y)
        {
//            Hyena.Log.Debug ("Start");
            old_x = x;
            old_y = y;

            this.scale = scale;
            this.shift_x = shift_x;
            this.shift_y = shift_y;

            start_x = x;
            start_y = y;

            segment_x = x;
            segment_y = y;

            vertices.Add (GetTransformedPoint (x,y));
        }

        /// <summary>
        /// Finallizes the selection and clears the polygon.
        /// </summary>
        public void Stop ()
        {
//            Hyena.Log.Debug ("Stop");
            if (vertices.Count > 0) {

                AddSegment (old_x,old_y);
                vertices.Add (GetTransformedPoint (start_x,start_y));

                DrawLine (start_x, start_y);
//                DebugDrawSegment (start_x, start_y);
            }

            Clear ();
        }

        /// <summary>
        /// Adds a new point to the selection.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        public void LineTo (double x, double y)
        {
//            Hyena.Log.Debug ("LineTo");
            DrawLine (x,y);

            Point p = new Point (x,y);

            if (p.DistanceTo (new Point (segment_x, segment_y)) > 20) {
                AddSegment (x, y);
                count = 0;
            } else {
                count ++;
            }

        }

        /// <summary>
        /// Draws a line to a new point in the selection.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        private void DrawLine (double x, double y)
        {
            Cairo.Context context = texture.Create();
            context.LineWidth = 5;
            context.Color = new Cairo.Color (1,0,0,0.7);

            context.MoveTo (old_x, old_y);
            context.LineTo (x, y);
            context.Stroke ();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();

            old_x = x;
            old_y = y;
        }

        /// <summary>
        /// Returns a transformed point which is calculated using the scale and the shift.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>  which specifies the untransformed x-coordinate.
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/> which specifies the untransformed y-coordinate.
        /// </param>
        /// <returns>
        /// A <see cref="Point"/>
        /// </returns>
        private Point GetTransformedPoint (double x, double y)
        {
            return new Point ((x+shift_x)/scale, (y+shift_y)/scale);
        }

        /// <summary>
        /// Adds a segment to the selection polygon.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        private void AddSegment (double x, double y)
        {
            if (vertices == null)
                return;

//            DebugDrawSegment (x, y);

            vertices.Add (GetTransformedPoint (x,y));

            segment_x = x;
            segment_y = y;
        }

        /// <summary>
        /// [Debug] Draws a segment of the selection polygon.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        private void DebugDrawSegment (double x, double y)
        {
            Cairo.Context context = texture.Create();
            context.LineWidth = 5;
            context.Color = new Cairo.Color (0,1,0,0.9);

            context.MoveTo (segment_x, segment_y);
            context.LineTo (x, y);
            context.Stroke ();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        /// <summary>
        /// Returns all points inside the selection polygon.
        /// </summary>
        /// <param name="points">
        /// A <see cref="List<SongPoint>"/> which specifies a list of all points tested.
        /// </param>
        /// <returns>
        /// A <see cref="List<SongPoint>"/>
        /// </returns>
        public List<SongPoint> GetPointsInside (List<SongPoint> points)
        {
            List<SongPoint> inside = new List<SongPoint> ();

            foreach (SongPoint p in points) {

                if (IsPointInside (p.XY)) {
                    inside.Add (p);
                }
            }

            return inside;
        }


        // Winding number algorithm by Dan Sunday
        // http://softsurfer.com/Archive/algorithm_0103/algorithm_0103.htm

        private bool IsPointInside (Point P)
        {
            int wn = 0;

             // loop through all edges of the polygon
            for (int i=0; i<vertices.Count-1; i++) {   // edge from V[i] to V[i+1]
                if (vertices[i].Y <= P.Y) {         // start y <= P.y
                    if (vertices[i+1].Y > P.Y)      // an upward crossing
                        if (isLeft( vertices[i], vertices[i+1], P) > 0)  // P left of edge
                            ++wn;            // have a valid up intersect
                }
                else {                       // start y > P.y (no test needed)
                    if (vertices[i+1].Y <= P.Y)     // a downward crossing
                        if (isLeft( vertices[i], vertices[i+1], P) < 0)  // P right of edge
                            --wn;            // have a valid down intersect
                }
            }
            return wn != 0;
        }

        private double isLeft ( Point P0, Point P1, Point P2)
        {
            double erg =  ( (P1.X - P0.X) * (P2.Y - P0.Y)
                            - (P2.X - P0.X) * (P1.Y - P0.Y) );
            if (Math.Abs (erg) < 0.001)
                erg = 0;

            return erg;
        }

        // ~~~~
    }
}

