// 
// NoNoiseService.cs
// 
// Author:
//   Manuel Keglevic <manuel.keglevic@gmail.com>
//   Thomas Schulz <tjom@gmx.at>
// 
// Copyright (c) 2011 Manuel Keglevic, Thomas Schulz
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

using Banshee.Gui;
using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;

using Mono.Addins;

// Other namespaces you might want:
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.Library;
using Clutter;
using Gtk;

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

        private uint ui_manager_id_menu;
        private uint ui_manager_id_tool_menu;

		private NoNoiseClutterSourceContents no_noise_contents;
        private bool scan_action_enabled = false;

        private bool pref_installed = false;
        private bool source_contents_set_up = false;
        private bool disposed = false;

        private Gtk.Action scan_action;
        protected Gtk.Action ScanAction {
            get {
                if (scan_action == null)
                    scan_action = (Gtk.Action) action_service.FindAction ("NoNoiseScan.NoNoiseScanAction");
                return scan_action;
            }
        }

        private Gtk.Action help_action;
        protected Gtk.Action HelpAction {
            get {
                if (help_action == null)
                    help_action = (Gtk.Action) action_service.FindAction ("NoNoiseScan.NoNoiseHelpAction");
                return help_action;
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

        private Page preferences;
        private Section pca;
        private PreferenceBase pb;
        private ComboBox cb;
        private readonly string[] CB_ENTRIES = new string [] {
                Enum.GetName (typeof(NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfccOptions.Mean),
                Enum.GetName (typeof(NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfccOptions.SquaredMean),
                Enum.GetName (typeof(NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfccOptions.Median),
                Enum.GetName (typeof(NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfccOptions.Minimum),
                Enum.GetName (typeof(NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfccOptions.Maximum)
            };

        public NoNoiseService ()
        {
            Hyena.Log.Debug ("No.Noise extension initializing.");
        }

        void IExtensionService.Initialize ()
        {
            preference_service = ServiceManager.Get<PreferenceService> ();
            action_service = ServiceManager.Get<InterfaceActionService> ();

            source_manager = ServiceManager.SourceManager;
            music_library = source_manager.MusicLibrary;

//            Hyena.Log.Information ("Service NoNoise Initialized: "
//                                   + "\naction_service " + (action_service == null ? "Null" : "OK")
//                                   + "\nsource_manager " + (source_manager == null ? "Null" : "OK")
//                                   + "\nmusic_library " + (music_library == null ? "Null" : "OK")
//                                   + "\npreference_service " + (preference_service == null ? "Null" : "OK"));

            InstallPreferences ();
            SetupInterfaceActions ();

            ServiceManager.ServiceStarted += OnServiceStarted;

            if (!SetupSourceContents ())
                source_manager.SourceAdded += OnSourceAdded;
        }

        /// <summary>
        /// Installs the NoNoise preferences dialog.
        /// </summary>
        private void InstallPreferences ()
        {
            if (!pref_installed) {
                preferences = preference_service.Add (new Page ("nonoise", AddinManager.CurrentLocalizer.GetString ("NoNoise"), 20));

                pca = preferences.Add (new Section ("pca", "PCA", 2));
                pb = new SchemaPreference<string> (NoNoiseSchemas.PcaMfcc, NoNoiseSchemas.PcaMfcc.ShortDescription,
                                                       NoNoiseSchemas.PcaMfcc.LongDescription);
                cb = new ComboBox (CB_ENTRIES);
                cb.Active = (int) Enum.Parse (typeof (NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfcc.Get ());
                cb.Changed += PcaUseHandler;
                pb.DisplayWidget = cb;
                cb.Destroyed += HandleCbDestroyed;
                pca.Add (pb);
                pca.Add (new SchemaPreference<bool> (NoNoiseSchemas.PcaUseDuration, NoNoiseSchemas.PcaUseDuration.ShortDescription,
                                                     NoNoiseSchemas.PcaUseDuration.LongDescription));
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

            // coincidence or real solution?
            if (args.Source is VideoLibrarySource) {
//                Hyena.Log.Debug ("NoNoise/Serv - src added, type: " + args.Source.GetType ().ToString ());
//                Hyena.Log.Debug ("NoNoise/Serv - vl added, cnt: " + music_library.TrackModel.Count);

                SetupSourceContents ();
            }
        }

        /// <summary>
        /// Sets up the interface actions (i.e. menu entries) and adds handlers.
        /// </summary>
        /// <returns>
        /// True.
        /// </returns>
        private bool SetupInterfaceActions ()
        {
            if (action_service.FindActionGroup ("NoNoiseView") == null) {
                ActionGroup no_noise_actions = new ActionGroup ("NoNoiseView");

                no_noise_actions.Add (new ToggleActionEntry [] {
                    new ToggleActionEntry ("NoNoiseVisibleAction", null,
                    AddinManager.CurrentLocalizer.GetString ("NoNoise Visualization"), null,
                    AddinManager.CurrentLocalizer.GetString ("Enable or disable the NoNoise visualization"),
                    null, NoNoiseSchemas.ShowNoNoise.Get ())
                });

                action_service.AddActionGroup (no_noise_actions);
                ui_manager_id_menu = action_service.UIManager.AddUiFromString (menu_xml);
            }


            if (action_service.FindActionGroup ("NoNoiseScan") == null) {
                ActionGroup scan_actions = new ActionGroup ("NoNoiseScan");

                scan_actions.Add (new ActionEntry [] {
                    new ActionEntry ("NoNoiseMenuAction", null, "NoNoise", null,
                                     null, null),
                    new ActionEntry ("NoNoiseScanAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Start NoNoise scan"), null,
                    AddinManager.CurrentLocalizer.GetString ("Start or pause the NoNoise scan"),
                    null),
                    new ActionEntry ("NoNoiseHelpAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Help"), null,
                    AddinManager.CurrentLocalizer.GetString ("Show the help dialog for the NoNoise plug-in"),
                    null)
                });

                action_service.AddActionGroup (scan_actions);
                ui_manager_id_tool_menu = action_service.UIManager.AddUiFromResource ("tool_menu.xml");
            }

            NoNoiseAction.Activated += OnNoNoiseToggle;
            ScanAction.Activated += OnScanAction;
            HelpAction.Activated += OnHelpAction;

            return true;
        }

        #region Menu action handlers
        private void OnScanAction (object sender, EventArgs e)
        {
            Hyena.Log.Information ("Scan action activated");
            if (scan_action_enabled) {
                ScanAction.Label = AddinManager.CurrentLocalizer.GetString ("Start NoNoise scan");

                no_noise_contents.Scan (false);
            } else {
                ScanAction.Label = AddinManager.CurrentLocalizer.GetString ("Pause NoNoise scan");

                no_noise_contents.Scan (true);
            }

            scan_action_enabled = !scan_action_enabled;
        }

        private void OnHelpAction (object sender, EventArgs e)
        {
            Hyena.Log.Debug ("Help action called");
            new NoNoiseHelpDialog ();
        }

        private void OnNoNoiseToggle (object sender, EventArgs e)
        {
            if (NoNoiseAction.Active) {
                NoNoiseSchemas.ShowNoNoise.Set (true);
                Clutter.Threads.Enter ();
                music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", no_noise_contents);
                Clutter.Threads.Leave ();
                Hyena.Log.Information ("NoNoise enabled");
            } else {
                NoNoiseSchemas.ShowNoNoise.Set (false);
                Clutter.Threads.Enter ();
                music_library.Properties.Remove("Nereid.SourceContents");
                Clutter.Threads.Leave ();
                 Hyena.Log.Information ("NoNoise disabled");
            }
        }
        #endregion

        #region Callbacks
        private void ScanFinished (object source, NoNoiseClutterSourceContents.ScanFinishedEventArgs args)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate () {
                scan_action_enabled = false;
                ScanAction.Label = AddinManager.CurrentLocalizer.GetString ("Start NoNoise scan");
                ScanAction.Sensitive = false;
            });
        }

        private void ScannableChanged (object source, NoNoiseClutterSourceContents.ToggleScannableEventArgs args)
        {
            Hyena.ThreadAssist.ProxyToMain (delegate () {
                ScanAction.Sensitive = args.Scannable;
            });
        }
        #endregion

        /// <summary>
        /// Initializes the <see cref="NoNoiseClutterSourceContente"/>.
        /// </summary>
        /// <returns>
        /// True if the contents have been set up.
        /// </returns>
        private bool SetupSourceContents ()
        {
            if (source_contents_set_up)
                return true;

            if (music_library == null || action_service == null)
                return false;

            no_noise_contents = new NoNoiseClutterSourceContents ();
            no_noise_contents.OnScanFinished += ScanFinished;
            no_noise_contents.OnToggleScannable += ScannableChanged;

            ScanAction.Sensitive = !BansheeLibraryAnalyzer.Singleton.IsLibraryScanned;
            SwitchPcaMfccOptions ();

            source_contents_set_up = true;

            no_noise_contents.SetSource(music_library);

            if (NoNoiseSchemas.ShowNoNoise.Get ()) {
                Clutter.Threads.Enter ();
                music_library.Properties.Set<ISourceContents> ("Nereid.SourceContents", no_noise_contents);
                Clutter.Threads.Leave ();
            }

            source_manager.SourceAdded -= OnSourceAdded;

//            Hyena.Log.Debug ("NoNoise/Serv - tm cnt: " + music_library.TrackModel.Count);

//            Hyena.Log.Information ("Service Foo Initialized: "
//                                   + "\naction_service " + (action_service == null ? "Null" : "OK")
//                                   + "\nsource_manager " + (source_manager == null ? "Null" : "OK")
//                                   + "\nmusic_library " + (music_library == null ? "Null" : "OK")
//                                   + "\npreference_service " + (preference_service == null ? "Null" : "OK"));
            return true;
        }

        /// <summary>
        /// Switches the enum value of NoNoiseSchemas.PcaMfcc and sets the PCA
        /// mode of BansheeLibraryAnalyzer accordingly.
        /// </summary>
        private void SwitchPcaMfccOptions ()
        {
            switch ((NoNoiseSchemas.PcaMfccOptions) Enum.Parse (typeof (NoNoiseSchemas.PcaMfccOptions),
                                                                NoNoiseSchemas.PcaMfcc.Get ())) {
            default:
            case NoNoiseSchemas.PcaMfccOptions.Mean:
                if (NoNoiseSchemas.PcaUseDuration.Get ())
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MEAN_DUR;
                else
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MEAN;
                break;

            case NoNoiseSchemas.PcaMfccOptions.SquaredMean:
                if (NoNoiseSchemas.PcaUseDuration.Get ())
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_SQR_MEAN_DUR;
                else
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_SQR_MEAN;
                break;

            case NoNoiseSchemas.PcaMfccOptions.Median:
                if (NoNoiseSchemas.PcaUseDuration.Get ())
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MED_DUR;
                else
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MED;
                break;

            case NoNoiseSchemas.PcaMfccOptions.Minimum:
                if (NoNoiseSchemas.PcaUseDuration.Get ())
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MIN_DUR;
                else
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MIN;
                break;

            case NoNoiseSchemas.PcaMfccOptions.Maximum:
                if (NoNoiseSchemas.PcaUseDuration.Get ())
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MAX_DUR;
                else
                    BansheeLibraryAnalyzer.Singleton.PcaMode = BansheeLibraryAnalyzer.PCA_MAX;
                break;
            }
        }

        /// <summary>
        /// Handles change events of the MFCC combo box.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void PcaUseHandler (object sender, EventArgs e)
        {
            Hyena.Log.Debug ("NoNoise/Serv - update handler called");
            NoNoiseSchemas.PcaMfcc.Set (cb.ActiveText);
        }

        /// <summary>
        /// Handles combo box destroyed events. This is used to only update the
        /// PCA when the preference dialog is closed and to recreate the combo
        /// box to prevent a fatal error.
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        private void HandleCbDestroyed (object sender, EventArgs e)
        {
            Hyena.Log.Debug ("NoNoise/Serv - combobox destroyed");

            SwitchPcaMfccOptions ();

            // create it again to prevent fatal error on re-opening preferences
            cb = new ComboBox (CB_ENTRIES);
            cb.Active = (int) Enum.Parse (typeof (NoNoiseSchemas.PcaMfccOptions), NoNoiseSchemas.PcaMfcc.Get ());
            cb.Changed += PcaUseHandler;
            pb.DisplayWidget = cb;
            cb.Destroyed += HandleCbDestroyed;
        }

        #region Dispose
        public void Dispose ()
        {
            if (disposed)
                return;
            Hyena.Log.Debug ("NoNoise/Serv - Disposing NoNoise services...");
            disposed = true;

            ServiceManager.ServiceStarted -= OnServiceStarted;
            source_manager.SourceAdded -= OnSourceAdded;

            no_noise_contents.OnScanFinished -= ScanFinished;
            no_noise_contents.OnToggleScannable -= ScannableChanged;

            NoNoiseAction.Activated -= OnNoNoiseToggle;
            ScanAction.Activated -= OnScanAction;
            HelpAction.Activated -= OnHelpAction;
            no_noise_action = null;
            scan_action = null;
            help_action = null;

            UninstallPreferences ();
            RemoveNoNoise ();

            BansheeLibraryAnalyzer.Singleton.Dispose ();
         }

        private void UninstallPreferences ()
        {
            Hyena.Log.Debug ("NoNoise/Serv - Uninstalling NoNoise preference page...");
            preference_service.Remove (preferences);
            preferences = null;
            pca = null;
            pref_installed = false;

            cb.Changed -= PcaUseHandler;
            cb.Destroyed -= HandleCbDestroyed;
            cb = null;
        }

        private void RemoveNoNoise ()
        {
            Hyena.Log.Debug ("NoNoise/Serv - Removing NoNoise view...");
            Clutter.Threads.Enter ();
            music_library.Properties.Remove ("Nereid.SourceContents");
            Clutter.Threads.Leave ();
            no_noise_contents.Dispose ();
            no_noise_contents = null;

            Hyena.Log.Debug ("NoNoise/Serv - Removing NoNoise actions...");
            action_service.RemoveActionGroup ("NoNoiseView");
            action_service.RemoveActionGroup ("NoNoiseScan");
            action_service.UIManager.RemoveUi (ui_manager_id_menu);
            action_service.UIManager.RemoveUi (ui_manager_id_tool_menu);

            preference_service = null;
            source_manager = null;
            music_library = null;
            action_service = null;
        }
        #endregion
    }
}

