// 
// Gui.cs
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
using Clutter;
using System.Collections.Generic;

namespace NoNoise.Visualization.Gui
{
    public class MainGui: Clutter.Group, IDisposable
    {
//        private Clutter.Rectangle zoom_in;
//        private Clutter.Rectangle zoom_out;
        private Stage stage;

        private CairoTexture debug_in;
        private CairoTexture debug_out;

        private InfoBox infobox;
        private InfoBox selection_info;

        private ZoomButton zoom_button_in;
        private ZoomButton zoom_button_out;

        private ToolbarToggleButton select_button;
        private ToolbarToggleButton reset_button;
        private Button remove_button;
        private Button playlist_button;

        private Group toolbar;
        private StatusBox status_box;
        private bool status_important = false;

        public MainGui (Stage stage) : base ()
        {
            this.stage = stage;
            Init ();
        }

        //Initializes the Gui Buttons - i.e. zoom in/out
        private void Init ()
        {
            Hyena.Log.Debug ("NoNoise/Vis - GUI init");

            Font standard = new Font () {
                Family = "Arial",
                Slant = Cairo.FontSlant.Normal,
                Weight = Cairo.FontWeight.Bold,
                Size = 12,
                Color = new Cairo.Color (0.1, 0.1, 0.1)
            };

            Font small = new Font () {
                Family = "Verdana",
                Slant = Cairo.FontSlant.Normal,
                Weight = Cairo.FontWeight.Normal,
                Size = 9,
                Color = new Cairo.Color (0.1, 0.1, 0.1)
            };

            StyleSheet style = new StyleSheet () {
                Foreground = new Cairo.Color (0.1, 0.1, 0.1,0.5),
                Background = new Cairo.Color (1, 1, 1, 0.9),
                Selection = new Cairo.Color (1, 0, 0, 0.9),
                Standard = standard,
                Highlighted = standard,
                Subtitle = small,
                Border = new Cairo.Color (1, 1, 1, 1),
                SelectionBoarder = new Cairo.Color (1, 0, 0, 0),
                BorderSize = 1.0
            };

            zoom_button_in = new ZoomButton (style, true);

            zoom_button_out = new ZoomButton (style, false);
            zoom_button_out.SetPosition (0,30);

            this.Add (zoom_button_in);
            this.Add (zoom_button_out);


            toolbar = new Group ();
            select_button = new ToolbarToggleButton ("select", "select", true, style,
                                                     ToolbarButton.Border.Left, 75,20);
            select_button.SetPosition (0,0);


            remove_button = new ToolbarButton ("remove", style,
                                               ToolbarButton.Border.None, 75,20);
            remove_button.SetPosition (152, 0);


            reset_button = new ToolbarToggleButton ("reset", "clear", false, style,
                                               ToolbarButton.Border.None,
                                               75,20);
            reset_button.SetPosition (76, 0);


            playlist_button = new ToolbarButton ("playlist", style,
                                               ToolbarButton.Border.Right, 75,20);
            playlist_button.SetPosition (228, 0);



            toolbar.Add (select_button);
            toolbar.Add (remove_button);
            toolbar.Add (reset_button);
            toolbar.Add (playlist_button);

            toolbar.AnchorPointFromGravity = Gravity.North;
            toolbar.SetPosition (500,5);
            this.Add (toolbar);

            infobox = new InfoBox (style, 200,400, false);
            infobox.AnchorPointFromGravity = Gravity.NorthEast;
            this.Add (infobox);

            selection_info = new InfoBox (style, 200, 400, true);
            selection_info.AnchorPointFromGravity = Gravity.SouthEast;
            this.Add (selection_info);

            status_box = new StatusBox (style, 400, 20);
            status_box.AnchorPointFromGravity = Gravity.SouthWest;
            this.Add (status_box);

//            InitDebug ();
            this.Reactive = false;
            InitHandler ();
        }

        private void DisposeHandler ()
        {
            stage.AllocationChanged -= HandleWindowSizeChanged;
            select_button.ButtonPressEvent -= HandleSelect_buttonButtonPressEvent;
            remove_button.ButtonPressEvent -= HandleRemove_buttonButtonPressEvent;
            reset_button.ButtonPressEvent -= HandleReset_buttonButtonPressEvent;
            playlist_button.ButtonPressEvent -= HandlePlaylist_buttonButtonPressEvent;
            zoom_button_out.ButtonPressEvent -= HandleZoom_button_outButtonPressEvent;
            zoom_button_in.ButtonPressEvent -= HandleZoom_button_inButtonPressEvent;
        }


        /// <summary>
        /// Initializes all handlers needed for gui interaction.
        /// </summary>
        private void InitHandler ()
        {
            stage.AllocationChanged += HandleWindowSizeChanged;
            select_button.ButtonPressEvent += HandleSelect_buttonButtonPressEvent;
            remove_button.ButtonPressEvent += HandleRemove_buttonButtonPressEvent;
            reset_button.ButtonPressEvent += HandleReset_buttonButtonPressEvent;
            playlist_button.ButtonPressEvent += HandlePlaylist_buttonButtonPressEvent;
            zoom_button_out.ButtonPressEvent += HandleZoom_button_outButtonPressEvent;
            zoom_button_in.ButtonPressEvent += HandleZoom_button_inButtonPressEvent;
        }

        void HandleZoom_button_outButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.ZoomOut));
        }

        void HandleZoom_button_inButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.ZoomIn));
        }

        void HandlePlaylist_buttonButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Playlist));
        }

        void HandleReset_buttonButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (button_clicked != null){

                    if (reset_button.IsOn)
                        button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Clear));
                    else
                        button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Reset));
            }
        }

        void HandleRemove_buttonButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Remove));
        }

        void HandleSelect_buttonButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Select));
        }

        /// <summary>
        /// Handler called when the allocation size of the infobox has changed.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="AllocationChangedArgs"/>
        /// </param>
        void HandleInfoboxAllocationChanged (object o, AllocationChangedArgs args)
        {
            selection_info.SetPosition (selection_info.X, infobox.Y + infobox.Height + 10);
        }

        /// <summary>
        /// Handler called when the size of the stage has changed.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="AllocationChangedArgs"/>
        /// </param>
        void HandleWindowSizeChanged (object o, AllocationChangedArgs args)
        {
            toolbar.SetPosition (0.5f+(float)Math.Round (stage.Width/2f-infobox.Width/2f+zoom_button_in.Width), toolbar.Y);
            infobox.SetPosition (stage.Width, 0);
            selection_info.SetPosition (stage.Width, stage.Height);
            status_box.SetPosition (0, stage.Height);
//            Hyena.Log.Information ("Stage size x " + stage.X + "x" + stage.Height);
        }

        /// <summary>
        /// Updates the text in the infobox.
        /// </summary>
        /// <param name="titles">
        /// A <see cref="List<String>"/> which specifies the song titles shown.
        /// </param>
        /// <param name="subtitles">
        /// A <see cref="List<String>"/> which specifies the artists shown.
        /// </param>
        public void UpdateInfoText (List<String> titles, List<String> subtitles)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                infobox.Update (titles, subtitles);
            });
        }

        /// <summary>
        /// Updates the text in the selection infobox.
        /// </summary>
        /// <param name="titles">
        /// A <see cref="List<String>"/> which specifies the song titles shown.
        /// </param>
        /// <param name="subtitles">
        /// A <see cref="List<String>"/> which specifies the artists shown.
        /// </param>
        public void UpdateSelection (List<String> titles, List<String> subtitles)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                selection_info.Update (titles, subtitles);
            });
        }

        /// <summary>
        /// Clears the text in the infobox
        /// </summary>
        public void ClearInfoText ()
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                infobox.Clear ();
            });
        }

        /// <summary>
        /// Clears the text in the selection infobox
        /// </summary>
        public void ClearInfoSelection ()
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                selection_info.Clear ();
            });
        }

        public void UpdateStatus (String text, bool waiting)
        {
            UpdateStatus (text, waiting, 0);
        }
        
        public void UpdateStatus (String text, bool waiting, int priority)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate() {
                if (priority > 0 || !status_important) {
    //                Hyena.Log.DebugFormat ("NoNoise/GUI - updating status with '{0}', priority: {1}", text, priority);
                    status_box.Update (text, waiting);
    
                    status_important = priority == 2;
                }
            });
        }

        public void SetResetButton (bool clear)
        {
            reset_button.SetState (clear);
        }

        /// <summary>
        /// [Debug] Draws a debug texture onto the given actor.
        /// </summary>
        /// <param name="actor">
        /// A <see cref="CairoTexture"/>
        /// </param>
        private void CreateDebugTexture (out CairoTexture actor)
        {
            actor = new CairoTexture (30,30);
            actor.Clear ();
            Cairo.Context context = actor.Create ();

            context.LineWidth = 2.0;

            context.Color = new Cairo.Color (0,0,0);
            context.Arc(15,15,11,0,2*Math.PI);
            context.Fill ();

            context.Color = new Cairo.Color (0.95,0.95,0.95);
            context.Arc(15,15,10,0,2*Math.PI);
            context.Stroke ();

            context.LineWidth = 0.0;
            context.Rectangle (12,12,6,6);
            context.Fill ();

            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        /// <summary>
        /// Fired when an element in the gui is clicked
        /// </summary>
        public event ButtonClickedEvent ButtonClicked {
            add { button_clicked += value; }
            remove { button_clicked -= value;}
        }

        public delegate void ButtonClickedEvent (Object source, ButtonClickedArgs args);

        private ButtonClickedEvent button_clicked;

        /// <summary>
        /// Arguments for the <see cref="ButtonClickedEvent"/>
        /// </summary>
        public class ButtonClickedArgs
        {
            /// <summary>
            /// Specifies the button clicked.
            /// </summary>
            public enum Button { ZoomIn, ZoomOut, Select, Reset, Playlist, Remove, Clear};

            public Button ButtonClicked {
                get;
                private set;
            }

            public ButtonClickedArgs (Button button)
            {
                this.ButtonClicked = button;
//                ButtonClicked = button;
            }
        }

        #region Debug

        /// <summary>
        /// Initializes two debug buttons.
        /// </summary>
        private void InitDebug ()
        {
            Hyena.Log.Debug ("Debug GUI init");

            CreateDebugTexture (out debug_in);
            debug_in.SetPosition (0,100);
            debug_in.Reactive = true;
            debug_in.ButtonPressEvent += HandleDebugOneEvent;
            this.Add (debug_in);

            CreateDebugTexture (out debug_out);
            debug_out.SetPosition (0,140);
            debug_out.Reactive = true;
            debug_out.ButtonPressEvent += HandleDebugTwoEvent;
            this.Add (debug_out);
        }

        /// <summary>
        /// Handler called when the second debug button is clicked.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ButtonPressEventArgs"/>
        /// </param>
        void HandleDebugTwoEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton (args.Event);

            if(button == 1)
            {
                Hyena.Log.Debug ("Debug two");

//                if (this.debug_event != null)
//                    debug_event (this,new DebugEventArgs (-1,"Debug two"));
                status_box.Update ("no spinner", false);
            }
        }

        /// <summary>
        /// Handler called when the first debug button is clicked.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ButtonPressEventArgs"/>
        /// </param>
        void HandleDebugOneEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton (args.Event);

            if(button == 1)
            {
                Hyena.Log.Debug ("Debug one");

//                if (this.debug_event != null)
//                    debug_event (this,new DebugEventArgs (1,"Debug one"));
                status_box.Update ("new text", true);
            }
        }

        public delegate void DebugEvent (Object source, DebugEventArgs args);

        private DebugEvent debug_event;

        /// <summary>
        /// Arguments for the <see cref="DebugEvent"/>.
        /// </summary>
        public class DebugEventArgs
        {
            private int val;
            private string info;

            public DebugEventArgs (int val, string info)
            {
                this.info = info;
                this.val = val;
            }

            /// <summary>
            /// Some int value.
            /// </summary>
            public int Value {
                get {return val;}
            }

            /// <summary>
            /// Some string info.
            /// </summary>
            public string Info {
                get {return info;}
            }
        }

        /// <summary>
        /// Event handler which is called when a debug button is clicked.
        /// </summary>
        public event DebugEvent DebugButtonPressedEvent {
            add { debug_event += value; }
            remove { debug_event -= value; }
        }

        public override void Dispose ()
        {
            select_button.Dispose ();
            reset_button.Dispose ();
            status_box.Dispose ();

            DisposeHandler ();
        }
        #endregion
    }
}

