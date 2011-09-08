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
        private Matrix m;

        public NoNoiseDBHandler ()
        {
            m = new Matrix (2, 2);
        }

        public void Test1 ()
        {
            m[0,0] = 1;
            m[0,1] = 0;
            m[1,0] = 0;
            m[1,1] = 1;

            string connectionString = "URI=file:/home/thomas/test.db,version=3";
            IDbConnection dbcon;
            dbcon = (IDbConnection) new SqliteConnection(connectionString);
            dbcon.Open();
            IDbCommand dbcmd = dbcon.CreateCommand();

            string sql = "SELECT ID, Data FROM MIRData";
            dbcmd.CommandText = sql;
            System.Data.IDataReader reader = dbcmd.ExecuteReader();
            while(reader.Read()) {
                int FirstName = reader.GetInt32 (0);
                FirstName = SqliteUtils.FromDbFormat<int> (reader[0]);
//                Log.Debug("Data Type: " + reader.GetDataTypeName(1));
                Matrix LastName = null;
                if (FirstName == 5)
                    LastName = SqliteUtils.FromDbFormat<Matrix> (reader[1]);
                if (LastName != null)
                    Log.Debug("Lastname: " + LastName);
                Log.Debug("Data: " + FirstName + " " + LastName);
           }
           // clean up
           reader.Close();
           reader = null;
           dbcmd.Dispose();
           dbcmd = null;
           dbcon.Close();
           dbcon = null;
        }
    }
}

