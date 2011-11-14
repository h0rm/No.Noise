// 
// PointGroup.cs
// 
// Author:
//   Manuel Keglevic <manuel.keglevic@gmail.com>
//   Thomas Schulz <tjom@gmx.at>
//
// Copyright (c) 2011 Manuel Keglevic, Thomas Schulz
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
using System.Threading;

namespace NoNoise.Visualization
{
    /// <summary>
    /// Argument class which holds information (id) about songs.
    /// </summary>
    public class SongInfoArgs
    {
        public SongInfoArgs (List<int> song_ids)
        {
            SongIDs = song_ids;
        }

        public SongInfoArgs (int id)
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

    public delegate void SongSelectedEvent (Object source, SongInfoArgs args);
    public delegate void SongHighlightEvent (Object source, SongInfoArgs args);
    public delegate void SelectionClearedEvent (Object source);
    public delegate void SongLeaveEvent (Object source);
    public delegate void SongStartPlayingEvent (Object source, SongInfoArgs args);
    public delegate void InitializedEvent (Object source);
    public delegate void LoadPcaDataFinishedEvent (Object source);

    public class SongGroup: Clutter.Group, IDisposable
    {

        #region Member variables

        private SongHighlightEvent song_enter;
        private SongLeaveEvent song_leave;

        private SongSelectedEvent song_selected;
        private SelectionClearedEvent selection_cleared;

        private SongStartPlayingEvent song_start_playing;
        private InitializedEvent initialized_event;
        private LoadPcaDataFinishedEvent pca_load_finished_event;

        private double zoom_level_mult = Math.Sqrt (2);
        private double zoom_level = 1.0;
        private bool zoom_initialized = false;
        private double cluster_w, cluster_h;

        private const int num_of_actors = 3000;

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

        private SongPointManager new_point_manager;
        private Object new_point_manager_lock = new Object ();
        private Object point_manager_lock = new Object ();
        private object cluster_thread_lock = new object ();

        private SongActorManager actor_manager;

        private Alpha clustering_animation_alpha;
        private Behaviour clustering_animation_behave;
        private Timeline clustering_animation_timeline;
        private uint clutter_animation_time = 3000;
        private Behaviour clustering_reverse_behave;
        private ClusteringAnimation clustering_animation = SongGroup.ClusteringAnimation.None;
        private enum ClusteringAnimation {Forward, Backward, None};


        private BehaviourScale zoom_animation_behave;
        private int diff_zoom_clustering = 0;

        private SelectionActor selection;
        private bool selection_enabled = false;
        private bool selection_toggle = false;

        private bool mouse_button_locked = false;

        private Gtk.ThreadNotify pca_finished;

        private Thread clustering_thread;
        #endregion

        #region Getter + Setter
        /// <summary>
        /// Is fired when the mouse pointer enters a point.
        /// </summary>
        public event SongHighlightEvent SongEntered {
            add { song_enter += value; }
            remove { song_enter -= value; }
        }

        /// <summary>
        /// Is fired when the mouse pointer leaves a point.
        /// </summary>
        public event SongLeaveEvent SongLeft {
            add { song_leave += value; }
            remove { song_leave -= value; }
        }

        /// <summary>
        /// Is fired when a selection is completed.
        /// </summary>
        public event SongSelectedEvent SongSelected {
            add { song_selected += value; }
            remove { song_selected -= value; }
        }

        /// <summary>
        /// Is fired when the selection is cleared.
        /// </summary>
        public event SelectionClearedEvent SelectionCleared {
            add { selection_cleared += value; }
            remove { selection_cleared -= value; }
        }

        public event SongStartPlayingEvent SongStartPlaying {
            add { song_start_playing += value; }
            remove { song_start_playing -= value; }
        }

        public event InitializedEvent InitializationFinished {
            add { initialized_event += value; }
            remove { initialized_event -= value; }
        }

        public event LoadPcaDataFinishedEvent LoadPcaFinished {
            add { pca_load_finished_event += value; }
            remove { pca_load_finished_event -= value; }
        }

        #endregion

        public SongGroup (Stage stage) : base ()
        {
            this.Initialized = false;
            this.stage = stage;
            actor_manager = new SongActorManager (num_of_actors);

            InitSelectionActor ();
//            Init();
        }

        public bool Initialized {
            get;
            private set;
        }

        /// <summary>
        /// Loads the list of <see cref="DataEntry"/> elements into the visualization.
        /// </summary>
        /// <param name="entries">
        /// A <see cref="List<DataEntry>"/>
        /// </param>
        public void LoadPcaData (List<DataEntry> entries)
        {
//            bool initialized = point_manager != null;

//            if (initialized)
//                ClearView ();

            lock (cluster_thread_lock) {

                if (clustering_thread != null && clustering_thread.IsAlive) {
                        clustering_thread.Abort ();
                        clustering_thread.Join ();
                }

                lock (new_point_manager_lock) {
    
                    new_point_manager = new SongPointManager (0, 0, 30000, 30000);
    
                    foreach (DataEntry e in entries) {
                        new_point_manager.Add (e.X*30000, e.Y*30000, e.ID);
                    }
                }

                pca_finished = new Gtk.ThreadNotify (new Gtk.ReadyEvent (ClusteringFinished));


                clustering_thread = new Thread (ClusterBackground);
                clustering_thread.Start ();
            }
        }

        private void ClusteringFinished ()
        {
            Hyena.Log.Debug ("NoNoise/Vis - Clustering finished");
            
            // Change point manager
            lock (point_manager_lock) {
                point_manager = null;

                if (new_point_manager == null)
                        return;
    
                point_manager = new_point_manager;
            }

            lock (new_point_manager_lock) {
                new_point_manager = null;
            }

            ClearView ();

            points_visible = new List<SongPoint> (num_of_actors);

            InitializeZoomLevel ();
            SecureUpdateClipping ();

            // Fire event
            if (pca_load_finished_event != null)
                pca_load_finished_event (this);
        }

        private void ClusterBackground ()
        {
            try {
                lock (new_point_manager_lock) {
                    new_point_manager.Cluster ();
                }

                pca_finished.WakeupMain ();

            } catch (ThreadAbortException ex) {
                Hyena.Log.Debug ("NoNoise/Vis - Clustering aborted");

                lock (new_point_manager_lock) {
                    new_point_manager = null;
                }
            }
        }

        #region Initialzation

        /// <summary>
        /// Initializes the <see cref="SongActorManager"/>.
        /// </summary>
        private void InitSongActors ()
        {
            actor_manager.Init (num_of_actors);

            foreach (SongActor a in actor_manager.Actors) {
                Add (a);
                animation_behave.Apply (a);
            }

        }

        /// <summary>
        /// Initializes the actor which is used for selection.
        /// </summary>
        private void InitSelectionActor ()
        {
            selection = new SelectionActor (1000,1000, new Cairo.Color (1,0,0,0.9));
            selection.SetPosition (0,0);
            stage.Add (selection);
        }

        /// <summary>
        /// Initializes all animations.
        /// </summary>
        private void InitAnimations ()
        {

            animation_timeline = new Timeline (animation_time);
            animation_alpha = new Alpha (animation_timeline, (ulong)AnimationMode.EaseInOutSine);
            animation_behave = new BehaviourScale (animation_alpha,1.0,1.0,1.0,1.0);

            clustering_animation_timeline = new Timeline (clutter_animation_time);
            clustering_animation_alpha = new Alpha (clustering_animation_timeline, (ulong)AnimationMode.EaseInOutSine);
            clustering_animation_behave = new BehaviourOpacity (clustering_animation_alpha, 255, 0);
            clustering_animation_timeline.Completed += HandleClusteringTimelineCompleted;

            clustering_reverse_behave = new BehaviourOpacity (clustering_animation_alpha, 0, 255);

            zoom_animation_behave = new BehaviourScale (animation_alpha, 1.0,1.0,1.0,1.0);
            zoom_animation_behave.Apply (this);
        }

        /// <summary>
        /// Initializes all Handlers.
        /// </summary>
        private void InitHandlers ()
        {
            stage.ButtonPressEvent += HandleStageButtonPressEvent;
            stage.ButtonReleaseEvent += HandleStageButtonReleaseEvent;
            stage.MotionEvent += HandleMotionEvent;
            stage.AllocationChanged += HandleWindowSizeChanged;

            foreach (SongActor a in actor_manager.Actors) {
                a.EnterEvent += HandleSongEnterEvent;
                a.LeaveEvent += HandleSongLeaveEvent;
            }
        }

        private void DisposeHandlers ()
        {
            stage.ButtonPressEvent -= HandleStageButtonPressEvent;
            stage.ButtonReleaseEvent -= HandleStageButtonReleaseEvent;
            stage.MotionEvent -= HandleMotionEvent;
            stage.AllocationChanged -= HandleWindowSizeChanged;

            foreach (SongActor a in actor_manager.Actors) {
                a.EnterEvent -= HandleSongEnterEvent;
                a.LeaveEvent -= HandleSongLeaveEvent;
            }
        }

        void HandleSongLeaveEvent (object o, LeaveEventArgs args)
        {
            SongActor sender = o as SongActor;
            if (sender.Owner != null)
                FireSongLeave ();
        }

        void HandleSongEnterEvent (object o, EnterEventArgs args)
        {
            SongActor sender = o as SongActor;
            if (sender.Owner != null)
                FireSongEnter (new SongInfoArgs (sender.Owner.GetAllIDs ()));
            else
                Hyena.Log.Warning ("NoNoise/Vis - No owner ");
        }

        /// <summary>
        /// Initializes the zoom and clustering levels.
        /// </summary>
        private void InitializeZoomLevel ()
        {
            int i = 0;
            zoom_level = 1;
            point_manager.GetWindowDimensions (0, 1500, out cluster_w, out cluster_h);

            // as long as window size is too small zoom out
            while (cluster_w < point_manager.Width) {
                cluster_w *= zoom_level_mult;
                i++;
            }

            point_manager.Level = i;
            diff_zoom_clustering = i - point_manager.Level;

            double width = stage.Width / point_manager.Width;
//            double height = stage.Height / point_manager.Height;

            this.SetZoomLevel (width);
        }

        /// <summary>
        /// Initializes the zoom and clustering level. This is only done once.
        /// </summary>
        public void InitOnShow ()
        {
            if (!zoom_initialized) {
                zoom_initialized = true;
                InitializeZoomLevel ();
                SecureUpdateView ();
            } else {
                StopAllAnimations ();
                InitializeZoomLevel ();
                SecureUpdateView ();
            }
        }

        Rectangle background;
        /// <summary>
        /// Initializes the prototype texture, the animations, and the event handler.
        /// </summary>
        public void Init ()
        {
            Reactive = true;

            point_manager = new SongPointManager (0,0,3000,3000);
            points_visible = new List<SongPoint> (num_of_actors);

            background = new Rectangle (new Color (0,0,0,1.0));
            background.SetSize (30000,30000);
            this.Add (background);
            InitAnimations ();
            InitSongActors ();
//            InitSelectionActor ();
            InitHandlers ();

            // Initialized -> ready to go
            this.Initialized =  true;
        }
        #endregion

        #region User Interaction functions
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

        /// <summary>
        /// Enables or disables selection.
        /// </summary>
        public void ToggleSelection ()
        {
            mouse_button_locked = true;

//            if (!selection_toggle)
//                ClearSelection ();

            selection_toggle = !selection_toggle;
        }

        /// <summary>
        /// Removes all currently selected points.
        /// </summary>
        public void RemoveSelected ()
        {
            mouse_button_locked = true;

            lock (point_manager_lock) {
                point_manager.RemoveSelection ();
            }

            ClearSelection ();
            UpdateView ();
        }

        /// <summary>
        /// All removed points are shown again.
        /// </summary>
        public void ResetRemovedPoints ()
        {
            mouse_button_locked = true;
            lock (point_manager_lock) {
                point_manager.ShowRemoved ();
            }

            ClearSelection ();
            UpdateView ();
        }

        /// <summary>
        /// Clears the selection and updates the view.
        /// </summary>
        public void ClearSongSelection ()
        {
            mouse_button_locked = true;

            ClearSelection ();
            UpdateView ();
        }

        /// <summary>
        /// Returns a list of song ids which correspond to the selected points.
        /// </summary>
        /// <returns>
        /// A <see cref="List<System.Int32>"/>
        /// </returns>
        public List<int> GetSelectedSongIDs ()
        {
            return point_manager.GetSelectedIDs ();
        }

        /// <summary>
        /// Hides or unhides the points according to the given list of shown songs.
        /// </summary>
        /// <param name="not_hidden">
        /// A <see cref="List<System.Int32>"/> which specifies all songs (by id) which are not hidden.
        /// </param>
        public void UpdateHiddenSongs (List<int> not_hidden)
        {
            lock (point_manager_lock) {
                point_manager.MarkHidded (not_hidden);
            }
            UpdateShownSelection ();
            UpdateView ();
        }
        #endregion

        #region Zoom
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

        /// <summary>
        /// Sets the zoom level to the given scale.
        /// </summary>
        /// <param name="scale">
        /// A <see cref="System.Double"/>
        /// </param>
        private void SetZoomLevel (double scale)
        {
            Hyena.Log.Debug ("NoNoise/Vis - Zoom reset");
            float trans_x = Width /2.0f, trans_y = Height /2.0f;

            double scale_x, scale_y;
            this.GetScale (out scale_x, out scale_y);
            this.SetPosition (0,0);

            this.SetScale (1.0, 1.0);

            float trans_x_unif = stage.Width/2.0f, trans_y_unif = stage.Height/2.0f;

            double pos_x = (double)this.X + ((double)trans_x_unif - (double)trans_x);
            double pos_y = (double)this.Y + ((double)trans_y_unif - (double)trans_y);

            this.SetPosition ((float)pos_x, (float)pos_y);

            this.SetScaleFull (scale, scale, trans_x, trans_y);

            zoom_level = scale;

            foreach (SongActor s in actor_manager.Actors)
                s.SetScale (1/zoom_level, 1/zoom_level);
        }
        #endregion

        #region Animations
        /// <summary>
        /// Starts the zoom animation in the given direction.
        /// </summary>
        /// <param name="inwards">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <param name="x">
        /// A <see cref="System.Single"/> which specifies the x-coordinate of the center point.
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Single"/> which specifies the y-coordinate of the center point.
        /// </param>
        private void ZoomOnPosition (bool inwards, float x, float y)
        {
//            //Transformed position
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
//                UpdateClipping ();
                SecureUpdateClipping ();
            };

            animation_timeline.Start();

//            SecureUpdateClipping ();

        }

        /// <summary>
        /// Start the clustering animation in the given direction and zooms in or out, respectively.
        /// </summary>
        /// <param name="forward">
        /// A <see cref="System.Boolean"/> which specifies the direction (true - towards higher clustering
        /// level, false - towards lower clustering level).
        /// </param>
        private void AnimateClustering (bool forward)
        {
            bool playing = clustering_animation_timeline.IsPlaying;
            bool old_forward = clustering_animation == ClusteringAnimation.Forward ? true : false;

            // Stop clustering animation and remove all actors from fade in / fade out animations
            clustering_animation_timeline.Stop ();
            clustering_animation_behave.RemoveAll ();
            clustering_reverse_behave.RemoveAll ();

            // Zoom in or out
            ZoomOnCenter (!forward);

            // If still playing complete animation
            if (playing)
                HandleClusteringTimelineCompleted (this, new EventArgs ());

            // If not playing or direction stays the same rewind timeline
            if (!playing || forward == old_forward) {
//                Hyena.Log.Debug ("Animation rewind");
                clustering_animation_timeline.Rewind ();
            }

            // If forward and no clustering level available increase diff, abort
            if (forward && (point_manager.IsMaxLevel || diff_zoom_clustering != 0)) {
                diff_zoom_clustering ++;
//                Hyena.Log.Information ("Diff " + diff_zoom_clustering);
                return;
            }

            // If not forward and no clustering level available decrease diff, abort
            if (!forward && (point_manager.IsMinLevel || diff_zoom_clustering != 0)) {
                diff_zoom_clustering --;
//                Hyena.Log.Information ("Diff " + diff_zoom_clustering);
                return;
            }

            // Start animation and store direction
            clustering_animation_timeline.Start ();
            clustering_animation = forward ? SongGroup.ClusteringAnimation.Forward :
                                             SongGroup.ClusteringAnimation.Backward;

            // Update view
            UpdateView ();
        }

        /// <summary>
        /// Stops all animations (zoom and clustering)
        /// </summary>
        private void StopAllAnimations ()
        {
            animation_timeline.Stop ();
            animation_timeline.Rewind ();

            clustering_animation_timeline.Stop ();
            clustering_animation_timeline.Rewind ();

            clustering_animation = SongGroup.ClusteringAnimation.None;
        }

        /// <summary>
        /// [Old] Start the old clustering animation in the given direction and zooms in
        /// or out, respectively.
        /// </summary>
        /// <param name="forward">
        /// A <see cref="System.Boolean"/>
        /// </param>
        private void AnimateClusteringOld (bool forward)
        {
            TimelineDirection dir = clustering_animation_timeline.Direction;

            bool playing = clustering_animation_timeline.IsPlaying;

            //forward -> forward
            if (playing && dir == TimelineDirection.Forward && forward) {
                HandleClusteringTimelineCompletedOld (this, new EventArgs ());
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
//                Hyena.Log.Information ("Diff ++" + diff_zoom_clustering);
                return;
            }

            // nach hinten aber kein clustering mehr
            if (!forward && point_manager.IsMinLevel) {
                diff_zoom_clustering --;
//                Hyena.Log.Information ("Diff --" + diff_zoom_clustering);
                return;
            }

            if (forward && diff_zoom_clustering != 0) {
                diff_zoom_clustering ++;
//                Hyena.Log.Information ("Diff --" + diff_zoom_clustering);
                return;
            }

            if (!forward && diff_zoom_clustering != 0) {
                diff_zoom_clustering --;
//                Hyena.Log.Information ("Diff ++" + diff_zoom_clustering);
                return;
            }

            //back -> back or back
            if (!forward && (!playing || dir != TimelineDirection.Forward)) {
                lock (point_manager_lock) {
                    point_manager.DecreaseLevel ();
                }
                UpdateView ();
            }

            clustering_animation_timeline.Direction =
                            forward ? TimelineDirection.Forward : TimelineDirection.Backward;

            //rewind if direction stays the same or not playing
            if (!playing || dir ==  clustering_animation_timeline.Direction) {
                clustering_animation_timeline.Rewind ();
                clustering_animation_behave.RemoveAll ();
            }

            clustering_animation_timeline.Start ();

//            UpdateClipping ();
            SecureUpdateClipping ();
        }

        /// <summary>
        /// Add either the fade in or the fade out clustering animation to the actor
        /// which is associated with the given point.
        /// </summary>
        /// <param name="p">
        /// A <see cref="SongPoint"/>
        /// </param>
        /// <param name="fade_in">
        /// A <see cref="System.Boolean"/>
        /// </param>
        private void AddClusteringAnimation (SongPoint p, bool fade_in)
        {

            switch (fade_in) {

            // case old level -> fade out animation
            case false:
//                Hyena.Log.Debug ("Fade out");
                if (clustering_animation_behave.IsApplied (p.Actor))
                    return;
                clustering_animation_behave.Apply (p.Actor);
                break;

            // case new level -> fade in animation
            case true:
//                Hyena.Log.Debug ("Fade in");
                if (clustering_reverse_behave.IsApplied (p.Actor))
                    return;
                clustering_reverse_behave.Apply (p.Actor);
                break;
            }

        }

        /// <summary>
        /// [Old] Adds the old clustering animation to the actor which is associated with
        /// the given point.
        /// </summary>
        /// <param name="p">
        /// A <see cref="SongPoint"/>
        /// </param>
        private void AddClusteringAnimationOld (SongPoint p)
        {
            if (p.Parent == null) {
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

        /// <summary>
        /// Removes the actor which is associated with the given point from all clustering
        /// animations.
        /// </summary>
        /// <param name="p">
        /// A <see cref="SongPoint"/>
        /// </param>
        private void RemoveClusteringAnimation (SongPoint p)
        {
            if (clustering_animation_behave.IsApplied (p.Actor))
                clustering_animation_behave.Remove (p.Actor);

            if (clustering_reverse_behave.IsApplied (p.Actor))
                clustering_reverse_behave.Remove (p.Actor);
        }

        /// <summary>
        /// Handle which is called after the clustering animation is completed.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void HandleClusteringTimelineCompleted (object sender, EventArgs e)
        {
            // Clustering completed, set to new level
            if (clustering_animation == ClusteringAnimation.Forward) {
                lock (point_manager_lock) {
                 point_manager.IncreaseLevel ();
                }
            }
            else {
                lock (point_manager_lock) {
                    point_manager.DecreaseLevel ();
                }
            }

            clustering_animation = ClusteringAnimation.None;
            UpdateView ();
        }

        /// <summary>
        /// [Old] Handle for the old clustering animation which is called after the
        /// animation is completed.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void HandleClusteringTimelineCompletedOld (object sender, EventArgs e)
        {
            if (clustering_animation_timeline.Direction == TimelineDirection.Forward) {
                lock (point_manager_lock) {
                    point_manager.IncreaseLevel ();
                }
            }

            UpdateView ();
        }
        #endregion

        #region Clipping

        /// <summary>
        /// Clears the list of visible points.
        /// </summary>
        private void ClearView ()
        {
            //remove all visible points
            foreach (SongPoint p in points_visible) {
                actor_manager.Free (p.Actor);
                p.Actor = null;
            }

            points_visible.Clear ();
        }

        /// <summary>
        /// Clears the list of visible points and updates the clipping window.
        /// </summary>
        private void UpdateView ()
        {
            ClearView ();
            UpdateClipping ();
        }

        /// <summary>
        /// Calculates the transformed position for the given coordinated. This replaces
        /// the buggy <see cref="Clutter.Actor.GetTransformedPosition"/>.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Single"/>
        /// </param>
        private new void GetTransformedPosition (out float x, out float y)
        {
            double sx, sy;
            float cx, cy;
            GetScaleCenter (out cx, out cy);
            GetScale (out sx, out sy);

            x = this.X + cx * (float)(1-sx);
            y = this.Y + cy * (float)(1-sy);

//            Hyena.Log.Debug ("Transformed position");
        }

        /// <summary>
        /// This function calculates the coordinates in point space for the current visible window.
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
        private void GetClippingWindow (out double x, out double y, out double width, out double height)
        {
            float tx, ty;
            double sx, sy;
            GetTransformedPosition (out tx, out ty);

            GetScale (out sx, out sy);

//            Hyena.Log.Information (String.Format ("Scale {0},{1}",sx, sy));
            x = (-(float)SongActor.CircleSize-tx)/sx;
            y = (-(float)SongActor.CircleSize-ty)/sy;
            width = (stage.Width+2*(float)SongActor.CircleSize)/sx;
            height = (stage.Height+2*(float)SongActor.CircleSize)/sy;
        }

        /// <summary>
        /// This function is used to recalculate the clipping. That means, points which are newly visible are
        /// shown and points which are not visible anymore are hidden.
        /// </summary>
        public void UpdateClipping ()
        {
            double x, y, width, height;
            GetClippingWindow (out x, out y, out width, out height );

            List<SongPoint> points;
            SongPoint p;

//            Hyena.Log.Information (String.Format ("Clipping window at {0},{1} with {2}x{3}",
//                                                  x, y, width, height));

            points = point_manager.GetPointsInWindow (x, y, width, height);
            int old_points_count = points.Count;


            // if animation is running show points from this level and the next
            if (clustering_animation != SongGroup.ClusteringAnimation.None) {

                int offset = clustering_animation == SongGroup.ClusteringAnimation.Forward ?
                    1 : -1;

                points.AddRange (point_manager.GetPointsInWindow (x, y, width, height, offset));
            }

//            Hyena.Log.Debug (String.Format ("Point count old={0} new={1}", old_points_count, points.Count));
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
//                    if (clustering_animation_behave.IsApplied (p.Actor) ||
//                        clustering_reverse_behave.IsApplied (p.Actor))
//                        continue;

//                    AddClusteringAnimationNew (p, false);
                }
            }

            // Check invisible points
            for (int i = 0; i < points.Count; i++)
            {
                if (!actor_manager.HasFree) {
                    Hyena.Log.Warning ("NoNoise/Vis - No free Actor left");
                    break;
                }

                p = points[i];

                if (p.Actor != null)
                    continue;

                if (!p.IsVisible)
                    continue;

                p.Actor = actor_manager.AllocateAtPosition (p);
                points_visible.Add (p);

                AddClusteringAnimation (p, i >= old_points_count);
            }

//            Hyena.Log.Information ("Clipping count " + points_visible.Count);
        }

        /// <summary>
        /// The clipping is updated after the next Paint event.
        /// </summary>
        private void SecureUpdateClipping ()
        {
            UpdateClipping ();
//            this.Painted += HandlePaintedUpdateClipping;
        }

        /// <summary>
        /// The view is updated after the next Paint event.
        /// </summary>
        private void SecureUpdateView ()
        {
            UpdateView ();
//            this.Painted += HandlePaintedUpdateView;
        }

        #endregion

        #region Selection
        /// <summary>
        /// Clears the selection.
        /// </summary>
        private void ClearSelection ()
        {

            lock (point_manager_lock) {
                point_manager.ClearSelection ();
            }

            UpdateView ();

            selection.Reset ();

            FireSelectionCleared ();
        }

        /// <summary>
        /// Updates the selected points according to their visibility and fires the appropriate event.
        /// </summary>
        private void UpdateShownSelection ()
        {
            FireSongSelected (new SongInfoArgs (point_manager.GetSelectedIDs ()));
        }

        /// <summary>
        /// Gets the selected points, updates the view and fires the appropriate event.
        /// </summary>
        private void UpdateSelection ()
        {
            List<SongPoint> list = selection.GetPointsInside (points_visible);

            foreach (SongPoint p in list) {

                p.MarkAsSelected ();

                if (p.Actor == null)
                    continue;

                p.Actor.SetPrototypeByColor (SongActor.Color.Red);
            }

            UpdateShownSelection ();
        }
        #endregion Selection

         #region private Handler

        /// <summary>
        /// [Old] Handle the zooming with the animation. This is used when for
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
        /// Handle the mouse motion for displacement and selection.
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

            EventHelper.GetCoords (args.Event, out x, out y);

            float newx = this.X + x - mouse_old_x;
            float newy = this.Y + y - mouse_old_y;

            if (mouse_down) {

                // calc transformed position
                float cx, cy;
                GetScaleCenter (out cx, out cy);

                double sx, sy;
                GetScale (out sx, out sy);

                // calculate stage center in non-scaled coordinates
                float tcx = stage.Width /2f - cx * (float)(1-sx);
                float tcy = stage.Height /2f - cy * (float)(1-sy);

                // Check if inside bounds
                newx = tcx < newx ? tcx : newx;
                newy = tcy < newy ? tcy : newy;

                newx = tcx - this.Width * (float)sx > newx ? tcx - this.Width * (float)sx : newx;
                newy = tcy - this.Height * (float)sx > newy ? tcy - this.Height * (float)sx : newy;

                this.SetPosition (newx, newy);

                SecureUpdateClipping ();

            } else if (selection_enabled){

                selection.LineTo (x, y);
            }

            mouse_old_x = x;
            mouse_old_y = y;
        }


        /// <summary>
        /// Handles changes of the window size.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="AllocationChangedArgs"/>
        /// </param>
        private void HandleWindowSizeChanged (object o, AllocationChangedArgs args)
        {
            if (stage.Width <= 1)
                return;


            selection.SetSize (stage.Width, stage.Height);

            SecureUpdateClipping ();
        }


        /// <summary>
        /// Handler for the <see cref="Painted"/> event which is called once to update
        /// the clipping.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void HandlePaintedUpdateClipping (object sender, EventArgs e)
        {
            UpdateClipping ();
            this.Painted -= HandlePaintedUpdateClipping;
        }

        /// <summary>
        /// Handler for the <see cref="Painted"/> event which is called once to update
        /// the view.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void HandlePaintedUpdateView (object sender, EventArgs e)
        {
            UpdateView ();
            this.Painted -= HandlePaintedUpdateView;
        }

        /// <summary>
        /// Handles the button release event for displacement and selection.
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

            if (button != 1)
                return;

            uint click_count = EventHelper.GetClickCount (args.Event);
//            Hyena.Log.Debug ("Click count = " + click_count);

            if (click_count >= 2 && click_count < 4) {
                float x, y;
                EventHelper.GetCoords (args.Event, out x, out y);

                Actor clicked = stage.GetActorAtPos (PickMode.Reactive, (int)x, (int)y);

                if (clicked != null) {
                    if ((clicked is SongActor)) {
                        if ((clicked as SongActor).Owner != null)
                            FireSongStartPlaying (new SongInfoArgs ((clicked as SongActor).Owner.GetAllIDs ()));
                    }
                }
            }

            if (selection_enabled) {
                selection.Stop ();
                UpdateSelection ();
            }

            selection_enabled = false;

            mouse_down = false;
        }

        /// <summary>
        /// Handles all button press events for displacement and selection.
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


            if (button != 1)
                return;

            if (selection_toggle) {

//                ClearSelection ();
                selection.Reset ();
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

        /// <summary>
        /// Fires <see cref="SongHighlightEvent"/>
        /// </summary>
        /// <param name="args">
        /// A <see cref="SongInfoArgs"/>
        /// </param>
        private void FireSongEnter (SongInfoArgs args)
        {
            if (mouse_down)
                return;

            if (song_enter != null)
                song_enter (this, args);
        }

        /// <summary>
        /// Fires <see cref="SongLeveEvent"/>
        /// </summary>
        private void FireSongLeave ()
        {
            if (song_leave != null)
                song_leave (this);
        }

        /// <summary>
        /// Fires <see cref="SongSelectedEvent"/>
        /// </summary>
        /// <param name="args">
        /// A <see cref="SongInfoArgs"/>
        /// </param>
        private void FireSongSelected (SongInfoArgs args)
        {
            if (song_selected != null)
                song_selected (this, args);
        }

        /// <summary>
        /// Fires <see cref="SelectionClearedEvent"/>
        /// </summary>
        private void FireSelectionCleared ()
        {
            if (selection_cleared != null)
                selection_cleared (this);
        }

        /// <summary>
        /// Fires <see cref="SongStartPlayingEvent"/>
        /// </summary>
        /// <param name="args">
        /// A <see cref="SongInfoArgs"/>
        /// </param>
        private void FireSongStartPlaying (SongInfoArgs args)
        {
//            Hyena.Log.Information ("Fire function");
            if (song_start_playing != null)
                song_start_playing (this, args);
        }

        #endregion

        public override void Dispose ()
        {
            StopAllAnimations ();
            DisposeHandlers ();
        }
    }
}

