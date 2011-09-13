//
// Foo1Source.cs
//
// Authors:
//   Cool Extension Author <cool.extension@author.com>
//
// Copyright (C) 2011 Cool Extension Author
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Threading;

using Mono.Addins;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;

using Banshee.Foo1.Bpm;
using Banshee.Foo1.PCA;

using Mirage;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Collections;

using Lastfm.Services;
using Lastfm;

namespace Banshee.Foo1
{
    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
    public class Foo1Source : Source
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 190;


        public Foo1Source () : base (AddinManager.CurrentLocalizer.GetString ("Foo1"),
                                               AddinManager.CurrentLocalizer.GetString ("Foo1"),
		                                       sort_order,
		                                       "extension-unique-id")
        {
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new CustomView ());
            this.OnUpdated();

            Hyena.Log.Information ("Testing!  Foo1 source has been instantiated!");
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { return 0; }
        }

        private class CustomView : ISourceContents
        {
            Gtk.Label label = new Gtk.Label ("Custom view for Foo1 extension is working!\nTopAlbums: " + topalbums);
            Gtk.Label emptylabel = new Gtk.Label("Shake your head it's empty!");
            Gtk.Box w = new Gtk.HBox(true, 4);
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            Gtk.Box bw = new Gtk.VButtonBox();
            Gtk.Button button = new Gtk.Button();
            Gtk.Label working = new Gtk.Label("retrieving artist information...");
            Session session;
            Gtk.ThreadNotify ready;

            private bool painintheassdummy = false;
//            private bool gatherMIRdata = false;
            private bool saveTrackInfosToDB = true;
            private bool dopca = true;
            private bool doprintlib = false;
            private bool doprintselection = false;

            private Banshee.NoNoise.Data.NoNoiseDBHandler db;
            private IBpmDetector detector;
            private int Bpm;

            string API_KEY =  "b6f3f5f1a92987f58a3ae75516c967a5";
            string API_SECRET =   "1022d8e3a796243f8105fe97d1156803";
            static string topalbums = "";

            public CustomView() {
                Hyena.Log.Debug("Foo1 - CustomView constructor");
                session = new Session(API_KEY, API_SECRET);

                db = new Banshee.NoNoise.Data.NoNoiseDBHandler ();

                // PCA
                if (dopca)
                    PcaForMusicLibrary ();

                // TrackInfo in NoNoise DB
                if (saveTrackInfosToDB)
                    WriteTrackInfosToDB ();

                // BPM detector
                detector = BpmDetectJob.GetDetector ();
                if (detector != null) {
                    detector.FileFinished += OnFileFinished;
                    Hyena.Log.Debug("Foo1 - Detector is not null");
                }

                // using Active Source
                Source source = ServiceManager.SourceManager.ActiveSource;
                ITrackModelSource track_source = source as ITrackModelSource;

                if (track_source != null) {
                    Hyena.Log.Debug("Foo1 - TS name: " + track_source.Name);
                    Hyena.Log.Debug("Foo1 - TS count: " + track_source.Count);
                    Hyena.Log.Debug("Foo1 - TS generic name: " + track_source.GenericName);
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

            private void DebugPrintMFCC (Matrix mfcc)
            {
                Hyena.Log.Debug("Cols: " + mfcc.columns);
                Hyena.Log.Debug("Rows: " + mfcc.rows);
                Hyena.Log.Debug("Mean length: " + mfcc.Mean().rows);
                mfcc.Mean().Print();
            }

            private void PcaForMusicLibrary ()
            {
                PCAnalyzer ana = new PCAnalyzer();

//                if (gatherMIRdata) {
                Banshee.Library.MusicLibrarySource ml = ServiceManager.SourceManager.MusicLibrary;
                Matrix mfcc;
                Dictionary<int, Matrix> mfccMap = db.GetMirageMatrices ();

                for (int i = 0; i < ml.TrackModel.Count; i++) {
                    try {
                        TrackInfo ti = ml.TrackModel[i];
                        string absPath = ti.Uri.AbsolutePath;
                        int bid = ml.GetTrackIdForUri(ti.Uri);

                        // WARN: A bid could theoretically be inserted/deleted between GetMirageMatrices ()
                        // and CointainsMirDataForTrack () such that if and else fail
                        if (!db.ContainsMirDataForTrack (bid)) {
                            mfcc = Analyzer.AnalyzeMFCC(absPath);

                            if (!db.InsertMatrix (mfcc, bid))
                                Hyena.Log.Error ("Foo1 - Matrix insert failed");
                        } else
                            mfcc = mfccMap[bid];

                        if (!ana.AddEntry (bid, ConvertMfccMean(mfcc.Mean())))
                            throw new Exception("AddEntry failed!");
                    } catch (Exception e) {
                        Hyena.Log.Exception("Foo1 - MFCC/DB Problem", e);
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
//                            Hyena.Log.Exception ("Foo1 - PCA Problem", e);
//                        }
//                    }
//                }

                try {
                    ana.PerformPCA ();
                    Hyena.Log.Debug(ana.GetCoordinateStrings ());
                    List<Banshee.NoNoise.Data.DataEntry> coords = ana.Coordinates;
                    db.ClearPcaData ();
                    if (!db.InsertPcaCoordinates (coords))
                        Hyena.Log.Error ("Foo1 - PCA coord insert failed");
                } catch (Exception e) {
                    Hyena.Log.Exception("PCA Problem", e);
                }
            }

            private double[] ConvertMfccMean (Mirage.Vector mean)
            {
                double[] data = new double[mean.d.Length];

                for (int i = 0; i < mean.d.Length; i++) {
                    data[i] = mean.d[i, 0];
                }

                return data;
            }

            private void WriteTrackInfosToDB ()
            {
                Banshee.Library.MusicLibrarySource ml = ServiceManager.SourceManager.MusicLibrary;

                for (int i = 0; i < ml.TrackModel.Count; i++) {
                    try {
                        TrackInfo ti = ml.TrackModel[i];
                        int bid = ml.GetTrackIdForUri(ti.Uri);

                        if (!db.ContainsInfoForTrack (bid)) {
                            if (!db.InsertTrackInfo (new Banshee.NoNoise.Data.TrackInfo (
                                                       bid, ti.ArtistName, ti.TrackTitle,
                                                       ti.AlbumTitle, (int)ti.Duration.TotalSeconds)))
                                Hyena.Log.Error ("Foo1 - TrackInfo insert failed");
                        }
                    } catch (Exception e) {
                        Hyena.Log.Exception("Foo1 - DB Problem", e);
                    }
                }
            }

            /// <summary>
            /// Prints detailed information about each song in the MusicLibrary as debug output.
            /// </summary>
            private void PrintMusicLibrary() {
                Banshee.Library.MusicLibrarySource ml = ServiceManager.SourceManager.MusicLibrary;
                Hyena.Log.Debug("Foo1 - ML count: " + ml.Count);
                Hyena.Log.Debug("Foo1 - ML child count: " + ml.Children.Count);
                Hyena.Log.Debug("Foo1 - ML TM count: " + ml.TrackModel.Count);
                Hyena.Log.Debug("\nFoo1 - ===========================================================");

                for (int i = 0; i < ServiceManager.SourceManager.MusicLibrary.TrackModel.Count; i++) {
                    Hyena.Log.Debug(GetMetaData(i));
                }

                Hyena.Log.Debug("Foo1 - ===========================================================\n");
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

                return "Foo1 - ML entry " + index + ": " +
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

            private void PrintSelection () {
                Selection s = new Selection();
                s.SelectRange(10, 23);

                ModelSelection<TrackInfo> ms = new ModelSelection<TrackInfo>(ServiceManager.SourceManager.MusicLibrary.TrackModel, s);

                foreach (TrackInfo ti in ms) {
                    Hyena.Log.Debug("Foo1 - Selection: " + ti.ArtistName + " - " + ti.TrackTitle + " (" + ti.AlbumTitle + ")");
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
                Hyena.Log.Debug("Foo1 - LastFM Albums queried");

                if (ta.Length <= 0)
                    Hyena.Log.Debug("Foo1 - Album list empty");
                else
                    Hyena.Log.Debug("Foo1 - Album list filled");

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
//                        int index = (int)TrackCache.IndexOf ((long)id); // auch nicht accessible...?
                        Hyena.Log.Debug("Foo1 - BPM...Track index: " + ServiceManager.SourceManager.MusicLibrary.GetTrackIdForUri(args.Uri));
//                        if (index >= 0) {
//                            TrackInfo track = TrackModel[index];
//                            if (track != null) {
//
//                            }
//                        }
                    }

                    Hyena.Log.Debug("Foo1 - BPM...Track ID: " + ServiceManager.SourceManager.MusicLibrary.GetTrackIdForUri(args.Uri));

                    Bpm = args.Bpm;
                    Hyena.Log.DebugFormat ("Foo1 - Detected BPM of {0} for {1}", Bpm, args.Uri);
                });
            }

            public bool SetSource (ISource source) { return true; }
            public void ResetSource () { }
            public Gtk.Widget Widget { get {
//                    if (sw.Children.Length == 0) {
//                        if (w.Children.Length == 0)
//                            w.Add(label);
//                            w.PackStart(label, true, false, 0);
//                        sw.AddWithViewport(w);
//                    }
                    return w; } }
            public ISource Source { get { return null; } }

            public void ButtonPushHandler (object obj, EventArgs args) {
                Hyena.Log.Debug("Foo1 - Button pressed");

                Thread thr = new Thread (new ThreadStart(ThreadRoutine));
                thr.Start();
                ready = new Gtk.ThreadNotify (new Gtk.ReadyEvent(Ready));

                w.Remove(bw);
                w.Add(working);
                w.ShowAll();
                Hyena.Log.Debug("Foo1 - working...");
            }

            private void Ready ()
            {
                label.Text = "Custom view for Foo1 extension is working!\nTopAlbums: " + topalbums;
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
        }

    }
}
