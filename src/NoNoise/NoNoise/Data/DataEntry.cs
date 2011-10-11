using System;
using System.Collections.Generic;

namespace NoNoise.Data
{
    /// <summary>
    /// This class encapsulates the PCA coordinates of a track with its banshee_id
    /// and additional data.
    /// </summary>
	public class DataEntry
    {
		private int id;			//unique id for each track
		private double pca_x;	//between 0...1
		private double pca_y;	//between 0...1
		
		public DataEntry (int id, double pca_x, double pca_y)
		{
			this.id = id;
			this.pca_x = pca_x;
			this.pca_y = pca_y;
		}
		
		/// <summary>
		/// unique id for each track
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

