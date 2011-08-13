// 
// MyWidget.cs
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
using Gtk;
using Cairo;


namespace Banshee.Foo
{

     class MyWidget : Gtk.DrawingArea {
        public MyWidget ()
        {
          //  ClutterHelper.Init();
          //  ClutterHelper.Main();
        }


        static void DrawRoundedRectangle (Cairo.Context gr, double x, double y, double width, double height, double radius)
        {
            gr.Save ();
    
            if ((radius > height / 2) || (radius > width / 2))
                radius = Min (height / 2, width / 2);
    
            gr.MoveTo (x, y + radius);
            gr.Arc (x + radius, y + radius, radius, Math.PI, -Math.PI / 2);
            gr.LineTo (x + width - radius, y);
            gr.Arc (x + width - radius, y + radius, radius, -Math.PI / 2, 0);
            gr.LineTo (x + width, y + height - radius);
            gr.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
            gr.LineTo (x + radius, y + height);
            gr.Arc (x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
    
            gr.ClosePath ();
            gr.Restore ();
        }
        static double Min (params double[] arr)
        {
            int minp = 0;
    
            for (int i = 1; i < arr.Length; i++)
                if (arr[i] < arr[minp])
                    minp = i;
    
            return arr[minp];
        }

        /// <summary>
        /// This function is called to when the widget has to be updated -> fancy drawing
        /// </summary>
        /// <param name="args">
        /// A <see cref="Gdk.EventExpose"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        protected override bool OnExposeEvent (Gdk.EventExpose args)
        {
            using (Context g = Gdk.CairoHelper.Create (args.Window)){
                g.Translate (250, 250);
                g.Rotate (0.2);
                g.Translate (-250, -250);

                DrawRoundedRectangle (g, 40, 40, 140, 140, 80);
                DrawRoundedRectangle (g, 320, 320, 140, 140, 80);
                DrawRoundedRectangle (g, 40, 320, 140, 140, 80);
                DrawRoundedRectangle (g, 320, 40, 140, 140, 80);
                DrawRoundedRectangle (g, 150, 180, 200, 140, 30);

                g.Color = new Color (1, 0.6, 0, 1);
                g.FillPreserve ();
                g.Color = new Color (1, 0.8, 0, 1);
                g.LineWidth = 8;
                g.Stroke ();
            }
            return true;
        }
    }
}

