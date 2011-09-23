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
        }

        public new void SetSize (float width, float height)
        {
            texture.SetSurfaceSize ((uint)Math.Ceiling(width), (uint)Math.Ceiling (height));
            texture.SetSize ((float)Math.Ceiling(width), (float)Math.Ceiling (height));
        }
        public Cairo.Color Color {
            get;
            set;
        }
        public void Reset ()
        {
            texture.Clear ();
            Cairo.Context context = texture.Create();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();

            vertices = new List<Point> ();

            count = 0;
        }


        public void Start (double x, double y, double scale, double shift_x, double shift_y)
        {
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

        public void Stop ()
        {
            if (vertices.Count > 0) {

                AddSegment (old_x,old_y);
                vertices.Add (GetTransformedPoint (start_x,start_y));

                DrawLine (start_x, start_y);
                DebugDrawSegment (start_x, start_y);
            }
        }

        public void LineTo (double x, double y)
        {
            DrawLine (x,y);

            Point p = new Point (x,y);

            if (p.DistanceTo (new Point (segment_x, segment_y)) > 20) {
//            if (count == 5) {
                AddSegment (x, y);
                count = 0;
            } else {
                count ++;
            }

        }

        private void DrawLine (double x, double y)
        {
//            Hyena.Log.Information ("Paint line " + count );

            Cairo.Context context = texture.Create();
            context.LineWidth = 5;
            context.Color = new Cairo.Color (1,0,0,0.9);

            context.MoveTo (old_x, old_y);
            context.LineTo (x, y);
            context.Stroke ();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();

            old_x = x;
            old_y = y;
        }

        private Point GetTransformedPoint (double x, double y)
        {
            return new Point ((x+shift_x)/scale, (y+shift_y)/scale);
//            return new Point (x, y);
        }
        private void AddSegment (double x, double y)
        {
            if (vertices == null)
                return;

            DebugDrawSegment (x, y);

            vertices.Add (GetTransformedPoint (x,y));

            segment_x = x;
            segment_y = y;
        }

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

        public List<SongPoint> GetPointsInside (List<SongPoint> points)
        {
            List<SongPoint> inside = new List<SongPoint> ();

            foreach (SongPoint p in points) {

                if (IsPointInside (p.XY)) {
                    inside.Add (p);
                    Hyena.Log.Information (p.ID + " is inside");
                }
            }

            Hyena.Log.Information ("Inside : " + inside.Count);
            return inside;
        }

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
    }
}

