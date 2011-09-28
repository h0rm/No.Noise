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
using NoNoise.Visualization.Util;

namespace NoNoise.Visualization
{
    public class SongPointManager
    {
        private int max_clustering_level = 8;
        private List<QuadTree<SongPoint>> tree_list;
        private const int min_points = 2;
        private int level = 0;
        private Dictionary<int,SongPoint> dict;

        public double Width {
            get;
            private set;
        }

        public double Height {
            get;
            private set;
        }

        public bool IsMaxLevel {
            get { return max_clustering_level == level; }
        }

        public bool IsMinLevel {
            get { return level == 0; }
        }

        public int Level {
            get { return level; }
            set {
                int lvl = (value > max_clustering_level) ? max_clustering_level : value;
                lvl = (lvl < 0) ? 0 : lvl;
                level = lvl;
            }
        }

        public void IncreaseLevel ()
        {
            Level = Level + 1;
            Hyena.Log.Debug ("New Clustering level " + Level);
        }

        public void DecreaseLevel ()
        {
            Level = Level - 1;
            Hyena.Log.Debug ("New Clustering level " + Level);
        }

        public SongPointManager (double x, double y, double width, double height)
        {
            Width = width;
            Height = height;

            tree_list = new List<QuadTree<SongPoint>> ();
            dict = new Dictionary<int, SongPoint> ();

            tree_list.Add (new QuadTree<SongPoint> (x, y, width, height));
        }

        public void Cluster ()
        {
            QuadTree<SongPoint> tree;

            int i;
            double w,h;

            for (i = 0; i < max_clustering_level; i++) {
                tree = tree_list[i].GetClusteredTree ();

                //check if number of points is above minimum
                if (tree.Count < min_points)
                    break;

                tree.GetWindowDimesions (500, out w, out h);
                Hyena.Log.Information ("Window dimension for 500 points: " + w + "x" + h);
                tree_list.Add (tree);
            }

            max_clustering_level = tree_list.Count-1;

            Hyena.Log.Information ("Max clustering level " + max_clustering_level);
        }

        public SongPoint GetPoint (int id)
        {
            if (!dict.ContainsKey(id))
                return null;

            return dict[id];
        }
        public List<SongPoint> Points {
            get { return tree_list[level].GetAllObjects (); }
        }

        public void Add (double x, double y, int id)
        {
            SongPoint point = new SongPoint (x, y, id);
            tree_list[0].Add (point);
            dict.Add (id,point);
        }

        public List<SongPoint> GetPointsInWindow (double x, double y, double width, double height)
        {
            return tree_list[level].GetObjects (new QRectangle (x, y, width, height));
        }

        public List<SongPoint> GetPointsInWindow (double x, double y, double width, double height, int level_offset)
        {

            int lvl = (level_offset + level > max_clustering_level) ? max_clustering_level : level_offset + level;
            lvl = (lvl < 0) ? 0 : lvl;

            return tree_list[lvl].GetObjects (new QRectangle (x, y, width, height));
        }

        public void GetWindowDimensions (int level, int num_of_points, out double w, out double h)
        {

            int lvl = (level > max_clustering_level) ? max_clustering_level : level;
            lvl = (lvl < 0) ? 0 : lvl;

            tree_list[lvl].GetWindowDimesions (num_of_points, out w, out h);
        }
        public void SetDefaultLevel (int numofpoints)
        {
            int i;

            //get lowest level with Count < numofpoints
            for (i = max_clustering_level; i >= 0; i--) {
                if (tree_list[i].Count > numofpoints)
                    break;
            }

            Level = i-1;
            Hyena.Log.Information ("Default level set to " + Level);
        }

        public void RemoveSelection ()
        {
            foreach (SongPoint p in tree_list[max_clustering_level].GetAllObjects())
                p.MarkRemovedifSelected ();
        }

        public void ClearSelection ()
        {
            foreach (SongPoint p in tree_list[max_clustering_level].GetAllObjects())
                p.ClearSelection ();
        }

        public void ShowRemoved ()
        {
            foreach (SongPoint p in tree_list[max_clustering_level].GetAllObjects())
                p.UnmarkRemoved ();
        }

        public List<SongPoint> GetSelected ()
        {
            List<SongPoint> ret = new List<SongPoint> ();

            foreach (SongPoint p in tree_list[0].GetAllObjects ()) {
                if (p.IsSelected)
                    ret.Add (p);
            }

            return ret;
        }

        public void MarkHidded (List<int> not_hidden)
        {
            Hyena.Log.Information ("Mark hidden");
            foreach (SongPoint p in tree_list[max_clustering_level].GetAllObjects())
                p.MarkHidden ();

            foreach (int i in not_hidden) {
                if (dict.ContainsKey (i))
                    dict[i].MarkShown ();
            }
        }
    }
}

