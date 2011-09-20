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

namespace NoNoise.Visualization.Gui
{
    public class MainGui: Clutter.Group
    {
//        private Clutter.Rectangle zoom_in;
//        private Clutter.Rectangle zoom_out;

        private CairoTexture zoom_in;
        private CairoTexture zoom_out;

        private CairoTexture debug_in;
        private CairoTexture debug_out;

        public MainGui () : base ()
        {
            Init ();
        }

        //Initializes the Gui Buttons - i.e. zoom in/out
        private void Init ()
        {
            Hyena.Log.Debug ("GUI init");

            //Create Texture
            CreateZoomTexture (out zoom_in, true);
            zoom_in.Reactive = true;
            //Attach Handler
            zoom_in.ButtonPressEvent += HandleZoomInEvent;

            //Create Texture
            CreateZoomTexture (out zoom_out, false);
            zoom_out.Reactive = true;
            zoom_out.SetPosition (0,40);
            //Attach Handler
            zoom_out.ButtonPressEvent += HandleZoomOutEvent;


            this.Add (zoom_in);
            this.Add (zoom_out);

            this.Reactive = true;

            InitDebug ();
        }

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

        //Event Handler which is called when the "-" button is pressed
        void HandleZoomOutEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton (args.Event);

            if(button == 1)
            {
                Hyena.Log.Debug ("Zoom out");

                //Call event Handler - TODO level allways 0
                if (this.zoom_changed != null)
                    zoom_changed (this,new ZoomLevelArgs(false,0));
            }
        }

        //Event Handler which is called when the "+" button is pressed
        void HandleZoomInEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton (args.Event);

            if(button == 1)
            {
                Hyena.Log.Debug ("Zoom in");

                //Call event Handler - TODO level allways 0
                if (this.zoom_changed != null)
                   zoom_changed (this,new ZoomLevelArgs(true,0));
            }
        }

        //The CairoTexture "+" or "-" with circle is created
        void CreateZoomTexture (out CairoTexture actor, bool inwards)
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

            context.MoveTo (10,15);
            context.LineTo (20,15);
            context.Stroke ();

            if (inwards)
            {
                context.MoveTo (15,10);
                context.LineTo (15,20);
                context.Stroke ();
            }
            ((IDisposable) context.Target).Dispose ();
            ((IDisposable) context).Dispose ();
        }

        public delegate void ZoomLevelChangedEvent (Object source, ZoomLevelArgs args);

        private ZoomLevelChangedEvent zoom_changed;

        //Event args which are used to update the Zoom level
        public class ZoomLevelArgs
        {
            private bool inwards;   //zoom in or out
            private int level;      //actual zoom level

            public ZoomLevelArgs (bool inwards, int level)
            {
                this.inwards = inwards;
                this.level = level;
            }

            public bool Inwards {
                get {return inwards;}
            }

            public int Level {
                get {return level;}
            }
        }

        //Event Handler which is called when the zoom level has changed
        public event ZoomLevelChangedEvent ZoomChangedEvent {
            add { zoom_changed += value; }
            remove { zoom_changed -= value; }
        }

        #region Debug

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
