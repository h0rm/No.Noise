using System;
using System.Collections.Generic;

namespace Banshee.NoNoise.Data
{
    /// <summary>
    /// This class encapsulates track information used for the NoNoise plug-in.
    /// </summary>
	public class TrackInfo
	{
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">
        /// The banshee_id
        /// </param>
        /// <param name="artist">
        /// Artist name
        /// </param>
        /// <param name="title">
        /// Song title
        /// </param>
        /// <param name="album">
        /// Album title
        /// </param>
        /// <param name="duration">
        /// Song duration in seconds
        /// </param>
        public TrackInfo (int id, string artist, string title, string album, int duration)
        {
            ID = id;
            Artist = artist;
            Title = title;
            Album = album;
            Duration = duration;
        }

        /// <summary>
        /// The banshee_id
        /// </summary>
        public int ID
        {
            get;
            private set;
        }

        /// <summary>
        /// Artist name
        /// </summary>
        public string Artist
        {
            get;
            set;
        }

        /// <summary>
        /// Song title
        /// </summary>
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// Album title
        /// </summary>
        public string Album
        {
            get;
            set;
        }

        /// <summary>
        /// Song duration in seconds
        /// </summary>
        public int Duration
        {
            get;
            set;
        }

        public override string ToString ()
        {
            return string.Format ("[NNTrackInfo: ID={0}, Artist={1}, Title={2}, Album={3}, Duration={4}]",
                                  ID, Artist, Title, Album, Duration);
        }
	}

    /// <summary>
    /// This class encapsulates additional information of a track used for the
    /// visualization.
    /// </summary>
	public class DataValue
	{
		
	}

    /// <summary>
    /// This class encapsulates the PCA coordinates of a track with its banshee_id
    /// and additional data.
    /// </summary>
	public class DataEntry
	{
		private int id;			//unique id for each track
		private double pca_x;	//between 0...1
		private double pca_y;	//between 0...1
		private DataValue val;  //additional information like color
		
		public DataEntry (int id, double pca_x, double pca_y, DataValue val)
		{
			this.id = id;
			this.pca_x = pca_x;
			this.pca_y = pca_y;
			this.val = val;
		}
		
		/// <summary>
		/// unique id for each track
		/// </summary>
		public int ID
		{
			get { return id; }
		}
		
		/// <summary>
		/// x pca value between 0...1
		/// </summary>
		public double X
		{
			get { return pca_x; }
            set { pca_x = value; }
		}
		
		/// <summary>
		/// y pca value between 0...1
		/// </summary>
		public double Y
		{
			get { return pca_y; }
            set { pca_y = value; }
		}
		
		/// <summary>
		/// additional information like color
		/// </summary>
		public DataValue Value
		{
			get { return val; }
            set { val = value; }
		}

        public override string ToString ()
        {
            return string.Format ("[DataEntry: ID={0}, X={1}, Y={2}, Value={3}]", ID, X, Y, Value);
        }
	}
	
	interface DataHandler
	{
		List<DataEntry> GetData ();		//returns a list of all tracks
		TrackInfo GetTrackInfo (int ID);	//returns all info to a given track
	}
}

