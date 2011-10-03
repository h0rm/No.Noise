// 
// Gui.cs
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

namespace NoNoise.Visualization.Gui
{
    public class MainGui: Clutter.Group
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

        private Button select_button;
        private Button reset_button;
        private Button remove_button;
        private Button playlist_button;

        private Group toolbar;

        public MainGui (Stage stage) : base ()
        {
            this.stage = stage;
            Init ();
        }

        //Initializes the Gui Buttons - i.e. zoom in/out
        private void Init ()
        {
            Hyena.Log.Debug ("GUI init");

            StyleSheet style = new StyleSheet (new Cairo.Color (0.1, 0.1, 0.1,0.5),
                                               new Cairo.Color (1, 1, 1, 0.9),
                                               new Cairo.Color (1, 0, 0, 0.9),
                                               new Font ("Arial",
                                               Cairo.FontSlant.Normal,
                                               Cairo.FontWeight.Bold,
                                               12, new Cairo.Color (0.1, 0.1, 0.1)),
                                               new Font ("Arial",
                                               Cairo.FontSlant.Normal,
                                               Cairo.FontWeight.Bold,
                                               12, new Cairo.Color (0.1, 0.1, 0.1)),
                                               new Font ("Verdana",
                                               Cairo.FontSlant.Normal,
                                               Cairo.FontWeight.Normal,
                                               9, new Cairo.Color (0.1, 0.1, 0.1)),
                                               new Cairo.Color (1, 1, 1, 1),
                                               new Cairo.Color (1, 0, 0, 0),
                                               1.0
                                               );

            zoom_button_in = new ZoomButton (style, true);

            zoom_button_out = new ZoomButton (style, false);
            zoom_button_out.SetPosition (0,30);

            this.Add (zoom_button_in);
            this.Add (zoom_button_out);


            toolbar = new Group ();
//            toolbar.SetSize (306,20);
            select_button = new ToolbarToggleButton ("select", style,
                                                     ToolbarButton.Border.Left, 75,20);
            select_button.SetPosition (0,0);


            remove_button = new ToolbarButton ("remove", style,
                                               ToolbarButton.Border.None, 75,20);
            remove_button.SetPosition (76, 0);


            reset_button = new ToolbarButton ("reset", style,
                                               ToolbarButton.Border.None,
                                               75,20);
            reset_button.SetPosition (152, 0);


            playlist_button = new ToolbarButton ("playlist", style,
                                               ToolbarButton.Border.Right, 75,20);
            playlist_button.SetPosition (228, 0);



            toolbar.Add (select_button);
            toolbar.Add (remove_button);
            toolbar.Add (reset_button);
//            toolbar.
            toolbar.Add (playlist_button);
//            this.Add (playlist_button);
//            toolbar.SetScale (1,1);

            toolbar.AnchorPointFromGravity = Gravity.North;
            toolbar.SetPosition (500,5);

            this.Add (toolbar);

            infobox = new InfoBox (style, 200,400, false);
            infobox.AnchorPointFromGravity = Gravity.NorthEast;
            this.Add (infobox);
//            infobox.Reactive = true;

            selection_info = new InfoBox (style, 200, 400, true);
            selection_info.AnchorPointFromGravity = Gravity.SouthEast;
            this.Add (selection_info);
//            selection_info.Reactive = true;
//            selection_info.SetPosition (500,500);
//            InitDebug ();
            this.Reactive = false;
            InitHandler ();
        }

        private void InitHandler ()
        {
            stage.AllocationChanged += HandleWindowSizeChanged;

            select_button.ButtonPressEvent += delegate {
                if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Select));
            };

            remove_button.ButtonPressEvent += delegate {
                if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Remove));
            };

            reset_button.ButtonPressEvent += delegate {
                if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Reset));
            };

            playlist_button.ButtonPressEvent += delegate {
                if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.Playlist));
            };

            zoom_button_in.ButtonPressEvent += delegate(object o, ButtonPressEventArgs args) {

                if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.ZoomIn));
            };

            zoom_button_out.ButtonPressEvent += delegate(object o, ButtonPressEventArgs args) {

                if (button_clicked != null)
                    button_clicked (this, new ButtonClickedArgs (ButtonClickedArgs.Button.ZoomOut));
            };

//            infobox.ButtonPressEvent += delegate {
//
//                infobox.Mode = infobox.Mode == InfoBox.Size.Expanded
//                    ? InfoBox.Size.Collapsed : InfoBox.Size.Expanded;
//
////                selection_info.Mode = infobox.Mode == InfoBox.Size.Expanded
////                    ? InfoBox.Size.Collapsed : InfoBox.Size.Expanded;
//
////                selection_info.Update ();
//                infobox.Update ();
//            };


//            infobox.AllocationChanged += HandleInfoboxAllocationChanged;

        }

        void HandleInfoboxAllocationChanged (object o, AllocationChangedArgs args)
        {
            selection_info.SetPosition (selection_info.X, infobox.Y + infobox.Height + 10);
        }


        void HandleWindowSizeChanged (object o, AllocationChangedArgs args)
        {
            toolbar.SetPosition (0.5f+(float)Math.Round (stage.Width/2f-infobox.Width/2f+zoom_button_in.Width), toolbar.Y);
            infobox.SetPosition (stage.Width, 0);
            selection_info.SetPosition (stage.Width, stage.Height);
            Hyena.Log.Information ("Stage size x " + stage.X + "x" + stage.Height);
        }

        public void UpdateInfoText (List<String> titles, List<String> subtitles)
        {
            infobox.Update (titles, subtitles);
        }

        public void UpdateSelection (List<String> titles, List<String> subtitles)
        {
            selection_info.Update (titles, subtitles);
        }

        public void ClearInfoText ()
        {
            infobox.Clear ();
        }

        public void ClearInfoSelection ()
        {
            selection_info.Clear ();
        }


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


        public event ButtonClickedEvent ButtonClicked {
            add { button_clicked += value; }
            remove { button_clicked -= value;}
        }

        public delegate void ButtonClickedEvent (Object source, ButtonClickedArgs args);

        private ButtonClickedEvent button_clicked;

        public struct ButtonClickedArgs
        {
            public enum Button { ZoomIn, ZoomOut, Select, Reset, Playlist, Remove};

            public Button ButtonClicked {
                get;
                private set;
            }

            public ButtonClickedArgs (Button button)
            {
                ButtonClicked = button;
            }
        }

        #region Debug

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

        void HandleDebugTwoEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton (args.Event);

            if(button == 1)
            {
                Hyena.Log.Debug ("Debug two");

                //Call event Handler - TODO level allways 0
                if (this.debug_event != null)
                    debug_event (this,new DebugEventArgs (-1,"Debug two"));
            }
        }

        void HandleDebugOneEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton (args.Event);

            if(button == 1)
            {
                Hyena.Log.Debug ("Debug one");

                //Call event Handler - TODO level allways 0
                if (this.debug_event != null)
                    debug_event (this,new DebugEventArgs (1,"Debug one"));
            }
        }

        public delegate void DebugEvent (Object source, DebugEventArgs args);

        private DebugEvent debug_event;

        public class DebugEventArgs
        {
            private int val;
            private string info;

            public DebugEventArgs (int val, string info)
            {
                this.info = info;
                this.val = val;
            }

            public int Value {
                get {return val;}
            }

            public string Info {
                get {return info;}
            }
        }
        //Event Handler which is called when the zoom level has changed
        public event DebugEvent DebugButtonPressedEvent {
            add { debug_event += value; }
            remove { debug_event -= value; }
        }
        #endregion
    }
}

