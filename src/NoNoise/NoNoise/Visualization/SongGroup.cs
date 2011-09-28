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

using NoNoise.Visualization.Util;
using NoNoise.Data;

namespace NoNoise.Visualization
{
    public class SongHighlightArgs
    {
        public SongHighlightArgs (List<int> song_ids)
        {
            SongIDs = song_ids;
        }

        public SongHighlightArgs (int id)
        {
            SongIDs = new List<int> ();
            SongIDs.Add (id);
        }

        public List<int> SongIDs {
            private set;
            get;
        }

        public int ID {
            get { return SongIDs[0]; }
        }
    }

    public delegate void SongHighlightEvent (Object source, SongHighlightArgs args);

    public class SongGroup: Clutter.Group
    {

        #region Member variables
        SongHighlightEvent song_enter;
        SongHighlightEvent song_leave;

        private readonly double zoom_level_mult = Math.Sqrt (2);
        private double zoom_level = 1.0;
        private bool zoom_initialized = false;
        private double cluster_w, cluster_h;

        private const int num_of_actors = 3000;

//        private List<Rectangle> debug_quad_rectangles;

        private Alpha animation_alpha;
        private BehaviourScale animation_behave;
        private Timeline animation_timeline;
        private uint animation_time = 1000;

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
        private uint clutter_animation_time = 3000;

        private BehaviourScale zoom_animation_behave;
        private int diff_zoom_clustering = 0;

        private SelectionActor selection;

        private bool selection_enabled = false;
        private List<SongPoint> points_selected = new List<SongPoint> ();

        private bool selection_toggle = false;

        private bool mouse_button_locked = false;
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

        public void LoadPcaData (List<DataEntry> entries)
        {

            point_manager = new SongPointManager (0, 0, 30000, 30000);

            foreach (DataEntry e in entries)
                point_manager.Add (e.X*30000, e.Y*30000, e.ID);

            point_manager.Cluster ();
//            point_manager.SetDefaultLevel (500);

            points_visible = new List<SongPoint> (num_of_actors);

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
                        points.Add (new Point(y, x));

                }

            }

            double max_x = points.Max (p => p.X);
            double max_y = points.Max (p => p.Y);

            point_manager = new SongPointManager (0, 0, max_x, max_y);

            for (int i=0; i < points.Count; i ++)
                point_manager.Add (points[i].X, points[i].Y, i);



            Stopwatch stop = new Stopwatch();
            stop.Start ();
            point_manager.Cluster ();
            stop.Stop ();


//            point_manager.SetDefaultLevel (500);
//            point_manager.GetWindowDimensions (0, 500, out cluster_w, out cluster_h);
//            cluster_w *= Math.Pow (zoom_level_mult, point_manager.Level);
//            cluster_h *= Math.Pow (zoom_level_mult, point_manager.Level);

//            SetZoomLevel (0.5);

            Hyena.Log.Information ("Clustering time: "+stop.ElapsedMilliseconds);

            points_visible = new List<SongPoint> (num_of_actors);

        }

        private void InitSongActors ()
        {

            actor_manager = new SongActorManager (num_of_actors);

            foreach (SongActor a in actor_manager.Actors) {
                Add (a);
                animation_behave.Apply (a);
            }

        }

        private void InitSelectionActor ()
        {
            selection = new SelectionActor (1000,1000, new Cairo.Color (1,0,0,0.8));
            selection.SetPosition (0,0);
            stage.Add (selection);

        }

        private void InitAnimations ()
        {

            animation_timeline = new Timeline (animation_time);
            animation_alpha = new Alpha (animation_timeline, (ulong)AnimationMode.EaseInOutSine);
            animation_behave = new BehaviourScale (animation_alpha,1.0,1.0,1.0,1.0);

            clustering_animation_timeline = new Timeline (clutter_animation_time);
            clustering_animation_alpha = new Alpha (clustering_animation_timeline, (ulong)AnimationMode.EaseInOutSine);
            clustering_animation_behave = new BehaviourOpacity (clustering_animation_alpha, 255, 0);
            clustering_animation_timeline.Completed += HandleClusteringTimelineCompleted;

            zoom_animation_behave = new BehaviourScale (animation_alpha, 1.0,1.0,1.0,1.0);

            zoom_animation_behave.Apply (this);
        }

        private void InitHandlers ()
        {
            stage.ButtonPressEvent += HandleStageButtonPressEvent;
            stage.ButtonReleaseEvent += HandleStageButtonReleaseEvent;
            stage.MotionEvent += HandleMotionEvent;
            stage.AllocationChanged += HandleWindowSizeChanged;

            int count = 0;
            foreach (SongActor a in actor_manager.Actors) {
                a.EnterEvent += delegate(object o, EnterEventArgs args) {
                    SongActor sender = o as SongActor;
                    if (sender.Owner != null)
                        FireSongEnter (new SongHighlightArgs (sender.Owner.GetAllIDs ()));
                    else
                        Hyena.Log.Information ("No owner ");
                };


                a.LeaveEvent += delegate(object o, LeaveEventArgs args) {
                    SongActor sender = o as SongActor;
                    if (sender.Owner != null)
                        FireSongLeave (new SongHighlightArgs (sender.Owner.GetAllIDs ()));
                };
                count ++;
            }

            Hyena.Log.Information ("Event count = " + count);
        }


        /// <summary>
        /// Initializes the prototype texture, the animations, and the event handler.
        /// </summary>
        public void Init ()
        {
            Hyena.Log.Information ("Initializing Song Group.");

            Reactive = true;

            InitAnimations ();
            InitSongActors ();
            InitSelectionActor ();
            InitHandlers ();
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
//            Hyena.Log.Information ("Zoom");
//            ClearSelection ();
            mouse_button_locked = true;
            if (inwards)
            {
                AnimateClustering (false);
            }
            else
            {
                AnimateClustering (true);
            }
        }

        public void ToggleSelection ()
        {
            mouse_button_locked = true;
            selection_toggle = !selection_toggle;
        }

        public void RemoveSelected ()
        {
            mouse_button_locked = true;
            point_manager.RemoveSelection ();
            point_manager.ClearSelection ();
            UpdateView ();
        }

        public void ResetRemovedPoints ()
        {
            mouse_button_locked = true;
            point_manager.ShowRemoved ();
            point_manager.ClearSelection ();
            UpdateView ();
        }

        public List<int> GetSelectedSongIDs ()
        {
            List<SongPoint> selected = point_manager.GetSelected ();

//            if (selected.Count == 0)
//                return null;

            List<int> ret = new List<int> (selected.Count);

            foreach (SongPoint p in selected)
                ret.Add (p.ID);

            return ret;
        }

        public void UpdateHiddenSongs (List<int> not_hidden)
        {
            point_manager.MarkHidded (not_hidden);
            UpdateView ();
        }

        /// <summary>
        /// This function is used to zoom in or out.
        /// </summary>
        /// <param name="inwards">
        /// A <see cref="System.Boolean"/> specifies the zooming direction.
        /// </param>
        private void ZoomOnCenter (bool inwards)
        {
            selection.Reset ();
            ZoomOnPosition (inwards, stage.Width/2.0f, stage.Height/2.0f);

        }

        private void SetZoomLevel (double scale)
        {
            zoom_level = scale;
            SetScale (zoom_level, zoom_level);

            foreach (SongActor s in actor_manager.Actors)
                s.SetScale (1/zoom_level, 1/zoom_level);
        }

        private void ZoomOnPosition (bool inwards, float x, float y)
        {
            //Transformed position
            float trans_x = 0, trans_y = 0;
            this.TransformStagePoint (x, y, out trans_x, out trans_y);

            double scale_x, scale_y;
            this.GetScale (out scale_x, out scale_y);
//            //raus zoomen
            this.SetScale (1.0, 1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            this.TransformStagePoint (x, y, out trans_x_unif, out trans_y_unif);

            double pos_x = (double)this.X + ((double)trans_x_unif - (double)trans_x);
            double pos_y = (double)this.Y + ((double)trans_y_unif - (double)trans_y);


            //punkt auf objekt schieben
            this.SetPosition ((float)pos_x, (float)pos_y);

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

            if (animation_timeline.IsPlaying)
                old_zoom_level = scale_x;

            animation_timeline.Stop();
            animation_timeline.Rewind ();

            this.SetScaleFull (old_zoom_level, old_zoom_level, trans_x, trans_y);


            zoom_animation_behave.SetBounds (old_zoom_level, old_zoom_level, zoom_level, zoom_level);
            animation_behave.SetBounds (1.0 / old_zoom_level, 1.0 /
                                        old_zoom_level, 1.0 / zoom_level, 1.0 / zoom_level);

            //update clipping every new frame
            animation_timeline.NewFrame += delegate {
                UpdateClipping ();
            };

            animation_timeline.Start();
        }


        private void AnimateClustering (bool forward)
        {
            TimelineDirection dir = clustering_animation_timeline.Direction;

            bool playing = clustering_animation_timeline.IsPlaying;

            //forward -> forward
            if (playing && dir == TimelineDirection.Forward && forward) {
                HandleClusteringTimelineCompleted (this, new EventArgs ());
                clustering_animation_timeline.Stop ();
            }


            //forward -> backward
//            if (playing && dir == TimelineDirection.Forward && !forward)
//                ZoomOnCenter (true);

            //backward -> forward
//            if (playing && dir == TimelineDirection.Backward && forward)
//                ZoomOnCenter (false);
            if (forward)
                ZoomOnCenter (false);

            if (!forward)
                ZoomOnCenter (true);

            // nach vorne aber kein clustering mehr
            if (forward && point_manager.IsMaxLevel) {
                diff_zoom_clustering ++;
                Hyena.Log.Information ("Diff ++" + diff_zoom_clustering);
                return;
            }

            // nach vorne aber kein clustering mehr
            if (!forward && point_manager.IsMinLevel) {
                diff_zoom_clustering --;
                Hyena.Log.Information ("Diff --" + diff_zoom_clustering);
                return;
            }

            if (forward && diff_zoom_clustering != 0) {
                diff_zoom_clustering ++;
                Hyena.Log.Information ("Diff --" + diff_zoom_clustering);
                return;
            }

            if (!forward && diff_zoom_clustering != 0) {
                diff_zoom_clustering --;
                Hyena.Log.Information ("Diff ++" + diff_zoom_clustering);
                return;
            }

//            Hyena.Log.Information ("Diff " + diff_zoom_clustering);

            //back -> back or back
            if (!forward && (!playing || dir != TimelineDirection.Forward)) {
                point_manager.DecreaseLevel ();
                UpdateView ();
            }

            clustering_animation_timeline.Direction =
                            forward ? TimelineDirection.Forward : TimelineDirection.Backward;

            //rewind if direction stays the same or not playing
            if (!playing || dir ==  clustering_animation_timeline.Direction) {
                clustering_animation_timeline.Rewind ();
                clustering_animation_behave.RemoveAll ();
                Hyena.Log.Information ("Rewind");
            }

            clustering_animation_timeline.Start ();

            UpdateClipping ();

            Hyena.Log.Information ("Animations started: "
                     + (clustering_animation_timeline.Direction == TimelineDirection.Forward ? "forward" : "backward"));
        }

        private void AddClusteringAnimation (SongPoint p)
        {
            if (p.Parent == null) {
                Hyena.Log.Information ("No parent" + p.ID);
                return;
            }

//            if (p.Parent.RightChild == null)
//                return;

            if (p.Parent.MainChild.Equals (p)) {
//                Hyena.Log.Information ("Not mainchild" + p.ID);
                return;
            }


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
//            Hyena.Log.Information ("Animations finished: "
//                     + (clustering_animation_timeline.Direction == TimelineDirection.Forward ? "forward" : "backward"));

            if (clustering_animation_timeline.Direction == TimelineDirection.Forward)
                point_manager.IncreaseLevel ();

            UpdateView ();
        }


        private void UpdateView ()
        {
//            Hyena.Log.Information ("Update view");
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

//            Hyena.Log.Information (System.String.Format("Clipping ({0},{1}) with {2}x{3}",tx,ty,sx,sy));

            x = (-(float)SongActor.CircleSize-tx)/sx;
            y = (-(float)SongActor.CircleSize-ty)/sy;
            width = (stage.Width+2*(float)SongActor.CircleSize)/sx;
            height = (stage.Height+2*(float)SongActor.CircleSize)/sy;
        }

        public void UpdateClipping ()
        {
//            Hyena.Log.Information ("Update Clipping");
            double x, y, width, height;
            GetClippingWindow (out x, out y, out width, out height );


            List<SongPoint> points;
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
                if (!actor_manager.HasFree) {
                    Hyena.Log.Warning ("No free Actor left");
                    break;
                }

                p = points[i];

                if (p.Actor != null)
                    continue;

                if (p.IsRemoved)
                    continue;

                p.Actor = actor_manager.AllocateAtPosition (p);

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
            if (!mouse_down && !selection_enabled)
                //wenn nicht geklickt
                return;

            float x, y;

//            args.Event.X
            EventHelper.GetCoords (args.Event, out x, out y);

            float newx = this.X + x - mouse_old_x;
            float newy = this.Y + y - mouse_old_y;

            if (mouse_down) {

                this.SetPosition (newx, newy);
                UpdateClipping ();

            } else if (selection_enabled){


                selection.LineTo (x, y);

            }

            mouse_old_x = x;
            mouse_old_y = y;
        }

        private void ClearSelection ()
        {
//            foreach (SongPoint p in points_selected)
//            {
//                if (p.Actor != null)
//                    p.Actor.SetPrototypeByColor (SongActor.Color.White);
//                p.Selected = false;
//            }

            Hyena.Log.Information ("Clear selection");
            point_manager.ClearSelection ();

            UpdateView ();

            points_selected = new List<SongPoint> ();

            selection.Reset ();

        }

        private void UpdateSelection ()
        {
            points_selected = selection.GetPointsInside (points_visible);
            for (int i = 0; i < points_selected.Count; i ++)
            {
//                points_selected[i].Selected = true;
                points_selected[i].MarkAsSelected ();
                points_selected[i].Actor.SetPrototypeByColor (SongActor.Color.Red);
            }

            UpdateClipping ();
        }

        private void InitializeZoomLevel ()
        {
            int i = 0;

            point_manager.GetWindowDimensions (0, 1500, out cluster_w, out cluster_h);
            Hyena.Log.Information ("Window dimensions " + cluster_w + "x" + cluster_h);
            // as long as window size is too small zoom out
            while (cluster_w < point_manager.Width) {
                cluster_w *= zoom_level_mult;
                i++;

            }

            point_manager.Level = i;
            diff_zoom_clustering = i - point_manager.Level;

            Hyena.Log.Information ("Zoom initialized with scale="+
                                   stage.Width / point_manager.Width + " level="+ point_manager.Level +
                                   " diff=" + diff_zoom_clustering);



            this.SetZoomLevel (stage.Width / point_manager.Width);
            this.SetPosition (0, stage.Height / 2f - (float)point_manager.Height*(float)zoom_level/2f);

        }

        private void HandleWindowSizeChanged (object o, AllocationChangedArgs args)
        {
            if (stage.Width == 1)
                return;

            if (!zoom_initialized) {
                zoom_initialized = true;

                InitializeZoomLevel ();

            }
            selection.SetSize (stage.Width, stage.Height);
            UpdateClipping ();
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
            if (mouse_button_locked) {
                mouse_button_locked = false;
                return;
            }

            uint button = EventHelper.GetButton (args.Event);

            if (selection_enabled) {
                selection.Stop ();
                UpdateSelection ();
            }

            selection_enabled = false;

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
            if (mouse_button_locked)
                return;

            uint button = EventHelper.GetButton (args.Event);

            EventHelper.GetCoords (args.Event, out mouse_old_x, out mouse_old_y);
            selection.Reset ();

            if (button != 1)
                return;

            if (selection_toggle) {

                ClearSelection ();
                selection_enabled = true;
                double scale;
                GetScale (out scale, out scale);
                float tx, ty;
                GetTransformedPosition (out tx, out ty);

                selection.Start (mouse_old_x, mouse_old_y, scale, -tx, -ty);
            }
            else {
                mouse_down = true;
            }

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

