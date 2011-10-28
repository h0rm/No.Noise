// 
// DataParser.cs
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
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using Hyena;

namespace NoNoise.Data
{
    /// <summary>
    /// Helper class for parsing matrices and vectors.
    /// </summary>
    public class DataParser
    {
        #region to string

        /// <summary>
        /// Converts a Mirage.Vector to a string representation with semicolons
        /// instead of commas.
        /// </summary>
        /// <param name="v">
        /// The <see cref="Mirage.Vector"/> to be converted
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> representation of the vector
        /// </returns>
        public static string MirageVectorToString (Mirage.Vector v)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append ("[");

            for (int i = 0; i < v.rows; i++) {
                if (i != 0)
                    sb.Append (";");
                sb.Append (v.d [i, 0].ToString (System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append ("]");
            return sb.ToString ();
        }
        #endregion

        #region from string

        /// <summary>
        /// Parses a Mirage.Vector from a string.
        /// </summary>
        /// <param name="input">
        /// A <see cref="System.String"/> representation of a vector
        /// </param>
        /// <returns>
        /// A <see cref="Mirage.Vector"/>
        /// </returns>
        public static Mirage.Vector ParseMirageVector (string input)
        {
            input = input.Substring (1, input.Length - 2);
            string[] rows = input.Split(';');
            Mirage.Vector v = new Mirage.Vector (rows.Length);

            try {
                for (int i = 0; i < rows.Length; i++) {
                    v.d [i, 0] = float.Parse(rows [i], System.Globalization.CultureInfo.InvariantCulture);
                }
            } catch (Exception e) {
                Log.Exception("NoNoise/DB - Mirage.Vector parse exception", e);
                return null;
            }
            return v;
        }
        #endregion

        #region unused

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
        public static string MatrixToString (Matrix m)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < m.RowCount; i++) {
                if (i == 0)
                    sb.Append ("[[");
                else
                    sb.Append (" [");

                for (int j = 0; j < m.ColumnCount; j++) {
                    if (j != 0)
                        sb.Append (";");
                    sb.Append (m [i, j]);
                }

                if (i == (m.RowCount - 1)) {
                    sb.Append("]]");
                    continue;
                }
                sb.AppendLine("]");
            }

            return sb.ToString();
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
        public static string MirageMatrixToString (Mirage.Matrix m)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < m.rows; i++) {
                if (i == 0)
                    sb.Append ("[[");
                else
                    sb.Append (" [");

                for (int j = 0; j < m.columns; j++) {
                    if (j != 0)
                        sb.Append (";");
                    sb.Append (m.d [i, j]);
                }

                if (i == (m.rows - 1)) {
                    sb.Append("]]");
                    continue;
                }
                sb.AppendLine("]");
            }

            return sb.ToString();
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
        public static Matrix ParseMatrix (string input)
        {
            double[][] d = null;
            string[] rows = input.Split('\n');
            d = new double[rows.Length][];

            try {
                for (int i = 0; i < rows.Length; i++) {
                    string r = rows[i];
                    int start;
                    r = r.Substring (start = (r.LastIndexOf("[") + 1), r.IndexOf("]") - start);
                    string[] cols = r.Split(';');
                    d[i] = new double[cols.Length];
                    for (int j = 0; j < cols.Length; j++) {
                        d[i][j] = double.Parse(cols[j]);
                    }
                }
            } catch (Exception e) {
                Log.Exception("NoNoise/DB - Matrix parse exception", e);
            }
            return new Matrix (d);
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
        public static Mirage.Matrix ParseMirageMatrix (string input)
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
                Log.Exception("NoNoise/DB - Mirage.Matrix parse exception", e);
                return null;
            }
            return m;
        }
        #endregion
    }
}

