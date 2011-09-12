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
using System.Data;
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

        public bool InsertMatrix (Matrix m)
        {
            IDbCommand dbcmd = null;
            try {
                dbcon = (IDbConnection) new SqliteConnection(connectionString);
                dbcon.Open();
                dbcmd = dbcon.CreateCommand();

                dbcmd.CommandText = string.Format("INSERT INTO MIRData (Data) VALUES ('{0}')", m.ToString ());
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

        public void Test1 ()
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
        }

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
    }
}

