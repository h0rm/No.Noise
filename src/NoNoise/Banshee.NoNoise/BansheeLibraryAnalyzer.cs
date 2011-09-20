// 
// BansheeLibraryAnalyzer.cs
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

using Banshee.Collection;
using Banshee.ServiceStack;

using NoNoise.Data;
using NoNoise.PCA;

namespace Banshee.NoNoise
{
    public class BansheeLibraryAnalyzer
    {
        private static BansheeLibraryAnalyzer bla = null;
        private NoNoiseDBHandler db;

        public static BansheeLibraryAnalyzer Singleton {
            get { return bla; }
        }

        private BansheeLibraryAnalyzer ()
        {
            db = new NoNoiseDBHandler ();
        }

        public static BansheeLibraryAnalyzer Init ()
        {
            bla = new BansheeLibraryAnalyzer ();
            bla.PcaForMusicLibrary ();
            bla.WriteTrackInfosToDB ();

            return bla;
        }

        /// <summary>
        /// Checks for each track in the music library if there is already
        /// MIR data in the database. If not, computes the MFCC matrix and
        /// stores it in the database.
        /// </summary>
        private void PcaForMusicLibrary ()
        {
            PCAnalyzer ana = new PCAnalyzer();

//                if (gatherMIRdata) {
            Banshee.Library.MusicLibrarySource ml = ServiceManager.SourceManager.MusicLibrary;
            Mirage.Matrix mfcc;
            Dictionary<int, Mirage.Matrix> mfccMap = db.GetMirageMatrices ();

            for (int i = 0; i < ml.TrackModel.Count; i++) {
                try {
                    TrackInfo ti = ml.TrackModel [i];
                    string absPath = ti.Uri.AbsolutePath;
                    int bid = ml.GetTrackIdForUri (ti.Uri);

                    // WARN: A bid could theoretically be inserted/deleted between GetMirageMatrices ()
                    // and CointainsMirDataForTrack () such that if and else fail
                    if (!db.ContainsMirDataForTrack (bid)) {
                        mfcc = Mirage.Analyzer.AnalyzeMFCC (absPath);

                        if (!db.InsertMatrix (mfcc, bid))
                            Hyena.Log.Error ("NoNoise - Matrix insert failed");
                    } else
                        mfcc = mfccMap[bid];

                    if (!ana.AddEntry (bid, ConvertMfccMean (mfcc.Mean ())))
                        throw new Exception("AddEntry failed!");
//                        if (!ana.AddEntry (bid, ConvertMfccMean(mfcc.Mean()), ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
//                        if (!ana.AddEntry (bid, null, ti.Bpm, ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
                } catch (Exception e) {
                    Hyena.Log.Exception("NoNoise - MFCC/DB Problem", e);
                }
            }
//                } else {
//                    Dictionary<int, Matrix> mfccMap = db.GetMirageMatrices ();
//
//                    foreach (int key in mfccMap.Keys) {
//                        try {
//                            if (!ana.AddEntry (key, ConvertMfccMean (mfccMap[key].Mean ())))
//                                throw new Exception ("AddEntry failed!");
//                        } catch (Exception e) {
//                            Hyena.Log.Exception ("NoNoise - PCA Problem", e);
//                        }
//                    }
//                }

            try {
                ana.PerformPCA ();
//                    Hyena.Log.Debug(ana.GetCoordinateStrings ());
                List<DataEntry> coords = ana.Coordinates;
                db.ClearPcaData ();
                if (!db.InsertPcaCoordinates (coords))
                    Hyena.Log.Error ("NoNoise - PCA coord insert failed");
            } catch (Exception e) {
                Hyena.Log.Exception("PCA Problem", e);
            }
        }

        /// <summary>
        /// Converts a Mirage.Vector to an array of doubles.
        /// </summary>
        /// <param name="mean">
        /// The <see cref="Mirage.Vector"/> to be converted
        /// </param>
        /// <returns>
        /// The <see cref="System.Double[]"/> representation of the vector
        /// </returns>
        private double[] ConvertMfccMean (Mirage.Vector mean)
        {
            double[] data = new double[mean.d.Length];

            for (int i = 0; i < mean.d.Length; i++) {
                data[i] = mean.d[i, 0];
            }

            return data;
        }

        /// <summary>
        /// Checks for each track in the music library if it is already in the
        /// database. If not, inserts it.
        /// </summary>
        private void WriteTrackInfosToDB ()
        {
            Banshee.Library.MusicLibrarySource ml = ServiceManager.SourceManager.MusicLibrary;

            for (int i = 0; i < ml.TrackModel.Count; i++) {
                try {
                    TrackInfo ti = ml.TrackModel [i];
                    int bid = ml.GetTrackIdForUri (ti.Uri);

                    if (!db.ContainsInfoForTrack (bid)) {
                        if (!db.InsertTrackInfo (new TrackData (
                                                   bid, ti.ArtistName, ti.TrackTitle,
                                                   ti.AlbumTitle, (int)ti.Duration.TotalSeconds)))
                            Hyena.Log.Error ("NoNoise - TrackInfo insert failed");
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception("NoNoise - DB Problem", e);
                }
            }
        }
    }
}

