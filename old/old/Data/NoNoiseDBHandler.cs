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
        private readonly string connectionString = "URI=file:/home/thomas/test.db,version=3";
        private readonly string CREATE_TABLE_PCADATAENTRIES =
            "CREATE TABLE PCADataEntries (Banshee_id INTEGER, ID INTEGER PRIMARY KEY, pca_x DOUBLE, pca_y DOUBLE)";
        private IDbConnection dbcon = null;

        public NoNoiseDBHandler ()
        {
        }

        public bool InsertMatrix (Mirage.Matrix m, int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Banshee_id, Data) VALUES ('{0}', '{1}')",
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
                dbcon = null;
            }

            return true;
        }

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

        public bool InsertMatrix (Matrix m, int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Banshee_id, Data) VALUES ('{0}', '{1}')",
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
                dbcon = null;
            }

            return true;
        }

        public bool InsertMatrixPK (Matrix m, int bid, int primaryKey)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format ("SELECT ID FROM MIRData WHERE ID = '{0}'", primaryKey);
                if (dbcmd.ExecuteScalar () != null)
                    return UpdateMatrix (m, bid, primaryKey, dbcmd);

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Banshee_id, Data, ID) VALUES ('{0}', '{1}', '{2}')",
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
                dbcon = null;
            }

            return true;
        }

        private bool UpdateMatrix (Matrix m, int bid, int primaryKey, IDbCommand dbcmd)
        {
            Log.Debug("Foo1/DB - Updating id " + primaryKey);
            dbcmd.CommandText = string.Format("UPDATE MIRData SET Data = '{0}', Banshee_id = '{1}' WHERE ID == '{2}'",
                                              MatrixToString (m), bid, primaryKey);
            dbcmd.ExecuteNonQuery ();

            return true;
        }

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

        public List<Matrix> GetMatrices ()
        {
            List<Matrix> ret = new List<Matrix> ();

            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "SELECT Data FROM MIRData";
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
                dbcon = null;
            }
        }

        public Dictionary<int, Mirage.Matrix> GetMirageMatrices ()
        {
            Dictionary<int, Mirage.Matrix> ret = new Dictionary<int, Mirage.Matrix> ();

            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "SELECT Data, ID, Banshee_id FROM MIRData";
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
                dbcon = null;
            }
        }

        private void CheckMatrix (string matrix)
        {
            string[] rows;
            Log.Debug ("MatrixRows: " + (rows = matrix.Split ('\n')).Length);
            foreach (string r in rows) {
                Log.Debug ("MatrixCols: " + r.Split (',').Length);
            }
        }

/*        public void Test1 ()
        {
            Matrix m = new Matrix (2, 3);
            m[0,0] = 1;
            m[0,1] = 1;
            m[0,2] = 1;
            m[1,0] = 0;
            m[1,1] = 0;
            m[1,2] = 0;
    
            dbcon = (IDbConnection) new SqliteConnection(connectionString);
            dbcon.Open();
            IDbCommand dbcmd = dbcon.CreateCommand();
    
            string insertMatrix = "INSERT INTO MIRData (Data) VALUES (@mat)";
            dbcmd.CommandText = insertMatrix;
            SqliteParameter mat = new SqliteParameter("@mat", m.ToString ());
            dbcmd.Parameters.Add(mat);
            dbcmd.Prepare();
            dbcmd.ExecuteNonQuery();
    
            string sql = "SELECT ID, Data FROM MIRData";
            dbcmd.CommandText = sql;
            System.Data.IDataReader reader = dbcmd.ExecuteReader();
            while(reader.Read()) {
                int FirstName = reader.GetInt32 (0);
                FirstName = SqliteUtils.FromDbFormat<int> (reader[0]);
                string LastName = SqliteUtils.FromDbFormat<string> (reader[1]);
                Matrix matrix = ParseMatrix (LastName);
                Log.Debug("Data: " + FirstName + " " + matrix);
           }
    
           // clean up
           reader.Close();
           reader = null;
           dbcmd.Dispose();
           dbcmd = null;
           dbcon.Close();
           dbcon = null;
        }*/

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
//                        Log.Debug(cols[j]);
                        d[i][j] = double.Parse(cols[j]);
                    }
                }
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Matrix parse exception", e);
            }
            return new Matrix (d);
        }

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
//                        Log.Debug(cols[j]);
                        m.d[i,j] = float.Parse(cols[j]);
                    }
                }
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Mirage.Matrix parse exception", e);
                return null;
            }
            return m;
        }

        public void ClearPcaData ()
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "DROP TABLE PCADataEntries";
                dbcmd.ExecuteNonQuery ();

                dbcmd.CommandText = CREATE_TABLE_PCADATAENTRIES;
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
                dbcon = null;
            }
        }

        public bool InsertPcaCoordinates (List<DataEntry> coords)
        {
            bool succ = true;
            foreach (DataEntry de in coords) {
                if (!InsertPcaCoordinate (de))
                    succ = false;
            }
            return succ;
        }

        public bool InsertPcaCoordinate (DataEntry de)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format(
                        "INSERT INTO PCADataEntries (Banshee_id, pca_x, pca_y) VALUES ('{0}', '{1}', '{2}')",
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
                dbcon = null;
            }

            return true;
        }

        public bool ContainsMirDataForTrack (int bid)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format ("SELECT ID FROM MIRData WHERE Banshee_id = '{0}'", bid);
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
                dbcon = null;
            }
        }
    }
}

