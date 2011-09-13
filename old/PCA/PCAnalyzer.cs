// 
// PCAnalyzer.cs
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
using System.Diagnostics;
using System.Collections.Generic;
using Hyena;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

using Banshee.NoNoise.Data;

namespace Banshee.Foo1.PCA
{
    public class PCAnalyzer
    {
        private Matrix m = null;
        private Vector mean = null;
        private List<Vector> differences = null;
        private List<Matrix> matrices = null;
        private Dictionary<int, Vector> vectorMap = new Dictionary<int, Vector> ();
        private int num_params = 0;
        private int num_columns = 0;
        private Vector base1;
        private Vector base2;
        private List<DataEntry> coords = null;

        /// <summary>
        /// A collection of DataEntry objects
        /// </summary>
        public List<DataEntry> Coordinates {
            get {
                return coords;
            }
        }

        /// <summary>
        /// Standard constructor
        /// </summary>
        public PCAnalyzer ()
        {
            num_params = -1;
        }

        /// <summary>
        /// Deprecated constructor
        /// </summary>
        /// <param name="num_params">
        /// The number of parameters, i.e. size of the feature vector
        /// </param>
        /// <param name="num_entries">
        /// The number of feature vectors
        /// </param>
        public PCAnalyzer (int num_params, int num_entries)
        {
            m = new Matrix(num_params, num_entries);
        }

        /// <summary>
        /// Deprecated constructor
        /// </summary>
        /// <param name="data">
        /// The matrix that should be used for the PCA (has to be a square covariance matrix)
        /// </param>
        public PCAnalyzer (Matrix data)
        {
            m = data;
        }

        /// <summary>
        /// Deprecated constructor
        /// </summary>
        /// <param name="data">
        /// The mirage matrix which will be converted to a math matrix that should be user for the PCA
        /// (has to be a square covariance matrix)
        /// </param>
        public PCAnalyzer (Mirage.Matrix data)
        {
            m = ConvertMatrix (data);
//            Console.WriteLine(m.ToString());
        }

        /// <summary>
        /// Converts a mirage matrix to a math matrix
        /// </summary>
        /// <param name="_m">
        /// A <see cref="Mirage.Matrix"/>
        /// </param>
        /// <returns>
        /// A <see cref="Matrix"/>
        /// </returns>
        private Matrix ConvertMatrix (Mirage.Matrix _m)
        {
            Log.Debug("Foo1/PCA - converting mirage matrix");
            double[][] d = Matrix.CreateMatrixData(_m.rows, _m.columns);

            for (int i = 0; i < _m.rows; i++)
                for (int j = 0; j < _m.columns; j++)
                    d[i][j] = _m.d[i,j];
            num_columns = _m.columns;

            Log.Debug("Foo1/PCA - conversion complete");

            return new Matrix (d);
        }

        /// <summary>
        /// Adds a new feature vector to the dataset for the PCA.
        /// The first feature vector determines the number of parameters.
        /// All following feature vectors have to have the same number
        /// of parameters.
        /// </summary>
        /// <param name="data">
        /// A feature vector
        /// </param>
        /// <returns>
        /// True if the feature vector has been successfully added,
        /// false otherwise.
        /// </returns>
        public bool AddEntry (int bid, double[] data)
        {
            if (num_params == -1)
                num_params = data.Length;
            else if (num_params != data.Length)
                return false;

            Vector v = new Vector(data);
            if (mean == null)
                mean = v;
            else
                mean = mean.Add(v);
            vectorMap.Add (bid, v);
            num_columns++;

            return true;
        }

        /// <summary>
        /// Constructs the covariance matrix, extracts the eigenvalues and
        /// eigenvectors and finds the two eigenvectors corresponding to the
        /// largest eigenvalues.
        ///
        /// Precondition: Entries added
        /// </summary>
        public void PerformPCA ()
        {
            if (num_columns == 0)
                return;     // TODO throw exception

            Log.Debug("Foo1/PCA - performing PCA");

            // calc mean
            mean = mean / num_columns;

            differences = new List<Vector>(vectorMap.Count);

            Debug.Assert(differences.Count == vectorMap.Count);

            // fill difference vectors
            foreach (Vector v in vectorMap.Values) {
                differences.Add(v.Subtract(mean));
            }

            matrices = new List<Matrix>(vectorMap.Count);

            Debug.Assert(differences.Count == matrices.Count);

            // fill matrices
            foreach (Vector d in differences) {
                matrices.Add(d.ToColumnMatrix().Multiply(d.ToRowMatrix()));
            }

            // build covariance matrix
            Matrix cov = matrices[0];
            for (int i = 1; i < matrices.Count; i++) {
                cov += matrices[i];
            }
            cov.Multiply(1.0/(double)(num_columns - 1));

            Log.Debug("Foo1/PCA - cov cols: " + cov.ColumnCount);
            Log.Debug("Foo1/PCA - cov rows: " + cov.RowCount);
            EigenvalueDecomposition eigen = cov.EigenvalueDecomposition;

            Complex[] eigenValues = eigen.EigenValues;
            Log.Debug("Foo1/PCA - Num of Eigenvalues: " + eigenValues.Length);
            string evals = "";
            foreach (Complex c in eigenValues)
                evals += c + "; ";

            Log.Debug("Foo1/PCA - Eigenvalues: " + evals);

            double[] maxVals = new double[2];
            maxVals[0] = maxVals[1] = double.NegativeInfinity;
            int[] maxInds = new int[2];
            maxInds[0] = maxInds[1] = -1;

            {
                int i = 0;

                foreach (Complex c in eigenValues) {
                    if (!c.IsReal)
                        Log.Warning("Foo1/PCA - complex is not real!");
    
                    double tmp = c.Real;
                    if (tmp > maxVals[0]) {
                        maxVals[1] = maxVals[0];
                        maxInds[1] = maxInds[0];
                        maxVals[0] = tmp;
                        maxInds[0] = i;
                    } else if (tmp > maxVals[1]) {
                        maxVals[1] = tmp;
                        maxInds[1] = i;
                    }
    
                    i++;
                }
            }

            Debug.Assert(maxInds[0] > -1 && maxInds[1] > -1);

            Log.Debug("Foo1/PCA - max eigenvalues: " + maxVals[0] + "; " + maxVals[1]);

            Matrix eigenVectors = eigen.EigenVectors;
            Log.Debug("Foo1/PCA - Num of Eigenvectors: " + eigenVectors.ColumnCount);

            Log.Debug("Foo1/PCA - max eigenvectors: " + eigenVectors.GetColumnVector(maxInds[0])
                      + ";\n" + eigenVectors.GetColumnVector(maxInds[1]));

            base1 = eigenVectors.GetColumnVector(maxInds[0]).Normalize();
            base2 = eigenVectors.GetColumnVector(maxInds[1]).Normalize();

            ComputeCoordinates ();
        }

        /// <summary>
        /// Computes the normalized 2D coordinates of all entries using the
        /// eigenvectors as basis vectors and stores the result in
        /// <value>coords</value>.
        /// </summary>
        private void ComputeCoordinates ()
        {
            coords = new List<DataEntry>(num_columns);

            // compute 2D coordinates
            foreach (int key in vectorMap.Keys) {
                coords.Add (GetCoordinate (key));
            }

            double[] maxVals = new double[2];
            maxVals[0] = maxVals[1] = double.NegativeInfinity;
            double[] minVals = new double[2];
            minVals[0] = minVals[1] = double.PositiveInfinity;

            // find min and max
            foreach (DataEntry de in coords) {
                double x = de.X;
                if (x > maxVals[0]) {
                    maxVals[0] = x;
                }
                if (x < minVals[0]) {
                    minVals[0] = x;
                }

                double y = de.Y;
                if (y > maxVals[1]) {
                    maxVals[1] = y;
                }
                if (y < minVals[1]) {
                    minVals[1] = y;
                }
            }

            // normalize
            double[] diff = new double[] {maxVals[0] - minVals[0], maxVals[1] - minVals[1]};
            foreach (DataEntry de in coords) {
                // shift
                de.X -= minVals[0];
                de.Y -= minVals[1];

                // scale
                de.X /= diff[0];
                de.Y /= diff[1];
            }
        }

        /// <summary>
        /// Returns a string containing the coordinates of the i-th entry
        /// computed with the eigenvectors as basis vectors.
        /// </summary>
        /// <param name="key">
        /// The index of the entry
        /// </param>
        /// <returns>
        /// A <see cref="DataEntry"/> with the coordinates
        /// </returns>
        public DataEntry GetCoordinate (int key)
        {
            Matrix m = new Matrix(2, num_params);
            m.SetRowVector(base1, 0);
            m.SetRowVector(base2, 1);

            Matrix coord = m.Multiply(vectorMap[key].ToColumnMatrix());

            Debug.Assert(coord.RowCount == 2 && coord.ColumnCount == 1);

            return new DataEntry (key, coord[0,0], coord[1,0], null);
        }

        /// <summary>
        /// Returns a string containing the normalized coordinates of all
        /// entries computed with the eigenvectors as basis vectors.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> with the normalized coordinates
        /// </returns>
        public string GetCoordinateStrings ()
        {
            string ret = "";
            foreach (DataEntry de in coords) {
                ret += de + "\n";
            }
            return ret;
        }
    }
}

