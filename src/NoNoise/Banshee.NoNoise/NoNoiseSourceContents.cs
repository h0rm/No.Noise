// 
// Foo1SourceContents.cs
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
using Banshee.Library;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Sources;

using Banshee.NoNoise.Bpm;
using NoNoise.PCA;
using NoNoise.Data;

using MathNet.Numerics.LinearAlgebra;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Collections;

using Lastfm.Services;
using Lastfm;

// TODO delete this class

namespace Banshee.NoNoise
{
    public class NoNoiseSourceContents : Banshee.Sources.Gui.ISourceContents
    {
        #region Members
        private Gtk.Label label = new Gtk.Label ("Custom view for NoNoise extension is working!\nTopAlbums: " + topalbums);
        private Gtk.Label emptylabel = new Gtk.Label("Shake your head it's empty!");
        private Gtk.Box w = new Gtk.HBox(true, 4);
        private Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
        private Gtk.Box bw = new Gtk.VButtonBox();
        private Gtk.Button button = new Gtk.Button();
        private Gtk.Label working = new Gtk.Label("retrieving artist information...");
        private Session session;
        private Gtk.ThreadNotify ready;
//        private Banshee.Library.MusicLibrarySource ml;
//        private MusicLibrarySource source;

        private bool painintheassdummy = false;
        private bool dotests = false;
//            private bool gatherMIRdata = false;
        private bool saveTrackInfosToDB = true;
        private bool dopca = true;
        private bool doprintlib = false;
        private bool doprintselection = false;

        private NoNoiseDBHandler db;
        private IBpmDetector detector;
        private int Bpm;

        public delegate void ScanFinishedEvent (object source, ScanFinishedEventArgs args);
        private ScanFinishedEvent scan_event;

        private string API_KEY =  "b6f3f5f1a92987f58a3ae75516c967a5";
        private string API_SECRET =   "1022d8e3a796243f8105fe97d1156803";
        private static string topalbums = "";
        #endregion

        public NoNoiseSourceContents ()
        {
            Hyena.Log.Debug("NoNoise - CustomView constructor");
            session = new Session(API_KEY, API_SECRET);

            db = new NoNoiseDBHandler ();

            BansheeLibraryAnalyzer.Init (null);

            if (dotests)
                PerformTests ();

            // PCA
//                if (dopca)
//                    PcaForMusicLibrary ();

            // TrackInfo in NoNoise DB
//                if (saveTrackInfosToDB)
//                    WriteTrackInfosToDB ();

            // BPM detector
            detector = BpmDetectJob.GetDetector ();
            if (detector != null) {
                detector.FileFinished += OnFileFinished;
                Hyena.Log.Debug("NoNoise - Detector is not null");
            }

            // using Active Source
            Source source = ServiceManager.SourceManager.ActiveSource;
            ITrackModelSource track_source = source as ITrackModelSource;

            if (track_source != null) {
                Hyena.Log.Debug("NoNoise - TS name: " + track_source.Name);
                Hyena.Log.Debug("NoNoise - TS count: " + track_source.Count);
                Hyena.Log.Debug("NoNoise - TS generic name: " + track_source.GenericName);
            }

            // using MusicLibrary
            if (doprintlib)
                PrintMusicLibrary();

            // selection test
            if (doprintselection)
                PrintSelection ();

            // button handler
            button.Pressed += ButtonPushHandler;
            button.Label = "get artist info";

            bw.Add(button);
            w.Add(emptylabel);
            w.Add(bw);

            w.ShowAll();
        }

        public void Scan (bool start)
        {
            Hyena.Log.Information ("NoNoise - Scan " + (start ? "started." : "paused."));
            BansheeLibraryAnalyzer.Singleton.Scan (start);
        }

        public void ScanFinished ()
        {
            Hyena.Log.Information ("NoNoise - Scan finished.");
            scan_event (this, new ScanFinishedEventArgs ("supi"));
        }

        #region Tests

        /// <summary>
        /// Runs several database and pca tests and prints debug output and failure/success messages.
        /// </summary>
        private void PerformTests ()
        {
            bool succ = true;
            try {
                succ &= DBMatrixTest ();
                succ &= DBMirageMatrixText ();
                succ &= DBMirageVectorTest ();
            } catch (Exception e) {
                Hyena.Log.Exception ("NoNoise - Tests failed", e);
                succ = false;
            }

            if (succ)
                Hyena.Log.Debug ("NoNoise - Tests finished successfully");
            else
                Hyena.Log.Debug ("NoNoise - Tests failed");
        }

        /// <summary>
        /// Test method for insert/select of a Math.Matrix into/from the database.
        /// </summary>
        /// <returns>
        /// True if the matrix is still the same after inserting it into the
        /// database and reading it from there again. False otherwise.
        /// </returns>
        private bool DBMatrixTest ()
        {
            Matrix m = new Matrix (20, 45);
            Random r = new Random ();
            for (int i = 0; i < m.RowCount; i++) {
                for (int j = 0; j < m.ColumnCount; j++) {
                    m [i, j] = (double) r.NextDouble ();
                }
            }

            Matrix m2 = DataParser.ParseMatrix (DataParser.MatrixToString (m));
            if (m.RowCount != m2.RowCount || m.ColumnCount != m2.ColumnCount) {
                Hyena.Log.Warning ("NoNoise/Testing - matrices don't have the same size");
                return false;
            }
            for (int i = 0; i < m.RowCount; i++) {
                for (int j = 0; j < m.ColumnCount; j++) {
                    if (!m [i, j].ToString ().Equals (m2 [i, j].ToString ())) {     // string precision
                        Hyena.Log.WarningFormat ("NoNoise/Testing - values at pos ({0}, {1}) are not the same ({2} | {3})",
                                                 i, j, m [i, j], m2 [i, j]);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Test method for insert/select of a Mirage.Matrix into/from the database.
        /// </summary>
        /// <returns>
        /// True if the matrix is still the same after inserting it into the
        /// database and reading it from there again. False otherwise.
        /// </returns>
        private bool DBMirageMatrixText ()
        {
            Mirage.Matrix m = new Mirage.Matrix (20, 45);
            Random r = new Random ();
            for (int i = 0; i < m.rows; i++) {
                for (int j = 0; j < m.columns; j++) {
                    m.d [i, j] = (float) r.NextDouble ();
                }
            }

            Mirage.Matrix m2 = DataParser.ParseMirageMatrix (DataParser.MirageMatrixToString (m));
            if (m.rows != m2.rows || m.columns != m2.columns) {
                Hyena.Log.Warning ("NoNoise/Testing - mirage matrices don't have the same size");
                return false;
            }
            for (int i = 0; i < m.rows; i++) {
                for (int j = 0; j < m.columns; j++) {
                    if (!m.d [i, j].ToString ().Equals (m2.d [i, j].ToString ())) {     // string precision
                        Hyena.Log.WarningFormat ("NoNoise/Testing - values at pos ({0}, {1}) are not the same ({2} | {3})",
                                                 i, j, m.d [i, j], m2.d [i, j]);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Test method for insert/select of a Mirage.Vector into/from the database.
        /// </summary>
        /// <returns>
        /// True if the vector is still the same after inserting it into the
        /// database and reading it from there again. False otherwise.
        /// </returns>
        private bool DBMirageVectorTest ()
        {
            Mirage.Vector v = new Mirage.Vector (20);
            Random r = new Random ();
            for (int i = 0; i < v.rows; i++) {
                v.d [i, 0] = (float) r.NextDouble ();
            }

            Mirage.Vector v2 = DataParser.ParseMirageVector (DataParser.MirageVectorToString (v));
            if (v.rows != v2.rows) {
                Hyena.Log.Warning ("NoNoise/Testing - mirage vectors don't have the same length");
                return false;
            }
            for (int i = 0; i < v.rows; i++) {
                if (!v.d [i, 0].ToString ().Equals (v2.d [i, 0].ToString ())) {     // string precision
                    Hyena.Log.WarningFormat ("NoNoise/Testing - values at pos {0} are not the same ({1} | {2})",
                                             i, v.d [i, 0], v2.d [i, 0]);
                    return false;
                }
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Prints debug information for an MFCC matrix.
        /// </summary>
        /// <param name="mfcc">
        /// A MFCC <see cref="Matrix"/>
        /// </param>
        private void DebugPrintMFCC (Mirage.Matrix mfcc)
        {
            Hyena.Log.Debug("Cols: " + mfcc.columns);
            Hyena.Log.Debug("Rows: " + mfcc.rows);
            Hyena.Log.Debug("Mean length: " + mfcc.Mean().rows);
            mfcc.Mean().Print();
        }

        /// <summary>
        /// Prints detailed information about each song in the MusicLibrary as debug output.
        /// </summary>
        private void PrintMusicLibrary() {
            Banshee.Library.MusicLibrarySource ml = ServiceManager.SourceManager.MusicLibrary;
            Hyena.Log.Debug("NoNoise - ML count: " + ml.Count);
            Hyena.Log.Debug("NoNoise - ML child count: " + ml.Children.Count);
            Hyena.Log.Debug("NoNoise - ML TM count: " + ml.TrackModel.Count);
            Hyena.Log.Debug("\nNoNoise - ===========================================================");

            for (int i = 0; i < ServiceManager.SourceManager.MusicLibrary.TrackModel.Count; i++) {
                Hyena.Log.Debug(GetMetaData(i));
            }

            Hyena.Log.Debug("NoNoise - ===========================================================\n");
        }

        /// <summary>
        /// Returns a string with a lot of meta data of the track at position index in the music library.
        /// </summary>
        /// <param name="index">
        /// A <see cref="System.Int32"/> declaring the index
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> with the meta data
        /// </returns>
        private string GetMetaData(int index) {
            TrackInfo ti = ServiceManager.SourceManager.MusicLibrary.TrackModel[index];
            ti.Copyright = "(c) No.Noise";
            ti.Update();

            return "NoNoise - ML entry " + index + ": " +
                    ti.ArtistName + " - " +
                    ti.TrackTitle + " (" +
                    ti.AlbumTitle + ")" + "\n" +
                    ti.Bpm + " bpm, " +
                    ti.BitRate + " kbit/s, " +
                    ti.DateAdded + ", " +
                    ti.Duration + ", " +
                    ti.FileSize + " byte, " +
                    ti.LastPlayed + ", " +
                    ti.PlayCount + ", " +
                    ti.SkipCount + ", " +
                    ti.TrackNumber + "/" +
                    ti.TrackCount + ".";
        }

        /// <summary>
        /// Prints a modelselection of the music library.
        /// </summary>
        private void PrintSelection () {
            Selection s = new Selection();
            s.SelectRange(10, 23);

            ModelSelection<TrackInfo> ms = new ModelSelection<TrackInfo>(ServiceManager.SourceManager.MusicLibrary.TrackModel, s);

            foreach (TrackInfo ti in ms) {
                Hyena.Log.Debug("NoNoise - Selection: " + ti.ArtistName + " - " + ti.TrackTitle + " (" + ti.AlbumTitle + ")");
            }
        }

        /// <summary>
        /// Queries last.fm for meta data corresponding to the given artist.
        /// </summary>
        /// <param name="artist">
        /// The name of the artist
        /// </param>
        /// <param name="session">
        /// The last.fm <see cref="Session"/>
        /// </param>
        private void QueryTopAlbums(string artist, Session session) {
            Artist art = new Artist(artist, session);
            TopAlbum[] ta = art.GetTopAlbums();
            Hyena.Log.Debug("NoNoise - LastFM Albums queried");

            if (ta.Length <= 0)
                Hyena.Log.Debug("NoNoise - Album list empty");
            else
                Hyena.Log.Debug("NoNoise - Album list filled");

            foreach (TopAlbum t in ta) {
                topalbums += t.Item.Name + "\n";
            }
        }

        private void DetectBPMs(TrackInfo track) {
            // on button pressed
            if (track != null) {
                detector.ProcessFile (track.Uri);
            }
        }

        private void OnFileFinished (object o, BpmEventArgs args)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate {
//                    if (track.Uri != args.Uri || args.Bpm == 0) {
//                        return;
//                    }

                int id = DatabaseTrackInfo.GetTrackIdForUri(args.Uri);
                if (id >= 0) {
                    TrackInfo ti = ServiceManager.SourceManager.MusicLibrary.DatabaseTrackModel[id];
//                        int index = (int)TrackCache.IndexOf ((long)id); // auch nicht accessible...?
                    Hyena.Log.Debug("NoNoise - BPM...Track index: " +
                                    ServiceManager.SourceManager.MusicLibrary.GetTrackIdForUri(args.Uri));
//                        if (index >= 0) {
//                            TrackInfo track = TrackModel[index];
//                            if (track != null) {
//
//                            }
//                        }
                }

                Hyena.Log.Debug("NoNoise - BPM...Track ID: " + ServiceManager.SourceManager.MusicLibrary
                                .GetTrackIdForUri(args.Uri));

                Bpm = args.Bpm;
                Hyena.Log.DebugFormat ("NoNoise - Detected BPM of {0} for {1}", Bpm, args.Uri);
            });
        }

        public bool SetSource (ISource source)
        {
            return false;
        }
        
        public void ResetSource () 
        {
        }
        
        public Gtk.Widget Widget { 
            get { return w; } 
        }
        
        public ISource Source { 
            get { return null; }
        }

        public void ButtonPushHandler (object obj, EventArgs args) {
            Hyena.Log.Debug("NoNoise - Button pressed");

            Thread thr = new Thread (new ThreadStart(ThreadRoutine));
            thr.Start();
            ready = new Gtk.ThreadNotify (new Gtk.ReadyEvent(Ready));

            w.Remove(bw);
            w.Add(working);
            w.ShowAll();
            Hyena.Log.Debug("NoNoise - busy...");
        }

        private void Ready ()
        {
            label.Text = "Custom view for NoNoise extension is working!\nTopAlbums: " + topalbums;
            w.Remove(working);
            sw.AddWithViewport(label);
            w.Add(sw);
            w.ShowAll();
        }

        private void ThreadRoutine ()
        {
            if (painintheassdummy)
                QueryTopAlbums("Muse", session);
            TrackInfo ti = ServiceManager.SourceManager.MusicLibrary.TrackModel[0];
            DetectBPMs(ti);
            ready.WakeupMain();
        }

        public class ScanFinishedEventArgs
        {
            public ScanFinishedEventArgs (string info)
            {
                Info = info;
            }

            public string Info {
                get;
                private set;
            }
        }

        //Event Handler which is called when the zoom level has changed
        public event ScanFinishedEvent OnScanFinished {
            add { scan_event += value; }
            remove { scan_event -= value; }
        }
    }
}

