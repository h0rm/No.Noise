// 
// Point.cs
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


namespace Banshee.Cluttertest
{
    public class Point
    {
        double x;
        double y;

        public Point (float x, float y) {
            this.x = x;
            this.y = y;
           // this.color = color;
        }

        public float X {
            get { return (float)x; }
            set { x = value; }
        }

        public float Y {
            get { return (float)y; }
            set { y = value; }
        }

        public void Add (Point p)
        {
            this.x += p.X;
            this.y += p.Y;
        }

        public void Subtract (Point p)
        {
            this.x -= p.X;
            this.y -= p.Y;
        }

        public void Add (float x, float y)
        {
            this.x += x;
            this.y += y;
        }

        public void Multiply (float factor)
        {
            this.x *= (double)factor;
            this.y *= (double)factor;
        }

        public void AddWithNormalization (Point p, int factor)
        {
            Multiply (factor);

            Normalize (factor+1);

            //Normalize (factor);
        }
        public void Normalize (int count)
        {
            this.x /= (double)count;
            this.y /= (double)count;
        }

        public double DistanceTo (Point p)
        {
            return Math.Sqrt ((p.X - this.X) * (p.X - this.X) + (p.Y - this.Y) * (p.Y - this.Y));
        }

    };
}
