// 
// SongPoint.cs
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
    /// <summary>
    /// This class is an abstract representation of a song.
    /// </summary>
    public class SongPoint :IStorable<SongPoint>
    {
        public SongPoint (double x, double y, string id)
        {
            X = x;
            Y = y;
            ID = id;
            Actor = null;
            Parent = null;
            LeftChild = null;
            RightChild = null;
        }

        /// <summary>
        /// The X position in the 2D space
        /// </summary>
        public double X {
            get;
            private set;
        }

        /// <summary>
        /// The Y position in the 2D space
        /// </summary>
        public double Y {
            get;
            private set;
        }

        public SongPoint Parent {
            get;
            private set;
        }

        public SongPoint LeftChild {
            get;
            private set;
        }

        public SongPoint RightChild {
            get;
            private set;
        }

        public Point XY {
            get { return new Point (X, Y); }
        }
        /// <summary>
        /// Linked Cluster actor which is used for rendering.
        /// </summary>
        public SongActor Actor {
            get;
            set;
        }

        /// <summary>
        /// Unique id which represents the song.
        /// </summary>
        public String ID {
            get;
            private set;
        }

        public SongPoint GetMerged (SongPoint other)
        {
            Point merged = this.XY;

            //fake merge, only left child
//            if (other != null) {
//                merged.Add (other.XY);
//                merged.Normalize (2);
//            }

            SongPoint parent = new SongPoint (merged.X, merged.Y, ID + other.ID);
            parent.LeftChild = this;
            parent.RightChild = other;

            this.Parent = parent;
            other.Parent = parent;

            return parent;
        }

    }
}

