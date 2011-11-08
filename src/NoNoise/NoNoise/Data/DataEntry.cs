//
// DataEntry.cs
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

namespace NoNoise.Data
{
    /// <summary>
    /// This class encapsulates the PCA coordinates of a track with its banshee_id.
    /// </summary>
	public class DataEntry
    {
		private int id;			// banshee id for each track
		private double pca_x;	// between 0...1
		private double pca_y;	// between 0...1

        /// <summary>
        /// Initializes a new instance of the <see cref="NoNoise.Data.DataEntry"/> class.
        /// </summary>
        /// <param name='id'>
        /// The banshee id.
        /// </param>
        /// <param name='pca_x'>
        /// The X value of the PCA. Has to be in [0..1].
        /// </param>
        /// <param name='pca_y'>
        /// The Y value of the PCA. Has to be in [0..1].
        /// </param>
		public DataEntry (int id, double pca_x, double pca_y)
		{
			this.id = id;
			this.pca_x = pca_x;
			this.pca_y = pca_y;
		}
		
		/// <summary>
		/// banshee id for each track
		/// </summary>
		public int ID {
			get { return id; }
		}
		
		/// <summary>
		/// x pca value between 0...1
		/// </summary>
		public double X {
			get { return pca_x; }
            set { pca_x = value; }
		}
		
		/// <summary>
		/// y pca value between 0...1
		/// </summary>
		public double Y {
			get { return pca_y; }
            set { pca_y = value; }
		}

        public override string ToString ()
        {
            return string.Format ("[DataEntry: ID={0}, X={1}, Y={2}]", ID, X, Y);
        }
	}
}

