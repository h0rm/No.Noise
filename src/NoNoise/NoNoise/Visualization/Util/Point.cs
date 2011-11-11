// 
// Point.cs
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


namespace NoNoise.Visualization.Util
{
    /// <summary>
    /// Helper class which represents a point coordinate.
    /// </summary>
    public class Point
    {
        double x;
        double y;

        /*public Point (float x, float y) {
            this.x = x;
            this.y = y;
           // this.color = color;
        }*/

        public Point (double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public float FloatX {
            get { return (float)x; }
        }

        public float FloatY {
            get { return (float)y; }
        }

        public double X {
            get { return x; }
            set { x = value; }
        }

        public double Y {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Adds the xy-values of another point to this point.
        /// </summary>
        /// <param name="p">
        /// A <see cref="Point"/>
        /// </param>
        public void Add (Point p)
        {
            this.x += p.X;
            this.y += p.Y;
        }

        /// <summary>
        /// Substracts the xy-values of another point of this point.
        /// </summary>
        /// <param name="p">
        /// A <see cref="Point"/>
        /// </param>
        public void Subtract (Point p)
        {
            this.x -= p.X;
            this.y -= p.Y;
        }

        /// <summary>
        /// Adds the given xy-values to this point.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        public void Add (double x, double y)
        {
            this.x += x;
            this.y += y;
        }

        /// <summary>
        /// Multiplies the xy-values with the given factor.
        /// </summary>
        /// <param name="factor">
        /// A <see cref="System.Double"/>
        /// </param>
        public void Multiply (double factor)
        {
            this.x *= factor;
            this.y *= factor;
        }

        /// <summary>
        /// Normalizes the point (i.e. divides the xy-values by the given count).
        /// </summary>
        /// <param name="count">
        /// A <see cref="System.Int32"/>
        /// </param>
        public void Normalize (int count)
        {
            this.x /= (double)count;
            this.y /= (double)count;
        }

        /// <summary>
        /// Returns the euclidean distance of this point to the given point.
        /// </summary>
        /// <param name="p">
        /// A <see cref="Point"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Double"/>
        /// </returns>
        public double DistanceTo (Point p)
        {
            return Math.Sqrt ((p.X - this.X) * (p.X - this.X) + (p.Y - this.Y) * (p.Y - this.Y));
        }


        override public String ToString ()
        {
            return "("+X+","+Y+")";
        }
    };
}

