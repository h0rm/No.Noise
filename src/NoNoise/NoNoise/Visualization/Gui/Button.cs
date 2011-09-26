// 
// Button.cs
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

namespace NoNoise.Visualization.Gui
{
    public abstract class Button : Clutter.Group
    {
        protected List<CairoTexture> textures;
        protected uint texture_width, texture_height;

        public Button (uint width, uint height)
        {
            Reactive = true;

            texture_width = width;
            texture_height = height;
        }

        protected void Initialize ()
        {
            textures = new List<CairoTexture>();
            GenerateTextures ();

            foreach (CairoTexture t in textures) {
                t.Hide ();
                Add (t);
            }

            if (textures.Count > 0 )
                textures[0].Show ();
        }


        protected abstract void GenerateTextures ();


    }
}

