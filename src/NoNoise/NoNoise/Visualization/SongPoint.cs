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
        public enum SelectionMode {None, Partial, Full};

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
            Selection = SelectionMode.None;
        }

        public SelectionMode Selection {
            get;
            private set;
        }
        public bool IsLeaf {
            get {
                return LeftChild == null && RightChild == null;
            }
        }
        public bool IsHidden {
            get;
            private set;
        }
        public bool IsRemoved {
            get;
            private set;
        }

        public bool IsVisible {
            get { return !IsRemoved && !IsHidden; }
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

        public void MarkHidden ()
        {
            IsHidden = true;

            if (LeftChild != null)
                LeftChild.MarkHidden ();

            if (RightChild != null)
                RightChild.MarkHidden ();
        }

        public void MarkShown ()
        {
            IsHidden = false;

            Parent.MarkShownUpwards ();
        }

        private void MarkShownUpwards ()
        {
            /// Cases
            /// 1 - this node is show - do nothing visited
            /// 2 - the only child (left) is shown - node is shown
            /// 3 - at least one child is shown - node is shown
            ///

            if (!IsHidden) //not hidden - stop node has been visited
                return;

            //rightchild null - check only left child
            if (RightChild == null && !LeftChild.IsHidden) {
                IsHidden = false;
                if (Parent != null) //if parent exists, propergate
                    Parent.MarkShownUpwards ();
                return;
            }

            if (RightChild == null)
                return;

            //both not null, check both
            if (!LeftChild.IsHidden || !RightChild.IsHidden) {
                IsHidden = false;
                if (Parent != null) //if parent exists, propergate
                    Parent.MarkShownUpwards ();
            }
        }

        public void UnmarkRemoved ()
        {
            IsRemoved = false;

            if (LeftChild != null)
                LeftChild.UnmarkRemoved ();

            if (RightChild != null)
                RightChild.UnmarkRemoved ();
        }

        public void MarkRemovedifSelected ()
        {
            if (IsSelected)
                IsRemoved = true;


            if (LeftChild != null)
                LeftChild.MarkRemovedifSelected ();

            if (RightChild != null)
                RightChild.MarkRemovedifSelected ();
        }

        private void SelectUpwards ()
        {
            /// Cases
            /// 1 - this node is selected - do nothing visited and nothing more to do
            /// 2 - only child (left) - node has same selection
            /// 3 - at least one child is partially selected - node is partially selected
            /// 4 - both children are fully selected - node is fully selected
            ///

            // case 1
            if (IsSelected) //selected - stop node has been visited
                return;

            // case 2
            if (RightChild == null) {
                Selection = LeftChild.Selection;
                if (Parent != null) //if parent exists, propergate
                    Parent.SelectUpwards ();
                return;
            }

            if (RightChild == null)
                return;

            // case 4
            if (LeftChild.IsSelected && RightChild.IsSelected) {
                IsSelected = true;

                if (Parent != null) //if parent exists, propergate
                    Parent.SelectUpwards ();
                return;
            }

            // case 3
            if (LeftChild.Selection != SelectionMode.None ||
                            RightChild.Selection != SelectionMode.None) {
                Selection = SelectionMode.Partial;

                if (Parent != null) //if parent exists, propergate
                    Parent.SelectUpwards ();
                return;
            }
        }

        public void MarkAsSelected ()
        {
            IsSelected = true;

            if (LeftChild != null)
                LeftChild.MarkAsSelected ();

            if (RightChild != null)
                RightChild.MarkAsSelected ();

            if (Parent != null)
                Parent.SelectUpwards ();
        }

        public void ClearSelection ()
        {
            IsSelected = false;

            if (LeftChild != null)
                LeftChild.ClearSelection ();

            if (RightChild != null)
                RightChild.ClearSelection ();
        }
        public List<int> GetAllIDs ()
        {
            List<int> ids = new List<int> ();
            if (IsLeaf && !IsRemoved)
                ids.Add (ID);

            if (LeftChild != null)
                if (!LeftChild.IsRemoved)
                    ids.AddRange (LeftChild.GetAllIDs ());
            if (RightChild != null)
                if (!RightChild.IsRemoved)
                    ids.AddRange (RightChild.GetAllIDs ());

            return ids;
        }
        
        public SongPoint GetMerged (SongPoint other)
        {
            Point merged = this.XY;

            //fake merge, only left child
//            if (other != null) {
//                merged.Add (other.XY);
//                merged.Normalize (2);
//            }

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

