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

