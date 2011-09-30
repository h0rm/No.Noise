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
// TODO track_updated listener + db updates

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
        private readonly bool STORE_ENTIRE_MATRIX = false;
        private readonly bool DB_CHEATER_MODE = false;

        #region Members
        private Banshee.Library.MusicLibrarySource ml;
//        private SortedDictionary<int, TrackInfo> library;
        private NoNoiseClutterSourceContents sc;
        private NoNoiseDBHandler db;
        private PCAnalyzer ana;
        private List<DataEntry> coords;
        private bool analyzing_lib;
        private bool lib_scanned;
        private bool data_up_to_date;
        private bool stop_scan;
        private bool added_while_scanning;
        private Gtk.ThreadNotify finished;
        private Thread thr;
        private object scan_synch;
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

        private BansheeLibraryAnalyzer ()
        {
            ml = ServiceManager.SourceManager.MusicLibrary;

            scan_synch = new object ();
//            lib_synch = new object ();
            db_synch = new object ();

//            Hyena.Log.Debug ("NoNoise/BLA - converting music lib");
//            new Thread (new ThreadStart (ConvertMusicLibrary)).Start ();

            db = new NoNoiseDBHandler ();

//            Testing ();
            // BPM detector
//            detector = BpmDetectJob.GetDetector ();
//            if (detector != null) {
//                detector.FileFinished += OnFileFinished;
//                Hyena.Log.Debug("NoNoise - Detector is not null");
//            }
//            new Thread (new ThreadStart (DetectBPMs)).Start ();

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
            lib_scanned = CheckLibScanned ();
            data_up_to_date = CheckDataUpToDate ();

            Hyena.Log.Debug ("NoNoise/BLA - adding library change handler");
            ml.TracksAdded += HandleTracksAdded;
            ml.TracksDeleted += HandleTracksDeleted;
            ml.TracksChanged += HandleTracksChanged;

            Hyena.Log.Debug ("NoNoise/BLA - starting pca query");
//            new Thread (new ThreadStart (GetPcaData)).Start ();
            GetPcaData ();

//            Hyena.Log.Debug ("NoNoise/BLA - blabla: " + coords [0].Value.Artist + " - " + coords [0].Value.Title);

            Hyena.Log.Debug ("NoNoise/BLA - starting pca/write track data threads");
            if (STORE_ENTIRE_MATRIX)
                new Thread (new ThreadStart (PcaForMusicLibrary)).Start ();
            else
                new Thread (new ThreadStart (PcaForMusicLibraryVectorEdition)).Start ();
            new Thread (new ThreadStart (WriteTrackInfosToDB)).Start ();
        }

        /// <summary>
        /// Initializes the singleton instance of this class and starts PCA
        /// computations if the library has been scanned already. Also causes
        /// TrackData to be written to the database if it is not current anymore.
        /// </summary>
        /// <param name="sc">
        /// The <see cref="NoNoiseClutterSourceContents"/> which is used as callback.
        /// </param>
        /// <param name="forceNew">
        /// If this is true then a new instance will be initialized even if an
        /// old one already exists. Otherwise Init () might return an existing
        /// instance.
        /// </param>
        /// <returns>
        /// The singleton instance of <see cref="BansheeLibraryAnalyzer"/>
        /// </returns>
        public static BansheeLibraryAnalyzer Init (NoNoiseClutterSourceContents sc, bool forceNew)
        {
            if (!forceNew && bla != null)
                return bla;

            bla = new BansheeLibraryAnalyzer ();
            bla.sc = sc;

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
            lock (db_synch) {       // TODO check for removed (<)
                cnt = db.GetMirDataCount ();
//                Hyena.Log.Debug ("NoNoise/DB - MIRData count: " + cnt);
            }

            lock (scan_synch) {
//                Hyena.Log.Debug ("NoNoise/DB - tm count: " + ml.TrackModel.Count);
                lib_scanned = (cnt == ml.TrackModel.Count);
            }
            Hyena.Log.Debug ("NoNoise/BLA - lib scanned: " + lib_scanned);

            if (cnt > ml.TrackModel.Count)      // FIXME should be in both checks, but who knows what happens
                new Thread (new ThreadStart (RemoveDeletedTracks)).Start (); // when they both run at the same time

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
            lock (db_synch) {       // TODO check for removed (<)
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
                if (STORE_ENTIRE_MATRIX)
                    thr = new Thread (new ThreadStart (ScanMusicLibrary));
                else
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
                    Hyena.Log.Exception ("NoNoise - MFCC/DB Problem", e);
                }
            }

            finished.WakeupMain ();
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
            Dictionary<int, Mirage.Vector> vectorMap = null;
            lock (db_synch) {
                db_cnt = db.GetMirDataCount ();
                vectorMap = db.GetMirageVectors ();
            }
            if (vectorMap == null)
                Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

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
    
                        if (!vectorMap.ContainsKey (bid)) {
                            mfcc = Mirage.Analyzer.AnalyzeMFCC (absPath);
    
                            lock (db_synch) {
                                if (!db.InsertVector (mfcc.Mean (), bid))
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

                if (STORE_ENTIRE_MATRIX)
                    new Thread (new ThreadStart (ScanMusicLibrary)).Start ();
                else
                    new Thread (new ThreadStart (ScanMusicLibraryVectorEdition)).Start ();

                return;
            }

            lock (scan_synch) {
                lib_scanned = true;
                analyzing_lib = false;
            }
            sc.ScanFinished ();
            if (STORE_ENTIRE_MATRIX)
                new Thread (new ThreadStart (PcaForMusicLibrary)).Start ();
            else
                new Thread (new ThreadStart (PcaForMusicLibraryVectorEditionForceNew)).Start ();
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

        private void PcaForMusicLibraryVectorEdition ()
        {
            PcaForMusicLibraryVectorEdition (false);
        }

        private void PcaForMusicLibraryVectorEditionForceNew ()
        {
            PcaForMusicLibraryVectorEdition (true);
        }

        /// <summary>
        /// Checks for each track in the music library if there is already
        /// MIR data in the database. If not, computes the MFCC matrix and
        /// stores it in the database.
        /// Vector Edition!
        /// </summary>
        private void PcaForMusicLibraryVectorEdition (bool forceNew)
        {
            if (data_up_to_date && !forceNew) {
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

            ana = new PCAnalyzer ();
            Dictionary<int, Mirage.Vector> vectorMap = null;
            lock (db_synch) {
                vectorMap = db.GetMirageVectors ();
            }
            if (vectorMap == null)
                Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
//            lock (lib_synch) {
//                foreach (int bid in library.Keys) {
                    try {
                        TrackInfo ti = dti as TrackInfo;
//                        TrackInfo ti = library [bid];
                        int bid = dti.TrackId;
    
                        if (!vectorMap.ContainsKey (bid)) {
                            Hyena.Log.Debug ("NoNoise/BLA - skipping bid: " + bid);
                            continue;
                        }
    
    //                    Hyena.Log.Debug ("bid: " + bid + ", uri: " + ti.Uri);
    
    //                    if (!ana.AddEntry (bid, ConvertMfccMean (vectorMap [bid])))
    //                        throw new Exception ("AddEntry failed!");
                        if (!ana.AddEntry (bid, ConvertMfccMean (vectorMap [bid]),
                                           ti.Duration.TotalSeconds))
                            throw new Exception("AddEntry failed!");
    //                        if (!ana.AddEntry (bid, null, ti.Bpm, ti.Duration.TotalSeconds))
    //                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception ("NoNoise - PCA Problem", e);
//                        return;
                    }
//                }
            }

            try {
                ana.PerformPCA ();

                lock (db_synch) {
                    db.ClearPcaData ();
                    if (!db.InsertPcaCoordinates (ana.Coordinates))
                        Hyena.Log.Error ("NoNoise - PCA coord insert failed");
                    coords = db.GetPcaCoordinates ();
                }
            } catch (Exception e) {
                Hyena.Log.Exception ("PCA Problem", e);
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

        /// <summary>
        /// Checks for each track in the music library if it is already in the
        /// database. If not, inserts it.
        /// </summary>
        private void WriteTrackInfosToDB ()
        {
            if (data_up_to_date) {
                Hyena.Log.Information ("NoNoise - Data already up2date - aborting write-track-infos.");
                return;
            }

            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
//            lock (lib_synch) {
//                foreach (int bid in library.Keys) {
                    try {
                        TrackInfo ti = dti as TrackInfo;
//                        TrackInfo ti = library [bid];
                        int bid = dti.TrackId;     // DB call from different thread! think that's the problem...
    
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
//                }
            }
        }

        #region Library updates

        /// <summary>
        /// Checks the music library for deleted tracks and removes them from the database.
        /// </summary>
        private void RemoveDeletedTracks ()
        {
            SortedList<int, int> ids = new SortedList<int, int> ();
            foreach (DatabaseTrackInfo dti in DatabaseTrackInfo.Provider.FetchAll ()) {
                int bid = dti.TrackId;
                ids.Add (bid, bid);
            }
//            foreach (int bid in library.Keys)
//                ids.Add (bid, bid);

            /// make one table the reference and let db synch the rest!!

//            Dictionary<int, Mirage.Vector> vectorMap = null;
//            Dictionary<int, Mirage.Matrix> mfccMap = null;
            Dictionary<int, TrackData> trackMap = null;
//            Dictionary<int, DataEntry> pcaMap = null;

            // get data from db
            lock (db_synch) {
//                if (STORE_ENTIRE_MATRIX)
//                    mfccMap = db.GetMirageMatrices ();
//                else
//                    vectorMap = db.GetMirageVectors ();
                trackMap = db.GetTrackDataDictionary ();
//                pcaMap = db.GetPcaCoordinatesDictionary ();
            }

            // remove deleted MIRData
//            if (STORE_ENTIRE_MATRIX) {
//                foreach (int id in mfccMap.Keys) {
//                    lock (lib_synch) {
//                        if (!library.ContainsKey (id)) {
//                            Hyena.Log.DebugFormat ("NoNoise/BLA - removing bid {0} from MIRData...", id);
//                            try {
//                                lock (db_synch) {
//                                    // remove from MIRData
//                                    db.RemoveMirDataForTrack (id);
//                                }
//                            } catch (Exception e) {
//                                Hyena.Log.Exception("NoNoise - DB remove problem", e);
//                            }
//                        }
//                    }
//                }
//            } else {
//                foreach (int id in vectorMap.Keys) {
//                    lock (lib_synch) {
//                        if (!library.ContainsKey (id)) {
//                            Hyena.Log.DebugFormat ("NoNoise/BLA - removing bid {0} from MIRData...", id);
//                            try {
//                                lock (db_synch) {
//                                    // remove from MIRData
//                                    db.RemoveMirDataForTrack (id);
//                                }
//                            } catch (Exception e) {
//                                Hyena.Log.Exception("NoNoise - DB remove problem", e);
//                            }
//                        }
//                    }
//                }
//            }

            // remove deleted TrackData
            foreach (int id in trackMap.Keys) {
//                lock (lib_synch) {
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
//                }
            }

            // update other tables
            Hyena.Log.Debug ("NoNoise/BLA - updating other tables...");
            lock (db_synch) {
                db.SynchTablesWithTrackData ();
            }

            // remove deleted PCAData
//            foreach (int id in pcaMap.Keys) {
//                lock (lib_synch) {
//                    if (!library.ContainsKey (id)) {
//                        Hyena.Log.DebugFormat ("NoNoise/BLA - removing bid {0} from PCAData...", id);
//                        try {
//                            lock (db_synch) {
//                                // remove from PCAData
//                                db.RemovePcaDataForTrack (id);
//                            }
//                        } catch (Exception e) {
//                            Hyena.Log.Exception("NoNoise - DB remove problem", e);
//                        }
//                    }
//                }
//            }
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
                CheckDataUpToDate ();
    
                if (!data_up_to_date) {
                    if (STORE_ENTIRE_MATRIX)
                        new Thread (new ThreadStart (PcaForMusicLibrary)).Start ();
                    else
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

            if (!CheckDataUpToDate ()) {
                try {
//                    UpdateMusicLibrary ();
                    RemoveDeletedTracks ();     // TODO in new thread ?
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
                // TODO implement
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
    }
}

