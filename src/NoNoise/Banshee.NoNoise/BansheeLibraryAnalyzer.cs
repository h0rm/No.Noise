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
using Banshee.Sources;

using NoNoise.Data;
using NoNoise.PCA;

// TODO something like compute_pca button
// TODO track_added/track_removed/track_updated listener + db updates

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
//        private Banshee.Collection.TrackListModel tm;
        private NoNoiseSourceContents sc;
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
        private object db_synch;
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

            ml.TracksAdded += HandleTracksAdded;
            ml.TracksDeleted += HandleTracksDeleted;
            ml.TracksChanged += HandleTracksChanged;

            if (STORE_ENTIRE_MATRIX)
                new Thread (new ThreadStart (PcaForMusicLibrary)).Start ();
            else
                new Thread (new ThreadStart (PcaForMusicLibraryVectorEdition)).Start ();
            new Thread (new ThreadStart (WriteTrackInfosToDB)).Start ();

            CheckGetTrackID ();
        }

        /// <summary>
        /// Initializes the singleton instance of this class and starts PCA
        /// computations if the library has been scanned already. Also causes
        /// TrackData to be written to the database if it is not current anymore.
        /// </summary>
        /// <param name="sc">
        /// The <see cref="NoNoiseSourceContents"/> which is used as callback.
        /// May NOT be null.
        /// </param>
        /// <returns>
        /// The singleton instance of <see cref="BansheeLibraryAnalyzer"/>
        /// </returns>
        public static BansheeLibraryAnalyzer Init (NoNoiseSourceContents sc)
        {
            bla = new BansheeLibraryAnalyzer ();
            bla.sc = sc;

//            bla.PcaForMusicLibrary ();
//            bla.WriteTrackInfosToDB ();

            return bla;
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
//            tm = ServiceManager.SourceManager.MusicLibrary.TrackModel;
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
//            tm = ServiceManager.SourceManager.MusicLibrary.TrackModel;
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
//            ml = ServiceManager.SourceManager.MusicLibrary;
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
                new Thread (new ThreadStart (PcaForMusicLibraryVectorEdition)).Start ();
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
        /// Checks for each track in the music library if there is already
        /// MIR data in the database. If not, computes the MFCC matrix and
        /// stores it in the database.
        /// Vector Edition!
        /// </summary>
        private void PcaForMusicLibraryVectorEdition ()
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

            Hyena.Log.Debug ("NoNoise/BLA - PcaFor... called");

            ana = new PCAnalyzer ();
            Dictionary<int, Mirage.Vector> vectorMap = null;
            lock (db_synch) {
                vectorMap = db.GetMirageVectors ();
            }
            if (vectorMap == null)
                Hyena.Log.Error ("NoNoise/BLA - vectorMap is null!");

            for (int i = 0; i < ml.TrackModel.Count; i++) {
                try {
                    TrackInfo ti = ml.TrackModel [i];
                    int bid = ml.GetTrackIdForUri (ti.Uri);

                    Hyena.Log.Debug ("bid: " + bid + ", uri: " + ti.Uri);

//                    if (!ana.AddEntry (bid, ConvertMfccMean (vectorMap [bid])))
//                        throw new Exception ("AddEntry failed!");
                    if (!ana.AddEntry (ml.GetTrackIdForUri (ti.Uri), ConvertMfccMean (vectorMap [bid]),
                                       ti.Duration.TotalSeconds)) {
                        Hyena.Log.Debug ("Getting uri again..." + ml.GetTrackIdForUri (ti.Uri));
                        throw new Exception("AddEntry failed!");
                    }
//                        if (!ana.AddEntry (bid, null, ti.Bpm, ti.Duration.TotalSeconds))
//                            throw new Exception("AddEntry failed!");
                } catch (Exception e) {
                    Hyena.Log.Exception ("NoNoise - PCA Problem", e);
                    return;
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

        private void CheckGetTrackID ()
        {
            Dictionary<int, TrackData> trackMap = null;
            lock (db_synch) {
                trackMap = db.GetTrackDataDictionary ();
            }
            if (trackMap == null)
                Hyena.Log.Error ("NoNoise/BLA - trackMap is null!");

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
                        }
                    } else {
                        Hyena.Log.Error ("NoNoise/BLA - No key: " + bid);
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception ("NoNoise/BLA - everything failed.", e);
                }
            }
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

        private void RemoveDeletedTracks ()
        {
            SortedList<int, int> ids = new SortedList<int, int> (ml.TrackModel.Count);
            for (int i = 0; i < ml.TrackModel.Count; i++) {
                int bid = ml.GetTrackIdForUri (ml.TrackModel [i].Uri);
                ids.Add (bid, bid);
            }

            Dictionary<int, Mirage.Vector> vectorMap = null;
            Dictionary<int, Mirage.Matrix> mfccMap = null;
            Dictionary<int, TrackData> trackMap = null;
            Dictionary<int, DataEntry> pcaMap = null;

            lock (db_synch) {
                if (STORE_ENTIRE_MATRIX)
                    mfccMap = db.GetMirageMatrices ();
                else
                    vectorMap = db.GetMirageVectors ();
                trackMap = db.GetTrackDataDictionary ();
                pcaMap = db.GetPcaCoordinatesDictionary ();
            }

            if (STORE_ENTIRE_MATRIX) {
                foreach (int id in mfccMap.Keys) {
                    if (!ids.ContainsKey (id)) {
                        try {
                            lock (db_synch) {
                                // TODO remove from MIRData
                                db.RemoveMirDataForTrack (id);
                            }
                        } catch (Exception e) {
                            Hyena.Log.Exception("NoNoise - DB remove problem", e);
                        }
                    }
                }
            } else {
                foreach (int id in vectorMap.Keys) {
                    if (!ids.ContainsKey (id)) {
                        try {
                            lock (db_synch) {
                                // TODO remove from MIRData
                                db.RemoveMirDataForTrack (id);
                            }
                        } catch (Exception e) {
                            Hyena.Log.Exception("NoNoise - DB remove problem", e);
                        }
                    }
                }
            }

            foreach (int id in trackMap.Keys) {
                if (!ids.ContainsKey (id)) {
                    try {
                        lock (db_synch) {
                            // TODO remove from TrackData
                            db.RemoveTrackDataForTrack (id);
                        }
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - DB remove problem", e);
                    }
                }
            }

            foreach (int id in pcaMap.Keys) {
                if (!ids.ContainsKey (id)) {
                    try {
                        lock (db_synch) {
                            // TODO remove from PCAData
                            db.RemovePcaDataForTrack (id);
                        }
                    } catch (Exception e) {
                        Hyena.Log.Exception("NoNoise - DB remove problem", e);
                    }
                }
            }
        }

        private void HandleTracksAdded (Source sender, TrackEventArgs args)
        {
            Hyena.Log.Debug ("NoNoise/BLA - tracks added (unhandled)");

            if (analyzing_lib) {
                // TODO check for missed files after the scan finished...
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
        }

        private void HandleTracksDeleted (Source sender, TrackEventArgs args)
        {
            Hyena.Log.Debug ("NoNoise/BLA - tracks deleted (unhandled)");

            if (!CheckDataUpToDate ()) {
                RemoveDeletedTracks ();     // TODO in new thread
            }
        }

        private void HandleTracksChanged (Source sender, TrackEventArgs args)
        {
            try {
                Hyena.Log.Debug ("NoNoise/BLA - tracks changed (unhandled): " +
                                 args.ChangedFields.ToString ());
            } catch (Exception e) {
                if (args == null || args.ChangedFields == null)
                    Hyena.Log.Debug ("NoNoise/BLA - tracks changed (unhandled): args or CF null");
                else
                    Hyena.Log.Debug ("NoNoise/BLA - tracks changed (unhandled): CF not null");
            }
        }
    }
}

