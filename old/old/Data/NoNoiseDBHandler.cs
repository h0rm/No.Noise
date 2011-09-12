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
        private string connectionString = "URI=file:/home/thomas/test.db,version=3";
        private IDbConnection dbcon = null;

        public NoNoiseDBHandler ()
        {
        }

        public bool InsertMatrix (Mirage.Matrix m)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Data) VALUES ('{0}')", MirageMatrixToString(m));
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

        public bool InsertMatrix (Matrix m)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Data) VALUES ('{0}')", MatrixToString (m));
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

        public bool InsertMatrix (Matrix m, int id)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format ("SELECT ID FROM MIRData WHERE ID = '{0}'", id);
                if (dbcmd.ExecuteScalar () != null)
                    return UpdateMatrix (m, id, dbcmd);

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Data, ID) VALUES ('{0}', '{1}')", MatrixToString (m), id);
                dbcmd.ExecuteNonQuery ();
            } catch (Exception e) {
                Log.Exception("Foo1/DB - Matrix insert failed for id: " + id, e);
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

        private bool UpdateMatrix (Matrix m, int id, IDbCommand dbcmd)
        {
            Log.Debug("Foo1/DB - Updating id " + id);
            dbcmd.CommandText = string.Format("UPDATE MIRData SET Data = '{0}' WHERE ID == '{1}'", MatrixToString (m), id);
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

        public List<Mirage.Matrix> GetMirageMatrices ()
        {
            List<Mirage.Matrix> ret = new List<Mirage.Matrix> ();

            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = "SELECT Data, ID FROM MIRData";
                System.Data.IDataReader reader = dbcmd.ExecuteReader();
                while(reader.Read()) {
                    Mirage.Matrix mat = ParseMirageMatrix (reader.GetString (0));
                    if (mat != null)
                        ret.Add (mat);
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
    }
}

