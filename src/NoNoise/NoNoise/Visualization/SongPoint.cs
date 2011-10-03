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

        /// <summary>
        /// Selection mode of a point
        /// </summary>
        public enum SelectionMode
        {
            /// <summary>
            /// Not selected.
            /// </summary>
            None = 0,

            /// <summary>
            /// Partially selected (i.e. some leaf points in the hierarchy below is selected).
            /// </summary>
            Partial = 2,

            /// <summary>
            /// Fully selected (i.e. all leaf points in the hierarchy below are selected).
            /// </summary>
            Full = 3
        };

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

        /// <summary>
        /// Gets and sets the selection mode of this point.
        /// </summary>
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

        /// <summary>
        /// Returns true if this point is a leaf node (i.e. no children).
        /// </summary>
        public bool IsLeaf {
            get {
                return LeftChild == null && RightChild == null;
            }
        }

        /// <summary>
        /// Returns true if this point is hidden (due to a search filter).
        /// </summary>
        public bool IsHidden {
            get;
            set;
        }

        /// <summary>
        /// Returns true if this point has been removed.
        /// </summary>
        public bool IsRemoved {
            get;
            set;
        }

        /// <summary>
        /// Returns true if this point is visible (i.e. some point in the hierarchy is visible).
        /// </summary>
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

        /// <summary>
        /// Returns true if this point is fully selected.
        /// </summary>
        public bool IsSelected {
            get { return Selection == SongPoint.SelectionMode.Full; }
            set { Selection = value ? SelectionMode.Full : SelectionMode.None; }
        }
        
        /// <summary>
        /// The X position in the 2D space.
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
        /// The Y position in the 2D space.
        /// </summary>
        public double Y {
            get {
                if (IsLeaf || MainChild == null)
                    return y;

                return MainChild.Y;
            }

            private set { y = value; }
        }

        /// <summary>
        /// Returns the parent point in the hierarchy.
        /// </summary>
        public SongPoint Parent {
            get;
            private set;
        }

        /// <summary>
        /// Returns the left child if visible, otherwise the right child.
        /// </summary>
        public SongPoint MainChild {
            get {
                if (LeftChild.IsVisible)
                    return LeftChild;

                return RightChild;
            }
        }

        /// <summary>
        /// Returns the left child. Can not be null.
        /// </summary>
        public SongPoint LeftChild {
            get;
            private set;
        }

        /// <summary>
        /// Returns the right child. Can be null.
        /// </summary>
        public SongPoint RightChild {
            get;
            private set;
        }

        /// <summary>
        /// Returns a <see cref="Point"/> with the xy coordinates of this point.
        /// </summary>
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

        /// <summary>
        /// Marks this point removed if it has been selected.
        /// </summary>
        public void MarkRemovedifSelected ()
        {
            if (IsSelected && IsVisible && IsLeaf)
                IsRemoved = true;
        }

        /// <summary>
        /// Marks this point and all points in this subtree selected.
        /// </summary>
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

        /// <summary>
        /// Collects all ids recursively and returns them as a list.
        /// </summary>
        /// <returns>
        /// A <see cref="List<System.Int32>"/>
        /// </returns>
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

        /// <summary>
        /// Returns a merged point which is parent to this point and the other point.
        /// </summary>
        /// <param name="other">
        /// A <see cref="SongPoint"/>
        /// </param>
        /// <returns>
        /// A <see cref="SongPoint"/>
        /// </returns>
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

