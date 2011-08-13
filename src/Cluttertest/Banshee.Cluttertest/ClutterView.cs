// 
// ClutterView.cs
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
using System.Runtime.InteropServices;
using Clutter;
//using Cairo;

namespace Banshee.Cluttertest
{
    public class ClutterView : Clutter.Embed
    {
//        private Animation animation;

        private const int SCROLL_AMOUNT=30;
        private CairoTexture texture;
        private List<Actor> circles;
        private List<Actor> circles_shown;

        private Group circle_group;

        private Behaviour behave;
        private Alpha alpha;
        private Timeline timeline;

        private Texture overview;
        private Rectangle overview_rec;
        private Group overview_group;

        private String last_name;

        private Rectangle info_rect;
        private Group info_group;
        private Text info_text;

        private float old_mouse_x = 0, old_mouse_y = 0;
        private bool mouse_down = false;

        private List<Position> positions;

        //private Group cogl_primitive;

        struct Position{
            double x;
            double y;

            public Position (double x, double y)
            {
                this.x = x;
                this.y = y;
            }
            public double X
            {
                get { return x; }
                set { x = value; }
            }

            public double Y
            {
                get { return y; }
                set { y = value; }
            }
        };
        static GLib.Value getValue(Color val){
            GLib.Value v = GLib.Value.Empty;
            v.Init(Color.GType);
            v.Val = val;
            return v;
        }

        //Timeline umdrehen für animation
        static void ReverseTimeline(Timeline timeline)
        {
            TimelineDirection d = timeline.Direction;
            
            if(d == TimelineDirection.Forward)
                timeline.Direction = TimelineDirection.Backward;
            else
                timeline.Direction = TimelineDirection.Forward;
        }

        public Actor GenerateCoglPrimitive ()
        {
            Hyena.Log.Information("Generate Cogl Primitive");

            Group prim = new Group();
            prim.SetSize (100,100);

            prim.Painted += HandleDrawCircle;
            prim.SetScale(0.2,0.2);
            //prim.SetPosition(500,500);
            //Stage.Add(cogl_primitive);

            return prim;
            //cogl_primitive.QueueRedraw();
        }

        void HandleDrawCircle (object sender, EventArgs e)
        {
            Cogl.General.PushMatrix();

            Cogl.Path.MoveTo (+100, +50);
            Cogl.Path.Arc (50,50,50,50,0,360);
            Cogl.General.SetSourceColor4f(1.0f,0.0f,0.0f,0.3f);
            Cogl.Path.FillPreserve();

            Cogl.General.SetSourceColor4f(1.0f,0.0f,0.0f,0.5f);
            Cogl.Path.Stroke();
            Cogl.General.PopMatrix();
        }


        //overview map zeichnen
        public void GenerateOverview()
        {
            overview_rec = new Rectangle(new Color(0,0,0,255));
            overview = new Texture(circle_group);
            overview_group = new Group();

            float w,h;
            overview.GetSize(out w,out h);
            overview_rec.SetSize(w,h);

            overview_group.Add(overview_rec);
            overview_group.Add(overview);

            overview_group.SetPosition(0,0);
            overview_group.SetScale(0.2,0.2);

            Stage.Add(overview_group);
            Stage.RaiseChild(texture,overview_group);
        }

        //Circle textur mit cairo in eine textur zeichnen
        public void UpdateTexture ()
        {
            Cairo.Context context = texture.Create();
            context.Color = new Cairo.Color(0.0,0.0,1.0);

            context.LineWidth = texture.SurfaceWidth/10.0;
            context.Arc(texture.SurfaceWidth/2.0,texture.SurfaceHeight/2.0,texture.SurfaceWidth/2.0-context.LineWidth/2.0,0,2*Math.PI);
            //context.Rectangle(texture.SurfaceWidth/2.0,texture.SurfaceWidth/2.0,texture.SurfaceWidth/2.0,texture.SurfaceWidth/2.0);

            context.Color = new Cairo.Color(0.0,1.0,0.0,0.3);
            context.FillPreserve();

            context.Color = new Cairo.Color(0.0,1.0,0.0,0.5);
            context.Stroke();

            ((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();
        }

        public void GenerateDualCircles(double width, double height, Actor prototype)
        {
            circles = new List<Actor>();
            circles_shown = new List<Actor>();

            Random r = new Random();

            for (int i=0; i<2000; i++)
            {
                Actor dummy = new Rectangle();
                dummy.SetPosition((float)(r.NextDouble()*width),(float)(r.NextDouble()*height));

                //prototyp clonen + an random position setzten innerhalb stage
                Clone clone = new Clone(prototype);

                dummy.Painted += delegate {
                    float x = 0, y = 0;
                    dummy.GetTransformedPosition(out x, out y);
                    clone.SetPosition(x,y);
                };
                //clone.Painted += HandleClonePainted;
                //clone.RedrawQueued +=HandleClonePainted;

                circles.Add(dummy);
                circles_shown.Add(clone);

                circle_group.Add(dummy);
                info_group.RaiseTop();
                //behave.Apply(clone);

                //clone.Flags = ActorFlags.
                Stage.Add(clone);
            }
        }

        public void GenerateCoglClones (float width, float height)
        {

            circles = new List<Actor>();

            Random r = new Random();

            for (int i=0; i<2000; i++)
            {
                Actor actor = GenerateCoglPrimitive();

                actor.AnchorPointFromGravity = Gravity.Center;
                actor.SetPosition((float)(r.NextDouble()*width),(float)(r.NextDouble()*height));

                actor.Reactive = true;

                //actor.EnterEvent += HandleCircleMouseEnter;     //Handler für mouse enter events
                //actor.LeaveEvent += delegate {                  //Handler für leave
                //    info_group.Hide();             //infobox ausblenden
                //};

                actor.Name = "Clone "+i;        //Name

                 circles.Add(actor);

                circle_group.Add(actor);
                info_group.RaiseTop();
                //behave.Apply(clone);
            }
        }

        public void GeneratePositions(float width, float height, int count)
        {
            positions = new List<Position>();

            Random r = new Random ();

            for (int i=0; i<2000; i++)
            {
                positions.Add (new Position((float)(r.NextDouble()*width),(float)(r.NextDouble()*height)));
                Hyena.Log.Information("Pos "+r.NextDouble()*width+":"+r.NextDouble()*height);
            }

            circle_group.Painted += delegate {
                Cogl.General.PushMatrix();


                foreach (Position p in positions)
                {
                    Cogl.General.Translate((float)p.X, (float)p.Y,0f);
                    Cogl.General.PushMatrix();
                    Cogl.Path.MoveTo (+20, +10);
                    Cogl.Path.Arc (10,10,10,10,0,360);
                    Cogl.General.SetSourceColor4f(1.0f,0.0f,0.0f,0.3f);
                    Cogl.Path.FillPreserve();
        
                    Cogl.General.SetSourceColor4f(1.0f,0.0f,0.0f,0.5f);
                    Cogl.Path.Stroke();
                    Cogl.General.PopMatrix();
                }

                Cogl.General.PopMatrix();
            };
        }
        public void GenerateCircles(double width, double height, Actor prototype)
        {

            circles = new List<Actor>();

            Random r = new Random();

            for (int i=0; i<2000; i++)
            {
                //prototyp clonen + an random position setzten innerhalb stage
                Clone clone = new Clone(prototype);
                clone.AnchorPointFromGravity = Gravity.Center;
                clone.SetPosition((float)(r.NextDouble()*width),(float)(r.NextDouble()*height));
                clone.SetScaleWithGravity(1.0,1.0,Gravity.NorthWest);
                //reactive damit signals geschickt werden
                clone.Reactive = true;

                clone.EnterEvent += HandleCircleMouseEnter;     //Handler für mouse enter events
                clone.LeaveEvent += delegate {                  //Handler für leave
                    info_group.Hide();             //infobox ausblenden
                };

                clone.Name = "Clone "+i;        //Name

                clone.EnterEvent += delegate {
                    clone.SetScale(1/zoom_level * 1.5, 1/zoom_level * 1.5);
                };

                clone.LeaveEvent += delegate {
                    clone.SetScale(1/zoom_level, 1/zoom_level);
                };

                //clone.Painted += HandleClonePainted;
                //clone.RedrawQueued +=HandleClonePainted;

                circles.Add(clone);

                circle_group.Add(clone);
                info_group.RaiseTop();
                behave.Apply(clone);

                //clone.Flags = ActorFlags.
            }

        }

        void HandleClonePainted (object sender, EventArgs e)
        {

            double x,y;
            circle_group.GetScale(out x, out y);
            (sender as Actor).SetScale(1/x, 1/y);

        }

        //Handler für enter events bei den clone circles
        void HandleCircleMouseEnter (object o, EnterEventArgs args)
        {

            //Maus Koordinaten holen
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //Ausgabe von name + x:y (x,y) irgendwie falsch
            Hyena.Log.Information("Mouse Enter Clone "+(o as Clone).Name + " - " + mouse_x +":"+mouse_y);

            //Punkt transformieren - damit an richtiger position mit scale und so
            float x=0, y=0;
            (o as Clone).GetTransformedPosition(out x,out y);

            //An position von circle infobox setzen + namen
            info_group.SetPosition(x,y);
            info_text.Value = (o as Clone).Name;

            //infobox anzeigen
            info_group.Show();
        }

        public void PrintName(String name){
            if (String.Compare(name,last_name) != 0){
                Hyena.Log.Information("Mouseover: "+name);
                last_name = name;
            }
        }

        private void InitCircles()
        {
            //Circle erzeugen
            texture = new CairoTexture(20,20);

            texture.SetPosition(-500,500);
            texture.SetSize(20,20);
            texture.AnchorPointFromGravity = Gravity.Center;

            Stage.AllocationChanged += StageChanged;
            Stage.Add(texture);
            UpdateTexture();



            circle_group = new Group();
            circle_group.Lower(info_rect);

            circle_group.Reactive = true;
            //circle_group.SetSize(3000,1000);
            //circle_group.SetClip(0,0,circle_group.Width,circle_group.Height);
            circle_group.SetPosition(-1000,0);
            //circle_group.ScrollEvent += HandleViewportScroll;
            circle_group.ScrollEvent += HandleAdaptiveZoom;

            //foreach (Actor a in circles)
             //   circle_group.Add(a);

            //circle_group.Depth = 3f;

            Stage.Add(circle_group);

            //GeneratePositions(5000,1000,2000);
             //clonen
            GenerateCircles(5000,1000,texture);
            //GenerateDualCircles(5000,1000,texture);
            //GenerateCoglClones(5000,1000);
        }

        private void InitInfoBox ()
        {
            //info rect erzeugen
            info_rect = new Rectangle(new Color(255,255,255,200));
            info_rect.SetSize(200,100);

            //info text
            info_text = new Text("Mono 12","Test");
            info_text.SetColor(new Color(0,0,0,255));
            info_text.SetPosition(10,40);

            //info group
            info_group = new Group();
            info_group.Add(info_rect);
            info_group.Add(info_text);
            info_group.SetPosition(500,500);
            info_group.AnchorPointFromGravity = Gravity.SouthWest;
            Stage.Add(info_group);
        }

//        private void InitScaleAnimation ()
//        {
//            animation = new Animation();
//            animation.Mode = (ulong)AnimationMode.EaseOutSine;
//            animation.SetDuration(500);
//
//            GLib.Value v3 = GLib.Value.Empty;
//            v3.Init(GLib.GType.Double);
//            v3.Val = 1.5;
//
//            animation.Object = rec;
//            animation.Bind("scale-x",v3);
//            animation.Bind("scale-y",v3);
//
//            animation.Bind("color",getValue(new Color(0,255,0,30)));
//
//            animation.Started += delegate {
//                System.Console.Out.WriteLine("Animation started");
//            };
//
//            animation.Loop = true;
//            animation.Timeline.Completed += delegate(object sender, EventArgs e) {
//                ReverseTimeline((Timeline)sender);
//            };
//            animation.Timeline.Start();
//        }

        private void InitCircleAnimation ()
        {
            //Für Behaviour 5 steps
            //1. Timeline
            timeline = new Timeline(1000);
            //timeline.Loop = true;
           /* timeline.Completed += delegate(object sender, EventArgs e) {
                ReverseTimeline((Timeline)sender);
            };*/

            //2. Alpha
            alpha = new Alpha(timeline, (ulong)AnimationMode.EaseOutCubic);

            //3. Behaviour
            behave = new BehaviourScale(alpha,1.0,1.0,1.2,1.2);
            //behave = new BehaviourDepth(alpha,1,10);
            //4. Apply
            //foreach (Actor a in circles)
              //  behave.Apply(a);

            //5. Start timeline
            //timeline.Start();

//            Stage.ButtonPressEvent += delegate(object o, ButtonPressEventArgs args) {
//                if (timeline.IsPlaying)
//                    timeline.Pause();
//                else
//                    timeline.Start();
//            };
        }

        public ClutterView () : base ()
        {
            Hyena.Log.Information("ClutterView constructing");

            SetSizeRequest(100,100);
            Stage.Color = new Color(0,0,0,255);

            Stage.ButtonPressEvent += HandleStageButtonPressEvent;
            Stage.ButtonReleaseEvent += HandleStageButtonReleaseEvent;
            Stage.MotionEvent += HandleStageMotionEvent;
            //Stage.KeyPressEvent += CheckZoomTransform;
        }

        public void Init()
        {
            InitCircleAnimation();
            InitInfoBox();
            InitCircles();
            //GenerateOverview ();
            //GenerateCoglPrimitive();

        }
        void HandleStageMotionEvent (object o, MotionEventArgs args)
        {
            if (!mouse_down)        //wenn nicht geklickt
                return;

            float x, y;
            EventHelper.GetCoords( args.Event, out x, out y);

            circle_group.SetPosition(circle_group.X + x - old_mouse_x, circle_group.Y + y - old_mouse_y);

            Hyena.Log.Information("Mouse Move delta " + (x - old_mouse_x) + ":" + (y - old_mouse_y) );
            old_mouse_x = x;
            old_mouse_y = y;
        }

        void HandleStageButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
        {
            Hyena.Log.Information("Mouse Up.");
            mouse_down = false;
        }

        void HandleStageButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            uint button = EventHelper.GetButton(args.Event);
            if(button == 3)
                CheckZoomTransform(args);

            EventHelper.GetCoords(args.Event, out old_mouse_x, out old_mouse_y);
            Hyena.Log.Information("Mouse "+button+" Down at " + old_mouse_x + ":" + old_mouse_y );
            mouse_down = true;
        }

        [StructLayout (LayoutKind.Sequential)]
        struct Coordinate {

            public double x;
            public double y;

            public Coordinate(double x, double y)
            {
                this.x = x;
                this.y = y;
            }
        };

        private double zoom_level = 1.0;
        const double zoom_level_mult = 2.0;

        void CheckZoomTransform (ButtonPressEventArgs args)
        {
            Hyena.Log.Information("State Zoom Animation");

            //Mouse position
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //Transformed position
            float trans_x = 0, trans_y = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x, out trans_y);

            //raus zoomen
            circle_group.SetScale(1.0,1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x_unif, out trans_y_unif);

            float pos_x = circle_group.X + (trans_x_unif - trans_x);
            float pos_y = circle_group.Y + (trans_y_unif - trans_y);

            //punkt auf objekt schieben
            circle_group.SetPosition(pos_x, pos_y);

            circle_group.SetScale(zoom_level,zoom_level);
            circle_group.SetScaleFull(zoom_level,zoom_level,trans_x,trans_y);

            zoom_level *= zoom_level_mult;


            circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-x"},new GLib.Value(zoom_level));
            circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-y"},new GLib.Value(zoom_level));

            foreach (Actor a in circles)
                a.SetScale(1.0f/zoom_level,1.0f/zoom_level);
        }


        void HandleAdaptiveZoom (object o, ScrollEventArgs args)
        {

           Hyena.Log.Information("Adaptive Zoom");

            //Mouse position
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //Transformed position
            float trans_x = 0, trans_y = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x, out trans_y);

            //raus zoomen
            circle_group.SetScale(1.0,1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x_unif, out trans_y_unif);

            float pos_x = circle_group.X + (trans_x_unif - trans_x);
            float pos_y = circle_group.Y + (trans_y_unif - trans_y);

            //punkt auf objekt schieben
            circle_group.SetPosition(pos_x, pos_y);

            //circle_group.SetScale(zoom_level,zoom_level);


            double old_zoom_level = zoom_level;

            //rein zoomen
            switch (args.Event.Direction)
            {
            case ScrollDirection.Up:
                zoom_level *= zoom_level_mult;
                break;

            case ScrollDirection.Down:
                zoom_level /= zoom_level_mult;
                break;
            }

            uint duration = 1000;

            if (timeline.Progress < 0.2 && timeline.IsPlaying) //case zu langsam - keine animationen
            {

                circle_group.Animation.Timeline.Stop();

                circle_group.SetScaleFull(zoom_level,zoom_level,trans_x,trans_y);

                Hyena.Log.Information("Quick Zoom - progress: "+timeline.Progress);

                behave.RemoveAll();
                //timeline.Stop();
                //zoom actor in andere richtung - warum center 0,0 geht weiß ich nicht ..
                foreach (Actor a in circle_group)
                    a.SetScale(1.0/zoom_level,1.0/zoom_level);

            }else
            {

                Hyena.Log.Information("Animated Zoom.");
                circle_group.SetScaleFull(old_zoom_level,old_zoom_level,trans_x,trans_y);

                circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,duration,new String[]{"scale-x"},new GLib.Value(zoom_level));
                circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,duration,new String[]{"scale-y"},new GLib.Value(zoom_level));

                behave.RemoveAll();
//                 Nur circle scaling wenn zu langsam
//                if (timeline.Progress < 0.2 && timeline.IsPlaying){
//                    foreach (Actor a in circle_group)
//                      a.SetScale(1.0/zoom_level,1.0/zoom_level);
//                }
//                else
//                {
                    timeline.Duration = duration;
                    behave = new BehaviourScale(alpha,1.0f/old_zoom_level,1.0f/old_zoom_level,1.0f/zoom_level,1.0f/zoom_level);
    
                    //neues behaviour an die circles andwenden
                    foreach (Actor a in circles){
                        //a.SetScale(1.0f/old_zoom_level,1.0f/old_zoom_level); zu langsam
                        behave.Apply(a);
                    }
//                }
            }
            timeline.Stop();
            timeline.Start();
        }
        void HandleStateZoomAnimation (object o, ScrollEventArgs args)
        {

            Hyena.Log.Information("State Zoom Animation");

            //Mouse position
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //Beim rauszoomen bildmittelpunkt, is irgendwie intuitiver - wenn ma rein/rauszoom doch nicht - wieder weg
//            if (args.Event.Direction == ScrollDirection.Down)
//            {
//                mouse_x = Stage.Width /2.0f;
//                mouse_y = Stage.Height /2.0f;
//            }

            //Transformed position
            float trans_x = 0, trans_y = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x, out trans_y);

            //raus zoomen
            circle_group.SetScale(1.0,1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x_unif, out trans_y_unif);

            float pos_x = circle_group.X + (trans_x_unif - trans_x);
            float pos_y = circle_group.Y + (trans_y_unif - trans_y);

            //punkt auf objekt schieben
            circle_group.SetPosition(pos_x, pos_y);

            circle_group.SetScale(zoom_level,zoom_level);
            circle_group.SetScaleFull(zoom_level,zoom_level,trans_x,trans_y);

            double old_zoom_level = zoom_level;

            //rein zoomen
            switch (args.Event.Direction)
            {
            case ScrollDirection.Up:
                zoom_level *= zoom_level_mult;
                break;

            case ScrollDirection.Down:
                zoom_level /= zoom_level_mult;
                break;
            }

            circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-x"},new GLib.Value(zoom_level));
            circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-y"},new GLib.Value(zoom_level));

            double c_x, c_y;
            Actor c = circles[0];

            c.GetScale(out c_x, out c_y);


            behave.RemoveAll();
            behave = new BehaviourScale(alpha,1.0f/old_zoom_level,1.0f/old_zoom_level,1.0f/zoom_level,1.0f/zoom_level);

            //neues behaviour an die circles andwenden
            foreach (Actor a in circles){
                //a.SetScale(1.0f/old_zoom_level,1.0f/old_zoom_level); zu langsam
                behave.Apply(a);
            }
            timeline.Stop();
            timeline.Start();
        }
        void HandleStateZoom (object o, ScrollEventArgs args)
        {

            Hyena.Log.Information("State Zoom");

            //Mouse position
            float mouse_x = 0, mouse_y = 0;
            EventHelper.GetCoords(args.Event, out mouse_x, out mouse_y);

            //Transformed position
            float trans_x = 0, trans_y = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x, out trans_y);

            //raus zoomen
            circle_group.SetScale(1.0,1.0);

            float trans_x_unif = 0, trans_y_unif = 0;
            circle_group.TransformStagePoint(mouse_x,mouse_y,out trans_x_unif, out trans_y_unif);

            float pos_x = circle_group.X + (trans_x_unif - trans_x);
            float pos_y = circle_group.Y + (trans_y_unif - trans_y);

            //punkt auf objekt schieben
            circle_group.SetPosition(pos_x, pos_y);

            //rein zoomen
            switch (args.Event.Direction)
            {
            case ScrollDirection.Up:
                zoom_level *= zoom_level_mult;
                break;

            case ScrollDirection.Down:
                zoom_level /= zoom_level_mult;
                break;
            }

            circle_group.SetScaleFull(zoom_level,zoom_level,trans_x,trans_y);

            Hyena.Log.Information("Zoom Level: "+zoom_level);

            //zoom actor in andere richtung - warum center 0,0 geht weiß ich nicht ..
            foreach (Actor a in circle_group)
                a.SetScaleFull(1.0/zoom_level,1.0/zoom_level,0,0);

        }
        void HandleViewportScroll (object o, ScrollEventArgs args)
        {
            //aktuelle Position
            float x=0;// = circle_group.X;

            //clip ändern damit auf beiden seiten SCROLL_AMOUNT mehr platz ist zum scrollen in beide richtungen
            //circle_group.SetClip(-x-SCROLL_AMOUNT,0,Stage.Width+2*SCROLL_AMOUNT,circle_group.Height);

            double scale_x,scale_y;
            double c_x, c_y;
            circle_group.GetScale(out scale_x, out scale_y);

            Actor c = circles[0];

            c.GetScale(out c_x, out c_y);

            switch (args.Event.Direction)
            {
            case ScrollDirection.Up:
                Hyena.Log.Information("Scroll UP: "+x);
                x = -SCROLL_AMOUNT;

                //x-y scale mit animation
                circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-x"},new GLib.Value(scale_x*2.0));
                circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-y"},new GLib.Value(scale_y*2));

                //neues behaviour für runterscalen der kreise wenn die group hochgescaled wird-> bleiben gleich groß
                behave.RemoveAll();
                behave = new BehaviourScale(alpha,c_x,c_y,c_x/2.0,c_y/2.0);

               break;

            case ScrollDirection.Down:
                Hyena.Log.Information("Scroll DOWN: "+x);
                x = SCROLL_AMOUNT;

                circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-x"},new GLib.Value(scale_x*0.5));
                circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]{"scale-y"},new GLib.Value(scale_y*0.5));

                //umgekehrt wie im anderen case
                behave.RemoveAll();
                behave = new BehaviourScale(alpha,c_x,c_y,c_x/0.5,c_y/0.5);

                break;
            }

            //neues behaviour an die circles andwenden
            foreach (Actor a in circles)
                    behave.Apply(a);
                timeline.Start();
            /*
            foreach (Actor a in circle_group)
            {
                a.Animatev((ulong)AnimationMode.EaseOutCubic,1000,new String[]
            }*/
            //circle_group.Depth += x;
            //Bereich abschneiden für die enden - nur zwischen [Stage.Width - circle_group.Width, 0]
            //x = (float)Math.Min(Math.Max(x, Stage.Width - circle_group.Width),0.0);

            //Animation für scroll
            //circle_group.Animatev((ulong)AnimationMode.EaseOutCubic,500,new String[]{"x"},new GLib.Value(x));

        }


        void StageChanged (object o, AllocationChangedArgs args)
        {
            Hyena.Log.Information("Stage size changed.");
            //SetZoomCenter ((float)(Stage.Width/2.0),(float)(Stage.Height/2.0));
            //circle_group.SetSize(Stage.Width,Stage.Height);
            //circle_group.SetClip(0,0,circle_group.Width,circle_group.Height);
        }

    }
}

