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
using System.Text;
using Mono.Data.SqliteClient;
using MathNet.Numerics.LinearAlgebra;
using Hyena.Data.Sqlite;
using Hyena;

namespace Banshee.NoNoise.Data
{
    public class NoNoiseDBHandler
    {
        #region Constants
        private readonly string connectionString = "URI=file:/home/thomas/test.db,version=3";
        private readonly string CREATE_TABLE_MIRDATA =
            "CREATE TABLE IF NOT EXISTS MIRData (banshee_id INTEGER, data CLOB, id INTEGER PRIMARY KEY)";
        private readonly string CREATE_TABLE_PCADATA =
            "CREATE TABLE IF NOT EXISTS PCAData (banshee_id INTEGER, id INTEGER PRIMARY KEY, pca_x DOUBLE, pca_y DOUBLE)";
        private readonly string CREATE_TABLE_TRACKDATA =
            "CREATE TABLE IF NOT EXISTS TrackData (album VARCHAR(32), artist VARCHAR(32), banshee_id INTEGER, duration INTEGER, id INTEGER PRIMARY KEY, title VARCHAR(32))";
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
            dbcon = (IDbConnection) new SqliteConnection(connectionString);
            CreateSchema ();
        }

        /// <summary>
        /// Creates the database schema, if it doesn't exist.
        /// </summary>
        private void CreateSchema ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = CREATE_TABLE_MIRDATA;
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_PCADATA;
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_TRACKDATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Schema creation failed", e);
                throw new Exception ("Unable to create DB schema!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }

        #region MIRData
        #region Math.Matrix
        /// <summary>
        /// Inserts a Math.Matrix into the database.
        /// </summary>
        /// <param name="m">
        /// The <see cref="Matrix"/> to be inserted
        /// </param>
        /// <param name="bid">
        /// The banshee_id of the corresponding track
        /// </param>
        /// <returns>
        /// True if the matrix was successfully inserted. False otherwise.
        /// </returns>
        public bool InsertMatrix (Matrix m, int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (banshee_id, data) VALUES ('{0}', '{1}')",
                                                  bid, MatrixToString (m));
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Matrix insert failed", e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }

            return true;
        }

        /// <summary>
        /// Inserts a Math.Matrix into the database with a given primary key.
        /// Updates the matrix in the database if the key already exists.
        /// </summary>
        /// <param name="m">
        /// The <see cref="Matrix"/> to be inserted
        /// </param>
        /// <param name="bid">
        /// The banshee_id
        /// </param>
        /// <param name="primaryKey">
        /// The primary key that should be used
        /// </param>
        /// <returns>
        /// True if the matrix was successfully inserted. False otherwise.
        /// </returns>
        public bool InsertMatrixPK (Matrix m, int bid, int primaryKey)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format ("SELECT id FROM MIRData WHERE id = '{0}'", primaryKey);
                if (dbcmd.ExecuteScalar () != null)
                    return UpdateMatrix (m, bid, primaryKey, dbcmd);

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (banshee_id, data, id) VALUES ('{0}', '{1}', '{2}')",
                                                  bid, MatrixToString (m), primaryKey);
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Matrix insert failed for id: " + primaryKey, e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }

            return true;
        }

        /// <summary>
        /// Updates a Math.Matrix for a given primary key. This method has to
        /// be called within an open database connection.
        /// </summary>
        /// <param name="m">
        /// The <see cref="Matrix"/> to be inserted
        /// </param>
        /// <param name="bid">
        /// The banshee_id
        /// </param>
        /// <param name="primaryKey">
        /// The primary key that should be used
        /// </param>
        /// <param name="dbcmd">
        /// A <see cref="IDbCommand"/> of an open connection
        /// </param>
        /// <returns>
        /// True if the matrix was successfully updated. False otherwise.
        /// </returns>
        private bool UpdateMatrix (Matrix m, int bid, int primaryKey, IDbCommand dbcmd)
        {
            Log.Debug("Foo1/DB - Updating id " + primaryKey);
            dbcmd.CommandText = string.Format("UPDATE MIRData SET data = '{0}', banshee_id = '{1}' WHERE id = '{2}'",
                                              MatrixToString (m), bid, primaryKey);
            dbcmd.ExecuteNonQuery ();

            return true;
        }

        /// <summary>
        /// Converts a Math.Matrix to a string representation with semicolons
        /// instead of commas.
        /// </summary>
        /// <param name="m">
        /// The <see cref="Matrix"/> to be converted
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> representation of the matrix
        /// </returns>
        private string MatrixToString (Matrix m)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            while (i < m.RowCount) {
                if (i == 0) {
                    sb.Append("[[");
                } else {
                    sb.Append(" [");
                }

                int j = 0;
                while (j < m.ColumnCount) {
                    if (j != 0) {
                        sb.Append(";");
                    }
                    sb.Append(m[i,j]);
                    j++;
                }
                if (i == (m.RowCount - 1)) {
                    sb.Append("]]");
                    break;
                }
                sb.AppendLine("]");
                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses Math.Matrix's from the database and returns them.
        /// </summary>
        /// <returns>
        /// A <see cref="List<Matrix>"/> with all matrices in the database
        /// </returns>
        public List<Matrix> GetMatrices ()
        {
            List<Matrix> ret = new List<Matrix> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "SELECT data FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader();
                while(reader.Read()) {
                    ret.Add (ParseMatrix (reader.GetString (0)));
                }

                return ret;
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Matrix read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }

        /// <summary>
        /// Parses a Math.Matrix from a string.
        /// </summary>
        /// <param name="input">
        /// A <see cref="System.String"/> representation of a matrix
        /// </param>
        /// <returns>
        /// A <see cref="Matrix"/>
        /// </returns>
        public Matrix ParseMatrix (string input)
        {
            double[][] d = null;
            string[] rows = input.Split('\n');
            d = new double[rows.Length][];

            try {
                for (int i = 0; i < rows.Length; i++) {
                    string r = rows[i];
                    int start, end;
                    r = r.Substring (start = (r.LastIndexOf("[") + 1), (end = r.IndexOf("]")) - start);
                    string[] cols = r.Split(',');
                    d[i] = new double[cols.Length];
                    for (int j = 0; j < cols.Length; j++) {
                        d[i][j] = double.Parse(cols[j]);
                    }
                }
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Matrix parse exception", e);
            }
            return new Matrix (d);
        }
        #endregion

        #region Mirage.Matrix
        /// <summary>
        /// Inserts a Mirage.Matrix into the database.
        /// </summary>
        /// <param name="m">
        /// The <see cref="Mirage.Matrix"/> to be inserted
        /// </param>
        /// <param name="bid">
        /// The banshee_id of the corresponding track
        /// </param>
        /// <returns>
        /// True if the matrix was successfully inserted. False otherwise.
        /// </returns>
        public bool InsertMatrix (Mirage.Matrix m, int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (banshee_id, data) VALUES ('{0}', '{1}')",
                                                  bid, MirageMatrixToString(m));
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Mirage.Matrix insert failed", e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }

            return true;
        }

        /// <summary>
        /// Converts a Mirage.Matrix to a string representation with semicolons
        /// instead of commas.
        /// </summary>
        /// <param name="m">
        /// The <see cref="Mirage.Matrix"/> to be converted
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> representation of the matrix
        /// </returns>
        private string MirageMatrixToString (Mirage.Matrix m)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            while (i < m.rows) {
                if (i == 0) {
                    sb.Append("[[");
                } else {
                    sb.Append(" [");
                }

                int j = 0;
                while (j < m.columns) {
                    if (j != 0) {
                        sb.Append(";");
                    }
                    sb.Append(m.d[i,j]);
                    j++;
                }
                if (i == (m.rows - 1)) {
                    sb.Append("]]");
                    break;
                }
                sb.AppendLine("]");
                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses Mirage.Matrix's from the database and returns them.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary<System.Int32, Mirage.Matrix>"/> containing
        /// all matrices in the database mapped to their corresponding banshee_id.
        /// </returns>
        public Dictionary<int, Mirage.Matrix> GetMirageMatrices ()
        {
            Dictionary<int, Mirage.Matrix> ret = new Dictionary<int, Mirage.Matrix> ();

            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "SELECT data, id, banshee_id FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader();
                while(reader.Read()) {
                    Mirage.Matrix mat = ParseMirageMatrix (reader.GetString (0));
                    int bid = reader.GetInt32 (2);
                    if (mat != null)
                        ret.Add (bid, mat);
                    else {
                        Log.Warning ("Foo1/DBNull - Matrix with id " + reader.GetInt32 (1) + " is null!");
                        Log.Debug (reader.GetString (0));
                        CheckMatrix (reader.GetString (0));
                    }
                }

                return ret;
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Mirage.Matrix read failed", e);
                return null;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }

        /// <summary>
        /// Parses a Mirage.Matrix from a string.
        /// </summary>
        /// <param name="input">
        /// A <see cref="System.String"/> representation of a matrix
        /// </param>
        /// <returns>
        /// A <see cref="Mirage.Matrix"/>
        /// </returns>
        public Mirage.Matrix ParseMirageMatrix (string input)
        {
            string[] rows = input.Split('\n');
            Mirage.Matrix m = null;

            try {
                for (int i = 0; i < rows.Length; i++) {
                    string r = rows[i];
                    int start;
                    r = r.Substring (start = (r.LastIndexOf("[") + 1), r.IndexOf("]") - start);
                    string[] cols = r.Split(';');
                    if (i == 0)
                        m = new Mirage.Matrix (rows.Length, cols.Length);
                    for (int j = 0; j < cols.Length; j++) {
                        m.d[i,j] = float.Parse(cols[j]);
                    }
                }
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Mirage.Matrix parse exception", e);
                return null;
            }
            return m;
        }
        #endregion

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
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format ("SELECT id FROM MIRData WHERE banshee_id = '{0}'", bid);
                return (dbcmd.ExecuteScalar () != null);
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Contains MIRData query failed for Banshee_id: " + bid, e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
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
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "DROP TABLE MIRData";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_MIRDATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Clear MIR Data failed", e);
                throw new Exception ("Clear MIR Data failed!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }

        /// <summary>
        /// Clears the PCAData table of the database.
        /// </summary>
        public void ClearPcaData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "DROP TABLE PCAData";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_PCADATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Clear PCA Data failed", e);
                throw new Exception ("Clear PCA Data failed!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }

        /// <summary>
        /// Clears the TrackData table of the database.
        /// </summary>
        public void ClearTrackData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "DROP TABLE TrackData";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_TRACKDATA;
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Clear Track Data failed", e);
                throw new Exception ("Clear Track Data failed!", e);
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }
        #endregion

        #region PCAData
        /// <summary>
        /// Inserts a list of DataEntry's into the PCAData table.
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
            foreach (DataEntry de in coords) {
                if (!InsertPcaCoordinate (de))
                    succ = false;
            }
            return succ;
        }

        /// <summary>
        /// Inserts one DataEntry into the PCAData table.
        /// </summary>
        /// <param name="de">
        /// The <see cref="DataEntry"/> to be inserted
        /// </param>
        /// <returns>
        /// True if the DataEntry was successfully inserted. False otherwise.
        /// </returns>
        public bool InsertPcaCoordinate (DataEntry de)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format(
                        "INSERT INTO PCAData (banshee_id, pca_x, pca_y) VALUES ('{0}', '{1}', '{2}')",
                        de.ID, de.X, de.Y);
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - DataEntry insert failed for DE: " + de, e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }

            return true;
        }
        #endregion

        #region TrackData
        /// <summary>
        /// Inserts one TrackInfo into the TrackData table.
        /// </summary>
        /// <param name="ti">
        /// The <see cref="TrackInfo"/> to be inserted
        /// </param>
        /// <returns>
        /// True if the TrackInfo was successfully inserted. False otherwise.
        /// </returns>
        public bool InsertTrackInfo (TrackInfo ti)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "INSERT INTO TrackData (banshee_id, artist, title, album, duration)" +
                                    " VALUES (@bid, @artist, @title, @album, @duration)";

                SqliteParameter id = new SqliteParameter("@bid", ti.ID);
                SqliteParameter artist = new SqliteParameter("@artist", ti.Artist);
                SqliteParameter title = new SqliteParameter("@title", ti.Title);
                SqliteParameter album = new SqliteParameter("@album", ti.Album);
                SqliteParameter duration = new SqliteParameter("@duration", ti.Duration);

                dbcmd.Parameters.Add(id);
                dbcmd.Parameters.Add(artist);
                dbcmd.Parameters.Add(title);
                dbcmd.Parameters.Add(album);
                dbcmd.Parameters.Add(duration);
    
                dbcmd.Prepare();

                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - TrackInfo insert failed for TI: " + ti, e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
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
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format ("SELECT id FROM TrackData WHERE banshee_id = '{0}'", bid);
                return (dbcmd.ExecuteScalar () != null);
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Contains TrackInfo query failed for banshee_id: " + bid, e);
                return false;
            } finally {
                if (dbcmd != null)
                    dbcmd.Dispose();
                dbcmd = null;
                if (dbcon != null)
                    dbcon.Close();
            }
        }
        #endregion
    }
}

