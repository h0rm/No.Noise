// 
// NoNoiseService.cs
// 
// Author:
//   horm <${AuthorEmail}>
// 
// Copyright (c) 2011 horm
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

using Banshee.Gui;
using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Library;
using Clutter;
using Gtk;
using Banshee.I18n;
using Banshee.Configuration;

namespace Banshee.NoNoise
{
    public class NoNoiseService : IExtensionService, IDisposable
    {
		string IService.ServiceName {
            get { return "No.Noise Service"; }
        }
		
		private SourceManager source_manager;
        private MusicLibrarySource music_library;
        private InterfaceActionService action_service;
        private PreferenceService preference_service;

		private ISourceContents no_noise_contents;
        private bool scan_action_enabled = false;

        private bool pref_installed = false;
        private bool source_contents_set_up = false;

        private Gtk.Action scan_action;
        protected Gtk.Action ScanAction {
            get {
                if (scan_action == null)
                    scan_action = (Gtk.Action) action_service.FindAction ("NoNoiseScan.NoNoiseScanAction");
                return scan_action;
            }
        }

        private ToggleAction no_noise_action;
        protected ToggleAction NoNoiseAction {
            get {
                if (no_noise_action == null)
                    no_noise_action = (ToggleAction) action_service.FindAction ("NoNoiseView.NoNoiseVisibleAction");
                return no_noise_action;
            }
        }



        private static string menu_xml = @"
            <ui>
                <menubar name=""MainMenu"">
                    <menu name=""ViewMenu"" action=""ViewMenuAction"">
                        <placeholder name=""BrowserViews"">
                            <menuitem name=""NoNoise"" action=""NoNoiseVisibleAction"" />
                        </placeholder>
                    </menu>
                </menubar>
            </ui>
        ";

        public NoNoiseService ()
        {
            Hyena.Log.Information ("Testing!  No.Noise extension has been instantiated!");
        }

        void IExtensionService.Initialize ()
        {
            preference_service = ServiceManager.Get<PreferenceService> ();
            action_service = ServiceManager.Get<InterfaceActionService> ();

            source_manager = ServiceManager.SourceManager;
            music_library = source_manager.MusicLibrary;

            Hyena.Log.Information ("Service Foo Initialized: "
                                   + "\naction_service " + (action_service == null ? "Null" : "OK")
                                   + "\nsource_manager " + (source_manager == null ? "Null" : "OK")
                                   + "\nmusic_library " + (music_library == null ? "Null" : "OK")
                                   + "\npreference_service " + (preference_service == null ? "Null" : "OK"));

            InstallPreferences ();
            SetupInterfaceActions ();

            ServiceManager.ServiceStarted += OnServiceStarted;

            if (!SetupSourceContents ())
                source_manager.SourceAdded += OnSourceAdded;
        }

        private Page preferences;
        private Section debug;

        void InstallPreferences ()
        {
            if (!pref_installed) {
                preferences = preference_service.Add (new Page ("nonoise", Catalog.GetString ("No.Noise"), 20));
        
                debug = preferences.Add (new Section ("debug", Catalog.GetString ("Debug"), 1));
                debug.Add (new SchemaPreference<bool> (Schemas.Startup, Schemas.Startup.ShortDescription, Schemas.Startup.LongDescription));
                pref_installed = true;
            }
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Preferences.PreferenceService) {
                preference_service = (PreferenceService)args.Service;
                InstallPreferences ();
            } else if (args.Service is Banshee.Gui.InterfaceActionService) {
                action_service = (InterfaceActionService)args.Service;
                SetupInterfaceActions ();
            }

            if (!(preference_service==null || action_service==null)) {
                ServiceManager.ServiceStarted -= OnServiceStarted;
                if (!SetupSourceContents ())
                    source_manager.SourceAdded += OnSourceAdded;
            }
        }

        void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source is MusicLibrarySource) {
                music_library = args.Source as MusicLibrarySource;
            }

            // TODO coincidence or real solution?
            if (args.Source is VideoLibrarySource) {
                Hyena.Log.Debug ("NoNoise/Serv - src added, type: " + args.Source.GetType ().ToString ());
                Hyena.Log.Debug ("NoNoise/Serv - vl added, cnt: " + music_library.TrackModel.Count);

                SetupSourceContents ();
            }
        }



        private bool SetupInterfaceActions ()
        {
            if (action_service.FindActionGroup ("NoNoiseView") == null) {
                ActionGroup no_noise_actions = new ActionGroup ("NoNoiseView");

                no_noise_actions.Add (new ToggleActionEntry [] {
                    new ToggleActionEntry ("NoNoiseVisibleAction", null,
                    Catalog.GetString ("No.Noise Visualization"), null,
                    Catalog.GetString ("Enable or disable the No.Noise visualization"),
                    null, true)
                });

                action_service.AddActionGroup (no_noise_actions);
                action_service.UIManager.AddUiFromString (menu_xml);
            }


            if (action_service.FindActionGroup ("NoNoiseScan") == null) {
                ActionGroup scan_actions = new ActionGroup ("NoNoiseScan");

                scan_actions.Add (new ActionEntry [] {
                    new ActionEntry ("NoNoiseScanAction", null,
                    Catalog.GetString ("Start No.Noise scan"), null,
                    Catalog.GetString ("Start or pause the No.Noise scan"),
                    null)
                });

                action_service.AddActionGroup (scan_actions);
                action_service.UIManager.AddUiFromResource ("tool_menu.xml");
            }

            NoNoiseAction.Activated += OnNoNoiseToggle;
            ScanAction.Activated += OnScanAction;
            return true;
        }

        void OnScanAction (object sender, EventArgs e)
        {
            Hyena.Log.Information ("Scan action activated");
            if (scan_action_enabled) {
                ScanAction.Label = "Start no.Noise scan";

                if (no_noise_contents is NoNoiseSourceContents)
                    ((NoNoiseSourceContents)no_noise_contents).Scan (false);
                else if (no_noise_contents is NoNoiseClutterSourceContents)
                    ((NoNoiseClutterSourceContents)no_noise_contents).Scan (false);
            } else {
                ScanAction.Label = "Pause no.Noise scan";

                if (no_noise_contents is NoNoiseSourceContents)
                    ((NoNoiseSourceContents)no_noise_contents).Scan (true);
                else if (no_noise_contents is NoNoiseClutterSourceContents)
                    ((NoNoiseClutterSourceContents)no_noise_contents).Scan (true);
            }

            scan_action_enabled = !scan_action_enabled;
        }

        void OnNoNoiseToggle (object sender, EventArgs e)
        {
            if (NoNoiseAction.Active) {
                Clutter.Threads.Enter ();
                music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", no_noise_contents);
                Clutter.Threads.Leave ();
                Hyena.Log.Information ("No.Noise enabled");
            } else {
                Clutter.Threads.Enter ();
                music_library.Properties.Remove("Nereid.SourceContents");
                Clutter.Threads.Leave ();
                 Hyena.Log.Information ("No.Noise disabled");
            }
        }

        private void ScanFinished (object source, NoNoiseClutterSourceContents.ScanFinishedEventArgs args)
        {
            scan_action_enabled = false;
            ScanAction.Label = "Start no.Noise scan";
            ScanAction.Sensitive = false;
        }

        private bool SetupSourceContents ()
        {
            if (source_contents_set_up)
                return true;

            // TODO handle real empty libraries...done.
            if (music_library == null || action_service == null)// || music_library.TrackModel.Count == 0)
                return false;

            no_noise_contents = GetSourceContents ();

            source_contents_set_up = true;

            if (no_noise_contents is NoNoiseClutterSourceContents)
                (no_noise_contents as NoNoiseClutterSourceContents).OnScanFinished += ScanFinished;

            no_noise_contents.SetSource(music_library);

            Clutter.Threads.Enter ();
            music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", no_noise_contents);
            Clutter.Threads.Leave ();

            source_manager.SourceAdded -= OnSourceAdded;

            Hyena.Log.Debug ("NoNoise/Serv - tm cnt: " + music_library.TrackModel.Count);

//            Hyena.Log.Information ("Service Foo Initialized: "
//                                   + "\naction_service " + (action_service == null ? "Null" : "OK")
//                                   + "\nsource_manager " + (source_manager == null ? "Null" : "OK")
//                                   + "\nmusic_library " + (music_library == null ? "Null" : "OK")
//                                   + "\npreference_service " + (preference_service == null ? "Null" : "OK"));
            return true;
        }

        private ISourceContents GetSourceContents ()
        {
            int startViz = 0;

            try {
                using (System.IO.StreamReader sr = new System.IO.StreamReader ("../../NoNoise.starter"))
                {
                    string line;
                    if ((line = sr.ReadLine ()) != null)
                        startViz = int.Parse (line);
                }
            } catch (Exception e) {
                Hyena.Log.Exception ("NoNoise - startup error", e);
            }

            switch (startViz) {
            case 1 :
                return new NoNoiseClutterSourceContents (false);

            case 2:
                return new NoNoiseClutterSourceContents (true);

            default:
                return new NoNoiseSourceContents ();
            }
            
        }

        public void Dispose ()
        {

//            Clutter.Threads.Enter ();
            music_library.Properties.Remove ("Nereid.SourceContents");
//            no_noise_contents.Dispose ();
//            Clutter.Threads.Leave ();

            NoNoiseAction.Activated -= OnNoNoiseToggle;
            source_manager.SourceAdded -= OnSourceAdded;
        }

        public static class Schemas {
            internal static readonly SchemaEntry<bool> Startup = new SchemaEntry<bool>(
                "nonoise", "startup",
                false,
                Catalog.GetString ("Enable No.Noise on startup"),
                Catalog.GetString ("Enable or disable the No.Noise visualization on startup")
            );
        }


    }
}

