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
    /// <summary>
    /// Gui toolbar button element which can be toggled
    /// </summary>
    public class ToolbarToggleButton : ToolbarButton
    {
        public enum State {On, Off};

        protected State state = State.Off;
        private bool toggle;

        /// <summary>
        /// Checks if the state is On or Off.
        /// </summary>
        public bool IsOn {
            get { return state == ToolbarToggleButton.State.On; }
        }

        public ToolbarToggleButton (String text_one, String text_two, bool auto_toggle, StyleSheet scheme, Border borders,
                                    uint width, uint height) : base (text_one, scheme, borders, width, height)
        {
            toggle = auto_toggle;

            CairoTexture texture = new CairoTexture (width, height);
            StyleSheet s = scheme;
            s.Foreground = scheme.Background;
            s.Background = scheme.Foreground;
            s.Standard = new Font (scheme.Standard.Family, scheme.Standard.Slant,
                                   scheme.Standard.Weight, scheme.Standard.Size, scheme.Background);
            s.Border = scheme.Foreground;
            Style = s;
            Text = text_two;
            Draw (texture);

            Style = scheme;
            Text = text_one;
            textures.Add (texture);
            texture.Hide ();
            this.Add (texture);

            InitializeHandlers ();
        }

        /// <summary>
        /// Initializes the handlers
        /// </summary>
        private void InitializeHandlers ()
        {
            ButtonPressEvent += HandleButtonPressEvent;
        }

        /// <summary>
        /// Handler which is called when the button is pressed (i.e. toggled).
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="args">
        /// A <see cref="ButtonPressEventArgs"/>
        /// </param>
        private void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (toggle)
                ToggleState ();
        }

        /// <summary>
        /// Function which is called when the state is changed.
        /// </summary>
        private void OnStateChanged ()
        {
            textures[0].Hide ();
            textures[1].Hide ();


            if (state == State.Off)
                textures[0].Show ();
            else
                textures[1].Show ();
        }

        /// <summary>
        /// Toggles the state.
        /// </summary>
        public void ToggleState ()
        {
            state = state == State.On ? State.Off : State.On;
            OnStateChanged ();
        }

        /// <summary>
        /// Sets the state.
        /// </summary>
        /// <param name="on"> true for on, false for off
        /// A <see cref="System.Boolean"/>
        /// </param>
        public void SetState (bool on)
        {
            state = on ? State.On : State.Off;
            OnStateChanged ();
        }

    }
}

