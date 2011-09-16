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
using System.Linq;
using Clutter;
using System.Collections.Generic;
using System.Diagnostics;
using Hyena;

namespace Banshee.Cluttertest
{
    public class Cluster : Clutter.Texture, IStorable
    {
        #region Static Functions

        static private List<QRectangle> debug_quads = new List<QRectangle> ();
        static private Cluster root;
        static private QuadTree<Cluster> quad_tree;
        static private int max_prototypes = 4;
        static private uint circle_size = 50;
        static private List<CairoTexture> prototype_list = new List<CairoTexture> ();

        static public void Init ()
        {
            GeneratePrototypes ();
            root = new Cluster ();


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

            if (arc != 0) {

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

        static public List<QRectangle> HierarchicalInitDebug (List<Cluster> points, double width, double height)
        {

            Hyena.Log.Debug ("Hierarchical Clustering Debug Init");
            root.AddChildren (points);

            return debug_quads;
        }
        static public List<QRectangle> HierarchicalInit (List<Cluster> points, double width, double height)
        {
            Hyena.Log.Debug ("Hierarchical Clustering Init");
            root.AddChildren (points);

            double max_x = points.Max (p => p.X);
            double max_y = points.Max (p => p.Y);

            double min_x = points.Min (p => p.X);
            double min_y = points.Min (p => p.Y);

            Hyena.Log.Debug ("Max : " + max_x + "|" + max_y );
            Hyena.Log.Debug ("Min : " + min_x + "|" + min_y );

            //Create quadtree
            quad_tree = new QuadTree<Cluster> (0,0, max_x+1, max_y+1);

            quad_tree.OnCreateQuad += delegate(OnCreateQuadArgs args) {
                //Hyena.Log.Debug ("New Quad created at (" + args.Rectangle.X + "," + args.Rectangle.Y
                 //                + ") with (" + args.Rectangle.Width + "," + args.Rectangle.Height);
//                debug_quads.Add (args.Rectangle);
            };

            lock (points) {
                foreach (Cluster c in points)
                    quad_tree.Add (c);
            }

            return debug_quads;
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

        //private Point first_point;
        private CairoTexture prototype;
        private Point first_point;
        private List<Cluster> children;
        private List<Point> positions;  //save old cluster positions

        public Cluster ():base ()
        {
            children = new List<Cluster> ();
            positions = new List<Point> ();
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
            if (children.Count == 0)    //save original position
                first_point = this.XY;
            else
                positions.Add (this.XY);

            Point merged = this.XY;
            merged.Add (other.XY);
            merged.Normalize (2);   //new merged coordinates

            children.Add (other);

            /*
            Point pos = other.XY;

            other.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           5000,new String[]{"x"},new GLib.Value (merged.X));

            other.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           5000,new String[]{"y"},new GLib.Value (merged.Y));
            */
            other.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           5000,new String[]{"opacity"},new GLib.Value (0));


            other.Animation.Completed += delegate {
                other.Hide ();
                //other.SetPosition (pos.X,pos.Y);
                //Hyena.Log.Debug ("Hidden");
            };
            //other.Hide ();          //hide merged cluster
            //this.SetPosition (merged.X, merged.Y);

            this.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           5000,new String[]{"x"},new GLib.Value (merged.X));

            this.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           5000,new String[]{"y"},new GLib.Value (merged.Y));
        }

        //Dont call this function if less than one child is in list
        public Cluster DemergeSimpleCluster ()
        {
            Cluster ret = children[children.Count-1];   //last cluster in list

            Point pos;

            if (children.Count == 1)
                pos = first_point;
            else
            {
                /*pos = this.XY;    //recalculate position without last cluster
                pos.Multiply (2);
                pos.Subtract (ret.XY);*/
                pos = positions[positions.Count-1];
                positions.RemoveAt (positions.Count-1);
            }

                this.SetPosition (pos.FloatX, pos.FloatY);

            //Point oldPos = ret.XY;
            //ret.SetPosition (this.X, this.Y);
            ret.Show();
            ret.Opacity = 0;
            ret.Animatev ((ulong)AnimationMode.EaseOutCubic,
                          1000,new String[]{"opacity"},new GLib.Value (255));
            /*
            ret.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           1000,new String[]{"x"},new GLib.Value (oldPos.X));

            ret.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           1000,new String[]{"y"},new GLib.Value (oldPos.Y));

            this.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           1000,new String[]{"x"},new GLib.Value (pos.X));

            this.Animatev ((ulong)AnimationMode.EaseOutCubic,
                           1000,new String[]{"y"},new GLib.Value (pos.Y));
            */
            children.RemoveAt (children.Count-1);   //remove last cluster

            return ret;
        }
    }
}

