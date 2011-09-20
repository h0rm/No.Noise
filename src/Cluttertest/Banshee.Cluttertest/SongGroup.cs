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
using System.Linq;

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

    public class SongGroup: Clutter.Group
    {

        #region Member variables
        SongHighlightEvent song_enter;
        SongHighlightEvent song_leave;

        private const double zoom_level_mult = 2.0;
        private double zoom_level = 1.0;

        private const int num_of_actors = 2000;

//        private List<Rectangle> debug_quad_rectangles;

        private Alpha animation_alpha;
        private Behaviour animation_behave;
        private Timeline animation_timeline;

        private bool mouse_down = false;
        private float mouse_old_x;
        private float mouse_old_y;

        private Stage stage;
        private List<SongPoint> points_visible;
        private SongPointManager point_manager;
        private SongActorManager actor_manager;

        private Alpha clustering_animation_alpha;
        private Behaviour clustering_animation_behave;
        private Timeline clustering_animation_timeline;

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

        public SongGroup (Stage stage) : base ()
        {
            this.stage = stage;
            Init();
        }

        public void ParseTextFile (string filename, int count)
        {
            List<Point> points = new List<Point> ();


            using (StreamReader sr = new StreamReader (filename))
            {
                string line;


                for (int i = 0; i < count; i++)
                {
                    if ((line = sr.ReadLine ()) == null)
                        break;

                    char[] delimiters = new char[] { '\t' };
                    string[] parts = line.Split (delimiters, StringSplitOptions.RemoveEmptyEntries);

                    double x = Math.Abs (float.Parse (parts[1], System.Globalization.CultureInfo.InvariantCulture)) * 20.0f;
                    double y = Math.Abs (float.Parse (parts[2], System.Globalization.CultureInfo.InvariantCulture)) * 20.0f;

                    if (x != 0 && y != 0)
                        points.Add (new Point(x, y));

                }

            }

            double max_x = points.Max (p => p.X);
            double max_y = points.Max (p => p.Y);

            point_manager = new SongPointManager (0, 0, max_x, max_y);

            foreach (Point p in points)
                point_manager.Add (p.X, p.Y, "test");

            Stopwatch stop = new Stopwatch();
            stop.Start ();
            point_manager.Cluster ();
            stop.Stop ();

            Hyena.Log.Information ("Clustering time: "+stop.ElapsedMilliseconds);

            points_visible = new List<SongPoint> (2000);

            foreach (SongActor a in actor_manager.Actors) {
                this.Add (a);
                animation_behave.Apply (a);
            }
        }


        /// <summary>
        /// Initializes the prototype texture, the animations, and the event handler.
        /// </summary>
        public void Init ()
        {
            Hyena.Log.Information ("Initializing Song Group.");

            actor_manager = new SongActorManager (num_of_actors);


            //Animation
            animation_timeline = new Timeline (1000);
            animation_alpha = new Alpha (animation_timeline, (ulong)AnimationMode.EaseOutCubic);
            animation_behave = new BehaviourScale (animation_alpha,1.0,1.0,1.2,1.2);

            clustering_animation_timeline = new Timeline (10000);
            clustering_animation_alpha = new Alpha (clustering_animation_timeline, (ulong)AnimationMode.EaseOutCubic);
            clustering_animation_behave = new BehaviourOpacity (clustering_animation_alpha, 255, 0);
            clustering_animation_timeline.Completed += HandleClusteringTimelineCompleted;

            this.Reactive = true;
            this.ScrollEvent += HandleAdaptiveZoom;

            stage.ButtonPressEvent += HandleStageButtonPressEvent;
            stage.ButtonReleaseEvent += HandleStageButtonReleaseEvent;
            stage.MotionEvent += HandleMotionEvent;
            stage.AllocationChanged += delegate {
                UpdateClipping ();
            };
            //this.LeaveEvent += HandleHandleLeaveEvent;
            //this.Stage.ButtonPressEvent += HandleGlobalButtonPressEven;
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
            {
//                ZoomOnCenter (true);
//                point_manager.Level = 0;
//                point_manager.IncreaseLevel ();
                AnimateClustering (true);
//                UpdateView ();
//                ZoomOnCenter (false);
            }
            else
            {
//                ZoomOnCenter (false);
//                point_manager.Level = 1;
//                ZoomOnCenter (true);
//                point_manager.DecreaseLevel ();
//                UpdateView ();
                AnimateClustering (false);
            }
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
            this.TransformStagePoint (x, y, out trans_x, out trans_y);

            //raus zoomen
            this.SetScale (1.0, 1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            this.TransformStagePoint (x, y, out trans_x_unif, out trans_y_unif);

            float pos_x = this.X + (trans_x_unif - trans_x);
            float pos_y = this.Y + (trans_y_unif - trans_y);

            //punkt auf objekt schieben
            this.SetPosition (pos_x, pos_y);

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

            animation_timeline.Stop();

            if (animation_timeline.Progress < 0.2 && animation_timeline.IsPlaying) {
                //case zu langsam - keine animationen

                this.Animation.Timeline.Stop ();

                this.SetScaleFull (zoom_level, zoom_level, trans_x, trans_y);

                animation_behave.RemoveAll ();

                //zoom actor in andere richtung - warum center 0,0 geht weiß ich nicht ..
                foreach (Actor a in this)
                    a.SetScale (1.0 / zoom_level, 1.0 / zoom_level);

            } else {

                this.SetScaleFull (old_zoom_level, old_zoom_level, trans_x, trans_y);

                this.Animatev ((ulong)AnimationMode.EaseOutCubic, duration, new String[] { "scale-x" }, new GLib.Value (zoom_level));
                this.Animatev ((ulong)AnimationMode.EaseOutCubic, duration, new String[] { "scale-y" }, new GLib.Value (zoom_level));

                animation_behave.RemoveAll ();

                animation_timeline.Duration = duration;
                animation_behave = new BehaviourScale (animation_alpha, 1.0f / old_zoom_level, 1.0f / old_zoom_level, 1.0f / zoom_level, 1.0f / zoom_level);

                //update clipping every new frame
                animation_timeline.NewFrame += delegate {
                    UpdateClipping ();
                };

                //neues behaviour an die circles andwenden
                foreach (Actor a in actor_manager.Actors)
                    animation_behave.Apply(a);
            }


            animation_timeline.Start();
        }

        private void AnimateClustering (bool forward)
        {
            if (clustering_animation_timeline.IsPlaying)
                return;

            clustering_animation_timeline.Direction =
                            forward ? TimelineDirection.Forward : TimelineDirection.Backward;

            clustering_animation_timeline.Rewind ();
            clustering_animation_behave.RemoveAll ();

            if (!forward) {
                clustering_animation_alpha.Mode = (ulong)AnimationMode.EaseInCubic;
                point_manager.DecreaseLevel ();
                UpdateView ();
            } else {
                clustering_animation_alpha.Mode = (ulong)AnimationMode.EaseOutCubic;
            }

            clustering_animation_timeline.Start ();

            UpdateClipping ();

            Hyena.Log.Information ("Animations started: "
                     + (clustering_animation_timeline.Direction == TimelineDirection.Forward ? "forward" : "backward"));
        }

        private void AddClusteringAnimation (SongPoint p)
        {
            if (p.Parent == null)
                return;

            if (p.Parent.RightChild == null)
                return;

            if (!p.Parent.RightChild.Equals (p))
                return;

            if (clustering_animation_behave.IsApplied (p.Actor))
                return;

            clustering_animation_behave.Apply (p.Actor);
        }

        private void RemoveClusteringAnimation (SongPoint p)
        {
            if (clustering_animation_behave.IsApplied (p.Actor))
                clustering_animation_behave.Remove (p.Actor);
        }

        private void HandleClusteringTimelineCompleted (object sender, EventArgs e)
        {
            Hyena.Log.Information ("Animations finished: "
                     + (clustering_animation_timeline.Direction == TimelineDirection.Forward ? "forward" : "backward"));

            if (clustering_animation_timeline.Direction == TimelineDirection.Forward)
                point_manager.IncreaseLevel ();

            UpdateView ();
        }


        private void UpdateView ()
        {
            Hyena.Log.Information ("Update view");
            //remove all visible points
            for (int i = 0; i < points_visible.Count; i++) {
                actor_manager.Free (points_visible[i].Actor);
                points_visible[i].Actor = null;

                points_visible[i] = points_visible[points_visible.Count-1];
                points_visible.RemoveAt (points_visible.Count-1);
                i--;
            }

            UpdateClipping ();
        }

        private void GetClippingWindow (out double x, out double y, out double width, out double height)
        {
            float tx, ty;
            double sx, sy;
            GetTransformedPosition (out tx, out ty);
            GetScale (out sx, out sy);

            x = (-(float)SongActor.CircleSize-tx)/sx;
            y = (-(float)SongActor.CircleSize-ty)/sy;
            width = (stage.Width+2*(float)SongActor.CircleSize)/sx;
            height = (stage.Height+2*(float)SongActor.CircleSize)/sy;
        }

        private void UpdateClipping ()
        {
            double x, y, width, height;
            GetClippingWindow (out x, out y, out width, out height );

            List<SongPoint> points;
            SongActor current;
            SongPoint p;

            points = point_manager.GetPointsInWindow (x, y, width, height);

            // Check visible points
            for (int i = 0; i < points_visible.Count; i++)
            {
                p = points_visible[i];

                if (p.X < x || p.X > x + width || p.Y < y || p.Y > y + height)
                {
                    actor_manager.Free (p.Actor);
                    RemoveClusteringAnimation (p);

                    p.Actor = null;

                    points_visible[i] = points_visible[points_visible.Count-1];
                    points_visible.RemoveAt (points_visible.Count-1);
                    i--;


                } else {
                    AddClusteringAnimation (p);
                }
            }

            // Check invisible points
            for (int i = 0; i < points.Count; i++)
            {
                if (!actor_manager.HasFree)
                    continue;

                p = points[i];

                if (p.Actor != null)
                    continue;

                current = actor_manager.AllocateAtPosition (p.X, p.Y);
                p.Actor = current;
                points_visible.Add (p);

                AddClusteringAnimation (p);
            }

        }

         #region private Handler

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

           //Hyena.Log.Information("Adaptive Zoom");

            //Mouse position
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //rein zoomen
            switch (args.Event.Direction)
            {
            case ScrollDirection.Up:
                ZoomOnPosition (true, mouse_x, mouse_y);
                break;

            case ScrollDirection.Down:
                ZoomOnPosition (false, mouse_x, mouse_y);
                break;
            }
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
            if (!mouse_down)
                //wenn nicht geklickt
                return;

            float x, y;
            EventHelper.GetCoords (args.Event, out x, out y);

            float newx = this.X + x - mouse_old_x;
            float newy = this.Y + y - mouse_old_y;

            this.SetPosition (newx, newy);
            mouse_old_x = x;
            mouse_old_y = y;

            Stopwatch stop = new Stopwatch ();
            stop.Start ();

            UpdateClipping ();

            stop.Stop ();
            Hyena.Log.Information ("Time to Update: "+stop.ElapsedTicks);
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

                void HandleHandleLeaveEvent (object o, LeaveEventArgs args)
        {
            mouse_down = false;
        }

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

            if (button != 1)
            {
                Hyena.Log.Debug ("Rechtsklick.");

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

