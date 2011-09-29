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
using NoNoise.Visualization.Util;
using System.Collections.Generic;

namespace NoNoise.Visualization
{
    /// <summary>
    /// This class is an abstract representation of a song.
    /// </summary>
    public class SongPoint :IStorable<SongPoint>
    {
        private double x;
        private double y;
        private SelectionMode selection;

        public enum SelectionMode {None = 0, Partial = 2, Full = 3};

        public SongPoint (double x, double y, int id)
        {
            X = x;
            Y = y;
            ID = id;
            Actor = null;
            Parent = null;
            LeftChild = null;
            RightChild = null;
            IsSelected = false;
            IsRemoved = false;
            IsHidden = false;
        }

        public SelectionMode Selection {
            get {
                if (IsLeaf)
                    return selection;

                if (RightChild != null) {
                    if (RightChild.IsSelected && LeftChild.IsSelected)
                        return SelectionMode.Full;

                    if (RightChild.Selection != SelectionMode.None
                        || LeftChild.Selection != SelectionMode.None)
                        return SelectionMode.Partial;
                }

                return LeftChild.Selection;
            }

            private set {
                selection = value;
            }
        }
        public bool IsLeaf {
            get {
                return LeftChild == null && RightChild == null;
            }
        }
        public bool IsHidden {
            get;
            set;
        }
        public bool IsRemoved {
            get;
            set;
        }

        public bool IsVisible {
            get {
                //Leaf, i know if im visible
                if (IsLeaf)
                    return !IsHidden && !IsRemoved;

                //Both children - check both
                if (RightChild != null)
                    return LeftChild.IsVisible || RightChild.IsVisible;

                //only left child
                return LeftChild.IsVisible;
            }
        }

        public bool IsSelected {
            get { return Selection == SongPoint.SelectionMode.Full; }
            set { Selection = value ? SelectionMode.Full : SelectionMode.None; }
        }
        
        /// <summary>
        /// The X position in the 2D space
        /// </summary>
        public double X {
            get {
                if (IsLeaf || MainChild == null)
                    return x;

                return MainChild.X;
            }

            private set { x = value; }
        }

        /// <summary>
        /// The Y position in the 2D space
        /// </summary>
        public double Y {
            get {
                if (IsLeaf || MainChild == null)
                    return y;

                return MainChild.Y;
            }

            private set { y = value; }
        }

        public SongPoint Parent {
            get;
            private set;
        }

        public SongPoint MainChild {
            get {
                if (LeftChild.IsVisible)
                    return LeftChild;

                return RightChild;
            }
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
        public int ID {
            get;
            private set;
        }

        public void MarkRemovedifSelected ()
        {
            if (IsSelected && IsVisible && IsLeaf)
                IsRemoved = true;
        }


        public void MarkAsSelected ()
        {
            if (!IsVisible)
                return;

            if (IsLeaf)
                IsSelected = true;

            if (LeftChild != null)
                LeftChild.MarkAsSelected ();

            if (RightChild != null)
                RightChild.MarkAsSelected ();

        }

//        public void ClearSelection ()
//        {
//            IsSelected = false;
//
////            if (LeftChild != null)
////                LeftChild.ClearSelection ();
////
////            if (RightChild != null)
////                RightChild.ClearSelection ();
//        }

        public List<int> GetAllIDs ()
        {
            List<int> ids = new List<int> ();
            if (IsLeaf && IsVisible)
                ids.Add (ID);

            if (LeftChild != null)
                if (LeftChild.IsVisible)
                    ids.AddRange (LeftChild.GetAllIDs ());
            if (RightChild != null)
                if (RightChild.IsVisible)
                    ids.AddRange (RightChild.GetAllIDs ());

            return ids;
        }
        
        public SongPoint GetMerged (SongPoint other)
        {
            Point merged = this.XY;

            SongPoint parent = new SongPoint (merged.X, merged.Y, -1);
            parent.LeftChild = this;
            parent.RightChild = other;

            this.Parent = parent;

            if (other != null)
                other.Parent = parent;

            return parent;
        }

    }
}

