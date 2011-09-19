// 
// SongPointManager.cs
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

namespace Banshee.Cluttertest
{
    public class SongPointManager
    {
        private List<QuadTree<SongPoint>> tree_list;

        private int level = 0;

        public int Level {
            get { return level; }
            set { level = value; }
        }

        public SongPointManager (double x, double y, double width, double height)
        {
            tree_list = new List<QuadTree<SongPoint>> ();
            tree_list.Add (new QuadTree<SongPoint> (x, y, width, height));
        }

        public void Cluster ()
        {
            tree_list.Add (tree_list[0].GetClusteredTree ());
        }

        public List<SongPoint> Points {
            get { return tree_list[level].GetAllObjects (); }
        }

        public void Add (double x, double y, string id)
        {
            tree_list[0].Add (new SongPoint (x, y, id));
        }

        public List<SongPoint> GetPointsInWindow (double x, double y, double width, double height)
        {
            return tree_list[level].GetObjects (new QRectangle (x, y, width, height));
        }
    }
}

