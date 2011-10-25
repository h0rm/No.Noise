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
    /// <summary>
    /// This class is used to store, cluster, retrieve and modify <see cref="SongPoint"/>s.
    /// </summary>
    public class SongPointManager
    {
        private int max_clustering_level = 8;
        private List<QuadTree<SongPoint>> tree_list;
        private const int min_points = 8;
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

        /// <summary>
        /// Returns true if the current clustering level equals the maximum clustering level.
        /// </summary>
        public bool IsMaxLevel {
            get { return max_clustering_level == level; }
        }

        /// <summary>
        /// Returns true if the current clustering level is zero.
        /// </summary>
        public bool IsMinLevel {
            get { return level == 0; }
        }

        /// <summary>
        /// Gets and sets the current clustering level.
        /// </summary>
        public int Level {
            get { return level; }
            set {
                int lvl = (value > max_clustering_level) ? max_clustering_level : value;
                lvl = (lvl < 0) ? 0 : lvl;
                level = lvl;
            }
        }

        /// <summary>
        /// Increases the current clustering level.
        /// </summary>
        public void IncreaseLevel ()
        {
            Level = Level + 1;
        }

        /// <summary>
        /// Decreases the clustering level.
        /// </summary>
        public void DecreaseLevel ()
        {
            Level = Level - 1;
        }

        public SongPointManager (double x, double y, double width, double height)
        {
            Width = width;
            Height = height;

            tree_list = new List<QuadTree<SongPoint>> ();
            dict = new Dictionary<int, SongPoint> ();

            tree_list.Add (new QuadTree<SongPoint> (x, y, width, height));
        }


        /// <summary>
        /// Clusters all points.
        /// </summary>
        public void Cluster ()
        {
            Hyena.Log.Information ("Clustering started");
            QuadTree<SongPoint> tree;

            int i;
            double w,h;

            Hyena.Log.Information ("[0] Clustering points " + tree_list[0].Count);
            for (i = 0; i < max_clustering_level; i++) {
//                tree = tree_list[i].GetClusteredTree (Width * Math.Sqrt (2) / ((double)(max_clustering_level - i -1)));
//                tree = tree_list[i].GetAdvancedClusteredTree (double.MaxValue);
//                tree = tree_list[i].GetClusteredTree (double.MaxValue);
                tree = tree_list[i].GetFastClusteredTree (double.MaxValue);

                Hyena.Log.Information ("["+(i+1)+"] Clustering points " + tree.Count);

                //check if number of points is above minimum
                if (tree.Count < min_points)
                    break;

                tree.GetWindowDimesions (500, out w, out h);
                tree_list.Add (tree);
            }

            max_clustering_level = tree_list.Count-1;

            Hyena.Log.Debug ("Max clustering level " + max_clustering_level);
        }

        /// <summary>
        /// Returns the point corresponding to the given id.
        /// </summary>
        /// <param name="id">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <returns>
        /// A <see cref="SongPoint"/>
        /// </returns>
        public SongPoint GetPoint (int id)
        {
            if (!dict.ContainsKey(id))
                return null;

            return dict[id];
        }

        /// <summary>
        /// Returns a list of all Points in the current clustering level.
        /// </summary>
        public List<SongPoint> Points {
            get { return tree_list[level].GetAllObjects (); }
        }

        /// <summary>
        /// Adds a point given by its coordinates and id.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="id">
        /// A <see cref="System.Int32"/>
        /// </param>
        public void Add (double x, double y, int id)
        {
            SongPoint point = new SongPoint (x, y, id);
            tree_list[0].Add (point);
            dict.Add (id,point);
        }

        /// <summary>
        /// Returns the points which are in the given clipping window.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="width">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="height">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <returns>
        /// A <see cref="List<SongPoint>"/>
        /// </returns>
        public List<SongPoint> GetPointsInWindow (double x, double y, double width, double height)
        {
            return tree_list[level].GetObjects (new QRectangle (x, y, width, height));
        }

        /// <summary>
        /// Returns the points which are in the given clipping window.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="width">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="height">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="level_offset">
        /// A <see cref="System.Int32"/> which specifies the level offset to the current clustering level.
        /// </param>
        /// <returns>
        /// A <see cref="List<SongPoint>"/>
        /// </returns>
        public List<SongPoint> GetPointsInWindow (double x, double y, double width, double height, int level_offset)
        {

            int lvl = (level_offset + level > max_clustering_level) ? max_clustering_level : level_offset + level;
            lvl = (lvl < 0) ? 0 : lvl;

            return tree_list[lvl].GetObjects (new QRectangle (x, y, width, height));
        }

        /// <summary>
        /// Calculated the optimal window dimension for the given clustering level and number of points.
        /// </summary>
        /// <param name="level">
        /// A <see cref="System.Int32"/> which specifies the clustering level.
        /// </param>
        /// <param name="num_of_points">
        /// A <see cref="System.Int32"/> which specifies the maximum number of points in the window.
        /// </param>
        /// <param name="w">
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="h">
        /// A <see cref="System.Double"/>
        /// </param>
        public void GetWindowDimensions (int level, int num_of_points, out double w, out double h)
        {

            int lvl = (level > max_clustering_level) ? max_clustering_level : level;
            lvl = (lvl < 0) ? 0 : lvl;

            tree_list[lvl].GetWindowDimesions (num_of_points, out w, out h);
        }

        /// <summary>
        /// Sets the initial clustering level according to a maximum number of points.
        /// </summary>
        /// <param name="numofpoints">
        /// A <see cref="System.Int32"/>
        /// </param>
        public void SetDefaultLevel (int numofpoints)
        {
            int i;

            //get lowest level with Count < numofpoints
            for (i = max_clustering_level; i >= 0; i--) {
                if (tree_list[i].Count > numofpoints)
                    break;
            }

            Level = i-1;
            Hyena.Log.Debug ("Default level set to " + Level);
        }

        /// <summary>
        /// Marks all selected leaf points as removed.
        /// </summary>
        public void RemoveSelection ()
        {
            foreach (SongPoint p in tree_list[0].GetAllObjects())
                p.MarkRemovedifSelected ();

            InvalidatePositions ();
        }

        /// <summary>
        /// Marks all leaf points as not selected
        /// </summary>
        public void ClearSelection ()
        {
            foreach (SongPoint p in tree_list[0].GetAllObjects())
                p.IsSelected = false;
        }

        /// <summary>
        /// Marks all leaf points as not removed.
        /// </summary>
        public void ShowRemoved ()
        {
            foreach (SongPoint p in tree_list[0].GetAllObjects())
                p.IsRemoved = false;

            InvalidatePositions ();
        }

//        /// <summary>
//        /// Returns all selected leaf points.
//        /// </summary>
//        /// <returns>
//        /// A <see cref="List<SongPoint>"/>
//        /// </returns>
//        public List<SongPoint> GetSelected ()
//        {
//            List<SongPoint> ret = new List<SongPoint> ();
//
//            foreach (SongPoint p in tree_list[0].GetAllObjects ()) {
//                if (p.IsSelected && p.IsVisible)
//                    ret.Add (p);
//            }
//
//            return ret;
//        }

        public List<int> GetSelectedIDs ()
        {
            List<int> ret = new List<int> ();

            foreach (SongPoint p in tree_list[0].GetAllObjects ()) {
                if (p.IsSelected && p.IsVisible)
                    ret.Add (p.ID);
            }

            return ret;
        }

        /// <summary>
        /// Marks all leaf points as hidden exept the points with the ids given in the list.
        /// </summary>
        /// <param name="not_hidden">
        /// A <see cref="List<System.Int32>"/>
        /// </param>
        public void MarkHidded (List<int> not_hidden)
        {
            foreach (SongPoint p in tree_list[0].GetAllObjects())
//                p.MarkHidden ();
                p.IsHidden = true;

            foreach (int i in not_hidden) {
                if (dict.ContainsKey (i))
//                    dict[i].MarkShown ();
                    dict[i].IsHidden = false;
            }

            InvalidatePositions ();
        }

        private void InvalidatePositions ()
        {
            foreach (SongPoint p in tree_list[0].GetAllObjects())
//                p.MarkHidden ();
                p.InvalidatePosition ();
        }
    }
}

