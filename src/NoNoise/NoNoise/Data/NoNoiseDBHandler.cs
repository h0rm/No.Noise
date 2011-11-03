// 
// NoNoiseDBHandler.cs
// 
// Author:
//   thomas <${AuthorEmail}>
// 
// Copyright (c) 2011 thomas
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
using System.Data;
using Mono.Data.Sqlite;
using Hyena;

namespace NoNoise.Data
{
    public class NoNoiseDBHandler
    {
        #region Constants
        // change connection string to final db !?
        private readonly string CONNECTION_STRING = "Data Source=nonoise.db,version=3";

        private readonly string CREATE_TABLE_MIRDATA =
            "CREATE TABLE IF NOT EXISTS MIRData (banshee_id INTEGER NOT NULL, mean CLOB NOT NULL, sqrmean CLOB NOT NULL, " +
            "median CLOB NOT NULL, min CLOB NOT NULL, max CLOB NOT NULL, id INTEGER PRIMARY KEY)";
        private readonly string CREATE_TABLE_PCADATA =
            "CREATE TABLE IF NOT EXISTS PCAData (banshee_id INTEGER NOT NULL, id INTEGER PRIMARY KEY, pca_x DOUBLE NOT NULL, " +
            "pca_y DOUBLE NOT NULL)";
        private readonly string CREATE_TABLE_TRACKDATA =
            "CREATE TABLE IF NOT EXISTS TrackData (banshee_id INTEGER NOT NULL, id INTEGER PRIMARY KEY)";

        private readonly string SELECT_MIRDATA_COUNT = "SELECT COUNT(*) FROM MIRData";
        private readonly string SELECT_PCADATA_COUNT = "SELECT COUNT(*) FROM PCAData";
        private readonly string SELECT_TRACKDATA_COUNT = "SELECT COUNT(*) FROM TrackData";

        private readonly string SYNCH_MIRDATA = "DELETE FROM MIRData WHERE banshee_id NOT IN " +
            "(SELECT banshee_id FROM TrackData)";
        private readonly string SYNCH_PCADATA = "DELETE FROM PCAData WHERE banshee_id NOT IN " +
            "(SELECT banshee_id FROM TrackData)";
        #endregion

        #region Members
        private IDbConnection dbcon = null;
        #endregion

        /// <summary>
        /// Constructor. Sets the connection to the database and creates the
        /// schema if it doesn't exist.
        /// </summary>
        public NoNoiseDBHandler ()
        {
            dbcon = (IDbConnection) new SqliteConnection (CONNECTION_STRING);
            CreateSchema ();
        }

        /// <summary>
        /// Creates the database schema, if it doesn't exist.
        /// </summary>
        private void CreateSchema ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = CREATE_TABLE_MIRDATA;
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_PCADATA;
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_TRACKDATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Schema creation failed", e);
                throw new Exception ("Unable to create DB schema!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        #region MIRData

        public bool InsertVectors (Mirage.Vector mean, Mirage.Vector sqrmean, Mirage.Vector median,
                                   Mirage.Vector min, Mirage.Vector max, int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("INSERT INTO MIRData (banshee_id, mean, sqrmean, median, min, max) " +
                    "VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}')", bid, DataParser.MirageVectorToString (mean),
                    DataParser.MirageVectorToString (sqrmean), DataParser.MirageVectorToString (median),
                    DataParser.MirageVectorToString (min), DataParser.MirageVectorToString (max));
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Mirage.Vectors insert failed", e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }

            return true;
        }

        /// <summary>
        /// Parses Mirage.Vector's from the database and returns them.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary<System.Int32, Mirage.Vector>"/> containing
        /// all vectors in the database mapped to their corresponding banshee_id.
        /// </returns>
        public Dictionary<int, Mirage.Vector> GetMirageMeanVectors ()
        {
            Dictionary<int, Mirage.Vector> ret = new Dictionary<int, Mirage.Vector> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT mean, id, banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    Mirage.Vector vec = DataParser.ParseMirageVector (reader.GetString (0));
                    int bid = reader.GetInt32 (2);
                    if (vec != null)
                        ret.Add (bid, vec);
                    else {
                        Log.WarningFormat ("NoNoise/DBNull - Vector with id {0} is null!", reader.GetInt32 (1));
                        Log.Debug (reader.GetString (0));
                    }
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Mirage.Vector read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        public Dictionary<int, Mirage.Vector> GetMirageSquaredMeanVectors ()
        {
            Dictionary<int, Mirage.Vector> ret = new Dictionary<int, Mirage.Vector> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT sqrmean, id, banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    Mirage.Vector vec = DataParser.ParseMirageVector (reader.GetString (0));
                    int bid = reader.GetInt32 (2);
                    if (vec != null)
                        ret.Add (bid, vec);
                    else {
                        Log.WarningFormat ("NoNoise/DBNull - Vector with id {0} is null!", reader.GetInt32 (1));
                        Log.Debug (reader.GetString (0));
                    }
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Mirage.Vector read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        public Dictionary<int, Mirage.Vector> GetMirageMedianVectors ()
        {
            Dictionary<int, Mirage.Vector> ret = new Dictionary<int, Mirage.Vector> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT median, id, banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    Mirage.Vector vec = DataParser.ParseMirageVector (reader.GetString (0));
                    int bid = reader.GetInt32 (2);
                    if (vec != null)
                        ret.Add (bid, vec);
                    else {
                        Log.WarningFormat ("NoNoise/DBNull - Vector with id {0} is null!", reader.GetInt32 (1));
                        Log.Debug (reader.GetString (0));
                    }
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Mirage.Vector read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        public Dictionary<int, Mirage.Vector> GetMirageMinVectors ()
        {
            Dictionary<int, Mirage.Vector> ret = new Dictionary<int, Mirage.Vector> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT min, id, banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    Mirage.Vector vec = DataParser.ParseMirageVector (reader.GetString (0));
                    int bid = reader.GetInt32 (2);
                    if (vec != null)
                        ret.Add (bid, vec);
                    else {
                        Log.WarningFormat ("NoNoise/DBNull - Vector with id {0} is null!", reader.GetInt32 (1));
                        Log.Debug (reader.GetString (0));
                    }
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Mirage.Vector read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        public Dictionary<int, Mirage.Vector> GetMirageMaxVectors ()
        {
            Dictionary<int, Mirage.Vector> ret = new Dictionary<int, Mirage.Vector> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT max, id, banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    Mirage.Vector vec = DataParser.ParseMirageVector (reader.GetString (0));
                    int bid = reader.GetInt32 (2);
                    if (vec != null)
                        ret.Add (bid, vec);
                    else {
                        Log.WarningFormat ("NoNoise/DBNull - Vector with id {0} is null!", reader.GetInt32 (1));
                        Log.Debug (reader.GetString (0));
                    }
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Mirage.Vector read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Prints debug information for a database string which represents a
        /// matrix.
        /// </summary>
        /// <param name="matrix">
        /// A <see cref="System.String"/> representation of a matrix
        /// </param>
        private void CheckMatrix (string matrix)
        {
            string[] rows;
            Log.Debug ("MatrixRows: " + (rows = matrix.Split ('\n')).Length);
            foreach (string r in rows) {
                Log.Debug ("MatrixCols: " + r.Split (',').Length);
            }
        }

        public SortedList<int, int> GetMirDataKeyList ()
        {
            SortedList<int, int> ret = new SortedList<int, int> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    int bid = reader.GetInt32 (0);
                    ret.Add (bid, bid);
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - MIRData key list read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Queries the database if it contains MIRData for the track with the
        /// given banshee_id.
        /// </summary>
        /// <param name="bid">
        /// The banshee_id
        /// </param>
        /// <returns>
        /// True if the banshee_id is already in the database. False otherwise.
        /// </returns>
        public bool ContainsMirDataForTrack (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("SELECT id FROM MIRData WHERE banshee_id = '{0}'", bid);
                return (dbcmd.ExecuteScalar () != null);
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - Contains MIRData query failed for Banshee_id: {0}", bid), e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Removes the MIR data for the track with the given banshee_id,
        /// if it is in the database.
        /// </summary>
        /// <param name='bid'>
        /// The banshee_id
        /// </param>
        public void RemoveMirDataForTrack (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("DELETE FROM MIRData WHERE banshee_id = '{0}'", bid);
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - Remove MIRData query failed for Banshee_id: {0}", bid), e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }
        #endregion

        #region PCAData

        /// <summary>
        /// Inserts a list of DataEntry's into the PCAData table within a single transaction.
        /// </summary>
        /// <param name="coords">
        /// A <see cref="List<DataEntry>"/> containing the data
        /// </param>
        /// <returns>
        /// True if all data was successfully inserted. False otherwise.
        /// </returns>
        public bool InsertPcaCoordinates (List<DataEntry> coords)
        {
            bool succ = true;
            IDbTransaction trans = null;
            try {
                dbcon.Open ();
                trans = dbcon.BeginTransaction ();

                foreach (DataEntry de in coords) {
                    succ &= InsertPcaCoordinate (de);
                }

                trans.Commit ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - PCA coordinates insert failed", e);
                succ = false;
                if (trans != null)
                    trans.Rollback ();
            } finally {
                if (dbcon != null)
                    dbcon.Close ();
            }
            return succ;
        }

        /// <summary>
        /// Inserts one DataEntry into the PCAData table.
        /// The database connection has to be opened before calling this method
        /// and should be closed afterwards.
        /// </summary>
        /// <param name="de">
        /// The <see cref="DataEntry"/> to be inserted
        /// </param>
        /// <returns>
        /// True if the DataEntry was successfully inserted. False otherwise.
        /// </returns>
        private bool InsertPcaCoordinate (DataEntry de)
        {
            IDbCommand dbcmd = null;
            try {
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "INSERT INTO PCAData (banshee_id, pca_x, pca_y) VALUES (@bid, @x, @y)";

                SqliteParameter id = new SqliteParameter ("@bid", de.ID);
                SqliteParameter x = new SqliteParameter ("@x", de.X);
                SqliteParameter y = new SqliteParameter ("@y", de.Y);
                x.DbType = DbType.Double;
                y.DbType = DbType.Double;
                dbcmd.Parameters.Add (id);
                dbcmd.Parameters.Add (x);
                dbcmd.Parameters.Add (y);
                dbcmd.Prepare ();

                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - DataEntry insert failed for DE: {0}", de), e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
            }

            return true;
        }

        /// <summary>
        /// Gets a list with all PCA coordinates stored in the database.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary<int, DataEntry>"/> containing all PCA coordinates
        /// from the database.
        /// </returns>
        public Dictionary<int, DataEntry> GetPcaCoordinatesDictionary ()
        {
            Dictionary<int, DataEntry> ret = new Dictionary<int, DataEntry> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT pca_x, pca_y, banshee_id FROM PCAData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    int bid = reader.GetInt32 (2);
                    DataEntry de = new DataEntry (bid, reader.GetDouble (0), reader.GetDouble (1));
                    ret.Add (bid, de);
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - PCA data read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Gets a list with all PCA coordinates stored in the database.
        /// </summary>
        /// <returns>
        /// A <see cref="List<DataEntry>"/> containing all PCA coordinates
        /// from the database.
        /// </returns>
        public List<DataEntry> GetPcaCoordinates ()
        {
            List<DataEntry> ret = new List<DataEntry> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT pca_x, pca_y, banshee_id FROM PCAData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    int bid = reader.GetInt32 (2);
                    DataEntry de = new DataEntry (bid, reader.GetDouble (0), reader.GetDouble (1));
                    ret.Add (de);
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - PCA data read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Removes the PCA data for the track with the given banshee_id,
        /// if it is in the database.
        /// </summary>
        /// <param name='bid'>
        /// The banshee_id
        /// </param>
        public void RemovePcaDataForTrack (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("DELETE FROM PCAData WHERE banshee_id = '{0}'", bid);
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - Remove PCAData query failed for Banshee_id: {0}", bid), e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }
        #endregion

        #region TrackData

        public bool InsertTrackIDs (List<int> ids)
        {
            SortedList<int, int> in_db = GetTrackDataKeyList ();
            if (in_db == null)
                return false;

            bool succ = true;
            IDbTransaction trans = null;
            try {
                dbcon.Open ();
                trans = dbcon.BeginTransaction ();

                foreach (int bid in ids) {
                    if (!in_db.ContainsKey (bid))
                        succ &= InsertTrackIDInTransaction (bid);
                }

                trans.Commit ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - PCA coordinates insert failed", e);
                succ = false;
                if (trans != null)
                    trans.Rollback ();
            } finally {
                if (dbcon != null)
                    dbcon.Close ();
            }
            return succ;
        }

        private bool InsertTrackIDInTransaction (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("INSERT INTO TrackData (banshee_id) VALUES ('{0}')", bid);

                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - TrackInfo insert failed for bid: {0}", bid), e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
            }

            return true;
        }

        public bool InsertTrackID (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "INSERT INTO TrackData (banshee_id) VALUES (@bid)";
                SqliteParameter id = new SqliteParameter ("@bid", bid);
                dbcmd.Parameters.Add (id);
                dbcmd.Prepare ();

                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - TrackInfo insert failed for bid: {0}", bid), e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }

            return true;
        }

        /// <summary>
        /// Queries the database if it contains TrackData for the track with
        /// the given banshee_id.
        /// </summary>
        /// <param name="bid">
        /// The banshee_id
        /// </param>
        /// <returns>
        /// True if the banshee_id is already in the database. False otherwise.
        /// </returns>
        public bool ContainsInfoForTrack (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("SELECT id FROM TrackData WHERE banshee_id = '{0}'", bid);
                return (dbcmd.ExecuteScalar () != null);
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - Contains TrackInfo query failed for banshee_id: {0}", bid), e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Gets a list with all banshee_id's from the TrackData table.
        /// </summary>
        /// <returns>
        /// A <see cref="List<System.Int32>"/> containing all banshee_id's
        /// in the TrackData table
        /// </returns>
        public SortedList<int, int> GetTrackDataKeyList ()
        {
            SortedList<int, int> ret = new SortedList<int, int> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "SELECT banshee_id FROM TrackData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader ();
                while (reader.Read ()) {
                    int bid = reader.GetInt32 (0);
                    ret.Add (bid, bid);
                }

                return ret;
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - TrackData key list read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Removes the track data for the track with the given banshee_id,
        /// if it is in the database.
        /// </summary>
        /// <param name='bid'>
        /// The banshee_id
        /// </param>
        public void RemoveTrackDataForTrack (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = string.Format ("DELETE FROM TrackData WHERE banshee_id = '{0}'", bid);
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception (string.Format ("NoNoise/DB - Remove TrackData query failed for Banshee_id: {0}", bid), e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }
        #endregion

        /// <summary>
        /// Updates all tables using the TrackData table as reference. After
        /// calling this method there won't be any entries in the other tables
        /// with banshee_id's which are not in TrackData. The opposit might
        /// not be the case (i.e. there might be entries missing in the other
        /// tables).
        /// </summary>
        public void SynchTablesWithTrackData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = SYNCH_MIRDATA;
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = SYNCH_PCADATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Synching tables failed.", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        #region Info Queries

        /// <summary>
        /// Gets the number of records in the MIRData table.
        /// </summary>
        /// <returns>
        /// The number of records in the MIRData table.
        /// </returns>
        public int GetMirDataCount ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = SELECT_MIRDATA_COUNT;
                return int.Parse (dbcmd.ExecuteScalar ().ToString ());
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - MIRData COUNT query failed", e);
                return -1;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Gets the number of records in the PCAData table.
        /// </summary>
        /// <returns>
        /// The number of records in the PCAData table.
        /// </returns>
        public int GetPcaDataCount ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = SELECT_PCADATA_COUNT;
                return int.Parse (dbcmd.ExecuteScalar ().ToString ());
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - PCAData COUNT query failed", e);
                return -1;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Gets the number of records in the TrackData table.
        /// </summary>
        /// <returns>
        /// The number of records in the TrackData table.
        /// </returns>
        public int GetTrackDataCount ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = SELECT_TRACKDATA_COUNT;
                return int.Parse (dbcmd.ExecuteScalar ().ToString ());
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - TrackData COUNT query failed", e);
                return -1;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }
        #endregion

        #region Clear Tables

        /// <summary>
        /// Clears the MIRData table of the database.
        /// </summary>
        public void ClearMirData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "DROP TABLE MIRData";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_MIRDATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Clear MIR Data failed", e);
                throw new Exception ("Clear MIR Data failed!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Clears the PCAData table of the database.
        /// </summary>
        public void ClearPcaData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "DROP TABLE PCAData";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_PCADATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Clear PCA Data failed", e);
                throw new Exception ("Clear PCA Data failed!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }

        /// <summary>
        /// Clears the TrackData table of the database.
        /// </summary>
        public void ClearTrackData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open ();
                dbcmd = dbcon.CreateCommand ();

                dbcmd.CommandText = "DROP TABLE TrackData";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_TRACKDATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception ("NoNoise/DB - Clear Track Data failed", e);
                throw new Exception ("Clear Track Data failed!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose ();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close ();
            }
        }
        #endregion
    }
}

