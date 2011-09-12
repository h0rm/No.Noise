// 
// Cluster.cs
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


        static private int max_prototypes = 4;
        static private List<CairoTexture> prototype_list = new List<CairoTexture> ();
        static private List<CairoTexture> indicator_prototypes = new List<CairoTexture> ();
        static private int max_indicators = 10;
        static private uint circle_size = 50;

        static private List<Point> center_list;

        static private int hierarchical_step;
        static private List<Cluster> marked_clusters;
        static private List<Cluster> clusters;

        #region static

        static public void Init ()
        {
		
            GeneratePrototypes ();
            GenerateIndicatorPrototypes ();
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

        static private void GenerateIndicatorPrototypes ()
        {
            indicator_prototypes.Clear ();

            for (int i=0; i < max_indicators; i++)
            {
                CairoTexture texture = new CairoTexture (circle_size,circle_size);
                indicator_prototypes.Add (texture);
                UpdateCirclePrototype (texture, 1,1,1,0.8,(double)(i+1)*10.0,0,1,0);
            }
        }

        static CairoTexture GetIndicatorPrototype (double percentage)
        {

            percentage = percentage > 100.0 ? 100.0 : percentage;
            Hyena.Log.Debug ("Indicator Prototype " + percentage);

            return indicator_prototypes[(int)(percentage/((double)max_indicators+0.01))];
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

        #endregion

        #region K-Means

        static public void KMeansInit (int k, float width, float height)
        {
            Hyena.Log.Debug ("Calculate Clusters");

            Debug.Assert(k <= 4);       //gibt noch nicht mehr farben

            center_list = new List<Point>();

            Random r = new Random ();

            for (int i=0; i<k; i++)
            {
                center_list.Add (new Point ((float)r.NextDouble () * width,
                                                (float)r.NextDouble () * height));

                Hyena.Log.Debug ("Center "+i+": "+center_list[i].X +" " + center_list[i].Y);
            }
        }



        static public void KMeansRefineStep (List<Cluster> clusters)
        {
            int [] count = new int[center_list.Count];
            Point [] sum = new Point[center_list.Count];

            for (int i= 0; i < center_list.Count; i++)
                sum[i] = new Point (0.0f,0.0f);

            foreach (Cluster circle in clusters)
            {
                double min = 1000000;
                int min_index = 2;

                for (int i=0; i < center_list.Count; i++)
                {
                    double dist = center_list[i].DistanceTo (new Point (circle.X, circle.Y) );
                    if (dist < min )
                    {
                        min_index = i;
                        min = dist;
                    }
                }

                //circle.CoglTexture = prototype_list[min_index].CoglTexture;
                circle.SetPrototypeByNumber (min_index);
                count[min_index] ++;
                sum[min_index].Add (circle.X, circle.Y);
            }

            for (int i= 0; i < center_list.Count; i++)
            {
                sum[i].Normalize (count[i]);
                center_list[i] = sum[i];

                Hyena.Log.Debug ("Center "+i+": "+center_list[i].X +" " + center_list[i].Y);
            }
        }
        #endregion

        #region Hierarchical clustering

        static public void HierarchicalInit (List<Cluster> points)
        {
            Hyena.Log.Debug ("Hierarchical Clustering Init");
            hierarchical_step = 0;
            marked_clusters = new List<Cluster> ();
            clusters = new List<Cluster> ();
            clusters.AddRange (points);
        }

        static private bool compute = true;

        static public bool HierarchicalCalculateStep (Group actor)
        {

            if (compute)
            {
                if (clusters.Count == 0 || hierarchical_step >= clusters.Count)
                    return true;

                marked_clusters = new List<Cluster> ();

                marked_clusters.Add (clusters[hierarchical_step]);
                clusters[hierarchical_step].SetPrototypeByNumber (1);

                Hyena.Log.Debug ("cluster zugegriffen");

                for (int i=hierarchical_step+1; i < clusters.Count; i++)
                {
                    if (clusters[i].IsVisible)
                    {
                        Point new_point = new Point (clusters[i].X, clusters[i].Y); //current point
                        int n = 0;
    
                        foreach (Cluster c_m in marked_clusters)
                        {
                            //int m = marked_clusters[j];     //current marked cluster

                            Point c_i = new Point (c_m.X, c_m.Y);
                            double distance = c_i.DistanceTo (new_point);
    
    
                            if (distance < 500)
                                n++;
                        }
    
                        if (n == marked_clusters.Count)
                        {
                            clusters[i].SetPrototypeByNumber (1);
                            marked_clusters.Add (clusters[i]);
                        }
                    }
                }

                //clusters[hierarchical_step].SetPosition (c.X, c.Y);
                hierarchical_step++;
            }
            else
            {

                //int c = marked_clusters[0];
                Point sum = new Point (0,0);

                foreach (Cluster m in marked_clusters)
                {
                    sum.Add (m.X, m.Y);
                    Hyena.Log.Debug ("Marked cluster "+m.Name);
                }

                foreach (Cluster m in marked_clusters)
                {
                    if (marked_clusters.IndexOf(m) != 0)
                    {
                        Hyena.Log.Debug ("Removed marked cluster "+m);
                        m.Hide ();
                        clusters.Remove (m);
                    }
                }

                sum.Normalize (marked_clusters.Count);

                if (marked_clusters.Count > 1)
                {
                    //Random r = new Random ();

                    marked_clusters[0].Prototype = GetIndicatorPrototype (marked_clusters.Count/3);
                    //marked_clusters[0].Prototype = GetIndicatorPrototype (r.Next(90));
                }
                else
                {
                    marked_clusters[0].SetPrototypeByNumber (3);

                }

                marked_clusters[0].SetPosition (sum.X, sum.Y);

            }


            compute = !compute;
            if(clusters.Count == 0)
                return true;    //finished
            else
                return false;   //not yet finished

        }

        static public bool HierarchicalNewCalculateStep (Group actor)
        {
            if (clusters.Count < 4)
                return true;

            for (int i=0; i < clusters.Count; i++)
            {
                double min_distance = double.MaxValue;
                Cluster cluster_current = clusters[i];

                Cluster cluster_found = clusters[i];

                Point point_found = new Point (cluster_current.X,cluster_current.Y);
                Point current = new Point (cluster_current.X,cluster_current.Y);

                for (int j=i+1; j < clusters.Count; j++)        //nähesten suchen
                {
                    Point new_point = new Point (clusters[j].X, clusters[j].Y);
                    double dist = current.DistanceTo (new_point);

                    if (dist < min_distance)
                    {
                        min_distance = dist;
                        point_found = new_point;
                        cluster_found = clusters[j];
                    }
                }

                if (min_distance != double.MaxValue)
                {
                    current.Add (point_found);
                    current.Normalize (2);
                    clusters.Remove (cluster_found);
                    cluster_current.SetPosition (current.X,current.Y);
                    cluster_found.Hide ();
                }
            }

            return false;
        }

        #endregion

        private CairoTexture prototype;

        public Cluster (uint x, uint y):base ()
        {
            this.SetPosition (x,y);
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

        public void SetPrototypeByNumber (int num)
        {
            Debug.Assert (num < prototype_list.Count);

            Prototype = prototype_list[num];
        }


    }
}

