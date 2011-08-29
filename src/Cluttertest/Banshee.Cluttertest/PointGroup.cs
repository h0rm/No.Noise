// 
// PointGroup.cs
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
using Clutter;
using System.Diagnostics;
using System.IO;

namespace Banshee.Cluttertest
{
    public class SongHighlightArgs
    {
        private float x;
        private float y;
        private string name;
        private int id;

        public SongHighlightArgs (float x, float y, string name, int id)
        {
            this.x = x;
            this.y = y;
            this.name = name;
            this.id = id;
        }

        public SongHighlightArgs (float x, float y, int id) : this (x, y, id + "", id) {}

        public float X {
             get { return x; }
        }

        public float Y {
             get { return y; }
        }

        public string Name {
             get { return name; }
        }

        public int ID {
             get { return id; }
        }
    }

    public delegate void SongHighlightEvent (Object source, SongHighlightArgs args);

    public class PointGroup: Clutter.Group
    {

        #region Member variables
        SongHighlightEvent song_enter;
        SongHighlightEvent song_leave;

        private const int scroll_amount = 30;
        private const double zoom_level_mult = 2.0;
        private const int circle_size = 70;

        private double zoom_level = 1.0;

        private List<Cluster> cluster_list;


        private Alpha animation_alpha;
        private Behaviour animation_behave;
        private Timeline animation_timeline;

        private bool mouse_down = false;
        private float mouse_old_x;
        private float mouse_old_y;

        private Shader shader;
       // private CairoTexture test_circle;
        #endregion

        #region Getter + Setter
        public event SongHighlightEvent SongEntered {
            add { song_enter += value; }
            remove { song_enter -= value; }
        }

        public event SongHighlightEvent SongLeft {
            add { song_leave += value; }
            remove { song_leave -= value; }
        }
        #endregion

        public PointGroup () : base ()
        {
            Init();
        }



        public void CompileShader ()
        {

            string shader_sources = @"
                uniform sampler2D tex;
                void main ()
                {
                    vec4 color = gl_Color * texture2D(tex, gl_TexCoord[0].xy);
                    //color.a = max(color.a,0.3);
                    gl_FragColor = color;
                }
                ";

            shader = new Shader ();
            shader.FragmentSource = shader_sources;
            shader.Compile ();
        }

        public void ParseTextFile (string filename)
        {
            cluster_list = new List<Cluster> ();

            using (StreamReader sr = new StreamReader(filename))
            {
                string line;


                for(int i=0; i < 2000; i++)
                {
                    if((line = sr.ReadLine()) == null)
                        break;

                    char[] delimiters = new char[] { '\t' };
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    AddCircle (Math.Abs(float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture))*20.0f,
                               Math.Abs(float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture))*20.0f,parts[0]);


                }

            }

            Cluster.KMeansInit (4, Width, Height);
            Cluster.HierarchicalInit (cluster_list);
        }
        /// <summary>
        /// A test function which generates randomly distributed circles.
        /// </summary>
        /// <param name="width">
        /// A <see cref="System.Double"/> which specifies the width of the distribution.
        /// </param>
        /// <param name="height">
        /// A <see cref="System.Double"/> which specifies the height of the distribution.
        /// </param>
        /// <param name="count">
        /// A <see cref="System.Int32"/> which specifies the number of circles.
        /// </param>
        public void TestGenerateCircles (double width, double height, int count)
        {
            cluster_list = new List<Cluster> ();

            Random r = new Random ();

            for (int i=0; i<count; i++)
                AddCircle ((float)(r.NextDouble ()*width), (float)(r.NextDouble ()*height),"");

            Cluster.KMeansInit (4, Width, Height);
            Cluster.HierarchicalInit (cluster_list);
            //CalculateClusters (4);
        }

        /// <summary>
        /// Adds a circle at the specified position.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Single"/> - x coordinate.
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Single"/> - y coordinate.
        /// </param>
        public void AddCircle (float x, float y, string name)
        {
            //Clone clone = new Clone (circle_prototype);
            Cluster clone = new Cluster (circle_size,circle_size);

            //Random r = new Random();

            //clone.CoglTexture = prototype_list[r.Next(4)].CoglTexture;
            //clone.CoglMaterial = prototype_list[r.Next(4)].CoglMaterial;
            //clone.SetPrototypeByNumber (r.Next(4));
            clone.SetPrototypeByNumber (3);

            clone.AnchorPointFromGravity = Gravity.Center;
            clone.SetPosition (x, y);
            clone.SetScaleWithGravity (1.0,1.0, Gravity.NorthWest);

            //reactive damit signals geschickt werden
            clone.Reactive = true;

            if (name == "")
                clone.Name = "Clone "+cluster_list.Count;        //Name
            else
                clone.Name = name;
            clone.EnterEvent += delegate {
                FireSongEnter (new SongHighlightArgs (x, y, clone.Name, cluster_list.Count));
            };

            clone.EnterEvent += delegate {
                FireSongLeave (new SongHighlightArgs (x, y, clone.Name, cluster_list.Count));
            };
//
//            clone.EnterEvent += delegate {
//                clone.SetScale(1/zoom_level * 1.5, 1/zoom_level * 1.5);
//            };
//
//            clone.LeaveEvent += delegate {
//                clone.SetScale(1/zoom_level, 1/zoom_level);
//            };

            //Hyena.Log.Debug ("Added "+clone.Name + " " + clone.X + ":" + clone.Y);
            cluster_list.Add (clone);

            this.Add (clone);
            animation_behave.Apply (clone);
        }

        /// <summary>
        /// Initializes the prototype texture, the animations, and the event handler.
        /// </summary>
        public void Init ()
        {
            Hyena.Log.Information ("Circles Start - Clusters");

            Cluster.Init ();

            //CompileShader ();
           // CalculateClusters (4);

            //Animation
            animation_timeline = new Timeline (1000);
            animation_alpha = new Alpha (animation_timeline, (ulong)AnimationMode.EaseOutCubic);
            animation_behave = new BehaviourScale (animation_alpha,1.0,1.0,1.2,1.2);

            //circles klonen
            this.Reactive = true;
            this.ScrollEvent += HandleAdaptiveZoom;
            this.ButtonPressEvent += HandleStageButtonPressEvent;
            this.ButtonReleaseEvent += HandleStageButtonReleaseEvent;
            this.MotionEvent += HandleMotionEvent;
            //this.LeaveEvent += HandleHandleLeaveEvent;
            //this.Stage.ButtonPressEvent += HandleGlobalButtonPressEven;
        }

        void HandleHandleLeaveEvent (object o, LeaveEventArgs args)
        {
            mouse_down = false;
        }

        #region private Handler

        /// <summary>
        /// Handles the entering of the mouse cursor at a circle position.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="EnterEventArgs"/>
        /// </param>
        private void HandleCircleMouseEnter (object o, EnterEventArgs args)
        {

            //Maus Koordinaten holen
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //Ausgabe von name + x:y (x,y) irgendwie falsch
            //Hyena.Log.Information("Mouse Enter Clone "+(o as Clone).Name + " - " + mouse_x +":"+mouse_y);

            //Punkt transformieren - damit an richtiger position mit scale und so
            float x=0, y=0;
            (o as Clone).GetTransformedPosition(out x,out y);

            //TODO ersetzen mit handler
            //An position von circle infobox setzen + namen
//            info_group.SetPosition(x,y);
//            info_text.Value = (o as Clone).Name;
//
//            //infobox anzeigen
//            info_group.Show();
        }

        /// <summary>
        /// This function is used cluster or decluster the data. Every time the function is
        /// called one clustering step is perfomed.
        /// </summary>
        /// <param name="inwards">
        /// A <see cref="System.Boolean"/> specifies if clustering (true) or declustering (false)
        /// should be perfomed.
        /// </param>
        public void ClusterOneStep (bool inwards)
        {
            if (inwards)
                Cluster.HierarchicalNewCalculateStep (this);
        }


        /// <summary>
        /// This function is used to zoom in or out.
        /// </summary>
        /// <param name="inwards">
        /// A <see cref="System.Boolean"/> specifies the zooming direction.
        /// </param>
        public void ZoomOnCenter (bool inwards)
        {
            ZoomOnPosition (inwards, Stage.Width/2.0f, Stage.Height/2.0f);
        }

        public void ZoomOnPosition (bool inwards, float x, float y)
        {
            //Transformed position
            float trans_x = 0, trans_y = 0;
            this.TransformStagePoint(x,y,out trans_x, out trans_y);

            //raus zoomen
            this.SetScale(1.0,1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            this.TransformStagePoint(x,y,out trans_x_unif, out trans_y_unif);

            float pos_x = this.X + (trans_x_unif - trans_x);
            float pos_y = this.Y + (trans_y_unif - trans_y);

            //punkt auf objekt schieben
            this.SetPosition(pos_x, pos_y);

            //circle_group.SetScale(zoom_level,zoom_level);


            double old_zoom_level = zoom_level;

            //rein zoomen
            switch (inwards)
            {
            case true:
                zoom_level *= zoom_level_mult;
                break;

            case false:
                zoom_level /= zoom_level_mult;
                break;
            }

            uint duration = 1000;

            if (animation_timeline.Progress < 0.2 && animation_timeline.IsPlaying) { //case zu langsam - keine animationen

                this.Animation.Timeline.Stop();

                this.SetScaleFull (zoom_level, zoom_level, trans_x, trans_y);

                animation_behave.RemoveAll();

                //zoom actor in andere richtung - warum center 0,0 geht weiß ich nicht ..
                foreach (Actor a in this)
                    a.SetScale (1.0/zoom_level,1.0/zoom_level);

            } else {

                this.SetScaleFull (old_zoom_level, old_zoom_level, trans_x, trans_y);

                this.Animatev ((ulong)AnimationMode.EaseOutCubic,duration, new String[]{"scale-x"}, new GLib.Value (zoom_level));
                this.Animatev ((ulong)AnimationMode.EaseOutCubic,duration, new String[]{"scale-y"}, new GLib.Value (zoom_level));

                animation_behave.RemoveAll();

                animation_timeline.Duration = duration;
                animation_behave = new BehaviourScale (animation_alpha, 1.0f/old_zoom_level, 1.0f/old_zoom_level, 1.0f/zoom_level, 1.0f/zoom_level);

                //neues behaviour an die circles andwenden
                foreach (Actor a in cluster_list)
                    animation_behave.Apply(a);
            }

            animation_timeline.Stop();
            animation_timeline.Start();
        }
        /// <summary>
        /// Handle the zooming with the animation. This is used when for
        /// zooming with the scroll wheel.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ScrollEventArgs"/>
        /// </param>
        private void HandleAdaptiveZoom (object o, ScrollEventArgs args)
        {

           Hyena.Log.Information("Adaptive Zoom");

            //Mouse position
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

//            //Transformed position
//            float trans_x = 0, trans_y = 0;
//            this.TransformStagePoint(mouse_x,mouse_y,out trans_x, out trans_y);
//
//            //raus zoomen
//            this.SetScale(1.0,1.0);
//
//            float trans_x_unif = 0, trans_y_unif = 0;
//            this.TransformStagePoint(mouse_x,mouse_y,out trans_x_unif, out trans_y_unif);
//
//            float pos_x = this.X + (trans_x_unif - trans_x);
//            float pos_y = this.Y + (trans_y_unif - trans_y);
//
//            //punkt auf objekt schieben
//            this.SetPosition(pos_x, pos_y);
//
//            //circle_group.SetScale(zoom_level,zoom_level);
//
//
//            double old_zoom_level = zoom_level;

            //rein zoomen
            switch (args.Event.Direction)
            {
            case ScrollDirection.Up:
                //zoom_level *= zoom_level_mult;
                ZoomOnPosition (true, mouse_x, mouse_y);
                break;

            case ScrollDirection.Down:
                //zoom_level /= zoom_level_mult;
                ZoomOnPosition (false, mouse_x, mouse_y);
                break;
            }

//            uint duration = 1000;
//
//            if (animation_timeline.Progress < 0.2 && animation_timeline.IsPlaying) { //case zu langsam - keine animationen
//
//                this.Animation.Timeline.Stop();
//
//                this.SetScaleFull (zoom_level, zoom_level, trans_x, trans_y);
//
//                animation_behave.RemoveAll();
//
//                //zoom actor in andere richtung - warum center 0,0 geht weiß ich nicht ..
//                foreach (Actor a in this)
//                    a.SetScale (1.0/zoom_level,1.0/zoom_level);
//
//            } else {
//
//                this.SetScaleFull (old_zoom_level, old_zoom_level, trans_x, trans_y);
//
//                this.Animatev ((ulong)AnimationMode.EaseOutCubic,duration, new String[]{"scale-x"}, new GLib.Value (zoom_level));
//                this.Animatev ((ulong)AnimationMode.EaseOutCubic,duration, new String[]{"scale-y"}, new GLib.Value (zoom_level));
//
//                animation_behave.RemoveAll();
//
//                animation_timeline.Duration = duration;
//                animation_behave = new BehaviourScale (animation_alpha, 1.0f/old_zoom_level, 1.0f/old_zoom_level, 1.0f/zoom_level, 1.0f/zoom_level);
//
//                //neues behaviour an die circles andwenden
//                foreach (Actor a in cluster_list)
//                    animation_behave.Apply(a);
//            }
//
//            animation_timeline.Stop();
//            animation_timeline.Start();
        }

        /// <summary>
        /// Handle the mouse motion for displacing the actor.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="MotionEventArgs"/>
        /// </param>
        private void HandleMotionEvent (object o, MotionEventArgs args)
        {
            if (!mouse_down)        //wenn nicht geklickt
                return;

            float x, y;
            EventHelper.GetCoords (args.Event, out x, out y);

            this.SetPosition (this.X + x - mouse_old_x, this.Y + y - mouse_old_y);
            mouse_old_x = x;
            mouse_old_y = y;
        }

        /// <summary>
        /// Handles the button release event for displacing the actor.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ButtonReleaseEventArgs"/>
        /// </param>
        private void HandleStageButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
        {
            //Hyena.Log.Information ("Mouse Up.");
            mouse_down = false;
        }

        /// <summary>
        /// Handles the button press event for displacing the actor.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ButtonPressEventArgs"/>
        /// </param>
        private void HandleStageButtonPressEvent (object o, ButtonPressEventArgs args)
        {

            uint button = EventHelper.GetButton (args.Event);

            if(button != 1)
            {
                Hyena.Log.Debug ("Rechtsklick.");
                //UpdateCirclePrototypeColor();

                //RefineClusters ();
                //Cluster.KMeansRefineStep (cluster_list);
                //Cluster.HierarchicalCalculateStep (this);
                Cluster.HierarchicalNewCalculateStep (this);
                return;
            }
            EventHelper.GetCoords (args.Event, out mouse_old_x, out mouse_old_y);
            mouse_down = true;
        }

        private void FireSongEnter (SongHighlightArgs args)
        {
            if (song_enter != null)
                song_enter (this, args);
        }

        private void FireSongLeave (SongHighlightArgs args)
        {
            if (song_leave != null)
                song_leave (this, args);
        }


        #endregion
    }
}

