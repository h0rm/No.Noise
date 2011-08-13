using System;
using System.Collections.Generic;

namespace Banshee.Cluttertest
{
	public class TrackInfo
	{
		
	}
	
	public class DataValue
	{
		
	}
	
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
		}
		
		/// <summary>
		/// y pca value between 0...1
		/// </summary>
		public double Y
		{
			get { return pca_y; }	
		}
		
		/// <summary>
		/// additional information like color
		/// </summary>
		public DataValue Value
		{
			get { return val; }	
		}
	}
	
	interface DataHandler
	{
		List<DataEntry> GetData ();		//returns a list of all tracks
		TrackInfo GetTrackInfo (int ID);	//returns all info to a given track
	}
}

