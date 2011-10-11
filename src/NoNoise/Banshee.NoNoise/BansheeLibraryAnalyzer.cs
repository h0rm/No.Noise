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
using Banshee.Collection.Database;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;

using Banshee.NoNoise.Bpm;

using NoNoise.Data;
using NoNoise.PCA;

// TODO something like compute_pca button
// TODO track_updated listener

namespace Banshee.NoNoise
{
    /// <summary>
    /// This class provides a singleton which serves as an interface between the
    /// GUI and the database.
    /// </summary>
    public class BansheeLibraryAnalyzer
    {
        private static BansheeLibraryAnalyzer bla = null;

        // TODO remove debug helper bools
//        private readonly bool STORE_ENTIRE_MATRIX = false;
        private readonly bool DB_CHEATER_MODE = false;

        #region Constants
        public const int PCA_MEAN = 0;
        public const int PCA_MEAN_DUR = 1;
        public const int PCA_SQR_MEAN = 2;
        public const int PCA_SQR_MEAN_DUR = 3;
        public const int PCA_MAX = 4;
        public const int PCA_MAX_DUR = 5;
        public const int PCA_MIN = 6;
        public const int PCA_MIN_DUR = 7;
        public const int PCA_MED = 8;
        public const int PCA_MED_DUR = 9;
        #endregion

        #region Members
        private int pca_mode = -1;
        private Banshee.Library.MusicLibrarySource ml;
        private NoNoiseClutterSourceContents sc;
        private NoNoiseDBHandler db;
        private IPcaAdder pca_adder;
        private List<DataEntry> coords;
        private bool analyzing_lib;
        private bool lib_scanned;
        private bool data_up_to_date;
        private bool stop_scan;
        private bool added_while_scanning;
        private bool updating_db;
        private Gtk.ThreadNotify finished;
        private Thread thr;
        private object scan_synch;
        private object pca_synch;
        private object update_synch;
//        private object lib_synch;   // WARN make sure it is never locked on lib_synch inside of another lock
        private object db_synch;

        private IBpmDetector detector;
        #endregion

        /// <summary>
        /// The current instance of this class. Is null until Init () has been called.
        /// </summary>
        public static BansheeLibraryAnalyzer Singleton {
            get { return bla; }
        }

        /// <summary>
        /// The PCA coordinates of the corresponding tracks.
        /// Is empty if there is no PCA data available yet.
        /// </summary>
        public List<DataEntry> PcaCoordinates {
            get { return coords; }
        }

        /// <summary>
        /// Boolean variable indicating whether the library is currently being scanned.
        /// </summary>
        public bool IsScanning {
            get { return analyzing_lib; }
        }

        /// <summary>
        /// Boolean variable indicating whether the library has already been scanned.
        /// </summary>
        public bool IsLibraryScanned {
            get { return lib_scanned; }
        }

        /// <summary>
        /// Attribute defining the features for the PCA computation.
        /// Valid values are any PCA_* constants.
        /// </summary>
        public int PcaMode {
            get { return pca_mode; }
            set {
                if (pca_mode == value)
                    return;
                lock (pca_synch) {
                    pca_mode = value;
                }
                SwitchPcaMode ();
            }
        }

        /// <summary>
        /// Gets the TrackInfo from the banshee database with the given banshee_id.
        /// </summary>
        /// <param name="bid">
        /// The banshee_id
        /// </param>
        /// <returns>
        /// The <see cref="TrackInfo"/> corresponding to the given banshee_id
        /// </returns>
        public TrackInfo GetTrackInfoFor (int bid)
        {
            return DatabaseTrackInfo.Provider.FetchSingle (bid) as TrackInfo;
        }

        private BansheeLibraryAnalyzer ()
        {
            ml = ServiceManager.SourceManager.MusicLibrary;

            scan_synch = new object ();
            pca_synch = new object ();
            update_synch = new object ();
            db_synch = new object ();

            db = new NoNoiseDBHandler ();
            
            GetPcaData ();

            /// REMOVE THIS
            if (DB_CHEATER_MODE) {
                analyzing_lib = false;
                lib_scanned = true;
                data_up_to_date = true;
                Hyena.Log.Information ("NoNoise/BLA - cheater mode - skipping checks");
                return;
            }
            /// SIHT EVOMER

            analyzing_lib = false;
            added_while_scanning = false;
            updating_db = false;
            lib_scanned = CheckLibScanned ();
            data_up_to_date = CheckDataUpToDate ();

            Hyena.Log.Debug ("NoNoise/BLA - adding library change handler");
            ml.TracksAdded += HandleTracksAdded;
            ml.TracksDeleted += HandleTracksDeleted;
            ml.TracksChanged += HandleTracksChanged;

//            Hyena.Log.Debug ("NoNoise/BLA - starting pca query");
//            new Thread (new ThreadStart (GetPcaData)).Start ();

//            Hyena.Log.Debug ("NoNoise/BLA - blabla: " + coords [0].Value.Artist + " - " + coords [0].Value.Title);
        }

        /// <summary>
        /// Initializes the singleton instance of this class and starts PCA
        /// computations if the library has been scanned already. Also causes
        /// TrackData to be written to the database if it is not current anymore.
        /// </summary>
        /// <param name="sc">
        /// The <see cref="NoNoiseClutterSourceContents"/> which is used as callback.
        /// </param>
        /// <param name="force_new">
        /// If this is true then a new instance will be initialized even if an
        /// old one already exists. Otherwise Init () might return an existing
        /// instance.
        /// </param>
        /// <returns>
        /// The singleton instance of <see cref="BansheeLibraryAnalyzer"/>
        /// </returns>
        public static BansheeLibraryAnalyzer Init (NoNoiseClutterSourceContents sc, bool force_new)
        {
            if (!force_new && bla != null)
                return bla;

            bla = new BansheeLibraryAnalyzer ();
            bla.sc = sc;

            Hyena.Log.Debug ("NoNoise/BLA - starting write track data threads");
//            bla.SwitchPcaMode ();
//            new Thread (new ThreadStart (PcaForMusicLibraryVectorEdition)).Start ();
            new Thread (new ThreadStart (bla.WriteTrackInfosToDB)).Start ();

            return bla;
        }

        /// <summary>
        /// Initializes the singleton instance of this class and starts PCA
        /// computations if the library has been scanned already. Also causes
        /// TrackData to be written to the database if it is not current anymore.
        /// Calling this method is similar to calling Init (sc, false).
        /// </summary>
        /// <param name="sc">
        /// The <see cref="NoNoiseClutterSourceContents"/> which is used as callback.
        /// May NOT be null.
        /// </param>
        /// <returns>
        /// The singleton instance of <see cref="BansheeLibraryAnalyzer"/>
        /// </returns>
        public static BansheeLibraryAnalyzer Init (NoNoiseClutterSourceContents sc)
        {
            return Init (sc, false);
        }

        /// <summary>
        /// Sets the instance of pca_adder according to the current PCA mode and
        /// starts PCA computation in a new thread afterwards.
        /// </summary>
        private void SwitchPcaMode ()
        {
            lock (pca_synch) {
                switch (pca_mode) {
                default:
                case PCA_MEAN:
                    pca_adder = new PcaMeanAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking mean adder");
                    break;
    
                case PCA_MEAN_DUR:
                    pca_adder = new PcaMeanDurationAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking mean duration adder");
                    break;
    
                case PCA_SQR_MEAN:
                    pca_adder = new PcaSqrMeanAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking squared mean adder");
                    break;
    
                case PCA_SQR_MEAN_DUR:
                    pca_adder = new PcaSqrMeanDurationAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking squared mean duration adder");
                    break;
    
                case PCA_MAX:
                    pca_adder = new PcaMaxAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking max adder");
                    break;
    
                case PCA_MAX_DUR:
                    pca_adder = new PcaMaxDurationAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking max duration adder");
                    break;
    
                case PCA_MIN:
                    pca_adder = new PcaMinAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking min adder");
                    break;
    
                case PCA_MIN_DUR:
                    pca_adder = new PcaMinDurationAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking min duration adder");
                    break;
    
                case PCA_MED:
                    pca_adder = new PcaMedianAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking median adder");
                    break;
    
                case PCA_MED_DUR:
                    pca_adder = new PcaMedianDurationAdder ();
                    Hyena.Log.Debug ("NoNoise/BLA - taking median duration adder");
                    break;
                }
            }
            new Thread (new ThreadStart (PcaForMusicLibraryVectorEditionForceNew)).Start ();
        }

        /// <summary>
        /// Sets the PcaCoordinates field with the coordinates from the database.
        /// </summary>
        private void GetPcaData ()
        {
            lock (db_synch) {
                coords = db.GetPcaCoordinates ();
            }

            Hyena.Log.Debug ("NoNoise/BLA - PCA coords size: " + coords.Count);
        }

        /// <summary>
        /// Checks whether the music library has been scanned or not and stores
        /// the result in lib_scanned.
        /// </summary>
        /// <returns>
        /// True if the music library has been scanned, false otherwise.
        /// </returns>
        private bool CheckLibScanned ()
        {
            int cnt = -1;
            lock (db_synch) {
                cnt = db.GetMirDataCount ();
//                Hyena.Log.Debug ("NoNoise/DB - MIRData count: " + cnt);
            }

            bool old_lib_scanned = lib_scanned;
            lock (scan_synch) {
//                Hyena.Log.Debug ("NoNoise/DB - tm count: " + ml.TrackModel.Count);
                lib_scanned = (cnt == ml.TrackModel.Count);
            }
            if (old_lib_scanned != lib_scanned && sc != null)
                sc.ScannableChanged (!lib_scanned);
            Hyena.Log.Debug ("NoNoise/BLA - lib scanned: " + lib_scanned);

            if (cnt > ml.TrackModel.Count && !updating_db)
                new Thread (new ThreadStart (RemoveDeletedTracks)).Start ();

            return lib_scanned;
        }

        /// <summary>
        /// Checks whether the pca and track data is up2date or not and stores
        /// the result in data_up_to_date.
        /// </summary>
        /// <returns>
        /// True if the pca and track data is up to date, false otherwise.
        /// </returns>
        private bool CheckDataUpToDate ()
        {
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

            if (cnt > ml.TrackModel.Count && !updating_db)
                new Thread (new ThreadStart (RemoveDeletedTracks)).Start ();

            return data_up_to_date;
        }

        /// <summary>
        /// Starts a new thread which scans the music library and stores the mirage
        /// data in the database.
        /// </summary>
        /// <param name="start">
        /// If start is true, the scan will be started. If it is false, the scan
        /// will be stopped. If start is true and the library is already being
        /// scanned, or if start is false and the library is not being scanned
        /// then this method does nothing.
        /// </param>
        public void Scan (bool start)
        {
            if (start == analyzing_lib)
                return;

            /// REMOVE THIS
            if (DB_CHEATER_MODE) {
                Hyena.Log.Information ("NoNoise/BLA - cheater mode - doing nothing");
                return;
            }
            /// SIHT EVOMER

            if (start) {
                if (CheckLibScanned ()) {
                    Finished ();
                    return;
                }
                lock (scan_synch) {
                    analyzing_lib = true;
                    stop_scan = false;
                }
                thr = new Thread (new ThreadStart (ScanMusicLibraryVectorEdition));
                finished = new Gtk.ThreadNotify (new Gtk.ReadyEvent (Finished));
                thr.Start();
            } else {
                lock (scan_synch) {
                    stop_scan = true;
                }
            }
            Hyena.Log.Information ("NoNoise/BLA - Scan " + (start ? "started." : "paused."));
        }

        /// <summary>
        /// Scans the music library and stores the mirage data in the database.
        /// Calls <see cref="Finished ()"/> when everything has been scanned.
        /// Vector Edition!
        /// </summary>
        private void ScanMusicLibraryVectorEdition ()
        {
            int ml_cnt = ml.TrackModel.Count;
            int db_cnt = 0;
            DateTime dt = DateTime.Now;

            Mirage.Matrix mfcc;
            List<int> keyList = null;
            lock (db_synch) {
                db_cnt = db.GetMirDataCount ();
                keyList = db.GetMirDataKeyList ();
            }
            if (keyList == null)
                Hyena.Log.Error ("NoNoise/BLA - keyList is null!");

//            for (int i = 0; i < ml.TrackModel.Count; i++) {
//            lock (lib_synch) {
            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                if (stop_scan) {
                    lock (scan_synch) {
                        analyzing_lib = false;
                    }
                    return;
                }

                try {
//                        TrackInfo ti = dti as TrackInfo;
//                        TrackInfo ti = library [bid];
                    string absPath = dti.Uri.AbsolutePath;
                    int bid = dti.TrackId;

                    if (!keyList.Contains (bid)) {
                        mfcc = Mirage.Analyzer.AnalyzeMFCC (absPath);

                        lock (db_synch) {
//                            if (!db.InsertVector (mfcc.Mean (), bid))
                            if (!db.InsertVectors (mfcc.Mean (), ConvertMfccToSqrMean (mfcc), ConvertMfccToMedian (mfcc),
                                                   ConvertMfccToMin (mfcc), ConvertMfccToMax (mfcc), bid))
                                Hyena.Log.Error ("NoNoise - Matrix insert failed");
                        }
                        db_cnt++;
                    }

                    if ((DateTime.Now - dt).TotalSeconds > 20.0) {
                        int perc = (int)((double)db_cnt / (double)ml_cnt * 100.0);
                        if (perc <= 100)
                            Hyena.Log.InformationFormat ("NoNoise/Scan - {0}% finished.", perc);
                        dt = DateTime.Now;
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception ("NoNoise - MFCC/DB Problem", e);
                }
            }
//            }

            finished.WakeupMain ();
        }

        /// <summary>
        /// Sets lib_scanned to true and tells the NoNoiseSourceContent that
        /// the scan finished.
        /// </summary>
        private void Finished ()
        {
            if (added_while_scanning) {     // TODO test this
                lock (scan_synch) {
                    added_while_scanning = false;
                }

                new Thread (new ThreadStart (ScanMusicLibraryVectorEdition)).Start ();

                return;
            }

            lock (scan_synch) {
                lib_scanned = true;
                analyzing_lib = false;
            }
            sc.ScanFinished ();
            new Thread (new ThreadStart (PcaForMusicLibraryVectorEditionForceNew)).Start ();
        }

        /// <summary>
        /// Checks for each track in the music library if there is already
        /// MIR data in the database. If not, computes the MFCC matrix and
        /// stores it in the database.
        /// Vector Edition!
        /// </summary>
        /// <param name="force_new">
        /// A <see cref="System.Boolean"/> indicating whether PCA coordinates
        /// should be computed even if old ones seem to be up to date.
        /// </param>
        private void PcaForMusicLibraryVectorEdition (bool force_new)
        {
            /// REMOVE THIS
            if (DB_CHEATER_MODE) {
                Hyena.Log.Information ("NoNoise/BLA - cheater mode - not calculating pca");
                return;
            }
            /// SIHT EVOMER

            if (data_up_to_date && !force_new) {
                Hyena.Log.Information ("NoNoise/BLA - Data already up2date - aborting pca.");
                return;
            }

            if (analyzing_lib) {
                Hyena.Log.Information ("NoNoise/BLA - Music library is currently beeing scanned - aborting pca.");
                return;
            }

            if (!lib_scanned) {
                Hyena.Log.Information ("NoNoise/BLA - No mirage data available for pca - aborting.");
                return;     // TODO something clever!
            }

            if (pca_adder == null) {
                Hyena.Log.Debug ("NoNoise/BLA - PCA adder is null - aborting.");
                return;
            }

            Hyena.Log.Debug ("NoNoise/BLA - PcaFor... called");

            try {
                PCAnalyzer ana = new PCAnalyzer ();
                lock (pca_synch) {
                    pca_adder.AddVectorsFromDB (ana);
                }
                ana.PerformPCA ();

                lock (db_synch) {
                    db.ClearPcaData ();
                    if (!db.InsertPcaCoordinates (ana.Coordinates))
                        Hyena.Log.Error ("NoNoise/BLA - PCA coord insert failed");
                    Hyena.Log.Debug ("NoNoise/BLA - PCA inserted into db");
                    coords = db.GetPcaCoordinates ();
                }
            } catch (DatabaseException e) {
                Hyena.Log.Exception ("NoNoise/BLA - Database Problem", e);
            } catch (Exception e) {
                Hyena.Log.Exception ("NoNoise/BLA - PCA Problem", e);
            }
            // TODO update once it works
            Hyena.ThreadAssist.ProxyToMain (sc.PcaCoordinatesUpdated);
        }

        /// <summary>
        /// This method is equivalent to PcaForMusicLibraryVectorEdition (false).
        /// <see cref="PcaForMusicLibraryVectorEdition (bool)"/>
        /// </summary>
        private void PcaForMusicLibraryVectorEdition ()
        {
            PcaForMusicLibraryVectorEdition (false);
        }

        /// <summary>
        /// This method is equivalent to PcaForMusicLibraryVectorEdition (true).
        /// <see cref="PcaForMusicLibraryVectorEdition (bool)"/>
        /// </summary>
        private void PcaForMusicLibraryVectorEditionForceNew ()
        {
            PcaForMusicLibraryVectorEdition (true);
        }

        #region Vector conversion

        /// <summary>
        /// Converts a Mirage.Vector to an array of doubles.
        /// </summary>
        /// <param name="vec">
        /// The <see cref="Mirage.Vector"/> to be converted
        /// </param>
        /// <returns>
        /// The <see cref="System.Double[]"/> representation of the vector
        /// </returns>
        private double[] ConvertMirageVector (Mirage.Vector vec)
        {
            double[] data = new double[vec.d.Length];

            for (int i = 0; i < vec.d.Length; i++) {
                data [i] = vec.d [i, 0];
            }

            return data;
        }

        /// <summary>
        /// Computes the squared mean for each row of the MFCC matrix.
        /// </summary>
        /// <param name="mfcc">
        /// The MFCC <see cref="Mirage.Matrix"/>
        /// </param>
        /// <returns>
        /// A <see cref="Mirage.Vector"/> containing the squared mean
        /// vector of the MFCC matrix
        /// </returns>
        private Mirage.Vector ConvertMfccToSqrMean (Mirage.Matrix mfcc)
        {
            Mirage.Vector data = new Mirage.Vector (mfcc.rows);

            for (int i = 0; i < mfcc.rows; i++) {
                data.d [i, 0] = 0;
                for (int j = 0; j < mfcc.columns; j++) {
                    data.d [i, 0] += (float) Math.Pow (mfcc.d [i, j], 2);
                }
                data.d [i, 0] = (float) Math.Sqrt (data.d [i, 0]);
            }

            return data;
        }

        /// <summary>
        /// Computes the maximum for each row of the MFCC matrix.
        /// </summary>
        /// <param name="mfcc">
        /// The MFCC <see cref="Mirage.Matrix"/>
        /// </param>
        /// <returns>
        /// A <see cref="Mirage.Vector"/> containing the maximum
        /// vector of the MFCC matrix
        /// </returns>
        private Mirage.Vector ConvertMfccToMax (Mirage.Matrix mfcc)
        {
            Mirage.Vector data = new Mirage.Vector (mfcc.rows);

            for (int i = 0; i < mfcc.rows; i++) {
                float max = float.NegativeInfinity;
                for (int j = 0; j < mfcc.columns; j++) {
                    max = Math.Max (max, mfcc.d [i, j]);
                }
                data.d [i, 0] = max;
            }

            return data;
        }

        /// <summary>
        /// Computes the minimum for each row of the MFCC matrix.
        /// </summary>
        /// <param name="mfcc">
        /// The MFCC <see cref="Mirage.Matrix"/>
        /// </param>
        /// <returns>
        /// A <see cref="Mirage.Vector"/> containing the minimum
        /// vector of the MFCC matrix
        /// </returns>
        private Mirage.Vector ConvertMfccToMin (Mirage.Matrix mfcc)
        {
            Mirage.Vector data = new Mirage.Vector (mfcc.rows);

            for (int i = 0; i < mfcc.rows; i++) {
                float min = float.PositiveInfinity;
                for (int j = 0; j < mfcc.columns; j++) {
                    min = Math.Min (min, mfcc.d [i, j]);
                }
                data.d [i, 0] = min;
            }

            return data;
        }

        /// <summary>
        /// Computes the median for each row of the MFCC matrix.
        /// </summary>
        /// <param name="mfcc">
        /// The MFCC <see cref="Mirage.Matrix"/>
        /// </param>
        /// <returns>
        /// A <see cref="Mirage.Vector"/> containing the median
        /// vector of the MFCC matrix
        /// </returns>
        private Mirage.Vector ConvertMfccToMedian (Mirage.Matrix mfcc)
        {
            Mirage.Vector data = new Mirage.Vector (mfcc.rows);

            for (int i = 0; i < mfcc.rows; i++) {
                float[] r = new float[mfcc.columns];

                for (int j = 0; j < mfcc.columns; j++) {
                    r [j] = mfcc.d [i, j];
                }

                Array.Sort<float> (r);
                data.d [i, 0] = r [r.Length / 2];
            }

            return data;
        }
        #endregion

        #region Library updates

        /// <summary>
        /// Checks for each track in the music library if it is already in the
        /// database. If not, inserts it.
        /// </summary>
        private void WriteTrackInfosToDB ()
        {
            /// REMOVE THIS
            if (DB_CHEATER_MODE) {
                Hyena.Log.Information ("NoNoise/BLA - cheater mode - not writing track infos");
                return;
            }
            /// SIHT EVOMER

//            if (data_up_to_date) {
//                Hyena.Log.Information ("NoNoise - Data already up2date - aborting write-track-infos.");
//                return;
//            }

            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                try {
                    TrackInfo ti = dti as TrackInfo;
                    int bid = dti.TrackId;

                    lock (db_synch) {
                        if (!db.ContainsInfoForTrack (bid)) {
//                            if (!db.InsertTrackID (bid)) {
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

        /// <summary>
        /// Checks the music library for deleted tracks and removes them from the database.
        /// </summary>
        private void RemoveDeletedTracks ()
        {
            lock (update_synch) {
                updating_db = true;
            }

            SortedList<int, int> ids = new SortedList<int, int> ();
            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                int bid = dti.TrackId;
                ids.Add (bid, bid);
            }

            // make one table the reference and let db synch the rest
            List<int> keyList = null;

            // get data from db
            lock (db_synch) {
                keyList = db.GetTrackDataKeyList ();
            }

            // remove deleted TrackData
            foreach (int id in keyList) {
                    if (!ids.ContainsKey (id)) {
                        Hyena.Log.DebugFormat ("NoNoise/BLA - removing bid {0} from TrackData...", id);
                        try {
                            lock (db_synch) {
                                // remove from TrackData
                                db.RemoveTrackDataForTrack (id);
                            }
                        } catch (Exception e) {
                            Hyena.Log.Exception("NoNoise - DB remove problem", e);
                        }
                    }
            }

            // update other tables
            Hyena.Log.Debug ("NoNoise/BLA - updating other tables...");
            lock (db_synch) {
                db.SynchTablesWithTrackData ();
            }

            lock (update_synch) {
                updating_db = false;
            }
        }
        #endregion

        #region Library change handlers

        /// <summary>
        /// Handles the tracks added event when the user adds new files to the music library.
        /// </summary>
        /// <param name='sender'>
        /// Sender.
        /// </param>
        /// <param name='args'>
        /// Arguments.
        /// </param>
        private void HandleTracksAdded (Source sender, TrackEventArgs args)
        {
            Hyena.Log.Debug ("NoNoise/BLA - tracks added (untested)");

            try {
//                UpdateMusicLibrary ();
    
                if (analyzing_lib) {
                    // check for missed files after the scan finished...
                    lock (scan_synch) {
                        added_while_scanning = true;
                    }
    
                    return;
                }
    
                CheckLibScanned ();
    
                if (!CheckDataUpToDate ()) {
                    new Thread (new ThreadStart (PcaForMusicLibraryVectorEdition)).Start ();
                    new Thread (new ThreadStart (WriteTrackInfosToDB)).Start ();
                }
            } catch (Exception e) {
                Hyena.Log.Exception ("NoNoise/BLA - tracks added handler exception", e);
            }
        }

        /// <summary>
        /// Handles the tracks deleted event when the user removes files from the music library.
        /// </summary>
        /// <param name='sender'>
        /// Sender.
        /// </param>
        /// <param name='args'>
        /// Arguments.
        /// </param>
        private void HandleTracksDeleted (Source sender, TrackEventArgs args)
        {
            Hyena.Log.Debug ("NoNoise/BLA - tracks deleted (untested)");

            if (!CheckDataUpToDate () && !updating_db) {
                try {
                    RemoveDeletedTracks ();
                } catch (Exception e) {
                    Hyena.Log.Exception ("NoNoise/BLA - tracks deleted handler exception", e);
                }
            }
        }

        /// <summary>
        /// Handles the tracks changed event when the user modifies files in the music library.
        /// </summary>
        /// <param name='sender'>
        /// Sender.
        /// </param>
        /// <param name='args'>
        /// Arguments.
        /// </param>
        private void HandleTracksChanged (Source sender, TrackEventArgs args)
        {
            try {
                // TODO remove
                Hyena.Log.Debug ("NoNoise/BLA - tracks changed (unhandled): " +
                                 args.ChangedFields.ToString ());
            } catch (Exception e) {
                if (args == null || args.ChangedFields == null)
                    Hyena.Log.Debug ("NoNoise/BLA - tracks changed (unhandled): args or CF null");
                else
                    Hyena.Log.Debug ("NoNoise/BLA - tracks changed (unhandled): CF not null");
            }
        }
        #endregion

        #region To delete

        /// <summary>
        /// Scans the music library and stores the mirage data in the database.
        /// Calls <see cref="Finished ()"/> when everything has been scanned.
        /// </summary>
        private void ScanMusicLibrary ()
        {
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

//            for (int i = 0; i < ml.TrackModel.Count; i++) {
            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                if (stop_scan) {
                    lock (scan_synch) {
                        analyzing_lib = false;
                    }
                    return;
                }

                try {
//                    TrackInfo ti = ml.TrackModel [i];
//                    string absPath = ti.Uri.AbsolutePath;
//                    int bid = ml.GetTrackIdForUri (ti.Uri);
                    string absPath = dti.Uri.AbsolutePath;
                    int bid = dti.TrackId;

                    if (!mfccMap.ContainsKey (bid)) {
                        mfcc = Mirage.Analyzer.AnalyzeMFCC (absPath);

                        lock (db_synch) {
                            if (!db.InsertMatrix (mfcc, bid))
                                Hyena.Log.Error ("NoNoise - Matrix insert failed");
                        }
                        db_cnt++;
                    }

                    if ((DateTime.Now - dt).TotalSeconds > 20.0) {
                        int perc = (int)((double)db_cnt / (double)ml_cnt * 100.0);
                        if (perc <= 100)
                            Hyena.Log.InformationFormat ("NoNoise/Scan - {0}% finished.", perc);
                        dt = DateTime.Now;
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception ("NoNoise - MFCC/DB Problem", e);
                }
            }

            finished.WakeupMain ();
        }

        /// <summary>
        /// Checks for each track in the music library if there is already
        /// MIR data in the database. If not, computes the MFCC matrix and
        /// stores it in the database.
        /// </summary>
        private void PcaForMusicLibrary ()
        {
            if (data_up_to_date) {
                Hyena.Log.Information ("NoNoise - Data already up2date - aborting pca.");
                return;
            }

            if (analyzing_lib) {
                Hyena.Log.Information ("NoNoise - Music library is currently beeing scanned - aborting pca.");
                return;
            }

            if (!lib_scanned) {
                Hyena.Log.Information ("NoNoise - No mirage data available for pca - aborting.");
                return;     // TODO something clever!
            }

            Hyena.Log.Debug ("NoNoise/BLA - PcaFor... called");

            PCAnalyzer ana = new PCAnalyzer();
            Dictionary<int, Mirage.Matrix> mfccMap = null;
            lock (db_synch) {
                mfccMap = db.GetMirageMatrices ();
            }
            if (mfccMap == null)
                Hyena.Log.Error ("NoNoise/BLA - mfccMap is null!");

            foreach (int bid in mfccMap.Keys) {
                try {
                    if (!ana.AddEntry (bid, ConvertMirageVector (mfccMap[bid].Mean ())))
                        throw new Exception("AddEntry failed!");
//                        if (!ana.AddEntry (bid, ConvertMfccMean(mfcc.Mean()), ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
//                        if (!ana.AddEntry (bid, null, ti.Bpm, ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
                } catch (Exception e) {
                    Hyena.Log.Exception("NoNoise - PCA Problem", e);
                }
            }

//            PcaTestings (ana, mfccMap);

            try {
                ana.PerformPCA ();

                lock (db_synch) {
                    db.ClearPcaData ();
                    if (!db.InsertPcaCoordinates (ana.Coordinates))
                        Hyena.Log.Error ("NoNoise - PCA coord insert failed");
                    coords = db.GetPcaCoordinates ();
                }
            } catch (Exception e) {
                Hyena.Log.Exception("PCA Problem", e);
            }
        }

        private void Testing ()
        {
            TrackInfo ti = ml.TrackModel [0];
//            int bid = ml.GetTrackIdForUri (ti.Uri);

            try {
                int ind = DatabaseTrackInfo.GetTrackIdForUri (ti.Uri);
                TrackInfo ti2 = DatabaseTrackInfo.Provider.FetchSingle (ind);
                Hyena.Log.DebugFormat ("NoNoise/BLA - index: {0}", ind);
                Hyena.Log.DebugFormat ("NoNoise/BLA - test result. title: {0} vs {1}",
                                       ti.TrackTitle, ti2.TrackTitle);
            } catch (Exception e) {
                Hyena.Log.Exception ("NoNoise/BLA - test failed", e);
            }
        }

        private void DetectBPMs (TrackInfo track)
        {
            // on button pressed
            if (track != null) {
                detector.ProcessFile (track.Uri);
            }
        }

        private void DetectBPMs ()
        {
            TrackInfo ti = ml.TrackModel [0];
            DetectBPMs (ti);
        }

        private void OnFileFinished (object o, BpmEventArgs args)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate {
                int id = DatabaseTrackInfo.GetTrackIdForUri (args.Uri);
                if (id >= 0) {
                    TrackInfo ti = DatabaseTrackInfo.Provider.FetchSingle (id);
                    Hyena.Log.Debug("NoNoise - BPM...Track: " + ti.TrackTitle);
                    ti.Bpm = args.Bpm;
                    ti.Update ();
                    Hyena.Log.DebugFormat ("NoNoise - Detected BPM of {0} for {1}", args.Bpm, ti.TrackTitle);
                }
            });
        }

        private void PcaTestings (PCAnalyzer ana)
        {
            Dictionary<int, Mirage.Vector> vectorMap = null;

            switch (pca_mode) {
            case PCA_MEAN:
                lock (db_synch) {
                    vectorMap = db.GetMirageMeanVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MEAN_DUR:
                lock (db_synch) {
                    vectorMap = db.GetMirageMeanVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                    try {
                        int bid = dti.TrackId;

                        if (!vectorMap.ContainsKey (bid)) {
                            Hyena.Log.Debug ("NoNoise/BLA - skipping bid: " + bid);
                            continue;
                        }
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid]),
                                           dti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_SQR_MEAN:
                lock (db_synch) {
                    vectorMap = db.GetMirageSquaredMeanVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_SQR_MEAN_DUR:
                lock (db_synch) {
                    vectorMap = db.GetMirageSquaredMeanVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                    try {
                        int bid = dti.TrackId;

                        if (!vectorMap.ContainsKey (bid)) {
                            Hyena.Log.Debug ("NoNoise/BLA - skipping bid: " + bid);
                            continue;
                        }
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid]),
                                           dti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MAX:
                lock (db_synch) {
                    vectorMap = db.GetMirageMaxVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MAX_DUR:
                lock (db_synch) {
                    vectorMap = db.GetMirageMaxVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                    try {
                        int bid = dti.TrackId;

                        if (!vectorMap.ContainsKey (bid)) {
                            Hyena.Log.Debug ("NoNoise/BLA - skipping bid: " + bid);
                            continue;
                        }
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid]),
                                           dti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MIN:
                lock (db_synch) {
                    vectorMap = db.GetMirageMinVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MIN_DUR:
                lock (db_synch) {
                    vectorMap = db.GetMirageMinVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                    try {
                        int bid = dti.TrackId;

                        if (!vectorMap.ContainsKey (bid)) {
                            Hyena.Log.Debug ("NoNoise/BLA - skipping bid: " + bid);
                            continue;
                        }
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid]),
                                           dti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MED:
                lock (db_synch) {
                    vectorMap = db.GetMirageMedianVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
                break;

            case PCA_MED_DUR:
                lock (db_synch) {
                    vectorMap = db.GetMirageMedianVectors ();
                }
                if (vectorMap == null)
                    Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

                foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                    try {
                        int bid = dti.TrackId;

                        if (!vectorMap.ContainsKey (bid)) {
                            Hyena.Log.Debug ("NoNoise/BLA - skipping bid: " + bid);
                            continue;
                        }
                        if (!ana.AddEntry (bid, ConvertMirageVector (vectorMap [bid]),
                                           dti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
                break;

            default:
                Hyena.Log.Debug ("NoNoise/BLA - default pca case, ignoring...");
                break;
            }
        }

        /// <summary>
        /// Compares the dictionary with the trackinfos to the music library
        /// and updates it (removes deleted tracks and adds new ones).
        /// </summary>
//        private void UpdateMusicLibrary ()
//        {
////            for (int i = 0; i < ml.TrackModel.Count; i++) {
//            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
////                TrackInfo ti = ml.TrackModel [i];
////                int bid = ml.GetTrackIdForUri (ti.Uri);
//                int bid = dti.TrackId;
//
//                lock (lib_synch) {
//                    if (!library.ContainsKey (bid)) {
//                        Hyena.Log.DebugFormat ("NoNoise/BLA - adding bid {0} to library...", bid);
//                        library.Add (bid, dti as TrackInfo);
//                    }
//                }
//            }
//
//            SortedList<int, int> ids = new SortedList<int, int> ();
////            for (int i = 0; i < ml.TrackModel.Count; i++) {
//            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
//                int bid = dti.TrackId;
//                ids.Add (bid, bid);
//            }
//
//            List<int> rem = new List<int> ();
//
//            lock (lib_synch) {
//                foreach (int bid in library.Keys) {
//                    if (!ids.ContainsKey (bid)) {
//                        Hyena.Log.DebugFormat ("NoNoise/BLA - removing track {0} from library...", bid);
//                        rem.Add (bid);
//                    }
//                }
//            }
//
//            foreach (int r in rem) {
//                lock (lib_synch) {
//                    library.Remove (r);
//                }
//            }
//
//            Hyena.Log.DebugFormat ("NoNoise/BLA - Updated music library. new size: {0}", library.Count);
//        }

        /// <summary>
        /// Stores the trackinfos from the music library in a sorted dictionary
        /// mapped to the banshee_id.
        /// </summary>
//        private void ConvertMusicLibrary ()
//        {
//            lock (lib_synch) {
//                library = new SortedDictionary<int, TrackInfo> ();
//
//                for (int i = 0; i < ml.TrackModel.Count; i++) {
//                    DatabaseTrackInfo dti = ml.TrackModel [i] as DatabaseTrackInfo;
//    //                int bid = ml.GetTrackIdForUri (ti.Uri);
//                    library.Add (dti.TrackId, dti as TrackInfo);
//                }
//            }
//
//            Hyena.Log.Debug ("NoNoise/BLA - library conversion finished");
//        }

        /*
         * rather useless
        private void CheckGetTrackID ()
        {
            Dictionary<int, TrackData> trackMap = null;
            lock (db_synch) {
                trackMap = db.GetTrackDataDictionary ();
            }
            if (trackMap == null) {
                Hyena.Log.Error ("NoNoise/BLA - trackMap is null!");
                return;
            }
            Hyena.Log.Debug ("NoNoise/BLA - trackMap size: " + trackMap.Count);

            int cnt = 0;

            for (int i = 0; i < ml.TrackModel.Count; i++) {
                try {
                    TrackInfo ti = ml.TrackModel [i];
                    int bid = ml.GetTrackIdForUri (ti.Uri);

                    if (trackMap.ContainsKey (bid)) {
                        if (!(ti.ArtistName.Equals (trackMap [bid].Artist) && ti.TrackTitle.Equals (trackMap [bid].Title)
                                                  && ti.AlbumTitle.Equals (trackMap [bid].Album))) {
                            Hyena.Log.ErrorFormat ("NoNoise/BLA - id and info do not match: artist: {0} vs {1}, title: " +
                             "{2} vs {3}, album: {4} vs {5}", ti.ArtistName, trackMap [bid].Artist, ti.TrackTitle,
                                                   trackMap [bid].Title, ti.AlbumTitle, trackMap [bid].Album);
                            cnt++;
                        }
                    } else {
                        Hyena.Log.Error ("NoNoise/BLA - No key: " + bid);
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception ("NoNoise/BLA - everything failed.", e);
                    return;
                }
            }
            Hyena.Log.DebugFormat ("NoNoise/BLA - {0} mismatches", cnt);
        }
        */
        #endregion

        #region PCA variants
        private interface IPcaAdder {
            /// <summary>
            /// Adds feature vectors from the database to the PCAnalyzer,
            /// depending on the implementing class.
            /// </summary>
            /// <param name="ana">
            /// The <see cref="PCAnalyzer"/>
            /// </param>
            void AddVectorsFromDB (PCAnalyzer ana);
        }

        private class PcaMeanAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMeanVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMeanDurationAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMeanVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        TrackInfo ti = DatabaseTrackInfo.Provider.FetchSingle (bid) as TrackInfo;

                        if (ti == null) {
                            if (!BansheeLibraryAnalyzer.Singleton.updating_db)
                                new Thread (new ThreadStart (BansheeLibraryAnalyzer.Singleton.RemoveDeletedTracks))
                                    .Start ();
                            continue;
                        }

                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid]),
                                           ti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaSqrMeanAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageSquaredMeanVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaSqrMeanDurationAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageSquaredMeanVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        TrackInfo ti = DatabaseTrackInfo.Provider.FetchSingle (bid) as TrackInfo;

                        if (ti == null) {
                            if (!BansheeLibraryAnalyzer.Singleton.updating_db)
                                new Thread (new ThreadStart (BansheeLibraryAnalyzer.Singleton.RemoveDeletedTracks))
                                    .Start ();
                            continue;
                        }

                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid]),
                                           ti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMaxAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMaxVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMaxDurationAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMaxVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        TrackInfo ti = DatabaseTrackInfo.Provider.FetchSingle (bid) as TrackInfo;

                        if (ti == null) {
                            if (!BansheeLibraryAnalyzer.Singleton.updating_db)
                                new Thread (new ThreadStart (BansheeLibraryAnalyzer.Singleton.RemoveDeletedTracks))
                                    .Start ();
                            continue;
                        }

                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid]),
                                           ti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMinAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMinVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMinDurationAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMinVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        TrackInfo ti = DatabaseTrackInfo.Provider.FetchSingle (bid) as TrackInfo;

                        if (ti == null) {
                            if (!BansheeLibraryAnalyzer.Singleton.updating_db)
                                new Thread (new ThreadStart (BansheeLibraryAnalyzer.Singleton.RemoveDeletedTracks))
                                    .Start ();
                            continue;
                        }

                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid]),
                                           ti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMedianAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMedianVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid])))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - PCA Problem", e);
                    }
                }
            }
        }

        private class PcaMedianDurationAdder : IPcaAdder {
            public void AddVectorsFromDB (PCAnalyzer ana)
            {
                Dictionary<int, Mirage.Vector> vectorMap = null;
                lock (BansheeLibraryAnalyzer.Singleton.db_synch) {
                    vectorMap = BansheeLibraryAnalyzer.Singleton.db.GetMirageMedianVectors ();
                }
                if (vectorMap == null)
                    throw new DatabaseException ("vectorMap is null!");

                foreach (int bid in vectorMap.Keys) {
                    try {
                        TrackInfo ti = DatabaseTrackInfo.Provider.FetchSingle (bid) as TrackInfo;

                        if (ti == null) {
                            if (!BansheeLibraryAnalyzer.Singleton.updating_db)
                                new Thread (new ThreadStart (BansheeLibraryAnalyzer.Singleton.RemoveDeletedTracks))
                                    .Start ();
                            continue;
                        }

                        if (!ana.AddEntry (bid, BansheeLibraryAnalyzer.Singleton.
                                           ConvertMirageVector (vectorMap [bid]),
                                           ti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    }
                }
            }
        }
        #endregion
    }
}

