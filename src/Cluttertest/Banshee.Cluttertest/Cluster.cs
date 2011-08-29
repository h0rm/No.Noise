// 
// HCluster.cs
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
using Clutter;
using System.Collections.Generic;
using System.Diagnostics;
using Hyena;

namespace Banshee.Cluttertest
{
    public class Cluster : Clutter.Texture
    {
        #region Static Functions

        static private Cluster root;
        static private int max_prototypes = 4;
        static private uint circle_size = 50;
        static private List<CairoTexture> prototype_list = new List<CairoTexture> ();

        static public void Init ()
        {
            GeneratePrototypes ();
            root = new Cluster (0,0);
        }

        static private void GeneratePrototypes ()
        {
            prototype_list.Clear ();

            for (int i=0; i < max_prototypes; i++)
            {
                CairoTexture texture = new CairoTexture (circle_size,circle_size);
                prototype_list.Add (texture);
                UpdateCirclePrototypeColor (texture, i);
            }
        }

        static private void UpdateCirclePrototypeColor (CairoTexture actor, int color_index)
        {
            Debug.Assert (color_index < max_prototypes);

            Hyena.Log.Debug ("Color changed to "+color_index);

            switch (color_index)
            {
            case 0:
                UpdateCirclePrototype (actor,0.0, 1.0, 0.0);
                break;

            case 1:
                UpdateCirclePrototype (actor,1.0, 0.0, 0.0);
                break;

            case 2:
                UpdateCirclePrototype (actor,0.0, 0.0, 1.0);
                break;

            case 3:
                UpdateCirclePrototype (actor,1.0, 1.0, 1.0);
                break;
            }
        }

        static private void UpdateCirclePrototype (CairoTexture actor, double r, double g, double b)
        {
            UpdateCirclePrototype (actor,r,g,b,0.55,0,0,0,0);
        }

        /// <summary>
        /// Redraws the circle_prototype which is used for all circles as template.
        /// This means a new circle is drawn with cairo and stored in a texture.
        /// </summary>
        static private void UpdateCirclePrototype (CairoTexture actor, double r, double g, double b, double a,
                                                   double arc, double a_r, double a_g, double a_b)
        {
            double size = (double)circle_size;

            Hyena.Log.Debug ("Color : " + r + " " + g + " " + b + " ");
            actor.Clear();
            Cairo.Context context = actor.Create();


            Cairo.Gradient pattern = new Cairo.RadialGradient(size/2.0,size/2.0,size/3.0,
                                                            size/2.0,size/2.0,size/2.0);


            //Cairo.Gradient pattern = new Cairo.LinearGradient(0,0,circle_size,circle_size);
            pattern.AddColorStop(0,new Cairo.Color (r,g,b,a));
            pattern.AddColorStop(1.0,new Cairo.Color (r,g,b,0.1));

            context.LineWidth = (double)size/5.0;
            context.Arc (size/2.0, size/2.0,
                         size/2.0-context.LineWidth/2.0,0,2*Math.PI);

            context.Save();

            context.Pattern = pattern;
            //context.Color = new Cairo.Color (r,g,b,0.3);
            context.Fill();

            //context.Restore ();

            if (arc != 0)
            {
                context.LineWidth = (double)size/10.0;
                context.Arc (size/2.0, size/2.0,
                         size/2.0-context.LineWidth/2.0,-Math.PI/2.0,2*Math.PI*arc/100.0-Math.PI/2.0);

                Hyena.Log.Debug ("Arc prototype "+ arc);
                context.Color = new Cairo.Color (a_r,a_g,a_b,0.5);
                context.Stroke ();
            }
            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        static public void HierarchicalInit (List<Cluster> points)
        {
            Hyena.Log.Debug ("Hierarchical Clustering Init");
            root.AddChildren (points);
        }

        static public void RefineOneStep ()
        {
            root.RefineChildren ();
        }

        static public void ClusterOneStep ()
        {
            root.ClusterChildren ();
        }

        #endregion

        private Point first_point;
        private CairoTexture prototype;
        private List<Cluster> children;

        public Cluster (uint x, uint y):base ()
        {
            this.SetPosition (x,y);
            first_point = new Point (x,y);
            children = new List<Cluster> ();
        }
        public CairoTexture Prototype
        {
            set
            {
                prototype = value;
                this.CoglTexture = prototype.CoglTexture;
            }

            get { return prototype; }
        }

        public Point XY {
            get {return new Point (this.X, this.Y);}
        }

        public void AddChildren (List<Cluster> children)
        {
            this.children.AddRange (children);
        }

        public void SetPrototypeByNumber (int num)
        {
            Debug.Assert (num < prototype_list.Count);

            Prototype = prototype_list[num];
        }

        /// <summary>
        /// This function is used to undo one clustering step to show more detail
        /// </summary>
        public void RefineChildren ()
        {
            Hyena.Log.Debug ("Refine Children, Count: "+children.Count);
            List<Cluster> new_cluster = new List<Cluster> ();

            foreach (Cluster c in children)
            {
                if (c.children.Count > 0)
                    new_cluster.Add (c.DemergeSimpleCluster ());
            }
            children.AddRange (new_cluster);
        }

        /// <summary>
        /// This function merges each child with another one, i.e. the number of
        /// clusters is devided by two.
        /// </summary>
        public void ClusterChildren ()
        {
            Hyena.Log.Debug ("Cluster Children, Count: "+children.Count);
            if (children.Count < 2)
                return;

            //for each cluster the closest cluster is found and merged
            for (int i=0; i < children.Count; i++)
            {
                double min_distance = double.MaxValue;

                Cluster current = children[i];
                Cluster found = children[i];

                for (int j=i+1; j < children.Count; j++)
                {
                    double dist = current.XY.DistanceTo (children[j].XY);

                    if (dist < min_distance)
                    {
                        min_distance = dist;
                        found = children[j];
                    }
                }

                if (min_distance < double.MaxValue )
                {
                    current.MergeWithCluster (found);
                    children.Remove (found);
                }
            }
        }

        /// <summary>
        /// This function merges a cluster into this cluster.
        /// </summary>
        /// <param name="other">
        /// A <see cref="Cluster"/>
        /// </param>
        public void MergeWithCluster (Cluster other)
        {
            if (children.Count == 0)        //save original position
                first_point = this.XY;

            Point merged = this.XY;
            merged.Add (other.XY);
            merged.Normalize (2);   //new merged coordinates

            children.Add (other);
            other.Hide ();          //hide merged cluster
            this.SetPosition (merged.X, merged.Y);
        }

        //Dont call this function if less than one child is in list
        public Cluster DemergeSimpleCluster ()
        {
            Cluster ret = children[children.Count-1];   //last cluster in list

            if (children.Count == 1){   //only one left
                this.SetPosition (first_point.X, first_point.Y);
                children = new List<Cluster> ();
            }
            else
            {
                Point pos = this.XY;    //recalculate position without last cluster
                pos.Multiply (2);
                pos.Subtract (ret.XY);
                this.SetPosition (pos.X, pos.Y);
                children.RemoveAt (children.Count-1);   //remove last cluster
            }
            ret.Show();     //show removed cluster
            return ret;
        }
    }
}

