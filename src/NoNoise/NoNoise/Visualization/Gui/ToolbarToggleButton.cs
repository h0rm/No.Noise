// 
// ToolbarToggleButton.cs
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
using Cairo;
using Clutter;

namespace NoNoise.Visualization.Gui
{
    public class ToolbarToggleButton : ToolbarButton
    {
        public enum State {On, Off};

        protected State state = State.Off;

        public ToolbarToggleButton (String text, StyleSheet scheme, Border borders,
                                    uint width, uint height) : base (text, scheme, borders, width, height)
        {
            CairoTexture texture = new CairoTexture (width, height);
            StyleSheet s = scheme;
            s.Foreground = scheme.Background;
            s.Background = scheme.Foreground;

            Style = s;

            Draw (texture);

            Style = scheme;
            textures.Add (texture);
            texture.Hide ();
            this.Add (texture);

            Hyena.Log.Information ("Textures " + textures.Count);
            InitializeHandlers ();
        }

        private void InitializeHandlers ()
        {
            ButtonPressEvent += HandleButtonPressEvent;
        }


        private void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            state = state == State.On ? State.Off : State.On;
            OnStateChanged ();
        }

        private void OnStateChanged ()
        {
            Hyena.Log.Information ("Textures " + textures.Count);
            textures[0].Hide ();
            textures[1].Hide ();


            if (state == State.Off)
                textures[0].Show ();
            else
                textures[1].Show ();
        }
    }
}

