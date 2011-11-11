// 
// StyleSheet.cs
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
using Cairo;

namespace NoNoise.Visualization.Gui
{
    /// <summary>
    /// Helper struct which represents a font
    /// </summary>
    public struct Font
    {
        /// <summary>
        /// Font family
        /// </summary>
        public String Family {
            get;
            set;
        }

        /// <summary>
        /// Font slant (for example italic)
        /// </summary>
        public FontSlant Slant {
            get;
            set;
        }

        /// <summary>
        /// Font weight (for example bold)
        /// </summary>
        public FontWeight Weight {
            get;
            set;
        }

        /// <summary>
        /// Font size
        /// </summary>
        public double Size {
            get;
            set;
        }

        /// <summary>
        /// Font color
        /// </summary>
        public Color Color {
            get;
            set;
        }
    }

    /// <summary>
    /// Style sheet which is used to specify the appearance of all gui elements
    /// </summary>
    public struct StyleSheet
    {
        /// <summary>
        /// Foreground color
        /// </summary>
        public Color Foreground {
            get;
            set;
        }

        /// <summary>
        /// Background color
        /// </summary>
        public Color Background {
            get;
            set;
        }

        /// <summary>
        /// Standard font
        /// </summary>
        public Font Standard {
            get;
            set;
        }

        /// <summary>
        /// Font for highlighted text
        /// </summary>
        public Font Highlighted {
            get;
            set;
        }

        /// <summary>
        /// Color of the selection infobox
        /// </summary>
        public Color Selection {
            get;
            set;
        }

        /// <summary>
        /// Font for the subtitles in the infobox
        /// </summary>
        public Font Subtitle {
            get;
            set;
        }

        /// <summary>
        /// Boarder color of all gui elements
        /// </summary>
        public Color Border {
            get;
            set;
        }

        /// <summary>
        /// Boarder color of the selection infobox
        /// </summary>
        public Color SelectionBoarder {
            get;
            set;
        }

        /// <summary>
        /// Boarder size of all gui elements
        /// </summary>
        public double BorderSize {
            get;
            set;
        }

    }
}

