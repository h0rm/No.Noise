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
using System.Threading;

using Banshee.Collection;
using Banshee.ServiceStack;

using NoNoise.Data;
using NoNoise.PCA;

namespace Banshee.NoNoise
{
    public class BansheeLibraryAnalyzer
    {
        private static BansheeLibraryAnalyzer bla = null;
        #region Members
        private Banshee.Library.MusicLibrarySource ml;
//        private Banshee.Collection.TrackListModel tm;
        private NoNoiseSourceContents sc;
        private NoNoiseDBHandler db;
        private PCAnalyzer ana;
        private List<DataEntry> coords;
        private bool analyzing_lib;
        private bool lib_scanned;
        private bool data_up_to_date;
        private bool stop_scan;
        private Gtk.ThreadNotify finished;
        private Thread thr;
        private object scan_synch;
        private object db_synch;
        #endregion

        public static BansheeLibraryAnalyzer Singleton {
            get { return bla; }
        }

        public List<DataEntry> PcaCoordinates {
            get { return coords; }
        }

        public bool IsScanning {
            get { return analyzing_lib; }
        }

//        public TrackListModel TrackModel {
//            get { return tm; }
//            set { tm = value;
//            Hyena.Log.Debug ("tm set, cnt: " + tm.Count); }
//        }

        private BansheeLibraryAnalyzer ()
        {
            ml = ServiceManager.SourceManager.MusicLibrary;     // used to be null on startup
//            Hyena.Log.Debug ("NoNoise - ml count: " + ml.TrackModel.Count);
//            if (ml.TrackModel.Count == 0) {
//                Hyena.Log.Debug ("NoNoise - tm count 0, adding handler");
//                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
//            }
            scan_synch = new object ();
            db_synch = new object ();

            db = new NoNoiseDBHandler ();
            coords = db.GetPcaCoordinates ();

            analyzing_lib = false;
            lib_scanned = CheckLibScanned ();
            data_up_to_date = CheckDataUpToDate ();

            new Thread (new ThreadStart(PcaForMusicLibrary)).Start ();
            new Thread (new ThreadStart(WriteTrackInfosToDB)).Start ();
        }

        public static BansheeLibraryAnalyzer Init (NoNoiseSourceContents sc)
        {
            bla = new BansheeLibraryAnalyzer ();
            bla.sc = sc;

//            bla.PcaForMusicLibrary ();
//            bla.WriteTrackInfosToDB ();

            return bla;
        }

        private bool CheckLibScanned ()
        {
//            tm = ServiceManager.SourceManager.MusicLibrary.TrackModel;
            int cnt = -1;
            lock (db_synch) {
                cnt = db.GetMirDataCount ();
//                Hyena.Log.Debug ("NoNoise/DB - MIRData count: " + cnt);
            }

            lock (scan_synch) {
//                Hyena.Log.Debug ("NoNoise/DB - tm count: " + ml.TrackModel.Count);
                lib_scanned = (cnt == ml.TrackModel.Count);
            }
            Hyena.Log.Debug ("NoNoise/BLA - lib scanned: " + lib_scanned);

            return lib_scanned;
        }

        private bool CheckDataUpToDate ()
        {
//            tm = ServiceManager.SourceManager.MusicLibrary.TrackModel;
            int cnt = -1;
            bool eq = false;
            lock (db_synch) {
                cnt = db.GetPcaDataCount ();
                eq = cnt == db.GetTrackDataCount ();
                eq &= cnt == ml.TrackModel.Count;
            }

            lock (scan_synch) {
                data_up_to_date = eq;
            }
            Hyena.Log.Debug ("NoNoise/BLA - data up to date: " + data_up_to_date);

            return data_up_to_date;
        }

        public void Scan (bool start)
        {
            if (start == analyzing_lib)
                return;

//            ml = ServiceManager.SourceManager.MusicLibrary;
//            Hyena.Log.Debug ("NN Scan - tm count: " + ml.TrackModel.Count);

            if (start) {
                if (CheckLibScanned ()) {
                    Finished ();
                    return;
                }
                lock (scan_synch) {
                    analyzing_lib = true;
                    stop_scan = false;
                }
                thr = new Thread (new ThreadStart(ScanMusicLibrary));
                finished = new Gtk.ThreadNotify (new Gtk.ReadyEvent(Finished));
                thr.Start();
            } else {
                lock (scan_synch) {
                    stop_scan = true;
                }
            }
            Hyena.Log.Information ("NoNoise/BLA - Scan " + (start ? "started." : "paused."));
        }

        private void ScanMusicLibrary ()
        {
//            ml = ServiceManager.SourceManager.MusicLibrary;
            int ml_cnt = ml.TrackModel.Count;
            int db_cnt = 0;
            DateTime dt = DateTime.Now;

            Mirage.Matrix mfcc;
            Dictionary<int, Mirage.Matrix> mfccMap = null;
            lock (db_synch) {
                db_cnt = db.GetMirDataCount ();
                mfccMap = db.GetMirageMatrices ();
            }
            if (mfccMap == null)
                Hyena.Log.Error ("NoNoise/BLA - mfccMap is null!");

            for (int i = 0; i < ml.TrackModel.Count; i++) {
                if (stop_scan) {
                    lock (scan_synch) {
                        analyzing_lib = false;
                    }
                    return;
                }

                try {
                    TrackInfo ti = ml.TrackModel [i];
                    string absPath = ti.Uri.AbsolutePath;
                    int bid = ml.GetTrackIdForUri (ti.Uri);

                    // WARN: A bid could theoretically be inserted/deleted between GetMirageMatrices ()
                    // and CointainsMirDataForTrack () such that if and else fail
                    if (!mfccMap.ContainsKey (bid)) {
                        mfcc = Mirage.Analyzer.AnalyzeMFCC (absPath);

                        lock (db_synch) {
                            if (!db.InsertMatrix (mfcc, bid))
                                Hyena.Log.Error ("NoNoise - Matrix insert failed");
                        }
                        db_cnt++;
                    }

                    if ((DateTime.Now - dt).TotalSeconds > 20.0) {
                        Hyena.Log.InformationFormat ("NoNoise/Scan - {0}% finished.",
                                                     (int)((double)db_cnt / (double)ml_cnt * 100.0));
                        dt = DateTime.Now;
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception("NoNoise - MFCC/DB Problem", e);
                }
            }

            finished.WakeupMain();
        }

        private void Finished ()
        {
            lock (scan_synch) {
                lib_scanned = true;
                analyzing_lib = false;
            }
            sc.ScanFinished ();
        }

        /// <summary>
        /// Checks for each track in the music library if there is already
        /// MIR data in the database. If not, computes the MFCC matrix and
        /// stores it in the database.
        /// </summary>
        private void PcaForMusicLibrary ()
        {
//            Hyena.Log.Debug ("NoNoise/PCA - ml.TrackModel count: "  + ml.TrackModel.Count);

            if (data_up_to_date) {
                Hyena.Log.Information ("NoNoise - Data already up2date - aborting pca.");
                return;
            }

            if (analyzing_lib) {
                Hyena.Log.Information ("NoNoise - Music library is currently beeing scanned - aborting pca.");
                return;     // TODO react!
            }

            if (!lib_scanned) {
                Hyena.Log.Information ("NoNoise - No mirage data available for pca - aborting.");
                return;     // TODO something clever!
            }

            ana = new PCAnalyzer();
            Dictionary<int, Mirage.Matrix> mfccMap = null;
            lock (db_synch) {
                mfccMap = db.GetMirageMatrices ();
            }
            if (mfccMap == null)
                Hyena.Log.Error ("NoNoise/BLA - mfccMap is null!");

            foreach (int bid in mfccMap.Keys) {
                try {
                    if (!ana.AddEntry (bid, ConvertMfccMean (mfccMap[bid].Mean ())))
                        throw new Exception("AddEntry failed!");
//                        if (!ana.AddEntry (bid, ConvertMfccMean(mfcc.Mean()), ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
//                        if (!ana.AddEntry (bid, null, ti.Bpm, ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
                } catch (Exception e) {
                    Hyena.Log.Exception("NoNoise - PCA Problem", e);
                }
            }

            try {
                ana.PerformPCA ();
//                    Hyena.Log.Debug(ana.GetCoordinateStrings ());
                coords = ana.Coordinates;

                lock (db_synch) {
                    db.ClearPcaData ();
                    if (!db.InsertPcaCoordinates (coords))
                        Hyena.Log.Error ("NoNoise - PCA coord insert failed");
                }
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
//            Hyena.Log.Debug ("NoNoise/TI - tm count: "  + ml.TrackModel.Count);

            if (data_up_to_date) {
                Hyena.Log.Information ("NoNoise - Data already up2date - aborting write-track-infos.");
                return;
            }

//            ml = ServiceManager.SourceManager.MusicLibrary;

            for (int i = 0; i < ml.TrackModel.Count; i++) {
                try {
                    TrackInfo ti = ml.TrackModel [i];
                    int bid = ml.GetTrackIdForUri (ti.Uri);

                    lock (db_synch) {
                        if (!db.ContainsInfoForTrack (bid)) {
                            if (!db.InsertTrackInfo (new TrackData (
                                                       bid, ti.ArtistName, ti.TrackTitle,
                                                       ti.AlbumTitle, (int)ti.Duration.TotalSeconds)))
                                Hyena.Log.Error ("NoNoise - TrackInfo insert failed");
                        }
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception("NoNoise - DB Problem", e);
                }
            }
        }
    }
}

