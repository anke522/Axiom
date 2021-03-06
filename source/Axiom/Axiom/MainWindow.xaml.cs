﻿/* ----------------------------------------------------------------------
Axiom UI
Copyright (C) 2017, 2018 Matt McManis
http://github.com/MattMcManis/Axiom
http://axiomui.github.io
mattmcmanis@outlook.com

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.If not, see <http://www.gnu.org/licenses/>. 
---------------------------------------------------------------------- */

using Axiom.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
// Disable XML Comment warnings
#pragma warning disable 1591
#pragma warning disable 1587
#pragma warning disable 1570

namespace Axiom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ViewModel vm = new ViewModel();

        // Axiom Current Version
        public static Version currentVersion;
        // Axiom GitHub Latest Version
        public static Version latestVersion;
        // Alpha, Beta, Stable
        public static string currentBuildPhase = "alpha";
        public static string latestBuildPhase;
        public static string[] splitVersionBuildPhase;

        public string TitleVersion {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Other Windows
        /// </summary>
        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        ///     Log Console
        /// </summary>
        public LogConsole logconsole = new LogConsole(((MainWindow)Application.Current.MainWindow));

        /// <summary>
        ///     Debug Console
        /// </summary>
        public static DebugConsole debugconsole;

        /// <summary>
        ///     File Properties Console
        /// </summary>
        public FilePropertiesWindow filepropwindow;

        /// <summary>
        ///     Script View
        /// </summary>
        //public static ScriptView scriptview; 

        /// <summary>
        ///     Configure Window
        /// </summary>
        //public static ConfigureWindow configurewindow;

        /// <summary>
        ///     File Queue
        /// </summary>
        //public FileQueue filequeue = new FileQueue();

        /// <summary>
        ///     Crop Window
        /// </summary>
        public static CropWindow cropwindow;

        /// <summary>
        ///     Optimize Advanced Window
        /// </summary>
        //public static OptimizeAdvancedWindow optadvwindow;

        /// <summary>
        ///     Optimize Advanced Window
        /// </summary>
        public static InfoWindow infowindow;

        /// <summary>
        ///     Update Window
        /// </summary>
        public static UpdateWindow updatewindow;


        // --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Variables
        /// </summary>
        // --------------------------------------------------------------------------------------------------------

        // Locks
        public static bool ready = true; // If 1 allow conversion, else stop
        public static bool script = false; // If 0 run ffmpeg, if 1 run generate script
        public static bool ffCheckCleared = false; // If 1, FFcheck no longer has to run for each convert

        // System
        public static string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + @"\"; // Axiom.exe directory

        // Input
        public static string inputDir; // Input File Directory
        public static string inputFileName; // (eg. myvideo.mp4 = myvideo)
        public static string inputExt; // (eg. .mp4)
        public static string input; // Single: Input Path + Filename No Ext + Input Ext (Browse Text Box) /// Batch: Input Path (Browse Text Box)
        public static string youtubedl; // YouTube Download

        // Output
        public static string outputDir; // Output Path
        public static string outputFileName; // Output Directory + Filename (No Extension)
        public static string outputExt; // (eg. .webm)
        public static string output; // Single: outputDir + outputFileName + outputExt /// Batch: outputDir + %~nf
        public static string outputNewFileName; // File Rename if File already exists

        // Batch
        public static string batchExt; // Batch user entered extension (eg. mp4 or .mp4)
        public static string batchInputAuto;


        // --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Volume Up Down
        /// </summary>
        /// <remarks>
        ///     Used for Volume Up Down buttons. Integer += 1 for each tick of the timer.
        ///     Timer Tick in MainWindow Initialize
        /// </remarks>
        // --------------------------------------------------------------------------------------------------------
        public DispatcherTimer dispatcherTimerUp = new DispatcherTimer(DispatcherPriority.Render);
        public DispatcherTimer dispatcherTimerDown = new DispatcherTimer(DispatcherPriority.Render);


        // --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Main Window Initialize
        /// </summary>
        // --------------------------------------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();

            // -----------------------------------------------------------------
            /// <summary>
            ///     Window & Components
            /// </summary>
            // -----------------------------------------------------------------
            // Set Min/Max Width/Height to prevent Tablets maximizing
            this.MinWidth = 768;
            this.MinHeight = 432;

            // -----------------------------------------------------------------
            /// <summary>
            ///     Control Binding
            /// </summary>
            // -----------------------------------------------------------------
            //ViewModel vm = new ViewModel();
            DataContext = vm;

            // -----------------------------------------------------------------
            /// <summary>
            /// Start the File Queue (Hidden)
            /// </summary>
            // disabled
            //StartFileQueue(); 
            // -----------------------------------------------------------------

            // -----------------------------------------------------------------
            /// <summary>
            /// Start the Log Console (Hidden)
            /// </summary>
            StartLogConsole();

            // -------------------------
            // Set Current Version to Assembly Version
            // -------------------------
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string assemblyVersion = fvi.FileVersion;
            currentVersion = new Version(assemblyVersion);

            // -------------------------
            // Title + Version
            // -------------------------
            TitleVersion = "Axiom ~ FFmpeg UI (" + Convert.ToString(currentVersion) + "-" + currentBuildPhase + ")";
            //DataContext = this;

            // -------------------------
            // Load Theme
            // -------------------------
            // --------------------------
            // First time use
            // --------------------------
            try
            {
                if (string.IsNullOrEmpty(Settings.Default["Theme"].ToString()))
                {
                    Configure.theme = "Axiom";

                    // Set ComboBox if Configure Window is Open
                    cboTheme.SelectedItem = "Axiom";

                    // Save Theme for next launch
                    Settings.Default["Theme"] = Configure.theme;
                    Settings.Default.Save();

                    // Change Theme Resource
                    App.Current.Resources.MergedDictionaries.Clear();
                    App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                    {
                        Source = new Uri("Theme" + Configure.theme + ".xaml", UriKind.RelativeOrAbsolute)
                    });
                }
                // --------------------------
                // Load Saved Settings Override
                // --------------------------
                else if (!string.IsNullOrEmpty(Settings.Default["Theme"].ToString())) // null check
                {
                    Configure.theme = Settings.Default["Theme"].ToString();

                    // Set ComboBox
                    cboTheme.SelectedItem = Configure.theme;

                    // Change Theme Resource
                    App.Current.Resources.MergedDictionaries.Clear();
                    App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                    {
                        Source = new Uri("Theme" + Configure.theme + ".xaml", UriKind.RelativeOrAbsolute)
                    });
                }


                // -------------------------
                // Log Text Theme SelectiveColorPreview
                // -------------------------
                if (Configure.theme == "Axiom")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#007DF2")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#72D4E8")); // Actions
                }
                else if (Configure.theme == "FFmpeg")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#5cb85c")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#5cb85c")); // Actions
                }
                else if (Configure.theme == "Cyberpunk")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#9f3ed2")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#9380fd")); // Actions
                }
                else if (Configure.theme == "Onyx")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#999999")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#777777")); // Actions
                }
                else if (Configure.theme == "Circuit")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ad8a4a")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2ebf93")); // Actions
                }
                else if (Configure.theme == "Prelude")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#999999")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#777777")); // Actions
                }
                else if (Configure.theme == "System")
                {
                    Log.ConsoleDefault = Brushes.White; // Default
                    Log.ConsoleTitle = (SolidColorBrush)(new BrushConverter().ConvertFrom("#007DF2")); // Titles
                    Log.ConsoleWarning = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E3D004")); // Warning
                    Log.ConsoleError = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44B35")); // Error
                    Log.ConsoleAction = (SolidColorBrush)(new BrushConverter().ConvertFrom("#72D4E8")); // Actions
                }
            }
            catch
            {
                MessageBox.Show("Problem loading Theme.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }


            // Log Console Message /////////
            logconsole.rtbLog.Document = new FlowDocument(Log.logParagraph); //start
            logconsole.rtbLog.BeginChange(); //begin change

            Log.logParagraph.Inlines.Add(new Bold(new Run(TitleVersion)) { Foreground = Log.ConsoleTitle });

            //Log.LogConsoleMessageAdd(TitleVersion,      // Message
            //                         "bold",            // Emphasis
            //                         Log.ConsoleAction, // Color
            //                         0);                // Linebreaks

            /// <summary>
            ///     System Info
            /// </summary>
            // Shows OS and Hardware information in Log Console
            SystemInfoDisplay();


            // -----------------------------------------------------------------
            /// <summary>
            ///     Load Saved Settings
            /// </summary>
            // -----------------------------------------------------------------  
            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Loading Saved Settings...")) { Foreground = Log.ConsoleAction });
            //Log.LogConsoleMessageAdd("Loading Saved Settings...", // Message
            //                         "bold",                      // Emphasis
            //                         Log.ConsoleAction,           // Color
            //                         1);                          // Linebreaks

            // Log Console Message /////////
            // Don't put in Configure Method, creates duplicate message /////////
            //Log.logParagraph.Inlines.Add(new LineBreak());
            //Log.logParagraph.Inlines.Add(new LineBreak());
            //Log.logParagraph.Inlines.Add(new Bold(new Run("Theme: ")) { Foreground = Log.ConsoleDefault });
            //Log.logParagraph.Inlines.Add(new Run(Configure.theme) { Foreground = Log.ConsoleDefault });


            // -------------------------
            // Prevent Loading Corrupt App.Config
            // -------------------------
            try
            {
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            }
            catch (ConfigurationErrorsException ex)
            {
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                }
                else
                {

                }
            }


            // -------------------------
            // Window Position
            // -------------------------
            if (Convert.ToDouble(Settings.Default["Left"]) == 0
                && Convert.ToDouble(Settings.Default["Top"]) == 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            // Load Saved
            else
            {
                this.Top = Settings.Default.Top;
                this.Left = Settings.Default.Left;
                this.Height = Settings.Default.Height;
                this.Width = Settings.Default.Width;

                if (Settings.Default.Maximized)
                {
                    WindowState = WindowState.Maximized;
                }
            }


            // -------------------------
            // Load FFmpeg.exe Path
            // -------------------------
            Configure.LoadFFmpegPath(this);

            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("FFmpeg: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run(Configure.ffmpegPath) { Foreground = Log.ConsoleDefault });

            // -------------------------
            // Load FFprobe.exe Path
            // -------------------------
            Configure.LoadFFprobePath(this);

            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("FFprobe: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run(Configure.ffprobePath) { Foreground = Log.ConsoleDefault });

            // -------------------------
            // Load FFplay.exe Path
            // -------------------------
            Configure.LoadFFplayPath(this);

            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("FFplay: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run(Configure.ffplayPath) { Foreground = Log.ConsoleDefault });

            // -------------------------
            // Load Log Enabled
            // -------------------------
            Configure.LoadLogCheckbox(this);

            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Log Enabled: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run(Convert.ToString(Configure.logEnable)) { Foreground = Log.ConsoleDefault });

            // -------------------------
            // Load Log Path
            // -------------------------
            Configure.LoadLogPath(this);

            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Log Path: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run(Configure.logPath) { Foreground = Log.ConsoleDefault });

            // -------------------------
            // Load Threads
            // -------------------------
            Configure.LoadThreads(this);

            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Using CPU Threads: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run(Configure.threads) { Foreground = Log.ConsoleDefault });


            //end change !important
            logconsole.rtbLog.EndChange();


            // -------------------------
            // Load Keep Window Toggle
            // -------------------------
            // Log Checkbox     
            // Safeguard Against Corrupt Saved Settings
            try
            {
                // --------------------------
                // First time use
                // --------------------------
                tglWindowKeep.IsChecked = Convert.ToBoolean(Settings.Default.KeepWindow);
            }
            catch
            {

            }

            // -------------------------
            // Load Auto Sort Script Toggle
            // -------------------------
            // Log Checkbox     
            // Safeguard Against Corrupt Saved Settings
            try
            {
                // --------------------------
                // First time use
                // --------------------------
                tglAutoSortScript.IsChecked = Convert.ToBoolean(Settings.Default.AutoSortScript);
            }
            catch
            {

            }


            // -------------------------
            // Load Updates Auto Check
            // -------------------------
            // Log Checkbox     
            // Safeguard Against Corrupt Saved Settings
            try
            {
                // --------------------------
                // First time use
                // --------------------------
                tglUpdatesAutoCheck.IsChecked = Convert.ToBoolean(Settings.Default.UpdatesAutoCheck);
            }
            catch
            {

            }


            // -------------------------
            // Volume Up/Down Button Timer Tick
            // Dispatcher Tick
            // In Intializer to prevent Tick from doubling up every MouseDown
            // -------------------------
            dispatcherTimerUp.Tick += new EventHandler(dispatcherTimerUp_Tick);
            dispatcherTimerDown.Tick += new EventHandler(dispatcherTimerDown_Tick);

            // --------------------------
            // ScriptView Copy/Paste
            // --------------------------
            DataObject.AddCopyingHandler(rtbScriptView, new DataObjectCopyingEventHandler(OnScriptCopy));
            DataObject.AddPastingHandler(rtbScriptView, new DataObjectPastingEventHandler(OnScriptPaste));

        } // End MainWindow

        


        // --------------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     MAIN METHODS
        /// </summary>
        /// <remarks>
        ///     Methods that belong to the MainWindow Class
        /// </remarks>
        // --------------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        ///    Window Loaded
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Control Defaults
            // -------------------------
            // ComboBox Item Sources
            //cboFormat.ItemsSource = FormatControls.FormatItemSource;
            cboMediaType.ItemsSource = FormatControls.MediaTypeItemSource;

            listViewSubtitles.SelectionMode = SelectionMode.Single;

            // Main
            cboPreset.SelectedIndex = 0;

            // Format
            cboFormat.SelectedIndex = 0;
            cboCut.SelectedIndex = 0;
            cboSpeed.SelectedItem = "Medium";
            cboHWAccel.SelectedIndex = 0;

            // Video
            cboVideoQuality.SelectedIndex = 0;
            cboFPS.SelectedIndex = 0;
            cboSize.SelectedIndex = 0;
            cboOptimize.SelectedIndex = 0;

            // Audio
            cboAudioQuality.SelectedIndex = 0;
            cboChannel.SelectedIndex = 0;
            cboSamplerate.SelectedIndex = 0;
            cboBitDepth.SelectedIndex = 0;
            cboBitDepth.IsEnabled = false;

            // Video Filters
            cboFilterVideo_Deband.SelectedIndex = 0;
            cboFilterVideo_Deshake.SelectedIndex = 0;
            cboFilterVideo_Deflicker.SelectedIndex = 0;
            cboFilterVideo_Dejudder.SelectedIndex = 0;
            cboFilterVideo_Dejudder.SelectedIndex = 0;
            cboFilterVideo_Denoise.SelectedIndex = 0;
            cboFilterVideo_SelectiveColor.SelectedIndex = 0;
            cboFilterVideo_SelectiveColor_Correction_Method.SelectedIndex = 0;

            // Audio Filters
            cboFilterAudio_Lowpass.SelectedIndex = 0;
            cboFilterAudio_Highpass.SelectedIndex = 0;
            cboFilterAudio_Headphones.SelectedIndex = 0;

            // Preset
            cboPreset.SelectedIndex = 0;

            // Batch Extension Box Disabled
            batchExtensionTextBox.IsEnabled = false;

            // Open Input/Output Location Disabled
            openLocationInput.IsEnabled = false;
            openLocationOutput.IsEnabled = false;


            // -------------------------
            // Load ComboBox Items
            // -------------------------
            // Filter Selective SelectiveColorPreview
            cboFilterVideo_SelectiveColor.ItemsSource = cboSelectiveColor_Items;

            // -------------------------
            // Startup Preset
            // -------------------------
            // Default Format is WebM
            if ((string)cboFormat.SelectedItem == "webm")
            {
                cboSubtitlesStream.SelectedItem = "none";
                cboAudioStream.SelectedItem = "1";
            }

            // -------------------------
            // Check for Available Updates
            // -------------------------
            Task.Factory.StartNew(() =>
            {
                UpdateAvailableCheck();
            });
        }

        /// <summary>
        ///     Close / Exit (Method)
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Force Exit All Executables
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        // Save Window Position
        void Window_Closing(object sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Settings.Default.Top = RestoreBounds.Top;
                Settings.Default.Left = RestoreBounds.Left;
                Settings.Default.Height = RestoreBounds.Height;
                Settings.Default.Width = RestoreBounds.Width;
                Settings.Default.Maximized = true;
            }
            else
            {
                Settings.Default.Top = this.Top;
                Settings.Default.Left = this.Left;
                Settings.Default.Height = this.Height;
                Settings.Default.Width = this.Width;
                Settings.Default.Maximized = false;
            }

            Settings.Default.Save();

            // Exit
            e.Cancel = true;
            System.Windows.Forms.Application.ExitThread();
            Environment.Exit(0);
        }


        /// <summary>
        ///     Clear Variables (Method)
        /// </summary>
        public static void ClearVariables(MainWindow mainwindow)
        {
            // FFmpeg
            //FFmpeg.cmdWindow = string.Empty;

            // FFprobe
            FFprobe.argsVideoCodec = string.Empty;
            FFprobe.argsAudioCodec = string.Empty;
            FFprobe.argsVideoBitrate = string.Empty;
            FFprobe.argsAudioBitrate = string.Empty;
            FFprobe.argsSize = string.Empty;
            FFprobe.argsDuration = string.Empty;
            FFprobe.argsFrameRate = string.Empty;

            FFprobe.inputVideoCodec = string.Empty;
            FFprobe.inputVideoBitrate = string.Empty;
            FFprobe.inputAudioCodec = string.Empty;
            FFprobe.inputAudioBitrate = string.Empty;
            FFprobe.inputSize = string.Empty;
            FFprobe.inputDuration = string.Empty;
            FFprobe.inputFrameRate = string.Empty;

            FFprobe.vEntryType = string.Empty;
            FFprobe.aEntryType = string.Empty;

            // Video
            Video.passSingle = string.Empty;
            Video.vCodec = string.Empty;
            Video.vQuality = string.Empty;
            Video.vBitMode = string.Empty;
            Video.vLossless = string.Empty;
            Video.vBitrate = string.Empty;
            Video.vMaxrate = string.Empty;
            Video.vOptions = string.Empty;
            Video.crf = string.Empty;
            Video.fps = string.Empty;
            Video.optTune = string.Empty;
            Video.optProfile = string.Empty;
            Video.optLevel = string.Empty;
            Video.aspect = string.Empty;
            Video.width = string.Empty;
            Video.height = string.Empty;

            if (Video.x265paramsList != null)
            {
                Video.x265paramsList.Clear();
                Video.x265paramsList.TrimExcess();
            }

            Video.x265params = string.Empty;

            // Clear Crop if ClearCrop Button Identifier is Empty
            if (mainwindow.buttonCropClearTextBox.Text == "Clear")
            {
                CropWindow.crop = string.Empty;
                CropWindow.divisibleCropWidth = null; //int
                CropWindow.divisibleCropHeight = null; //int
            }

            Format.trim = string.Empty;
            Format.trimStart = string.Empty;
            Format.trimEnd = string.Empty;
            //batchExt = string.Empty;

            VideoFilters.vFilter = string.Empty;
            VideoFilters.geq = string.Empty;

            if (VideoFilters.vFiltersList != null)
            {
                VideoFilters.vFiltersList.Clear();
                VideoFilters.vFiltersList.TrimExcess();
            }

            Video.v2PassArgs = string.Empty;
            Video.pass1Args = string.Empty; // Batch 2-Pass
            Video.pass2Args = string.Empty; // Batch 2-Pass
            Video.pass1 = string.Empty;
            Video.pass2 = string.Empty;
            Video.image = string.Empty;
            Video.optimize = string.Empty;
            Video.speed = string.Empty;
            Video.hwaccel = string.Empty;

            // Subtitle
            Video.sCodec = string.Empty;

            // Audio
            Audio.aCodec = string.Empty;
            Audio.aQuality = string.Empty;
            Audio.aBitMode = string.Empty;
            Audio.aBitrate = string.Empty;
            Audio.aChannel = string.Empty;
            Audio.aSamplerate = string.Empty;
            Audio.aBitDepth = string.Empty;
            Audio.aBitrateLimiter = string.Empty;
            AudioFilters.aFilter = string.Empty;
            Audio.aVolume = string.Empty;
            Audio.aLimiter = string.Empty;

            if (AudioFilters.aFiltersList != null)
            {
                AudioFilters.aFiltersList.Clear();
                AudioFilters.aFiltersList.TrimExcess();
            }

            // Batch
            FFprobe.batchFFprobeAuto = string.Empty;
            Video.batchVideoAuto = string.Empty;
            Audio.batchAudioAuto = string.Empty;
            Audio.aBitrateLimiter = string.Empty;

            // Streams
            //Streams.map = string.Empty;
            Streams.vMap = string.Empty;
            Streams.cMap = string.Empty;
            Streams.sMap = string.Empty;
            Streams.aMap = string.Empty;
            Streams.mMap = string.Empty;

            // General
            //outputNewFileName = string.Empty;

            // Do not Empty:
            //
            //inputDir
            //inputFileName
            //inputExt
            //input
            //outputDir
            //outputFileName
            //FFmpeg.ffmpegArgs
            //FFmpeg.ffmpegArgsSort
            //CropWindow.divisibleCropWidth
            //CropWindow.divisibleCropHeight
            //CropWindow.cropWidth
            //CropWindow.cropHeight
            //CropWindow.cropX
            //CropWindow.cropY
        }


        /// <summary>
        ///     Remove Linebreaks (Method)
        /// </summary>
        /// <remarks>
        ///     Used for Selected Controls FFmpeg Arguments
        /// </remarks>
        public static String RemoveLineBreaks(string lines)
        {
            lines = lines
                .Replace(Environment.NewLine, "")
                .Replace("\r\n\r\n", "")
                .Replace("\r\n", "")
                .Replace("\n\n", "")
                .Replace("\n", "")
                .Replace("\u2028", "")
                .Replace("\u000A", "")
                .Replace("\u000B", "")
                .Replace("\u000C", "")
                .Replace("\u000D", "")
                .Replace("\u0085", "")
                .Replace("\u2028", "")
                .Replace("\u2029", "");

            return lines;
        }


        /// <summary>
        ///     Replace Linebreaks with Space (Method)
        /// </summary>
        /// <remarks>
        ///     Used for Script View Custom Edited Script
        /// </remarks>
        public static String ReplaceLineBreaksWithSpaces(string lines)
        {
            // Replace Linebreaks with Spaces to avoid arguments touching

            lines = lines
                .Replace(Environment.NewLine, " ")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\u2028", " ")
                .Replace("\u000A", " ")
                .Replace("\u000B", " ")
                .Replace("\u000C", " ")
                .Replace("\u000D", " ")
                .Replace("\u0085", " ")
                .Replace("\u2028", " ")
                .Replace("\u2029", " ");

            return lines;
        }


        /// <summary>
        ///     Start Log Console (Method)
        /// </summary>
        public void StartLogConsole()
        {
            // Open LogConsole Window
            logconsole = new LogConsole(this);
            logconsole.Hide();

            // Position with Show();

            logconsole.rtbLog.Cursor = Cursors.Arrow;
        }


        /// <summary>
        ///     System Info
        /// </summary>
        public void SystemInfoDisplay()
        {
            // -----------------------------------------------------------------
            /// <summary>
            ///     System Info
            /// </summary>
            /// <remarks>
            ///     Detect and Display System Hardware
            /// </remarks>
            // -----------------------------------------------------------------
            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("System Info:")) { Foreground = Log.ConsoleAction });
            Log.logParagraph.Inlines.Add(new LineBreak());

            /// <summary>
            /// OS
            /// </summary>
            try
            {
                ManagementClass os = new ManagementClass("Win32_OperatingSystem");

                foreach (ManagementObject obj in os.GetInstances())
                {
                    // Log Console Message /////////
                    Log.logParagraph.Inlines.Add(new Run(Convert.ToString(obj["Caption"])) { Foreground = Log.ConsoleDefault });
                    Log.logParagraph.Inlines.Add(new LineBreak());

                }
                os.Dispose();
            }
            catch
            {

            }


            /// <summary>
            /// CPU
            /// </summary>
            try
            {
                ManagementObjectSearcher cpu = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name FROM Win32_Processor");

                foreach (ManagementObject obj in cpu.Get())
                {
                    // Log Console Message /////////
                    Log.logParagraph.Inlines.Add(new Run(Convert.ToString(obj["Name"])) { Foreground = Log.ConsoleDefault });
                    Log.logParagraph.Inlines.Add(new LineBreak());
                }
                cpu.Dispose();

                // Max Threads
                foreach (var item in new System.Management.ManagementObjectSearcher("Select NumberOfLogicalProcessors FROM Win32_ComputerSystem").Get())
                {
                    Configure.maxthreads = String.Format("{0}", item["NumberOfLogicalProcessors"]);
                }
            }
            catch
            {

            }


            /// <summary>
            /// GPU
            /// </summary>
            try
            {
                ManagementObjectSearcher gpu = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name, AdapterRAM FROM Win32_VideoController");

                foreach (ManagementObject obj in gpu.Get())
                {
                    Log.logParagraph.Inlines.Add(new Run(Convert.ToString(obj["Name"]) + " " + Convert.ToString(Math.Round(Convert.ToDouble(obj["AdapterRAM"]) * 0.000000001, 3) + "GB")) { Foreground = Log.ConsoleDefault });
                    Log.logParagraph.Inlines.Add(new LineBreak());
                }
            }
            catch
            {

            }


            /// <summary>
            /// RAM
            /// </summary>
            try
            {
                Log.logParagraph.Inlines.Add(new Run("RAM ") { Foreground = Log.ConsoleDefault });

                double capacity = 0;
                int memtype = 0;
                string type;
                int speed = 0;

                ManagementObjectSearcher ram = new ManagementObjectSearcher("root\\CIMV2", "SELECT Capacity, MemoryType, Speed FROM Win32_PhysicalMemory");

                foreach (ManagementObject obj in ram.Get())
                {
                    capacity += Convert.ToDouble(obj["Capacity"]);
                    memtype = Int32.Parse(obj.GetPropertyValue("MemoryType").ToString());
                    speed = Int32.Parse(obj.GetPropertyValue("Speed").ToString());
                }

                capacity *= 0.000000001; // Convert Byte to GB
                capacity = Math.Round(capacity, 3); // Round to 3 decimal places

                // Select RAM Type
                switch (memtype)
                {
                    case 20:
                        type = "DDR";
                        break;
                    case 21:
                        type = "DDR2";
                        break;
                    case 17:
                        type = "SDRAM";
                        break;
                    default:
                        if (memtype == 0 || memtype > 22)
                            type = "DDR3";
                        else
                            type = "Unknown";
                        break;
                }

                // Log Console Message /////////
                Log.logParagraph.Inlines.Add(new Run(Convert.ToString(capacity) + "GB " + type + " " + Convert.ToString(speed) + "MHz") { Foreground = Log.ConsoleDefault });
                Log.logParagraph.Inlines.Add(new LineBreak());

                ram.Dispose();
            }
            catch
            {

            }

            // End System Info
        }

        /// <summary>
        ///     Start File Queue (Method)
        /// </summary>
        //public void StartFileQueue()
        //{
        //    MainWindow mainwindow = this;

        //    // Open File Queue Window
        //    filequeue = new FileQueue(mainwindow);
        //    filequeue.Hide();

        //    // Position with Show();
        //}


        /// <summary>
        ///     Normalize Value (Method)
        /// <summary>
        public static double NormalizeValue(double val, double valmin, double valmax, double min, double max, double midpoint)
        {
            double mid = (valmin + valmax) / 2.0;
            if (val < mid)
            {
                return (val - valmin) / (mid - valmin) * (midpoint - min) + min;
            }
            else
            {
                return (val - mid) / (valmax - mid) * (max - midpoint) + midpoint;
            }
        }
        //public static double NormalizeValue(double val, double valmin, double valmax, double min, double max, double ffdefault)
        //{
        //    // (((sliderValue - sliderValueMin) / (sliderValueMax - sliderValueMin)) * (NormalizeMax - NormalizeMin)) + NormalizeMin

        //    return (((val - valmin) / (valmax - valmin)) * (max - min)) + min;
        //}


        /// <summary>
        ///     Limit to Range (Method)
        /// <summary>
        public static double LimitToRange(double value, double inclusiveMinimum, double inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }


        /// <summary>
        ///    FFcheck (Method)
        /// </summary>
        /// <remarks>
        ///     Check if FFmpeg and FFprobe is on Computer 
        /// </remarks>
        public void FFcheck()
        {
            try
            {
                // Environment Variables
                var envar = Environment.GetEnvironmentVariable("PATH");

                //MessageBox.Show(envar); //debug

                // -------------------------
                // FFmpeg
                // -------------------------
                // If Auto Mode
                if (Configure.ffmpegPath == "<auto>")
                {
                    // Check default current directory
                    if (File.Exists(appDir + "ffmpeg\\bin\\ffmpeg.exe"))
                    {
                        // let pass
                        ffCheckCleared = true;
                    }
                    else
                    {
                        int found = 0;

                        // Check Environment Variables
                        foreach (var envarPath in envar.Split(';'))
                        {
                            var exePath = Path.Combine(envarPath, "ffmpeg.exe");
                            if (File.Exists(exePath)) { found = 1; }
                        }

                        if (found == 1)
                        {
                            // let pass
                            ffCheckCleared = true;
                        }
                        else
                        {
                            /* lock */
                            ready = false;
                            ffCheckCleared = false;
                            MessageBox.Show("Cannot locate FFmpeg Path in Environment Variables or Current Folder.",
                                            "Error",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning);
                        }

                    }
                }
                // If User Defined Path
                else if (Configure.ffmpegPath != "<auto>" && !string.IsNullOrEmpty(Configure.ffprobePath))
                {
                    var dirPath = Path.GetDirectoryName(Configure.ffmpegPath).TrimEnd('\\') + @"\";
                    var fullPath = Path.Combine(dirPath, "ffmpeg.exe");

                    // Make Sure ffmpeg.exe Exists
                    if (File.Exists(fullPath))
                    {
                        // let pass
                        ffCheckCleared = true;
                    }
                    else
                    {
                        /* lock */
                        ready = false;
                        ffCheckCleared = false;
                        MessageBox.Show("Cannot locate FFmpeg Path in User Defined Path.",
                                        "Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                    }

                    // If Configure Path is ffmpeg.exe and not another Program
                    if (string.Equals(Configure.ffmpegPath, fullPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // let pass
                        ffCheckCleared = true;
                    }
                    else
                    {
                        /* lock */
                        ready = false;
                        ffCheckCleared = false;
                        MessageBox.Show("FFmpeg Path must link to ffmpeg.exe.",
                                        "Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                    }
                }

                // -------------------------
                // FFprobe
                // -------------------------
                // If Auto Mode
                if (Configure.ffprobePath == "<auto>")
                {
                    // Check default current directory
                    if (File.Exists(appDir + "ffmpeg\\bin\\ffprobe.exe"))
                    {
                        // let pass
                        ffCheckCleared = true;
                    }
                    else
                    {
                        int found = 0;

                        // Check Environment Variables
                        foreach (var envarPath in envar.Split(';'))
                        {
                            var exePath = Path.Combine(envarPath, "ffprobe.exe");
                            if (File.Exists(exePath)) { found = 1; }
                        }

                        if (found == 1)
                        {
                            // let pass
                            ffCheckCleared = true;
                        }
                        else
                        {
                            /* lock */
                            ready = false;
                            ffCheckCleared = false;
                            MessageBox.Show("Cannot locate FFprobe Path in Environment Variables or Current Folder.",
                                        "Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        }

                    }
                }
                // If User Defined Path
                else if (Configure.ffprobePath != "<auto>" && !string.IsNullOrEmpty(Configure.ffprobePath))
                {
                    var dirPath = Path.GetDirectoryName(Configure.ffprobePath).TrimEnd('\\') + @"\";
                    var fullPath = Path.Combine(dirPath, "ffprobe.exe");

                    // Make Sure ffprobe.exe Exists
                    if (File.Exists(fullPath))
                    {
                        // let pass
                        ffCheckCleared = true;
                    }
                    else
                    {
                        /* lock */
                        ready = false;
                        ffCheckCleared = false;
                        MessageBox.Show("Cannot locate FFprobe Path in User Defined Path.",
                                        "Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                    }

                    // If Configure Path is FFmpeg.exe and not another Program
                    if (string.Equals(Configure.ffprobePath, fullPath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // let pass
                        ffCheckCleared = true;
                    }
                    else
                    {
                        /* lock */
                        ready = false;
                        ffCheckCleared = false;
                        MessageBox.Show("Error: FFprobe Path must link to ffprobe.exe.",
                                        "Error",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Unknown Error trying to locate FFmpeg or FFprobe.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }


        /// <summary>
        ///    FFmpeg Path (Method)
        /// </summary>
        public static String FFmpegPath()
        {
            // -------------------------
            // FFmpeg.exe and FFprobe.exe Paths
            // -------------------------
            // If Configure FFmpeg Path is <auto>
            if (Configure.ffmpegPath == "<auto>")
            {
                if (File.Exists(appDir + "ffmpeg\\bin\\ffmpeg.exe"))
                {
                    // use included binary
                    FFmpeg.ffmpeg = "\"" + appDir + "ffmpeg\\bin\\ffmpeg.exe" + "\"";
                }
                else if (!File.Exists(appDir + "ffmpeg\\bin\\ffmpeg.exe"))
                {
                    // use system installed binaries
                    FFmpeg.ffmpeg = "ffmpeg";
                }
            }
            // Use User Custom Path
            else
            {
                FFmpeg.ffmpeg = "\"" + Configure.ffmpegPath + "\"";
            }

            // Return Value
            return FFmpeg.ffmpeg;
        }


        /// <remarks>
        ///     FFprobe Path
        /// </remarks>
        public static void FFprobePath()
        {
            // If Configure FFprobe Path is <auto>
            if (Configure.ffprobePath == "<auto>")
            {
                if (File.Exists(appDir + "ffmpeg\\bin\\ffprobe.exe"))
                {
                    // use included binary
                    FFprobe.ffprobe = "\"" + appDir + "ffmpeg\\bin\\ffprobe.exe" + "\"";
                }
                else if (!File.Exists(appDir + "ffmpeg\\bin\\ffprobe.exe"))
                {
                    // use system installed binaries
                    FFprobe.ffprobe = "ffprobe";
                }
            }
            // Use User Custom Path
            else
            {
                FFprobe.ffprobe = "\"" + Configure.ffprobePath + "\"";
            }

            // Return Value
            //return FFprobe.ffprobe;
        }


        /// <remarks>
        ///     FFplay Path
        /// </remarks>
        public static void FFplayPath()
        {
            // If Configure FFprobe Path is <auto>
            if (Configure.ffplayPath == "<auto>")
            {
                if (File.Exists(appDir + "ffmpeg\\bin\\ffplay.exe"))
                {
                    // use included binary
                    FFplay.ffplay = "\"" + appDir + "ffmpeg\\bin\\ffplay.exe" + "\"";
                }
                else if (!File.Exists(appDir + "ffmpeg\\bin\\ffplay.exe"))
                {
                    // use system installed binaries
                    FFplay.ffplay = "ffplay";
                }
            }
            // Use User Custom Path
            else
            {
                FFplay.ffplay = "\"" + Configure.ffplayPath + "\"";
            }

            // Return Value
            //return FFplay.ffplay;
        }


        /// <summary>
        ///    Thread Detect (Method)
        /// </summary>
        public static String ThreadDetect(MainWindow mainwindow)
        {
            // -------------------------
            // Default
            // -------------------------
            if ((string)mainwindow.cboThreads.SelectedItem == "default")
            {
                Configure.threads = string.Empty;
            }

            // -------------------------
            // Optimal
            // -------------------------
            else if ((string)mainwindow.cboThreads.SelectedItem == "optimal"
                || string.IsNullOrEmpty(Configure.threads))
            {
                Configure.threads = "-threads 0";
            }

            // -------------------------
            // All
            // -------------------------
            else if ((string)mainwindow.cboThreads.SelectedItem == "all"
                || string.IsNullOrEmpty(Configure.threads))
            {
                Configure.threads = "-threads " + Configure.maxthreads;
            }

            // -------------------------
            // Custom
            // -------------------------
            else
            {
                Configure.threads = "-threads " + mainwindow.cboThreads.SelectedItem.ToString();
            }

            // Return Value
            return Configure.threads;
        }



        /// <summary>
        ///    Batch Input Directory (Method)
        /// </summary>
        // Directory Only, Needed for Batch
        public static String BatchInputDirectory(MainWindow mainwindow)
        {
            // -------------------------
            // Batch
            // -------------------------
            if (mainwindow.tglBatch.IsChecked == true)
            {
                inputDir = mainwindow.tbxInput.Text; // (eg. C:\Input Folder\)
            }

            // -------------------------
            // Empty
            // -------------------------
            // Input Textbox & Output Textbox Both Empty
            if (string.IsNullOrWhiteSpace(mainwindow.tbxInput.Text))
            {
                inputDir = string.Empty;
            }


            // Return Value
            return inputDir;
        }



        /// <summary>
        ///    Input Path (Method)
        /// </summary>
        public static String InputPath(MainWindow mainwindow)
        {
            // -------------------------
            // Single File
            // -------------------------
            if (mainwindow.tglBatch.IsChecked == false)
            {
                // Input Directory
                // If not Empty
                //if (!mainwindow.tbxInput.Text.Contains("www.youtube.com")
                //    && !mainwindow.tbxInput.Text.Contains("youtube.com"))
                //{
                if (!string.IsNullOrWhiteSpace(mainwindow.tbxInput.Text))
                {
                    //inputDir = Path.GetDirectoryName(mainwindow.tbxInput.Text.TrimEnd('\\') + @"\"); // (eg. C:\Input Folder\)
                    inputDir = Path.GetDirectoryName(mainwindow.tbxInput.Text).TrimEnd('\\') + @"\"; // (eg. C:\Input Folder\)
                    inputFileName = Path.GetFileNameWithoutExtension(mainwindow.tbxInput.Text);
                    inputExt = Path.GetExtension(mainwindow.tbxInput.Text);
                }

                // Input
                input = mainwindow.tbxInput.Text; // (eg. C:\Input Folder\file.wmv)
                //}
                //else
                //{
                //    input = "\"" + "%appdata%/YouTube/" + "" + "\"";
                //}
            }

            // -------------------------
            // Batch
            // -------------------------
            else if (mainwindow.tglBatch.IsChecked == true)
            {
                // Add slash to Batch Browse Text folder path if missing
                mainwindow.tbxInput.Text = mainwindow.tbxInput.Text.TrimEnd('\\') + @"\";

                inputDir = mainwindow.tbxInput.Text; // (eg. C:\Input Folder\)

                inputFileName = "%~f";

                // Input
                input = inputDir + inputFileName; // (eg. C:\Input Folder\)
            }

            // -------------------------
            // Empty
            // -------------------------
            // Input Textbox & Output Textbox Both Empty
            if (string.IsNullOrWhiteSpace(mainwindow.tbxInput.Text))
            {
                inputDir = string.Empty;
                inputFileName = string.Empty;
                input = string.Empty;
            }


            // Return Value
            return input;
        }



        /// <summary>
        ///    Output Path (Method)
        /// </summary>
        public static String OutputPath(MainWindow mainwindow)
        {
            // Get Output Extension (Method)
            FormatControls.OutputFormatExt(mainwindow);

            // -------------------------
            // Single File
            // -------------------------
            if (mainwindow.tglBatch.IsChecked == false)
            {
                // Input Not Empty, Output Empty
                // Default Output to be same as Input Directory
                if (!string.IsNullOrWhiteSpace(mainwindow.tbxInput.Text)
                    && string.IsNullOrWhiteSpace(mainwindow.tbxOutput.Text))
                {
                    mainwindow.tbxOutput.Text = inputDir + inputFileName + outputExt;
                }

                // Input Empty, Output Not Empty
                if (!string.IsNullOrWhiteSpace(mainwindow.tbxOutput.Text))
                {
                    outputDir = Path.GetDirectoryName(mainwindow.tbxOutput.Text).TrimEnd('\\') + @"\";

                    outputFileName = Path.GetFileNameWithoutExtension(mainwindow.tbxOutput.Text);
                }

                // -------------------------
                // File Renamer
                // -------------------------
                // Auto Renamer
                // Pressing Script or Convert while Output is empty
                if (inputDir == outputDir
                    && inputFileName == outputFileName
                    && string.Equals(inputExt, outputExt, StringComparison.CurrentCultureIgnoreCase))
                {
                    outputFileName = mainwindow.FileRenamer(inputFileName);
                }

                // -------------------------
                // Image Sequence Renamer
                // -------------------------
                if ((string)mainwindow.cboMediaType.SelectedItem == "Sequence")
                {
                    outputFileName = "image-%03d"; //must be this name
                }

                // -------------------------
                // Output
                // -------------------------
                output = outputDir + outputFileName + outputExt; // (eg. C:\Output Folder\ + file + .mp4)    

                // Update TextBox
                if (!string.IsNullOrWhiteSpace(mainwindow.tbxOutput.Text))
                {
                    mainwindow.tbxOutput.Text = output;
                }
            }

            // -------------------------
            // Batch
            // -------------------------
            else if (mainwindow.tglBatch.IsChecked == true)
            {
                // Add slash to Batch Output Text folder path if missing
                mainwindow.tbxOutput.Text = mainwindow.tbxOutput.Text.TrimEnd('\\') + @"\";

                // Input Not Empty, Output Empty
                // Default Output to be same as Input Directory
                if (!string.IsNullOrWhiteSpace(mainwindow.tbxInput.Text) && string.IsNullOrWhiteSpace(mainwindow.tbxOutput.Text))
                {
                    mainwindow.tbxOutput.Text = mainwindow.tbxInput.Text;
                }

                outputDir = mainwindow.tbxOutput.Text;

                // Output             
                output = outputDir + "%~nf" + outputExt; // (eg. C:\Output Folder\%~nf.mp4)
            }

            // -------------------------
            // Empty
            // -------------------------
            // Input Textbox & Output Textbox Both Empty
            if (string.IsNullOrWhiteSpace(mainwindow.tbxOutput.Text))
            {
                outputDir = string.Empty;
                outputFileName = string.Empty;
                output = string.Empty;
            }


            // Return Value
            return output;
        }



        /// <summary>
        ///    Batch Extension Period Check (Method)
        /// </summary>
        public static void BatchExtCheck(MainWindow mainwindow)
        {
            // Add period if Batch Extension if User did not enter
            if (!string.IsNullOrWhiteSpace(mainwindow.batchExtensionTextBox.Text) &&
                mainwindow.batchExtensionTextBox.Text != "extension" &&
                mainwindow.batchExtensionTextBox.Text != ".")
            {
                batchExt = "." + mainwindow.batchExtensionTextBox.Text;
            }
            else
            {
                batchExt = string.Empty;
            }

            //// Add period if Batch Extension if User did not enter
            //if (!mainwindow.batchExtensionTextBox.Text.Contains("."))
            //{
            //    mainwindow.batchExtensionTextBox.Text = "." + mainwindow.batchExtensionTextBox.Text;
            //}

            //// Clear Batch Extension Text if Only period
            //if (mainwindow.batchExtensionTextBox.Text == ".")
            //{
            //    mainwindow.batchExtensionTextBox.Text = string.Empty;
            //    batchExt = string.Empty;
            //}
        }


        /// <summary>
        ///    Delete 2 Pass Logs Lock Check (Method)
        /// </summary>
        /// <remarks>
        ///     Check if File is in use by another Process (FFmpeg writing 2 Pass log)
        /// </remarks>
        //protected virtual bool IsFileLocked(FileInfo file)
        //{
        //    FileStream stream = null;

        //    try
        //    {
        //        stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        //    }
        //    catch (IOException)
        //    {
        //        //the file is unavailable because it is:
        //        //still being written to
        //        //or being processed by another thread
        //        //or does not exist (has already been processed)
        //        return true;
        //    }
        //    finally
        //    {
        //        if (stream != null)
        //            stream.Close();
        //    }

        //    //file is not locked
        //    return false;
        //}



        /// <summary>
        ///    File Renamer (Method)
        /// </summary>
        public String FileRenamer(string filename)
        {
            string output = outputDir + filename + outputExt;
            string outputNewFileName = string.Empty;

            int count = 1;

            if (File.Exists(output))
            {
                while (File.Exists(output))
                {
                    outputNewFileName = string.Format("{0}({1})", filename + " ", count++);
                    output = Path.Combine(outputDir, outputNewFileName + outputExt);
                }
            }
            else
            {
                // stay default
                outputNewFileName = filename;
            }

            return outputNewFileName;
        }



        /// <summary>
        ///    YouTube Download (Method)
        /// </summary>
        public static String YouTubeDownload(string input)
        {
            if (input.Contains("www.youtube.com")
                || input.Contains("youtube.com"))
            {
                youtubedl = "cd " + "\"" + appDir + "youtube-dl" + "\"" + " && youtube-dl.exe " + input + " -o %appdata%/YouTube/%(title)s.%(ext)s &&";
            }

            return youtubedl;
        }



        /// <summary>
        ///    Check if Script has been Edited (Method)
        /// </summary>
        public static bool CheckScriptEdited(MainWindow mainwindow)
        {
            bool edited = false;

            // -------------------------
            // Check if Script has been modified
            // -------------------------
            if (!string.IsNullOrWhiteSpace(ScriptView.GetScriptRichTextBoxContents(mainwindow))
                && !string.IsNullOrEmpty(FFmpeg.ffmpegArgs))
            {
                //MessageBox.Show(RemoveLineBreaks(ScriptView.GetScriptRichTextBoxContents(mainwindow))); //debug
                //MessageBox.Show(FFmpeg.ffmpegArgs); //debug

                // Compare RichTextBox Script Against FFmpeg Generated Args
                if (RemoveLineBreaks(ScriptView.GetScriptRichTextBoxContents(mainwindow)) != FFmpeg.ffmpegArgs)
                {
                    // Yes/No Dialog Confirmation
                    MessageBoxResult result = MessageBox.Show("The Convert button will override and replace your custom script with the selected controls."
                                                              + "\r\n\r\nPress the Run button instead to execute your script."
                                                              + "\r\n\r\nContinue Convert?",
                                                              "Edited Script Detected",
                                                              MessageBoxButton.YesNo,
                                                              MessageBoxImage.Warning);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            // Continue
                            break;
                        case MessageBoxResult.No:
                            // Halt
                            edited = true;
                            break;
                    }
                }
            }

            return edited;
        }



        /// <summary>
        ///    Ready Halts (Method)
        /// </summary>
        public static void ReadyHalts(MainWindow mainwindow)
        {
            // -------------------------
            // Check if FFmpeg & FFprobe Exists
            // -------------------------
            if (ffCheckCleared == false)
            {
                mainwindow.FFcheck();
            }

            // -------------------------
            // Do not allow Auto without FFprobe being installed or linked
            // -------------------------
            if (string.IsNullOrEmpty(FFprobe.ffprobe))
            {
                if ((string)mainwindow.cboVideoQuality.SelectedItem == "Auto"
                    || (string)mainwindow.cboAudioQuality.SelectedItem == "Auto")
                {
                    // Log Console Message /////////
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Bold(new Run("Auto Quality Mode Requires FFprobe in order to Detect File Info.")) { Foreground = Log.ConsoleWarning });

                    /* lock */
                    ready = false;
                    MessageBox.Show("Auto Quality Mode Requires FFprobe in order to Detect File Info.",
                                    "Notice",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
                }
            }

            // -------------------------
            // Do not allow Script to generate if Browse Empty & Auto, since there is no file to detect bitrates/codecs
            // -------------------------
            if (mainwindow.tglBatch.IsChecked == false) // Ignore if Batch
            {
                if (string.IsNullOrWhiteSpace(mainwindow.tbxInput.Text)) // empty check
                {
                    // -------------------------
                    // Both Video & Audio are Auto Quality
                    // Combined Single Warning
                    // -------------------------
                    if ((string)mainwindow.cboVideoQuality.SelectedItem == "Auto"
                        && (string)mainwindow.cboAudioQuality.SelectedItem == "Auto"
                        && (string)mainwindow.cboVideoCodec.SelectedItem != "Copy"
                        && (string)mainwindow.cboAudioCodec.SelectedItem != "Copy"
                        )
                    {
                        // Log Console Message /////////
                        Log.logParagraph.Inlines.Add(new LineBreak());
                        Log.logParagraph.Inlines.Add(new LineBreak());
                        Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: Video & Audio Quality require an input file in order to detect bitrate settings.")) { Foreground = Log.ConsoleWarning });

                        /* lock */
                        ready = false;
                        script = false;
                        // Warning
                        MessageBox.Show("Video & Audio Auto Quality require an input file in order to detect bitrate settings.",
                                        "Notice",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }

                    // -------------------------
                    // Either Video & Audio are Auto Quality
                    // Warning for each
                    // -------------------------
                    else
                    {
                        // -------------------------
                        // Video Auto Quality
                        // -------------------------
                        if ((string)mainwindow.cboVideoQuality.SelectedItem == "Auto")
                        {
                            if ((string)mainwindow.cboVideoCodec.SelectedItem != "Copy")
                            {
                                // Log Console Message /////////
                                Log.logParagraph.Inlines.Add(new LineBreak());
                                Log.logParagraph.Inlines.Add(new LineBreak());
                                Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: Video Auto Quality requires an input file in order to detect bitrate settings.")) { Foreground = Log.ConsoleWarning });

                                /* lock */
                                ready = false;
                                script = false;
                                // Warning
                                MessageBox.Show("Video Auto Quality requires an input file in order to detect bitrate settings.",
                                                "Notice",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                            }
                        }

                        // -------------------------
                        // Audio Auto Quality
                        // -------------------------
                        if ((string)mainwindow.cboAudioQuality.SelectedItem == "Auto")
                        {
                            if ((string)mainwindow.cboAudioCodec.SelectedItem != "Copy")
                            {
                                // Log Console Message /////////
                                Log.logParagraph.Inlines.Add(new LineBreak());
                                Log.logParagraph.Inlines.Add(new LineBreak());
                                Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: Audio Auto Quality requires an input file in order to detect bitrate settings.")) { Foreground = Log.ConsoleWarning });

                                /* lock */
                                ready = false;
                                script = false;
                                // Warning
                                MessageBox.Show("Audio Auto Quality requires an input file in order to detect bitrate settings.",
                                                "Notice",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                            }
                        }
                    }                  
                }
            }

            // -------------------------
            // Halt if Single File Input with no Extension
            // -------------------------
            if (mainwindow.tglBatch.IsChecked == false && mainwindow.tbxInput.Text.EndsWith("\\"))
            {
                // Log Console Message /////////
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: Please choose an input file.")) { Foreground = Log.ConsoleWarning });

                /* lock */
                ready = false;
                // Warning
                MessageBox.Show("Please choose an input file.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }

            // -------------------------
            // Do not allow Batch Copy to same folder if file extensions are the same (to avoid file overwrite)
            // -------------------------
            if (mainwindow.tglBatch.IsChecked == true
                && string.Equals(inputDir, outputDir, StringComparison.CurrentCultureIgnoreCase)
                | string.Equals(batchExt, outputExt, StringComparison.CurrentCultureIgnoreCase))
            {
                //MessageBox.Show(inputDir); //debug
                //MessageBox.Show(outputDir); //debug

                // Log Console Message /////////
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: Please choose an output folder different than the input folder to avoid file overwrite.")) { Foreground = Log.ConsoleWarning });

                /* lock */
                ready = false;
                // Warning
                MessageBox.Show("Please choose an output folder different than the input folder to avoid file overwrite.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }

            // -------------------------
            // Throw Error if VP8/VP9 & CRF does not have Bitrate -b:v
            // -------------------------
            if ((string)mainwindow.cboVideoCodec.SelectedItem == "VP8"
                || (string)mainwindow.cboVideoCodec.SelectedItem == "VP9")
            {
                if (!string.IsNullOrWhiteSpace(mainwindow.crfCustom.Text)
                    && string.IsNullOrWhiteSpace(mainwindow.vBitrateCustom.Text))
                {
                    // Log Console Message /////////
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: VP8/VP9 CRF must also have Bitrate. \n(e.g. 0 for Constant, 1234k for Constrained)")) { Foreground = Log.ConsoleWarning });

                    /* lock */
                    ready = false;
                    // Notice
                    MessageBox.Show("VP8/VP9 CRF must also have Bitrate. \n(e.g. 0 for Constant, 1234k for Constrained)",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
                }
            }
        }



        // --------------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     CONTROLS
        /// </summary>
        // --------------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------------

        // --------------------------------------------------------------------------------------------------------
        // Configure
        // --------------------------------------------------------------------------------------------------------

        // --------------------------------------------------
        // FFmpeg Textbox Click
        // --------------------------------------------------
        private void textBoxFFmpegPathConfig_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Configure.FFmpegFolderBrowser(this);
        }


        // --------------------------------------------------
        // FFmpeg Textbox (Text Changed)
        // --------------------------------------------------
        private void textBoxFFmpegPathConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            // dont use
        }


        // --------------------------------------------------
        // FFmpeg Auto Path Button (On Click)
        // --------------------------------------------------
        private void buttonFFmpegAuto_Click(object sender, RoutedEventArgs e)
        {
            // Set the ffmpegPath string
            Configure.ffmpegPath = "<auto>";

            // Display Folder Path in Textbox
            textBoxFFmpegPathConfig.Text = "<auto>";

            // FFmpeg Path path for next launch
            Settings.Default["ffmpegPath"] = "<auto>";
            Settings.Default.Save();
            Settings.Default.Reload();
        }


        // --------------------------------------------------
        // FFprobe Textbox Click
        // --------------------------------------------------
        private void textBoxFFprobePathConfig_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Configure.FFprobeFolderBrowser(this);
        }


        // --------------------------------------------------
        // FFprobe Textbox (Text Changed)
        // --------------------------------------------------
        private void textBoxFFprobePathConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            // dont use
        }


        // --------------------------------------------------
        // FFprobe Auto Path Button (On Click)
        // --------------------------------------------------
        private void buttonFFprobeAuto_Click(object sender, RoutedEventArgs e)
        {
            // Set the ffprobePath string
            Configure.ffprobePath = "<auto>"; //<auto>

            // Display Folder Path in Textbox
            textBoxFFprobePathConfig.Text = "<auto>";

            // Save 7-zip Path path for next launch
            Settings.Default["ffprobePath"] = "<auto>";
            Settings.Default.Save();
            Settings.Default.Reload();
        }


        // --------------------------------------------------
        // FFplay Textbox Click
        // --------------------------------------------------
        private void textBoxFFplayPathConfig_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Configure.FFplayFolderBrowser(this);
        }


        // --------------------------------------------------
        // FFplay Textbox (Text Changed)
        // --------------------------------------------------
        private void textBoxFFplayPathConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            // dont use
        }


        // --------------------------------------------------
        // FFplay Auto Path Button (On Click)
        // --------------------------------------------------
        private void buttonFFplayAuto_Click(object sender, RoutedEventArgs e)
        {
            // Set the ffplayPath string
            Configure.ffplayPath = "<auto>"; //<auto>

            // Display Folder Path in Textbox
            textBoxFFplayPathConfig.Text = "<auto>";

            // Save 7-zip Path path for next launch
            Settings.Default["ffplayPath"] = "<auto>";
            Settings.Default.Save();
            Settings.Default.Reload();
        }

        // --------------------------------------------------
        // Log Checkbox (Checked)
        // --------------------------------------------------
        private void checkBoxLogConfig_Checked(object sender, RoutedEventArgs e)
        {
            // Enable the Log
            Configure.logEnable = true;

            // -------------------------
            // Prevent Loading Corrupt App.Config
            // -------------------------
            try
            {
                // must be done this way or you get "convert object to bool error"
                if (checkBoxLogConfig.IsChecked == true)
                {
                    // Save Checkbox Settings
                    Settings.Default.checkBoxLog = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();

                    // Save Log Enable Settings
                    Settings.Default.logEnable = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                if (checkBoxLogConfig.IsChecked == false)
                {
                    // Save Checkbox Settings
                    Settings.Default.checkBoxLog = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();

                    // Save Log Enable Settings
                    Settings.Default.logEnable = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Properties.Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }

        }


        // --------------------------------------------------
        // Log Checkbox (Unchecked)
        // --------------------------------------------------
        private void checkBoxLogConfig_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable the Log
            Configure.logEnable = false;

            // -------------------------
            // Prevent Loading Corrupt App.Config
            // -------------------------
            try
            {
                // must be done this way or you get "convert object to bool error"
                if (checkBoxLogConfig.IsChecked == true)
                {
                    // Save Checkbox Settings
                    Settings.Default.checkBoxLog = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();

                    // Save Log Enable Settings
                    Settings.Default.logEnable = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                if (checkBoxLogConfig.IsChecked == false)
                {
                    // Save Checkbox Settings
                    Settings.Default.checkBoxLog = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();

                    // Save Log Enable Settings
                    Settings.Default.logEnable = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Properties.Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }


        // --------------------------------------------------
        // Log Textbox (On Click)
        // --------------------------------------------------
        private void textBoxLogConfig_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Configure.logFolderBrowser(this);
        }

        // --------------------------------------------------
        // Log Auto Path Button (On Click)
        // --------------------------------------------------
        private void buttonLogAuto_Click(object sender, RoutedEventArgs e)
        {
            // Uncheck Log Checkbox
            checkBoxLogConfig.IsChecked = false;

            // Clear Path in Textbox
            textBoxLogConfig.Text = string.Empty;

            // Set the logPath string
            Configure.logPath = string.Empty;

            // Save Log Path path for next launch
            Settings.Default["logPath"] = string.Empty;
            Settings.Default.Save();
            Settings.Default.Reload();
        }


        // --------------------------------------------------
        // Thread Select ComboBox
        // --------------------------------------------------
        private void threadSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Custom ComboBox Editable
            if ((string)cboThreads.SelectedItem == "Custom" || cboThreads.SelectedValue == null)
            {
                cboThreads.IsEditable = true;
            }

            // Other Items Disable Editable
            if ((string)cboThreads.SelectedItem != "Custom" && cboThreads.SelectedValue != null)
            {
                cboThreads.IsEditable = false;
            }

            // Maintain Editable Combobox while typing
            if (cboThreads.IsEditable == true)
            {
                cboThreads.IsEditable = true;

                // Clear Custom Text
                cboThreads.SelectedIndex = -1;
            }

            // Set the threads to pass to MainWindow
            Configure.threads = cboThreads.SelectedItem.ToString();

            // Save Thread Number for next launch
            //Settings.Default["cboThreads"] = cboThreads.SelectedItem.ToString();
            Settings.Default["threads"] = cboThreads.SelectedItem.ToString();
            Settings.Default.Save();
            Settings.Default.Reload();
        }
        // --------------------------------------------------
        // Thread Select ComboBox - Allow Only Numbers
        // --------------------------------------------------
        private void threadSelect_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow Numbers or Backspace
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) && e.Key != Key.Back)
            {
                e.Handled = true;
            }
        }

        // --------------------------------------------------
        // Theme Select ComboBox
        // --------------------------------------------------
        private void themeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Configure.theme = cboTheme.SelectedItem.ToString();

            // Change Theme Resource
            App.Current.Resources.MergedDictionaries.Clear();
            App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("Theme" + Configure.theme + ".xaml", UriKind.RelativeOrAbsolute)
            });

            // Save Theme for next launch
            Settings.Default["Theme"] = cboTheme.SelectedItem.ToString();
            Settings.Default.Save();
            Settings.Default.Reload();
        }

        // --------------------------------------------------
        // Hardware Acceleration
        // --------------------------------------------------
        //private void tglHWAccel_Checked(object sender, RoutedEventArgs e)
        //{
        //    tglHWAccel.Content = "On";
        //}
        //private void tglHWAccel_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    tglHWAccel.Content = "Off";
        //}


        // --------------------------------------------------
        // Reset Saved Settings Button
        // --------------------------------------------------
        private void buttonClearAllSavedSettings_Click(object sender, RoutedEventArgs e)
        {
            // Revert FFmpeg
            textBoxFFmpegPathConfig.Text = "<auto>";
            Configure.ffmpegPath = textBoxFFmpegPathConfig.Text;

            // Revert FFprobe
            textBoxFFprobePathConfig.Text = "<auto>";
            Configure.ffprobePath = textBoxFFprobePathConfig.Text;

            // Revert Log
            checkBoxLogConfig.IsChecked = false;
            textBoxLogConfig.Text = string.Empty;
            Configure.logPath = string.Empty;

            // Revert Threads
            cboThreads.SelectedItem = "optimal";
            Configure.threads = string.Empty;


            // Yes/No Dialog Confirmation
            //
            MessageBoxResult result = MessageBox.Show(
                                                "Reset Saved Settings?",
                                                "Settings",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Exclamation
                                                );
            switch (result)
            {
                case MessageBoxResult.Yes:

                    // Reset AppData Settings
                    Settings.Default.Reset();
                    Settings.Default.Reload();

                    // Restart Program
                    Process.Start(Application.ResourceAssembly.Location);
                    Application.Current.Shutdown();

                    break;

                case MessageBoxResult.No:

                    break;
            }
        }


        // --------------------------------------------------
        // Delete Saved Settings Button
        // --------------------------------------------------
        private void buttonDeleteSettings_Click(object sender, RoutedEventArgs e)
        {
            string userProfile = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%");
            string appDataPath = "\\AppData\\Local\\Axiom";

            // Check if Directory Exists
            if (Directory.Exists(userProfile + appDataPath))
            {
                // Show Yes No Window
                System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show(
                    "Delete " + userProfile + appDataPath, "Delete Directory Confirm", System.Windows.Forms.MessageBoxButtons.YesNo);
                // Yes
                if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                {
                    // Delete leftover 2 Pass Logs in Program's folder and Input Files folder
                    using (Process delete = new Process())
                    {
                        delete.StartInfo.UseShellExecute = false;
                        delete.StartInfo.CreateNoWindow = false;
                        delete.StartInfo.RedirectStandardOutput = true;
                        delete.StartInfo.FileName = "cmd.exe";
                        delete.StartInfo.Arguments = "/c RD /Q /S " + "\"" + userProfile + appDataPath;
                        delete.Start();
                        delete.WaitForExit();
                        //delete.Close();
                    }
                }
                // No
                else if (dialogResult == System.Windows.Forms.DialogResult.No)
                {
                    //do nothing
                }
            }
            // If Axiom Folder Not Found
            else
            {
                MessageBox.Show("No Previous Settings Found.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }


        // --------------------------------------------------------------------------------------------------------
        // Main
        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        ///     Info Button
        /// </summary>
        private Boolean IsInfoWindowOpened = false;
        private void buttonInfo_Click(object sender, RoutedEventArgs e)
        {
            // Prevent Monitor Resolution Window Crash
            //
            try
            {
                // Check if Window is already open
                if (IsInfoWindowOpened) return;

                // Start Window
                infowindow = new InfoWindow();

                // Only allow 1 Window instance
                infowindow.ContentRendered += delegate { IsInfoWindowOpened = true; };
                infowindow.Closed += delegate { IsInfoWindowOpened = false; };

                // Keep Window on Top
                infowindow.Owner = Window.GetWindow(this);

                // Detect which screen we're on
                var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                var thisScreen = allScreens.SingleOrDefault(s => this.Left >= s.WorkingArea.Left && this.Left < s.WorkingArea.Right);
                if (thisScreen == null) thisScreen = allScreens.First();

                // Position Relative to MainWindow
                infowindow.Left = Math.Max((this.Left + (this.Width - infowindow.Width) / 2), thisScreen.WorkingArea.Left);
                infowindow.Top = Math.Max((this.Top + (this.Height - infowindow.Height) / 2), thisScreen.WorkingArea.Top);

                // Open Window
                infowindow.Show();
            }
            // Simplified
            catch
            {
                // Check if Window is already open
                if (IsInfoWindowOpened) return;

                // Start Window
                infowindow = new InfoWindow();

                // Only allow 1 Window instance
                infowindow.ContentRendered += delegate { IsInfoWindowOpened = true; };
                infowindow.Closed += delegate { IsInfoWindowOpened = false; };

                // Keep Window on Top
                infowindow.Owner = Window.GetWindow(this);

                // Position Relative to MainWindow
                infowindow.Left = Math.Max((this.Left + (this.Width - infowindow.Width) / 2), this.Left);
                infowindow.Top = Math.Max((this.Top + (this.Height - infowindow.Height) / 2), this.Top);

                // Open Window
                infowindow.Show();
            }
        }


        /// <summary>
        ///     Configure Settings Window Button
        /// </summary>
        private void buttonConfigure_Click(object sender, RoutedEventArgs e)
        {
            //// Prevent Monitor Resolution Window Crash
            ////
            //try
            //{
            //    // Detect which screen we're on
            //    var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
            //    var thisScreen = allScreens.SingleOrDefault(s => this.Left >= s.WorkingArea.Left && this.Left < s.WorkingArea.Right);
            //    if (thisScreen == null) thisScreen = allScreens.First();

            //    // Open Configure Window
            //    configurewindow = new ConfigureWindow(this);

            //    // Keep Window on Top
            //    configurewindow.Owner = Window.GetWindow(this);

            //    // Position Relative to MainWindow
            //    // Keep from going off screen
            //    configurewindow.Left = Math.Max((this.Left + (this.Width - configurewindow.Width) / 2), thisScreen.WorkingArea.Left);
            //    configurewindow.Top = Math.Max(this.Top - configurewindow.Height - 12, thisScreen.WorkingArea.Top);

            //    // Open Winndow
            //    configurewindow.ShowDialog();
            //}
            //// Simplified
            //catch
            //{
            //    // Open Configure Window
            //    configurewindow = new ConfigureWindow(this);

            //    // Keep Window on Top
            //    configurewindow.Owner = Window.GetWindow(this);

            //    // Position Relative to MainWindow
            //    configurewindow.Left = Math.Max((this.Left + (this.Width - configurewindow.Width) / 2), this.Left);
            //    configurewindow.Top = Math.Max((this.Top + (this.Height - configurewindow.Height) / 2), this.Top);

            //    // Open Winndow
            //    configurewindow.ShowDialog();
            //}
        }


        /// <summary>
        ///     Log Console Window Button
        /// </summary>
        private void buttonLogConsole_Click(object sender, RoutedEventArgs e)
        {
            // Prevent Monitor Resolution Window Crash
            //
            try
            {
                // Detect which screen we're on
                var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                var thisScreen = allScreens.SingleOrDefault(s => this.Left >= s.WorkingArea.Left && this.Left < s.WorkingArea.Right);
                if (thisScreen == null) thisScreen = allScreens.First();

                // Position Relative to MainWindow
                // Keep from going off screen
                logconsole.Left = Math.Min(this.Left + this.ActualWidth + 12, thisScreen.WorkingArea.Right - logconsole.Width);
                logconsole.Top = Math.Min(this.Top + 0, thisScreen.WorkingArea.Bottom - logconsole.Height);

                // Open Winndow
                logconsole.Show();
            }
            // Simplified
            catch
            {
                // Position Relative to MainWindow
                // Keep from going off screen
                logconsole.Left = this.Left + this.ActualWidth + 12;
                logconsole.Top = this.Top;

                // Open Winndow
                logconsole.Show();
            }
        }

        /// <summary>
        ///     Debug Console Window Button
        /// </summary>
        private Boolean IsDebugConsoleOpened = false;
        private void buttonDebugConsole_Click(object sender, RoutedEventArgs e)
        {
            // Prevent Monitor Resolution Window Crash
            //
            try
            {
                // Check if Window is already open
                if (IsDebugConsoleOpened) return;

                // Start Window
                debugconsole = new DebugConsole(this);

                // Only allow 1 Window instance
                debugconsole.ContentRendered += delegate { IsDebugConsoleOpened = true; };
                debugconsole.Closed += delegate { IsDebugConsoleOpened = false; };

                // Detect which screen we're on
                var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                var thisScreen = allScreens.SingleOrDefault(s => this.Left >= s.WorkingArea.Left && this.Left < s.WorkingArea.Right);
                if (thisScreen == null) thisScreen = allScreens.First();


                // Position Relative to MainWindow
                // Keep from going off screen
                debugconsole.Left = Math.Max(this.Left - debugconsole.Width - 12, thisScreen.WorkingArea.Left);
                debugconsole.Top = Math.Max(this.Top - 0, thisScreen.WorkingArea.Top);

                // Write Variables to Debug Window (Method)
                DebugConsole.DebugWrite(debugconsole, this);

                // Open Window
                debugconsole.Show();
            }
            // Simplified
            catch
            {
                // Check if Window is already open
                if (IsDebugConsoleOpened) return;

                // Start Window
                debugconsole = new DebugConsole(this);

                // Only allow 1 Window instance
                debugconsole.ContentRendered += delegate { IsDebugConsoleOpened = true; };
                debugconsole.Closed += delegate { IsDebugConsoleOpened = false; };

                // Position Relative to MainWindow
                // Keep from going off screen
                debugconsole.Left = this.Left - debugconsole.Width - 12;
                debugconsole.Top = this.Top;

                // Write Variables to Debug Window (Method)
                DebugConsole.DebugWrite(debugconsole, this);

                // Open Window
                debugconsole.Show();
            }
        }

        /// <summary>
        ///     File Properties Button
        /// </summary>
        private Boolean IsFilePropertiesOpened = false;
        private void buttonProperties_Click(object sender, RoutedEventArgs e)
        {
            // Prevent Monitor Resolution Window Crash
            //
            try
            {
                // Check if Window is already open
                if (IsFilePropertiesOpened) return;

                // Start window
                //MainWindow mainwindow = this;
                filepropwindow = new FilePropertiesWindow(this);

                // Only allow 1 Window instance
                filepropwindow.ContentRendered += delegate { IsFilePropertiesOpened = true; };
                filepropwindow.Closed += delegate { IsFilePropertiesOpened = false; };

                // Detect which screen we're on
                var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
                var thisScreen = allScreens.SingleOrDefault(s => this.Left >= s.WorkingArea.Left && this.Left < s.WorkingArea.Right);
                if (thisScreen == null) thisScreen = allScreens.First();

                // Position Relative to MainWindow
                // Keep from going off screen
                filepropwindow.Left = Math.Max((this.Left + (this.Width - filepropwindow.Width) / 2), thisScreen.WorkingArea.Left);
                filepropwindow.Top = Math.Max((this.Top + (this.Height - filepropwindow.Height) / 2), thisScreen.WorkingArea.Top);

                // Write Properties to Textbox in FilePropertiesWindow Initialize

                // Open Window
                filepropwindow.Show();
            }
            // Simplified
            catch
            {
                // Check if Window is already open
                if (IsFilePropertiesOpened) return;

                // Start window
                filepropwindow = new FilePropertiesWindow(this);

                // Only allow 1 Window instance
                filepropwindow.ContentRendered += delegate { IsFilePropertiesOpened = true; };
                filepropwindow.Closed += delegate { IsFilePropertiesOpened = false; };

                // Position Relative to MainWindow
                // Keep from going off screen
                filepropwindow.Left = Math.Max((this.Left + (this.Width - filepropwindow.Width) / 2), this.Left);
                filepropwindow.Top = Math.Max((this.Top + (this.Height - filepropwindow.Height) / 2), this.Top);

                // Write Properties to Textbox in FilePropertiesWindow Initialize

                // Open Window
                filepropwindow.Show();
            }
        }


        /// <summary>
        ///    Website Button
        /// </summary>
        private void buttonWebsite_Click(object sender, RoutedEventArgs e)
        {
            // Open Axiom Website URL in Default Browser
            Process.Start("https://axiomui.github.io");

        }


        /// <summary>
        ///    Update Button
        /// </summary>
        private Boolean IsUpdateWindowOpened = false;
        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Proceed if Internet Connection
            // -------------------------
            if (UpdateWindow.CheckForInternetConnection() == true)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                WebClient wc = new WebClient();
                wc.Headers.Add(HttpRequestHeader.UserAgent, "Axiom (https://github.com/MattMcManis/Axiom)" + " v" + currentVersion + "-" + currentBuildPhase + " Update Check");
                //wc.Headers.Add("Accept-Encoding", "gzip,deflate"); //error

                // -------------------------
                // Parse GitHub .version file
                // -------------------------
                string parseLatestVersion = string.Empty;

                try
                {
                    parseLatestVersion = wc.DownloadString("https://raw.githubusercontent.com/MattMcManis/Axiom/master/.version");
                }
                catch
                {
                    MessageBox.Show("GitHub version file not found.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);

                    return;
                }

                // -------------------------
                // Split Version & Build Phase by dash
                // -------------------------
                if (!string.IsNullOrEmpty(parseLatestVersion)) //null check
                {
                    try
                    {
                        // Split Version and Build Phase
                        splitVersionBuildPhase = Convert.ToString(parseLatestVersion).Split('-');

                        // Set Version Number
                        latestVersion = new Version(splitVersionBuildPhase[0]); //number
                        latestBuildPhase = splitVersionBuildPhase[1]; //alpha
                    }
                    catch
                    {
                        MessageBox.Show("Error reading version.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                        return;
                    }

                    // Debug
                    //MessageBox.Show(Convert.ToString(latestVersion));
                    //MessageBox.Show(latestBuildPhase);

                    // -------------------------
                    // Check if Axiom is the Latest Version
                    // -------------------------
                    // Update Available
                    if (latestVersion > currentVersion)
                    {
                        // Yes/No Dialog Confirmation
                        //
                        MessageBoxResult result = MessageBox.Show("v" + Convert.ToString(latestVersion) + "-" + latestBuildPhase + "\n\nDownload Update?",
                                                             "Update Available",
                                                             MessageBoxButton.YesNo);
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                // Check if Window is already open
                                if (IsUpdateWindowOpened) return;

                                // Start Window
                                updatewindow = new UpdateWindow();

                                // Keep in Front
                                updatewindow.Owner = Window.GetWindow(this);

                                // Only allow 1 Window instance
                                updatewindow.ContentRendered += delegate { IsUpdateWindowOpened = true; };
                                updatewindow.Closed += delegate { IsUpdateWindowOpened = false; };

                                // Position Relative to MainWindow
                                // Keep from going off screen
                                updatewindow.Left = Math.Max((this.Left + (this.Width - updatewindow.Width) / 2), this.Left);
                                updatewindow.Top = Math.Max((this.Top + (this.Height - updatewindow.Height) / 2), this.Top);

                                // Open Window
                                updatewindow.Show();
                                break;
                            case MessageBoxResult.No:
                                break;
                        }
                    }

                    // Update Not Available
                    //
                    else if (latestVersion <= currentVersion)
                    {
                        MessageBox.Show("This version is up to date.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                        return;
                    }

                    // Unknown
                    //
                    else // null
                    {
                        MessageBox.Show("Could not find download. Try updating manually.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                        return;
                    }
                }

                // Version is Null
                //
                else
                {
                    MessageBox.Show("GitHub version file returned empty.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                    return;
                }
            }
            else
            {
                MessageBox.Show("Could not detect Internet Connection.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                return;
            }
        }

        /// <summary>
        ///    Updates Auto Check - Checked
        /// </summary>
        private void tglUpdatesAutoCheck_Checked(object sender, RoutedEventArgs e)
        {
            // Update Toggle Text
            tblkUpdatesAutoCheck.Text = "On";

            //Prevent Loading Corrupt App.Config
            try
            {
                // Save Toggle Settings
                // must be done this way or you get "convert object to bool error"
                if (tglUpdatesAutoCheck.IsChecked == true)
                {
                    Settings.Default.UpdatesAutoCheck = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                else if (tglUpdatesAutoCheck.IsChecked == false)
                {
                    Settings.Default.UpdatesAutoCheck = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }
        /// <summary>
        ///    Updates Auto Check - Unchecked
        /// </summary>
        private void tglUpdatesAutoCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            // Update Toggle Text
            tblkUpdatesAutoCheck.Text = "Off";

            // Prevent Loading Corrupt App.Config
            try
            {
                // Save Toggle Settings
                // must be done this way or you get "convert object to bool error"
                if (tglUpdatesAutoCheck.IsChecked == true)
                {
                    Settings.Default.UpdatesAutoCheck = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                else if (tglUpdatesAutoCheck.IsChecked == false)
                {
                    Settings.Default.UpdatesAutoCheck = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }

        /// <summary>
        ///    Update Available Check
        /// </summary>
        public void UpdateAvailableCheck()
        {
            //if (tglUpdatesAutoCheck.IsChecked == true)
            if (tglUpdatesAutoCheck.Dispatcher.Invoke((() => { return tglUpdatesAutoCheck.IsChecked; })) == true)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                WebClient wc = new WebClient();
                wc.Headers.Add(HttpRequestHeader.UserAgent, "Axiom (https://github.com/MattMcManis/Axiom)" + " v" + currentVersion + "-" + currentBuildPhase + " Update Check");
                //wc.Headers.Add("Accept-Encoding", "gzip,deflate"); //error

                // -------------------------
                // Parse GitHub .version file
                // -------------------------
                string parseLatestVersion = string.Empty;

                try
                {
                    parseLatestVersion = wc.DownloadString("https://raw.githubusercontent.com/MattMcManis/Axiom/master/.version");
                }
                catch
                {
                    return;
                }

                // -------------------------
                // Split Version & Build Phase by dash
                // -------------------------
                if (!string.IsNullOrEmpty(parseLatestVersion)) //null check
                {
                    try
                    {
                        // Split Version and Build Phase
                        splitVersionBuildPhase = Convert.ToString(parseLatestVersion).Split('-');

                        // Set Version Number
                        latestVersion = new Version(splitVersionBuildPhase[0]); //number
                        latestBuildPhase = splitVersionBuildPhase[1]; //alpha
                    }
                    catch
                    {
                        return;
                    }

                    // Check if Axiom is the Latest Version
                    // Update Available
                    if (latestVersion > currentVersion)
                    {
                        //updateAvailable = " ~ Update Available: " + "(" + Convert.ToString(latestVersion) + "-" + latestBuildPhase + ")";

                        Dispatcher.Invoke(new Action(delegate
                        {
                            TitleVersion = "Axiom ~ FFmpeg UI (" + Convert.ToString(currentVersion) + "-" + currentBuildPhase + ")"
                                            + " ~ Update Available: " + "(" + Convert.ToString(latestVersion) + "-" + latestBuildPhase + ")";
                        }));
                    }
                    // Update Not Available
                    else if (latestVersion <= currentVersion)
                    {
                        return;
                    }
                }
            }
        }



        /// <summary>
        ///    Log Button
        /// </summary>
        private void buttonLog_Click(object sender, RoutedEventArgs e)
        {
            // Call Method to get Log Path
            Log.DefineLogPath(this);

            //MessageBox.Show(Configure.logPath.ToString()); //debug

            // Open Log
            if (File.Exists(Configure.logPath + "output.log"))
            {
                Process.Start("notepad.exe", "\"" + Configure.logPath + "output.log" + "\"");
            }
            else
            {
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: Output Log has not been created yet.")) { Foreground = Log.ConsoleWarning });

                MessageBox.Show("Output Log has not been created yet.",
                                        "Notice",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
            }
        }


        /// <summary>
        ///     Filter - Selective SelectiveColorPreview - ComboBox
        /// </summary>
        public static List<VideoFilters.FilterVideoSelectiveColor> cboSelectiveColor_Items = new List<VideoFilters.FilterVideoSelectiveColor>()
        {
            new VideoFilters.FilterVideoSelectiveColor("Reds", Colors.Red),
            new VideoFilters.FilterVideoSelectiveColor("Yellows", Colors.Yellow),
            new VideoFilters.FilterVideoSelectiveColor("Greens", Colors.Green),
            new VideoFilters.FilterVideoSelectiveColor("Cyans", Colors.Cyan),
            new VideoFilters.FilterVideoSelectiveColor("Blues", Colors.Blue),
            new VideoFilters.FilterVideoSelectiveColor("Magentas", Colors.Magenta),
            new VideoFilters.FilterVideoSelectiveColor("Whites", Colors.White),
            new VideoFilters.FilterVideoSelectiveColor("Neutrals", Colors.Gray),
            new VideoFilters.FilterVideoSelectiveColor("Blacks", Colors.Black),
        };
        //public static List<VideoFilters.FilterVideoSelectiveColor> _cboSelectiveColor_Previews
        //{
        //    get { return _cboSelectiveColor_Previews; }
        //    set { _cboSelectiveColor_Previews = value; }
        //}

        private void cboFilterVideo_SelectiveColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Switch Tab SelectiveColorPreview
            tabControl_SelectiveColor.SelectedIndex = 0;

            var selectedItem = (VideoFilters.FilterVideoSelectiveColor)cboFilterVideo_SelectiveColor.SelectedItem;
            string color = selectedItem.SelectiveColorName;

            if (color == "Reds")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Reds.IsSelected = true;
            }
            else if (color == "Yellows")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Yellows.IsSelected = true;
            }
            else if (color == "Greens")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Greens.IsSelected = true;
            }
            else if (color == "Cyans")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Cyans.IsSelected = true;
            }
            else if (color == "Blues")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Blues.IsSelected = true;
            }
            else if (color == "Magentas")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Magentas.IsSelected = true;
            }
            else if (color == "Whites")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Whites.IsSelected = true;
            }
            else if (color == "Neutrals")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Neutrals.IsSelected = true;
            }
            else if (color == "Blacks")
            {
                tabControl_SelectiveColor.SelectedItem = selectedItem;
                tabItem_SelectiveColor_Blacks.IsSelected = true;
            }
        }

        /// <summary>
        ///     Filter Video - Selective Color Sliders
        /// </summary>
        // Reds Cyan
        private void slFiltersVideo_SelectiveColor_Reds_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Reds_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Reds_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Reds_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Reds Magenta
        private void slFiltersVideo_SelectiveColor_Reds_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Reds_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Reds_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Regs Yellow
        private void slFiltersVideo_SelectiveColor_Reds_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Reds_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Reds_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Yellows Cyan
        private void slFiltersVideo_SelectiveColor_Yellows_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Yellows_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Yellows_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        private void tbxFiltersVideo_SelectiveColor_Yellows_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Yellows Magenta
        private void slFiltersVideo_SelectiveColor_Yellows_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Yellows_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Yellows_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Yellows_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Yellows Yellow
        private void slFiltersVideo_SelectiveColor_Yellows_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Yellows_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Yellows_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Yellows_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Greens Cyan
        private void slFiltersVideo_SelectiveColor_Greens_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Greens_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Greens_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Greens_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Greens Magenta
        private void slFiltersVideo_SelectiveColor_Greens_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Greens_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Greens_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Greens_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Greens Yellow
        private void slFiltersVideo_SelectiveColor_Greens_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Greens_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Greens_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Greens_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Cyans Cyan
        private void slFiltersVideo_SelectiveColor_Cyans_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Cyans_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Cyans_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Cyans_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Cyans Magenta
        private void slFiltersVideo_SelectiveColor_Cyans_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Cyans_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Cyans_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Cyans_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Cyans Yellow
        private void slFiltersVideo_SelectiveColor_Cyans_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Cyans_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Cyans_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Cyans_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Blues Cyan
        private void slFiltersVideo_SelectiveColor_Blues_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Blues_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Blues_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Blues_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Blues Magneta
        private void slFiltersVideo_SelectiveColor_Blues_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Blues_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Blues_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Blues_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Blues Yellow
        private void slFiltersVideo_SelectiveColor_Blues_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Blues_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Blues_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Blues_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Magentas Cyan
        private void slFiltersVideo_SelectiveColor_Magentas_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Magentas_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Magentas_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Magentas_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Magentas Magenta
        private void slFiltersVideo_SelectiveColor_Magentas_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Magentas_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Magentas_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        private void tbxFiltersVideo_SelectiveColor_Magentas_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Magentas Yellow
        private void slFiltersVideo_SelectiveColor_Magentas_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Magentas_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Magentas_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Magentas_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Whites Cyan
        private void slFiltersVideo_SelectiveColor_Whites_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Whites_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Whites_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Whites_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Whites Magenta
        private void slFiltersVideo_SelectiveColor_Whites_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Whites_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Whites_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Whites_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Whites Yellow
        private void slFiltersVideo_SelectiveColor_Whites_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Whites_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Whites_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Whites_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Neutrals Cyan
        private void slFiltersVideo_SelectiveColor_Neutrals_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Neutrals_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Neutrals_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Neutrals_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Neutrals Magenta
        private void slFiltersVideo_SelectiveColor_Neutrals_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Neutrals_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Neutrals_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Neutrals_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Neutrals Yellow
        private void slFiltersVideo_SelectiveColor_Neutrals_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Neutrals_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Neutrals_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Neutrals_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Blacks Cyan
        private void slFiltersVideo_SelectiveColor_Blacks_Cyan_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Blacks_Cyan.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Blacks_Cyan_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Blacks_Cyan_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Blacks Magenta
        private void slFiltersVideo_SelectiveColor_Blacks_Magenta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Blacks_Magenta.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Blacks_Magenta_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Blacks_Magenta_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        // Blacks Yellow
        private void slFiltersVideo_SelectiveColor_Blacks_Yellow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_SelectiveColor_Blacks_Yellow.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_SelectiveColor_Blacks_Yellow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_SelectiveColor_Blacks_Yellow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }


        /// <summary>
        ///     Filter Video - Selective Color Reset
        /// </summary>
        private void btnFilterVideo_SelectiveColorReset_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default

            // Reds Cyan
            slFiltersVideo_SelectiveColor_Reds_Cyan.Value = 0;
            // Reds Magenta
            slFiltersVideo_SelectiveColor_Reds_Magenta.Value = 0;
            // Regs Yellow
            slFiltersVideo_SelectiveColor_Reds_Yellow.Value = 0;

            // Yellows Cyan
            slFiltersVideo_SelectiveColor_Yellows_Cyan.Value = 0;
            // Yellows Magenta
            slFiltersVideo_SelectiveColor_Yellows_Magenta.Value = 0;
            // Yellows Yellow
            slFiltersVideo_SelectiveColor_Yellows_Yellow.Value = 0;

            // Greens Cyan
            slFiltersVideo_SelectiveColor_Greens_Cyan.Value = 0;
            // Greens Magenta
            slFiltersVideo_SelectiveColor_Greens_Magenta.Value = 0;
            // Greens Yellow
            slFiltersVideo_SelectiveColor_Greens_Yellow.Value = 0;

            // Cyans Cyan
            slFiltersVideo_SelectiveColor_Cyans_Cyan.Value = 0;
            // Cyans Magenta
            slFiltersVideo_SelectiveColor_Cyans_Magenta.Value = 0;
            // Cyans Yellow
            slFiltersVideo_SelectiveColor_Cyans_Yellow.Value = 0;

            // Blues Cyan
            slFiltersVideo_SelectiveColor_Blues_Cyan.Value = 0;
            // Blues Magneta
            slFiltersVideo_SelectiveColor_Blues_Magenta.Value = 0;
            // Blues Yellow
            slFiltersVideo_SelectiveColor_Blues_Yellow.Value = 0;

            // Magentas Cyan
            slFiltersVideo_SelectiveColor_Magentas_Cyan.Value = 0;
            // Magentas Magenta
            slFiltersVideo_SelectiveColor_Magentas_Magenta.Value = 0;
            // Magentas Yellow
            slFiltersVideo_SelectiveColor_Magentas_Yellow.Value = 0;

            // Whites Cyan
            slFiltersVideo_SelectiveColor_Whites_Cyan.Value = 0;
            // Whites Magenta
            slFiltersVideo_SelectiveColor_Whites_Magenta.Value = 0;
            // Whites Yellow
            slFiltersVideo_SelectiveColor_Whites_Yellow.Value = 0;

            // Neutrals Cyan
            slFiltersVideo_SelectiveColor_Neutrals_Cyan.Value = 0;
            // Neutrals Magenta
            slFiltersVideo_SelectiveColor_Neutrals_Magenta.Value = 0;
            // Neutrals Yellow
            slFiltersVideo_SelectiveColor_Neutrals_Yellow.Value = 0;

            // Blacks Cyan
            slFiltersVideo_SelectiveColor_Blacks_Cyan.Value = 0;
            // Blacks Magenta
            slFiltersVideo_SelectiveColor_Blacks_Magenta.Value = 0;
            // Blacks Yellow
            slFiltersVideo_SelectiveColor_Blacks_Yellow.Value = 0;


            VideoControls.AutoCopyVideoCodec(this);
        }


        /// <summary>
        ///     Filter Video - EQ Sliders
        /// </summary>
        // Brightness
        private void slFiltersVideo_EQ_Brightness_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_EQ_Brightness.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_EQ_Brightness_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_EQ_Brightness_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // Reset Empty to 0
            if (string.IsNullOrWhiteSpace(tbxFiltersVideo_EQ_Brightness.Text))
            {
                tbxFiltersVideo_EQ_Brightness.Text = "0";
            }

            VideoControls.AutoCopyVideoCodec(this);
        }

        // Contrast
        private void slFiltersVideo_EQ_Contrast_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_EQ_Contrast.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_EQ_Contrast_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_EQ_Contrast_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Saturation
        private void slFiltersVideo_EQ_Saturation_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_EQ_Saturation.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_EQ_Saturation_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_EQ_Saturation_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Gamma
        private void slFiltersVideo_EQ_Gamma_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFiltersVideo_EQ_Gamma.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }
        private void slFiltersVideo_EQ_Gamma_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }
        private void tbxFiltersVideo_EQ_Gamma_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        // Reset
        private void btnFilterVideo_EQ_Reset_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default

            // Brightness
            slFiltersVideo_EQ_Brightness.Value = 0;
            // Contrast
            slFiltersVideo_EQ_Contrast.Value = 0;
            // Saturation
            slFiltersVideo_EQ_Saturation.Value = 0;
            // Gamma
            slFiltersVideo_EQ_Gamma.Value = 0;

            VideoControls.AutoCopyVideoCodec(this);
        }



        /// <summary>
        ///     Filter Video - Deband
        /// </summary>
        private void cboFilterVideo_Deband_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        /// <summary>
        ///     Filter Video - Deshake
        /// </summary>
        private void cboFilterVideo_Deshake_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        /// <summary>
        ///     Filter Video - Deflicker
        /// </summary>
        private void cboFilterVideo_Deflicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        /// <summary>
        ///     Filter Video - Dejudder
        /// </summary>
        private void cboFilterVideo_Dejudder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        /// <summary>
        ///     Filter Video - Denoise
        /// </summary>
        private void cboFilterVideo_Denoise_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }



        /// <summary>
        ///     Scaling Video
        /// </summary>
        private void cboScaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }

        /// <summary>
        ///     Pixel Format
        /// </summary>
        private void cboPixelFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.AutoCopyVideoCodec(this);
        }



        /// <summary>
        ///     Audio Limiter
        /// </summary>
        private void slAudioLimiter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slAudioLimiter.Value = 1;

            AudioControls.AutoCopyAudioCodec(this);
        }

        private void slAudioLimiter_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }

        private void tbxAudioLimiter_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }

        /// <summary>
        ///     Filter Audio - Remove Click
        /// </summary>
        //private void slFilterAudio_RemoveClick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    // Reset to default
        //    slFilterAudio_RemoveClick.Value = 0;

        //    AudioControls.AutoCopyAudioCodec(this);
        //}

        //private void slFilterAudio_RemoveClick_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    AudioControls.AutoCopyAudioCodec(this);
        //}

        //private void tbxFilterAudio_RemoveClick_PreviewKeyUp(object sender, KeyEventArgs e)
        //{
        //    AudioControls.AutoCopyAudioCodec(this);
        //}


        /// <summary>
        ///     Filter Audio - Contrast
        /// </summary>
        private void slFilterAudio_Contrast_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFilterAudio_Contrast.Value = 0;

            AudioControls.AutoCopyAudioCodec(this);
        }

        private void slFilterAudio_Contrast_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }
        private void tbxFilterAudio_Contrast_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }

        /// <summary>
        ///     Filter Audio - Extra Stereo
        /// </summary>
        private void slFilterAudio_ExtraStereo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFilterAudio_ExtraStereo.Value = 0;

            AudioControls.AutoCopyAudioCodec(this);
        }

        private void slFilterAudio_ExtraStereo_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }
        private void tbxFilterAudio_ExtraStereo_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }

        /// <summary>
        ///     Filter Audio - Tempo
        /// </summary>
        private void slFilterAudio_Tempo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Reset to default
            slFilterAudio_Tempo.Value = 100;

            AudioControls.AutoCopyAudioCodec(this);
        }

        private void slFilterAudio_Tempo_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }

        private void tbxFilterAudio_Tempo_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            AudioControls.AutoCopyAudioCodec(this);
        }


        /// <summary>
        ///    Script View Copy/Paste
        /// </summary>
        private void OnScriptPaste(object sender, DataObjectPastingEventArgs e)
        {
            // Copy Pasted Script
            //string script = ScriptView.GetScriptRichTextBoxContents(this);

            //// Select All Text
            //TextRange textRange = new TextRange(
            //    rtbScriptView.Document.ContentStart,
            //    rtbScriptView.Document.ContentEnd
            //);

            //// Remove Formatting
            //textRange.ClearAllProperties();

            // Clear Text
            //ScriptView.ClearScriptView(this);
            //ScriptView.scriptParagraph.Inlines.Clear();

            // Remove Double Paragraph Spaces
            //rtbScriptView.Document = new FlowDocument(ScriptView.scriptParagraph);

            //rtbScriptView.BeginChange();
            //ScriptView.scriptParagraph.Inlines.Add(new Run(script.Replace("\n","")));
            //rtbScriptView.EndChange();
        }

        private void OnScriptCopy(object sender, DataObjectCopyingEventArgs e)
        {
            
        }


        /// <summary>
        ///    Script Button
        /// </summary>
        private void btnScript_Click(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Clear Variables before Run
            // -------------------------
            ClearVariables(this);


            // Log Console Message /////////
            Log.WriteAction = () =>
            {
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run("...............................................")) { Foreground = Log.ConsoleAction });
            };
            Log.LogActions.Add(Log.WriteAction);

            // Log Console Message /////////
            DateTime localDate = DateTime.Now;

            // Log Console Message /////////
            Log.WriteAction = () =>
            {
                
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run(Convert.ToString(localDate))) { Foreground = Log.ConsoleAction });
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run("Generating Script...")) { Foreground = Log.ConsoleTitle });
                
            };
            Log.LogActions.Add(Log.WriteAction);


            // -------------------------
            // Enable Script
            // -------------------------
            script = true;

            // -------------------------
            // Reset Sort
            // -------------------------
            ScriptView.sort = false;
            txblScriptSort.Text = "Sort";

            // -------------------------
            // Batch Extention Period Check
            // -------------------------
            BatchExtCheck(this);

            // -------------------------
            // Set FFprobe Path
            // -------------------------
            FFprobePath();

            // -------------------------
            // Ready Halts
            // -------------------------
            ReadyHalts(this);

            // -------------------------
            // Single
            // -------------------------
            if (tglBatch.IsChecked == false)
            {
                // -------------------------
                // FFprobe Detect Metadata
                // -------------------------
                FFprobe.Metadata(this);

                // -------------------------
                // FFmpeg Generate Arguments (Single)
                // -------------------------
                //disabled if batch
                FFmpeg.FFmpegSingleGenerateArgs(this);
            }

            // -------------------------
            // Batch
            // -------------------------
            else if (tglBatch.IsChecked == true)
            {
                // -------------------------
                // FFprobe Video Entry Type Containers
                // -------------------------
                FFprobe.VideoEntryType(this);

                // -------------------------
                // FFprobe Video Entry Type Containers
                // -------------------------
                FFprobe.AudioEntryType(this);

                // -------------------------
                // FFmpeg Generate Arguments (Batch)
                // -------------------------
                //disabled if single file
                FFmpeg.FFmpegBatchGenerateArgs(this);
            }

            // -------------------------
            // Write All Log Actions to Console
            // -------------------------
            Log.LogWriteAll(this);

            // -------------------------
            // Generate Script
            // -------------------------
            FFmpeg.FFmpegScript(this);

            // -------------------------
            // Re-Sort
            // Reset Sort
            // -------------------------
            //if (ScriptView.sort == true)
            //{
            //    MessageBox.Show("here");

            //    // Clear Old Text
            //    //ClearScriptView();
            //    ScriptView.scriptParagraph.Inlines.Clear();

            //    // Write FFmpeg Args Sort
            //    rtbScriptView.Document = new FlowDocument(ScriptView.scriptParagraph);
            //    rtbScriptView.BeginChange();
            //    ScriptView.scriptParagraph.Inlines.Add(new Run(FFmpeg.ffmpegArgsSort));
            //    rtbScriptView.EndChange();

            //    // Change Button Back to Inline
            //    txblScriptSort.Text = "Inline";

            //}
            //else if (ScriptView.sort == false)
            //{
            //    txblScriptSort.Text = "Sort";
            //}

            // -------------------------
            // Auto Sort Toggle
            // -------------------------
            if (tglAutoSortScript.IsChecked == true)
            {
                Sort();
            }

            // -------------------------
            // Clear Variables for next Run
            // -------------------------
            ClearVariables(this);
            GC.Collect();
        }

        /// <summary>
        /// Run Button
        /// </summary>
        private void btnScriptRun_Click(object sender, RoutedEventArgs e)
        {
            // Use Arguments from Script TextBox
            //FFmpeg.ffmpegArgs = ScriptView.GetScriptRichTextBoxContents(this)
            //    .Replace(Environment.NewLine, "") //Remove Linebreaks
            //    .Replace("\n", "")
            //    .Replace("\r\n", "")
            //    .Replace("\u2028", "")
            //    .Replace("\u000A", "")
            //    .Replace("\u000B", "")
            //    .Replace("\u000C", "")
            //    .Replace("\u000D", "")
            //    .Replace("\u0085", "")
            //    .Replace("\u2028", "")
            //    .Replace("\u2029", "")
            //    ;

            //// Run FFmpeg
            //FFmpeg.FFmpegConvert(this);

            // -------------------------
            // Use Arguments from Script TextBox
            // -------------------------
            FFmpeg.ffmpegArgs = ReplaceLineBreaksWithSpaces(
                                        ScriptView.GetScriptRichTextBoxContents(this)
                                    );

            // -------------------------
            // Start FFmpeg
            // -------------------------
            FFmpeg.FFmpegStart(this);
        }


        /// <summary>
        ///    CMD Button
        /// </summary>
        private void buttonCmd_Click(object sender, RoutedEventArgs e)
        {
            // launch command prompt
            Process.Start("CMD.exe", "/k cd %userprofile%");

        }


        /// <summary>
        ///    Keep Window Toggle Checked
        /// </summary>
        private void tglWindowKeep_Checked(object sender, RoutedEventArgs e)
        {
            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Keep FFmpeg Window Toggle: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run("On") { Foreground = Log.ConsoleDefault });

            //Prevent Loading Corrupt App.Config
            try
            {
                // Save Toggle Settings
                // must be done this way or you get "convert object to bool error"
                if (tglWindowKeep.IsChecked == true)
                {
                    Settings.Default.KeepWindow = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                else if (tglWindowKeep.IsChecked == false)
                {
                    Settings.Default.KeepWindow = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }
        /// <summary>
        ///    Keep Window Toggle Unchecked
        /// </summary>
        private void tglWindowKeep_Unchecked(object sender, RoutedEventArgs e)
        {
            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Keep FFmpeg Window Toggle: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run("Off") { Foreground = Log.ConsoleDefault });

            // Prevent Loading Corrupt App.Config
            try
            {
                // Save Toggle Settings
                // must be done this way or you get "convert object to bool error"
                if (tglWindowKeep.IsChecked == true)
                {
                    Settings.Default.KeepWindow = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                else if (tglWindowKeep.IsChecked == false)
                {
                    Settings.Default.KeepWindow = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }

        /// <summary>
        ///    Auto Sort Script Toggle - Checked
        /// </summary>
        private void tglAutoSortScript_Checked(object sender, RoutedEventArgs e)
        {
            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Auto Sort Script Toggle: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run("On") { Foreground = Log.ConsoleDefault });

            //Prevent Loading Corrupt App.Config
            try
            {
                // Save Toggle Settings
                // must be done this way or you get "convert object to bool error"
                if (tglAutoSortScript.IsChecked == true)
                {
                    Settings.Default.AutoSortScript = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                else if (tglAutoSortScript.IsChecked == false)
                {
                    Settings.Default.AutoSortScript = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }
        /// <summary>
        ///    Auto Sort Script Toggle - Unchecked
        /// </summary>
        private void tglAutoSortScript_Unchecked(object sender, RoutedEventArgs e)
        {
            // Log Console Message /////////
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new LineBreak());
            Log.logParagraph.Inlines.Add(new Bold(new Run("Auto Sort Script Toggle: ")) { Foreground = Log.ConsoleDefault });
            Log.logParagraph.Inlines.Add(new Run("Off") { Foreground = Log.ConsoleDefault });

            // Prevent Loading Corrupt App.Config
            try
            {
                // Save Toggle Settings
                // must be done this way or you get "convert object to bool error"
                if (tglAutoSortScript.IsChecked == true)
                {
                    Settings.Default.AutoSortScript = true;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
                else if (tglAutoSortScript.IsChecked == false)
                {
                    Settings.Default.AutoSortScript = false;
                    Settings.Default.Save();
                    Settings.Default.Reload();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                // Delete Old App.Config
                string filename = ex.Filename;

                if (File.Exists(filename) == true)
                {
                    File.Delete(filename);
                    Settings.Default.Upgrade();
                    // Properties.Settings.Default.Reload();
                }
                else
                {

                }
            }
        }


        /// <summary>
        ///    Pass ComboBox
        /// </summary>
        private void cboPass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // Pass Controls
            // -------------------------
            VideoControls.EncodingPass(this);

            // -------------------------
            // Display Bit-rate in TextBox
            // -------------------------
            VideoBitrateDisplay();
        }
        private void cboPass_DropDownClosed(object sender, EventArgs e)
        {
            // User willingly selected a Pass
            VideoControls.passUserSelected = true;
        }


        /// <summary>
        ///    Play File Button
        /// </summary>
        private void buttonPlayFile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(@output))
            {
                Process.Start("\"" + output + "\"");
            }
            else
            {
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new LineBreak());
                Log.logParagraph.Inlines.Add(new Bold(new Run("Notice: File does not yet exist.")) { Foreground = Log.ConsoleWarning });

                MessageBox.Show("File does not yet exist.",
                                "Notice",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }


        /// <summary>
        ///    Input Button
        /// </summary>
        private void btnInput_Click(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Single File
            // -------------------------
            if (tglBatch.IsChecked == false)
            {
                // Open Select File Window
                Microsoft.Win32.OpenFileDialog selectFile = new Microsoft.Win32.OpenFileDialog();

                // Remember Last Dir
                //
                try
                {
                    string previousPath = Settings.Default.inputDir.ToString();

                    // Use Previous Path if Not Null
                    if (!string.IsNullOrEmpty(previousPath))
                    {
                        selectFile.InitialDirectory = previousPath;
                    }
                }
                catch
                {

                }

                // Show Dialog Box
                Nullable<bool> result = selectFile.ShowDialog();

                // Process Dialog Box
                if (result == true)
                {
                    // Display path and file in Output Textbox
                    tbxInput.Text = selectFile.FileName;

                    // Set Input Dir, Name, Ext
                    inputDir = Path.GetDirectoryName(tbxInput.Text).TrimEnd('\\') + @"\";

                    inputFileName = Path.GetFileNameWithoutExtension(tbxInput.Text);

                    inputExt = Path.GetExtension(tbxInput.Text);

                    // Save Previous Path
                    Settings.Default.inputDir = inputDir;
                    Settings.Default.Save();

                }

                // -------------------------
                // Prevent Losing Codec Copy after cancel closing Browse Folder Dialog Box 
                // Set Video & Audio Codec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
                // -------------------------
                VideoControls.AutoCopyVideoCodec(this);
                VideoControls.AutoCopySubtitleCodec(this);
                AudioControls.AutoCopyAudioCodec(this);
            }
            // -------------------------
            // Batch
            // -------------------------
            else if (tglBatch.IsChecked == true)
            {
                // Open Batch Folder
                System.Windows.Forms.FolderBrowserDialog inputFolder = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = inputFolder.ShowDialog();
                

                // Show Input Dialog Box
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // Display Folder Path in Textbox
                    tbxInput.Text = inputFolder.SelectedPath.TrimEnd('\\') + @"\";

                    // Input Directory
                    inputDir = Path.GetDirectoryName(tbxInput.Text.TrimEnd('\\') + @"\");
                }

                // -------------------------
                // Prevent Losing Codec Copy after cancel closing Browse Folder Dialog Box 
                // Set Video & Audio Codec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
                // -------------------------
                VideoControls.AutoCopyVideoCodec(this);
                VideoControls.AutoCopySubtitleCodec(this);
                AudioControls.AutoCopyAudioCodec(this);
            }
        }


        /// <summary>
        ///    Input Textbox
        /// </summary>
        private void tbxInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if (!tbxInput.Text.Contains("www.youtube.com")
            //    && !tbxInput.Text.Contains("youtube.com"))
            //{
            if (!string.IsNullOrEmpty(tbxInput.Text))
            {
                // Remove stray slash if closed out early (duplicate code?)
                if (tbxInput.Text == "\\")
                {
                    tbxInput.Text = string.Empty;
                }

                // Get input file extension
                inputExt = Path.GetExtension(tbxInput.Text);


                // Enable / Disable "Open Input Location" Buttion
                if (!string.IsNullOrWhiteSpace(tbxInput.Text))
                {
                    bool exists = Directory.Exists(Path.GetDirectoryName(tbxInput.Text));

                    if (exists)
                    {
                        openLocationInput.IsEnabled = true;
                    }
                    else
                    {
                        openLocationInput.IsEnabled = false;
                    }
                }

                // Set Video & Audio Codec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
                VideoControls.AutoCopyVideoCodec(this);
                VideoControls.AutoCopySubtitleCodec(this);
                AudioControls.AutoCopyAudioCodec(this);
            }             
            //}
        }

        /// <summary>
        ///    Input Textbox - Drag and Drop
        /// </summary>
        private void tbxInput_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = DragDropEffects.Copy;
        }

        private void tbxInput_PreviewDrop(object sender, DragEventArgs e)
        {
            var buffer = e.Data.GetData(DataFormats.FileDrop, false) as string[];
            tbxInput.Text = buffer.First();

            // Set Input Dir, Name, Ext
            inputDir = Path.GetDirectoryName(tbxInput.Text).TrimEnd('\\') + @"\";
            inputFileName = Path.GetFileNameWithoutExtension(tbxInput.Text);
            inputExt = Path.GetExtension(tbxInput.Text);

            // Set Video & Audio Codec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
            VideoControls.AutoCopyVideoCodec(this);
            VideoControls.AutoCopySubtitleCodec(this);
            AudioControls.AutoCopyAudioCodec(this);
        }

        /// <summary>
        ///    Output Textbox - Drag and Drop
        /// </summary>
        private void tbxOutput_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = DragDropEffects.Copy;
        }

        private void tbxOutput_PreviewDrop(object sender, DragEventArgs e)
        {
            var buffer = e.Data.GetData(DataFormats.FileDrop, false) as string[];
            tbxOutput.Text = buffer.First();
        }


        /// <summary>
        ///    Open Input Folder Button
        /// </summary>
        private void openLocationInput_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(inputDir))
            {
                Process.Start("explorer.exe", @inputDir);
            }
        }


        /// <summary>
        ///    Output Button
        /// </summary>
        private void btnOutput_Click(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Single File
            // -------------------------
            if (tglBatch.IsChecked == false)
            {
                // Get Output Ext
                FormatControls.OutputFormatExt(this);


                // Open 'Save File'
                Microsoft.Win32.SaveFileDialog saveFile = new Microsoft.Win32.SaveFileDialog();


                // 'Save File' Default Path same as Input Directory
                //
                try
                {
                    string previousPath = Settings.Default.outputDir.ToString();
                    // Use Input Path if Previous Path is Null
                    if (string.IsNullOrEmpty(previousPath))
                    {
                        saveFile.InitialDirectory = inputDir;
                    }
                }
                catch
                {

                }
                                
                // Remember Last Dir
                //saveFile.RestoreDirectory = true;
                // Default Extension
                saveFile.DefaultExt = outputExt;

                // Default file name if empty
                if (string.IsNullOrEmpty(inputFileName))
                {
                    saveFile.FileName = "File";
                }
                // If file name exists
                else
                {
                    // Output Path
                    outputDir = inputDir;

                    // File Renamer
                    // Get new output file name (1) if already exists
                    outputFileName = FileRenamer(inputFileName);

                    // Same as input file name
                    saveFile.FileName = outputFileName;
                }


                // Show Dialog Box
                Nullable<bool> result = saveFile.ShowDialog();

                // Process Dialog Box
                if (result == true)
                {
                    // Display path and file in Output Textbox
                    tbxOutput.Text = saveFile.FileName;

                    // Output Path
                    outputDir = Path.GetDirectoryName(tbxOutput.Text).TrimEnd('\\') + @"\";

                    // Output Filename (without extension)
                    outputFileName = Path.GetFileNameWithoutExtension(tbxOutput.Text);

                    // Add slash to inputDir path if missing
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        if (!outputDir.EndsWith("\\"))
                        {
                            outputDir = outputDir.TrimEnd('\\') + @"\";
                        }
                    }

                    // Save Previous Path
                    Settings.Default.outputDir = outputDir;
                    Settings.Default.Save();
                }
            }

            // -------------------------
            // Batch
            // -------------------------
            else if (tglBatch.IsChecked == true)
            {
                // Open 'Select Folder'
                System.Windows.Forms.FolderBrowserDialog outputFolder = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = outputFolder.ShowDialog();


                // Process Dialog Box
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // Display path and file in Output Textbox
                    tbxOutput.Text = outputFolder.SelectedPath.TrimEnd('\\') + @"\";

                    // Remove Double Slash in Root Dir, such as C:\
                    tbxOutput.Text = tbxOutput.Text.Replace(@"\\", @"\");

                    // Output Path
                    outputDir = Path.GetDirectoryName(tbxOutput.Text.TrimEnd('\\') + @"\");

                    // Add slash to inputDir path if missing
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        if (!outputDir.EndsWith("\\"))
                        {
                            outputDir = outputDir.TrimEnd('\\') + @"\";
                        }   
                    }
                }
            }

        }


        /// <summary>
        ///    Output Textbox
        /// </summary>
        private void tbxOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Remove stray slash if closed out early
            if (tbxOutput.Text == "\\")
            {
                tbxOutput.Text = string.Empty;
            }

            // Enable / Disable "Open Output Location" Buttion
            if (!string.IsNullOrWhiteSpace(tbxOutput.Text))
            {
                bool exists = Directory.Exists(Path.GetDirectoryName(tbxOutput.Text));

                if (exists)
                {
                    openLocationOutput.IsEnabled = true;
                }
                else
                {
                    openLocationOutput.IsEnabled = false;
                }
            }
        }


        /// <summary>
        ///    Open Output Folder Button
        /// </summary>
        private void openLocationOutput_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(outputDir))
            {
                Process.Start("explorer.exe", @outputDir);
            }
        }


        /// <summary>
        ///     Video Bitrate Custom Number Textbox
        /// </summary>
        // Got Focus
        private void vBitrateCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            //// Clear Textbox on first use
            //if (vBitrateCustom.Text == string.Empty)
            //{
            //    TextBox tbvb = (TextBox)sender;
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus += vBitrateCustom_GotFocus; //used to be -=
            //}
        }
        // Lost Focus
        private void vBitrateCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            //// Change Textbox back to Bitrate
            //TextBox tbvb = sender as TextBox;
            //if (tbvb.Text.Trim().Equals(string.Empty))
            //{
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus -= vBitrateCustom_GotFocus; //used to be +=

            //    //vBitrateCustom.Foreground = TextBoxDarkBlue;
            //}
        }


        /// <summary>
        ///     Video Minrate Custom Number Textbox
        /// </summary>
        // Got Focus
        private void vMinrateCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            //// Clear Textbox on first use
            //if (vMinrateCustom.Text == string.Empty)
            //{
            //    TextBox tbvb = (TextBox)sender;
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus += vMinrateCustom_GotFocus; //used to be -=
            //}
        }
        // Lost Focus
        private void vMinrateCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            //// Change Textbox back to Bitrate
            //TextBox tbvb = sender as TextBox;
            //if (tbvb.Text.Trim().Equals(string.Empty))
            //{
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus -= vMinrateCustom_GotFocus; //used to be +=
            //}
        }


        /// <summary>
        ///     Video Maxrate Custom Number Textbox
        /// </summary>
        // Got Focus
        private void vMaxrateCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            //// Clear Textbox on first use
            //if (vMinrateCustom.Text == string.Empty)
            //{
            //    TextBox tbvb = (TextBox)sender;
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus += vMaxrateCustom_GotFocus; //used to be -=
            //}
        }
        // Lost Focus
        private void vMaxrateCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            //// Change Textbox back to Bitrate
            //TextBox tbvb = sender as TextBox;
            //if (tbvb.Text.Trim().Equals(string.Empty))
            //{
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus -= vMaxrateCustom_GotFocus; //used to be +=
            //}
        }


        /// <summary>
        ///     Video Bufsize Custom Number Textbox
        /// </summary>
        // Got Focus
        private void vBufsizeCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            //// Clear Textbox on first use
            //if (vBufsizeCustom.Text == string.Empty)
            //{
            //    TextBox tbvb = (TextBox)sender;
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus += vBufsizeCustom_GotFocus; //used to be -=
            //}
        }
        // Lost Focus
        private void vBufsizeCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            //// Change Textbox back to Bitrate
            //TextBox tbvb = sender as TextBox;
            //if (tbvb.Text.Trim().Equals(string.Empty))
            //{
            //    tbvb.Text = string.Empty;
            //    tbvb.GotFocus -= vBufsizeCustom_GotFocus; //used to be +=
            //}
        }


        /// <summary>
        ///     Video CRF Custom Number Textbox
        /// </summary>
        private void crfCustom_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow Numbers or Backspace
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) && e.Key != Key.Back)
            {
                e.Handled = true;
            }
        }
        // Got Focus
        private void crfCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear Textbox on first use
            if (crfCustom.Text == string.Empty)
            {
                TextBox tbcrf = (TextBox)sender;
                tbcrf.Text = string.Empty;
                tbcrf.GotFocus += crfCustom_GotFocus; //used to be -=
            }
        }
        // Lost Focus
        private void crfCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            // Change Textbox back to CRF
            TextBox tbcrf = sender as TextBox;
            if (tbcrf.Text.Trim().Equals(string.Empty))
            {
                tbcrf.Text = string.Empty;
                tbcrf.GotFocus -= crfCustom_GotFocus; //used to be +=
            }
        }


        /// <summary>
        /// Video VBR Toggle - Checked
        /// </summary>
        private void tglVideoVBR_Checked(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // MPEG-4 VBR can only use 1 Pass
            // -------------------------
            if ((string)cboVideoCodec.SelectedItem == "MPEG-2"
                || (string)cboVideoCodec.SelectedItem == "MPEG-4")
            {
                // Change ItemSource
                VideoControls.Pass_ItemSource = new List<string>()
                {
                    "1 Pass",
                };

                // Populate ComboBox from ItemSource
                cboPass.ItemsSource = VideoControls.Pass_ItemSource;

                // Select Item
                cboPass.SelectedItem = "1 Pass";
            }


            // -------------------------
            // Display Bit-rate in TextBox
            // -------------------------
            VideoBitrateDisplay();
        }

        /// <summary>
        /// Video VBR Toggle - Unchecked
        /// </summary>
        private void tglVideoVBR_Unchecked(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // MPEG-2 / MPEG-4 CBR Reset
            // -------------------------
            if ((string)cboVideoCodec.SelectedItem == "MPEG-2"
                || (string)cboVideoCodec.SelectedItem == "MPEG-4")
            {
                // Change ItemSource
                VideoControls.Pass_ItemSource = new List<string>()
                {
                    "2 Pass",
                    "1 Pass",
                };

                // Populate ComboBox from ItemSource
                cboPass.ItemsSource = VideoControls.Pass_ItemSource;

                // Select Item
                cboPass.SelectedItem = "2 Pass";
            }

            // -------------------------
            // Display Bit-rate in TextBox
            // -------------------------
            VideoBitrateDisplay();
        }


        /// <summary>
        ///     FPS ComboBox
        /// </summary>
        private void cboFPS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Custom ComboBox Editable
            if ((string)cboFPS.SelectedItem == "Custom" || string.IsNullOrEmpty((string)cboFPS.SelectedItem))
            {
                cboFPS.IsEditable = true;
            }

            // Other Items Disable Editable
            if ((string)cboFPS.SelectedItem != "Custom" && !string.IsNullOrEmpty((string)cboFPS.SelectedItem))
            {
                cboFPS.IsEditable = false;
            }

            // Maintain Editable Combobox while typing
            if (cboFPS.IsEditable == true)
            {
                cboFPS.IsEditable = true;

                // Clear Custom Text
                cboFPS.SelectedIndex = -1;
            }

            // Disable Copy on change
            VideoControls.AutoCopyVideoCodec(this);
            VideoControls.AutoCopySubtitleCodec(this);

        }


        /// <summary>
        ///     Audio Custom Bitrate kbps Textbox
        /// </summary>
        private void audioCustom_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow Numbers or Backspace
            if (!(e.Key >= Key.D0 && e.Key <= Key.D9) && e.Key != Key.Back)
            {
                e.Handled = true;
            }
        }
        // Got Focus
        private void audioCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear Textbox on first use
            if (audioCustom.Text == string.Empty)
            {
                TextBox tbac = (TextBox)sender;
                tbac.Text = string.Empty;
                tbac.GotFocus += audioCustom_GotFocus; //used to be -=
            }
        }
        // Lost Focus
        private void audioCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            // Change Textbox back to kbps
            TextBox tbac = sender as TextBox;
            if (tbac.Text.Trim().Equals(string.Empty))
            {
                tbac.Text = string.Empty;
                tbac.GotFocus -= audioCustom_GotFocus; //used to be +=
            }
        }


        /// <summary>
        ///     Samplerate ComboBox
        /// </summary>
        private void cboSamplerate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Switch to Copy if inputExt & outputExt match
            AudioControls.AutoCopyAudioCodec(this);
        }


        /// <summary>
        ///     Bit Depth ComboBox
        /// </summary>
        private void cboBitDepth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Switch to Copy if inputExt & outputExt match
            AudioControls.AutoCopyAudioCodec(this);
        }


        /// <summary>
        ///    Volume TextBox Changed
        /// </summary>
        private void volumeUpDown_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Disable Volume instead of running AutoCopyAudioCodec each time 
            // This needs to be re-thought, calling method on every timer tick
            AudioControls.AutoCopyAudioCodec(this);
        }
        /// <summary>
        ///    Volume TextBox KeyDown
        /// </summary>
        private void volumeUpDown_KeyDown(object sender, KeyEventArgs e)
        {
            try //error if other letters or symbols get in
            {
                // Only allow Numbers or Backspace
                if (!(e.Key >= Key.D0 && e.Key <= Key.D9) && e.Key != Key.Back)
                {
                    e.Handled = true;
                }
                // Allow Percent %
                if ((e.Key == Key.D5) && e.Key == Key.RightShift | e.Key == Key.LeftShift)
                {
                    e.Handled = true;
                }
            }
            catch
            {

            }
        }

        /// <summary>
        ///    Volume Buttons
        /// </summary>
        // -------------------------
        // Up
        // -------------------------
        // Volume Up Button Click
        private void volumeUpButton_Click(object sender, RoutedEventArgs e)
        {
            int value;
            int.TryParse(volumeUpDown.Text, out value);

            value += 1;
            volumeUpDown.Text = value.ToString();
        }
        // Up Button Each Timer Tick
        private void dispatcherTimerUp_Tick(object sender, EventArgs e)
        {
            int value;
            int.TryParse(volumeUpDown.Text, out value);

            value += 1;
            volumeUpDown.Text = value.ToString();
        }
        // Hold Up Button
        private void volumeUpButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Timer      
            dispatcherTimerUp.Interval = new TimeSpan(0, 0, 0, 0, 100); //100ms
            dispatcherTimerUp.Start();
        }
        // Up Button Released
        private void volumeUpButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Disable Timer
            dispatcherTimerUp.Stop();
        }
        // -------------------------
        // Down
        // -------------------------
        // Volume Down Button Click
        private void volumeDownButton_Click(object sender, RoutedEventArgs e)
        {
            int value;
            int.TryParse(volumeUpDown.Text, out value);

            value -= 1;
            volumeUpDown.Text = value.ToString();
        }
        // Down Button Each Timer Tick
        private void dispatcherTimerDown_Tick(object sender, EventArgs e)
        {
            int value;
            int.TryParse(volumeUpDown.Text, out value);

            value -= 1;
            volumeUpDown.Text = value.ToString();
        }
        // Hold Down Button
        private void volumeDownButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Timer      
            dispatcherTimerDown.Interval = new TimeSpan(0, 0, 0, 0, 100); //100ms
            dispatcherTimerDown.Start();
        }
        // Down Button Released
        private void volumeDownButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Disable Timer
            dispatcherTimerDown.Stop();
        }


        /// <summary>
        ///    Video Codec Combobox
        /// </summary>
        private void cboVideoCodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // Video Codec Controls
            // -------------------------
            VideoControls.VideoCodecControls(this);

            // -------------------------
            // Video Encoding Pass Controls
            // -------------------------
            VideoControls.EncodingPass(this);

            // -------------------------
            // Pixel Format
            // -------------------------
            VideoControls.PixelFormat(this);

            // -------------------------
            // Optimize Controls
            // -------------------------
            VideoControls.OptimizeControls(this);

            // -------------------------
            // Display Video Bit-rate in TextBox
            // -------------------------
            // Must be after EncodingPass
            VideoBitrateDisplay();

            // -------------------------
            // Enable/Disable Video VBR
            // -------------------------
            if ((string)cboVideoCodec.SelectedItem == "VP8"
                || (string)cboVideoCodec.SelectedItem == "VP9"
                || (string)cboVideoCodec.SelectedItem == "x264" 
                || (string)cboVideoCodec.SelectedItem == "x265"
                || (string)cboVideoCodec.SelectedItem == "Copy")
            {
                tglVideoVBR.IsChecked = false;
                tglVideoVBR.IsEnabled = false;
            }
            // All other codecs
            else
            {
                // Do not check, only enable
                tglVideoVBR.IsEnabled = true;
            }

            // -------------------------
            // Enable/Disable Hardware Acceleration
            // -------------------------
            if ((string)cboVideoCodec.SelectedItem == "x264" 
                || (string)cboVideoCodec.SelectedItem == "x265")
            {
                cboHWAccel.IsEnabled = true;
            }
            else
            {
                cboHWAccel.SelectedItem = "off";
                cboHWAccel.IsEnabled = false;
            }

            // -------------------------
            // Enable/Disable Optimize Tune, Profile, Level
            // -------------------------
            //if ((string)cboVideoCodec.SelectedItem == "x264")
            //{
            //    // Enable
            //    cboOptTune.IsEnabled = true;
            //    cboOptProfile.IsEnabled = true;
            //    cboOptLevel.IsEnabled = true;
            //}
            //else
            //{
            //    // Disable
            //    cboOptTune.IsEnabled = false;
            //    cboOptProfile.IsEnabled = false;
            //    cboOptLevel.IsEnabled = false;

            //    cboOptTune.SelectedItem = "none";
            //    cboOptProfile.SelectedItem = "none";
            //    cboOptLevel.SelectedItem = "none";
            //    Video.optFlags = string.Empty;
            //}
        }

        /// <summary>
        ///    Subtitle Codec Combobox
        /// </summary>
        private void cboSubtitleCodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoControls.VideoCodecControls(this);

            // -------------------------
            // None Codec
            // -------------------------
            if ((string)cboSubtitleCodec.SelectedItem == "None")
            {
                cboSubtitlesStream.SelectedItem = "none";
                cboSubtitlesStream.IsEnabled = false;
            }

            // -------------------------
            // Burn Codec
            // -------------------------
            else if((string)cboSubtitleCodec.SelectedItem == "Burn")
            {
                // Force Select External
                // Can't burn All subtitle streams
                cboSubtitlesStream.SelectedItem = "external";
                cboSubtitlesStream.IsEnabled = true;
            }

            // -------------------------
            // Copy Codec
            // -------------------------
            else if ((string)cboSubtitleCodec.SelectedItem == "Copy")
            {
                //cboSubtitlesStream.SelectedItem = "all";
                cboSubtitlesStream.IsEnabled = true;
            }

            // -------------------------
            // All Other Codecs
            // -------------------------
            else
            {
                cboSubtitlesStream.IsEnabled = true;
            }
        }


        /// <summary>
        ///    Audio Codec Combobox
        /// </summary>
        private void cboAudioCodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioControls.AudioCodecControls(this);
        }


        /// <summary>
        ///    Format Combobox
        /// </summary>
        private void cboFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // Output Control Selections
            // -------------------------
            FormatControls.OuputFormatDefaults(this);

            // -------------------------
            // Get Output Extension
            // -------------------------
            FormatControls.OutputFormatExt(this);

            // -------------------------
            // Output ComboBox Options
            // -------------------------
            FormatControls.OutputFormat(this);

            // -------------------------
            // Change All MainWindow Items
            // -------------------------
            VideoControls.VideoCodecControls(this);
            AudioControls.AudioCodecControls(this);

            // -------------------------
            // Pass Controls
            // -------------------------
            VideoControls.EncodingPass(this);

            // -------------------------
            // Optimize Controls
            // -------------------------
            VideoControls.OptimizeControls(this);

            // -------------------------
            // File Renamer
            // -------------------------
            // Add (1) if File Names are the same
            if (!string.IsNullOrEmpty(inputDir)
                && string.Equals(inputFileName, outputFileName, StringComparison.CurrentCultureIgnoreCase))
            {
                outputFileName = FileRenamer(inputFileName);
            }

            // -------------------------
            // Default to Auto
            // -------------------------
            // Always Default Video to Auto if Input Ext matches Format Output Ext
            if ((string)cboVideoQuality.SelectedItem != "Auto" 
                && string.Equals(inputExt, outputExt, StringComparison.CurrentCultureIgnoreCase))
            {
                cboVideoQuality.SelectedItem = "Auto";
            }
            // Always Default Video to Auto if Input Ext matches Format Output Ext
            if ((string)cboAudioQuality.SelectedItem != "Auto" 
                && string.Equals(inputExt, outputExt, StringComparison.CurrentCultureIgnoreCase))
            {
                cboAudioQuality.SelectedItem = "Auto";
            }

            // -------------------------
            // Single File - Update Ouput Textbox with current Format extension
            // -------------------------
            if (tglBatch.IsChecked == false && !string.IsNullOrWhiteSpace(tbxOutput.Text))
            {
                tbxOutput.Text = outputDir + outputFileName + outputExt;
            }
            
        }


        /// <summary>
        ///    Media Type Combobox
        /// </summary>
        private void cboMediaType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormatControls.MediaType(this); 
        }


        /// <summary>
        ///    Video Quality Combobox
        /// </summary>
        private void cboVideo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // Auto Copy Video Codec Controls
            // -------------------------
            VideoControls.AutoCopyVideoCodec(this);

            // -------------------------
            // Auto Copy Subtitle Codec Controls
            // -------------------------
            VideoControls.AutoCopySubtitleCodec(this);

            // -------------------------
            // Video Quality Controls
            // -------------------------
            VideoControls.VideoQualityControls(this);

            // -------------------------
            // Pixel Format
            // -------------------------
            VideoControls.PixelFormat(this);


            // -------------------------
            // Enable Video Bitrate Custom
            // -------------------------
            // Enable
            if ((string)cboVideoQuality.SelectedItem == "Custom")
            {
                crfCustom.IsEnabled = true;
                vBitrateCustom.IsEnabled = true;
                vMinrateCustom.IsEnabled = true;
                vMaxrateCustom.IsEnabled = true;
                vBufsizeCustom.IsEnabled = true;

                // Disable CRF for Theora
                if ((string)cboVideoCodec.SelectedItem == "Theora")
                {
                    crfCustom.IsEnabled = false;
                }
            }
            // Disable
            else
            {
                crfCustom.IsEnabled = false;
                vBitrateCustom.IsEnabled = false;
                vMinrateCustom.IsEnabled = false;
                vMaxrateCustom.IsEnabled = false;
                vBufsizeCustom.IsEnabled = false;
            }

            // -------------------------
            // Pass Controls Method
            // -------------------------
            VideoControls.EncodingPass(this);

            // -------------------------
            // Pass - Default to CRF
            // -------------------------
            // Keep in Video SelectionChanged
            // If Video Not Auto and User Willingly Selected Pass is false
            if ((string)cboVideoQuality.SelectedItem != "Auto" 
                && VideoControls.passUserSelected == false)
            {
                cboPass.SelectedItem = "CRF";
            }


            // -------------------------
            // Display Bit-rate in TextBox
            // -------------------------
            VideoBitrateDisplay();

        } // End Video Combobox


        /// <summary>
        ///    Video Display Bit-rate
        /// </summary>
        public void VideoBitrateDisplay()
        {
            // -------------------------
            // Clear Variables before Run
            // -------------------------
            ClearVariables(this);
            vBitrateCustom.Text = string.Empty;
            vMinrateCustom.Text = string.Empty;
            vMaxrateCustom.Text = string.Empty;
            vBufsizeCustom.Text = string.Empty;
            crfCustom.Text = string.Empty;


            if ((string)cboVideoQuality.SelectedItem != "Auto"
                && (string)cboVideoQuality.SelectedItem != "Lossless"
                && (string)cboVideoQuality.SelectedItem != "Custom"
                && (string)cboVideoQuality.SelectedItem != "None"
                && !string.IsNullOrEmpty((string)cboVideoQuality.SelectedItem))
            {
                // Display Bitrate in TextBox
                // Display Controls at the end of VideoQuality() Method
                Video.VideoQuality(this);

                //if ((string)cboVideoCodec.SelectedItem == "x265")
                //{
                //    vBitrateCustom.Text = Video.vBitrate;
                //}

                //// Display Bit-rate in TextBox
                //if (!string.IsNullOrEmpty(Video.vBitrate))
                //{
                //    vBitrateCustom.Text = Video.vBitrate;
                //}

                //// Display Minrate in TextBox
                //if (!string.IsNullOrEmpty(Video.vMinrate))
                //{
                //    vMinrateCustom.Text = Video.vMinrate;
                //}

                //// Display Maxrate in TextBox
                //if (!string.IsNullOrEmpty(Video.vMaxrate))
                //{
                //    vMaxrateCustom.Text = Video.vMaxrate;
                //}

                //// Display Bufsize in TextBox
                //if (!string.IsNullOrEmpty(Video.vBufsize))
                //{
                //    vBufsizeCustom.Text = Video.vBufsize;
                //}

                //// Display CRF in TextBox
                //if (!string.IsNullOrEmpty(Video.crf))
                //{
                //    crfCustom.Text = Video.crf.Replace("-crf ", "");
                //}
            }
            else
            {
                crfCustom.Text = string.Empty;

                vBitrateCustom.Text = string.Empty;
                vMinrateCustom.Text = string.Empty;
                vMaxrateCustom.Text = string.Empty;
                vBufsizeCustom.Text = string.Empty;
            }
        }

        /// <summary>
        ///    Audio Quality Combobox
        /// </summary>
        private void cboAudio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // Custom
            // -------------------------
            //enable Audio Custom
            if ((string)cboAudioQuality.SelectedItem == "Custom")
            {
                audioCustom.IsEnabled = true;
            }
            else
            {
                audioCustom.IsEnabled = false;
                //audioCustom.Text = "kbps";
                audioCustom.Text = string.Empty;
            }

            // -------------------------
            // Audio
            // -------------------------
            // Always Enable Audio for AAC codec
            if ((string)cboAudioCodec.SelectedItem == "AAC")
            {
                cboAudioQuality.IsEnabled = true;
            }

            // -------------------------
            // Audio Codec
            // -------------------------
            // Always switch to ALAC if M4A is Lossless
            if ((string)cboFormat.SelectedItem == "m4a" && (string)cboAudioQuality.SelectedItem == "Lossless")
            {
                cboAudioCodec.SelectedItem = "ALAC";
            }

            // -------------------------
            // VBR
            // -------------------------
            // Disable and Uncheck VBR if, Lossless or Mute
            if ((string)cboAudioQuality.SelectedItem == "Lossless" 
                || (string)cboAudioQuality.SelectedItem == "Mute")
            {
                tglAudioVBR.IsEnabled = false;
                tglAudioVBR.IsChecked = false;
            }

            // Disable VBR if AC3, ALAC, FLAC, PCM, Copy
            if ((string)cboAudioCodec.SelectedItem == "AC3"
                || (string)cboAudioCodec.SelectedItem == "ALAC"
                || (string)cboAudioCodec.SelectedItem == "FLAC"
                || (string)cboAudioCodec.SelectedItem == "PCM"
                || (string)cboAudioCodec.SelectedItem == "Copy")
            {
                tglAudioVBR.IsEnabled = false;
            }
            // Enable VBR for Vorbis, Opus, LAME, AAC
            if ((string)cboAudioCodec.SelectedItem == "Vorbis" 
                || (string)cboAudioCodec.SelectedItem == "Opus" 
                || (string)cboAudioCodec.SelectedItem == "LAME" 
                || (string)cboAudioCodec.SelectedItem == "AAC")
            {
                tglAudioVBR.IsEnabled = true;
            }

            // If AUTO, Check or Uncheck VBR
            if ((string)cboAudioQuality.SelectedItem == "Auto")
            {
                if ((string)cboAudioCodec.SelectedItem == "Vorbis")
                {
                    tglAudioVBR.IsChecked = true;
                }
                if ((string)cboAudioCodec.SelectedItem == "Opus" 
                    || (string)cboAudioCodec.SelectedItem == "AAC" 
                    || (string)cboAudioCodec.SelectedItem == "AC3" 
                    || (string)cboAudioCodec.SelectedItem == "LAME" 
                    || (string)cboAudioCodec.SelectedItem == "ALAC" 
                    || (string)cboAudioCodec.SelectedItem == "FLAC" 
                    || (string)cboAudioCodec.SelectedItem == "PCM" 
                    || (string)cboAudioCodec.SelectedItem == "Copy")
                {
                    tglAudioVBR.IsChecked = false;
                }
            }

            // Quality VBR Override
            // Disable / Enable VBR
            if ((string)cboAudioQuality.SelectedItem == "Lossless" || (string)cboAudioQuality.SelectedItem == "Mute")
            {
                tglAudioVBR.IsEnabled = false;
            }

            // Call Method (Needs to be at this location)
            // Set Audio Codec Combobox to "Copy" if Input Extension is Same as Output Extension and Audio Quality is Auto
            AudioControls.AutoCopyAudioCodec(this);


            // -------------------------
            // Display Bit-rate in TextBox
            // -------------------------
            if ((string)cboAudioQuality.SelectedItem != "Auto"
                && (string)cboAudioQuality.SelectedItem != "Lossless"
                && (string)cboAudioQuality.SelectedItem != "Custom"
                && (string)cboAudioQuality.SelectedItem != "Mute"
                && (string)cboAudioQuality.SelectedItem != "None"
                && !string.IsNullOrEmpty((string)cboAudioQuality.SelectedItem))
            {
                audioCustom.Text = cboAudioQuality.SelectedItem.ToString() + "k";
            }
            else
            {
                //audioCustom.Text = "kbps";
                audioCustom.Text = string.Empty;
            }

            // -------------------------
            // Mute
            // -------------------------
            if ((string)cboAudioQuality.SelectedItem == "Mute")
            {
                // -------------------------
                // Disable
                // -------------------------

                // Channel
                //cboChannel.SelectedItem = "Source";
                cboChannel.IsEnabled = false;

                // Stream
                //cboAudioStream.SelectedItem = "none";
                cboAudioStream.IsEnabled = false;

                // Samplerate
                //cboSamplerate.SelectedItem = "auto";
                cboSamplerate.IsEnabled = false;

                // BitDepth
                //cboBitDepth.SelectedItem = "auto";
                cboBitDepth.IsEnabled = false;

                // Volume
                volumeUpDown.IsEnabled = false;
                volumeUpButton.IsEnabled = false;
                volumeDownButton.IsEnabled = false;
            }
            else
            {
                // -------------------------
                // Enable
                // -------------------------

                // Don't select item, to avoid changing user selection each time Quality is changed.

                // Channel
                cboChannel.IsEnabled = true;

                // Stream
                cboAudioStream.IsEnabled = true;

                // Samplerate
                cboSamplerate.IsEnabled = true;

                // BitDepth
                cboBitDepth.IsEnabled = true;

                // Volume
                volumeUpDown.IsEnabled = true;
                volumeUpButton.IsEnabled = true;
                volumeDownButton.IsEnabled = true;
            }

        } // End audio_SelectionChanged



        /// <summary>
        ///    Size Combobox
        /// </summary>
        private void cboSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set Video Codec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
            VideoControls.AutoCopyVideoCodec(this);
            VideoControls.AutoCopySubtitleCodec(this);

            // Enable Aspect Custom
            if ((string)cboSize.SelectedItem == "Custom")
            {
                widthCustom.IsEnabled = true;
                heightCustom.IsEnabled = true;

                widthCustom.Text = "auto";
                heightCustom.Text = "auto";
            }
            else
            {
                widthCustom.IsEnabled = false;
                heightCustom.IsEnabled = false;
                widthCustom.Text = "auto";
                heightCustom.Text = "auto";
            }

            // Change TextBox Resolution numbers
            if ((string)cboSize.SelectedItem == "Source")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "auto";
            }
            else if ((string)cboSize.SelectedItem == "8K")
            {
                widthCustom.Text = "7680";
                heightCustom.Text = "auto";
            }
            else if ((string)cboSize.SelectedItem == "4K")
            {
                widthCustom.Text = "4096";
                heightCustom.Text = "auto";
            }
            else if ((string)cboSize.SelectedItem == "4K UHD")
            {
                widthCustom.Text = "3840";
                heightCustom.Text = "auto";
            }
            else if ((string)cboSize.SelectedItem == "2K")
            {
                widthCustom.Text = "2048";
                heightCustom.Text = "auto";
            }
            else if ((string)cboSize.SelectedItem == "1440p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "1440";
            }
            else if ((string)cboSize.SelectedItem == "1200p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "1200";
            }
            else if ((string)cboSize.SelectedItem == "1080p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "1080";
            }
            else if ((string)cboSize.SelectedItem == "720p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "720";
            }
            else if ((string)cboSize.SelectedItem == "480p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "480";
            }
            else if ((string)cboSize.SelectedItem == "320p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "320";
            }
            else if ((string)cboSize.SelectedItem == "240p")
            {
                widthCustom.Text = "auto";
                heightCustom.Text = "240";
            }
        }
        // -------------------------
        // Width Textbox Change
        // -------------------------
        // Got Focus
        private void widthCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear textbox on focus if default text "width"
            if (widthCustom.Focus() == true && widthCustom.Text == "auto")
            {
                widthCustom.Text = string.Empty;
            }
        }
        // Lost Focus
        private void widthCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            // Change textbox back to "width" if left empty
            if (string.IsNullOrWhiteSpace(widthCustom.Text))
            {
                widthCustom.Text = "auto";
            }
        }

        // -------------------------
        // Height Textbox Change
        // -------------------------
        // Got Focus
        private void heightCustom_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear textbox on focus if default text "width"
            if (heightCustom.Focus() == true && heightCustom.Text == "auto")
            {
                heightCustom.Text = string.Empty;
            }
        }
        // Lost Focus
        private void heightCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            // Change textbox back to "width" if left empty
            if (string.IsNullOrWhiteSpace(heightCustom.Text))
            {
                heightCustom.Text = "auto";
            }
        }


        /// <summary>
        ///    Cut Combobox
        /// </summary>
        private void cboCut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormatControls.CutControls(this); 
        }

        // -------------------------
        // Frame Start Textbox Change
        // -------------------------
        // Got Focus
        private void frameStart_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear textbox on focus if default text "width"
            if (frameStart.Focus() == true && frameStart.Text == "Frame")
            {
                frameStart.Text = string.Empty;
            }
        }
        // Lost Focus
        private void frameStart_LostFocus(object sender, RoutedEventArgs e)
        {
            // Change textbox back to "auto" if left empty
            if (string.IsNullOrWhiteSpace(frameStart.Text))
            {
                frameStart.Text = "Frame";
            }
        }

        // -------------------------
        // Frame End Textbox Change
        // -------------------------
        // Got Focus
        private void frameEnd_GotFocus(object sender, RoutedEventArgs e)
        {
            // Clear textbox on focus if default text "auto"
            if (frameEnd.Focus() == true && frameEnd.Text == "Range")
            {
                frameEnd.Text = string.Empty;
            }
        }
        // Lost Focus
        private void frameEnd_LostFocus(object sender, RoutedEventArgs e)
        {
            // Change textbox back to "auto" if left empty
            if (string.IsNullOrWhiteSpace(frameEnd.Text))
            {
                frameEnd.Text = "Range";
            }
        }


        /// <summary>
        ///    Crop Window Button
        /// </summary>
        private void buttonCrop_Click(object sender, RoutedEventArgs e)
        {
            // Start Window
            cropwindow = new CropWindow(this);

            // Detect which screen we're on
            var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
            var thisScreen = allScreens.SingleOrDefault(s => this.Left >= s.WorkingArea.Left && this.Left < s.WorkingArea.Right);

            // Position Relative to MainWindow
            // Keep from going off screen
            cropwindow.Left = Math.Max((this.Left + (this.Width - cropwindow.Width) / 2), thisScreen.WorkingArea.Left);
            cropwindow.Top = Math.Max(this.Top - cropwindow.Height - 12, thisScreen.WorkingArea.Top);

            // Keep Window on Top
            cropwindow.Owner = Window.GetWindow(this);

            // Open Window
            cropwindow.ShowDialog();
        }


        /// <summary>
        ///    Crop Clear Button
        /// </summary>
        private void buttonCropClear_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //cropwindow.textBoxCropWidth.Text = string.Empty;
            //cropwindow.textBoxCropHeight.Text = string.Empty;
            //cropwindow.textBoxCropX.Text = string.Empty;
            //cropwindow.textBoxCropY.Text = string.Empty;

            VideoFilters.vFilter = string.Empty;

                if (VideoFilters.vFiltersList != null)
                {
                    VideoFilters.vFiltersList.Clear();
                    VideoFilters.vFiltersList.TrimExcess();
                }

            // Trigger the CropWindow Clear Button (only way it will clear the string)
            //cropwindow.buttonClear_Click(sender, e);
            CropWindow.CropClear(this);

            //}
            //catch
            //{

            //}
        }

        /// <summary>
        ///    Subtitle
        /// </summary>
        private void cboSubtitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // External
            // -------------------------
            if ((string)cboSubtitlesStream.SelectedItem == "external")
            {
                // Enable External ListView and Buttons
                listViewSubtitles.IsEnabled = true;

                btnAddSubtitles.IsEnabled = true;
                btnRemoveSubtitle.IsEnabled = true;
                btnSortSubtitleUp.IsEnabled = true;
                btnSortSubtitleDown.IsEnabled = true;
                btnClearSubtitles.IsEnabled = true;

                listViewSubtitles.Opacity = 1;
            }
            else
            {
                // Disable External ListView and Buttons
                listViewSubtitles.IsEnabled = false;

                btnAddSubtitles.IsEnabled = false;
                btnRemoveSubtitle.IsEnabled = false;
                btnSortSubtitleUp.IsEnabled = false;
                btnSortSubtitleDown.IsEnabled = false;
                btnClearSubtitles.IsEnabled = false;

                listViewSubtitles.Opacity = 0.1;
            }

            // -------------------------
            // Select Subtitle Codec
            // -------------------------
            //VideoControls.SubtitleCodecControls(this);
        }


        /// <summary>
        ///    Presets
        /// </summary>
        private void cboPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Presets.Preset(this); // Method
        }


        /// <summary>
        ///    Optimize Combobox
        /// </summary>
        private void cboOptimize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // -------------------------
            // Optimize Controls
            // -------------------------
            VideoControls.OptimizeControls(this);
        }




        /// <summary>
        ///    Audio Limiter Toggle
        /// </summary>
        //private void tglAudioLimiter_Checked(object sender, RoutedEventArgs e)
        //{
        //    //// Enable Limit TextBox
        //    //if (tglAudioLimiter.IsChecked == true)
        //    //{
        //    //    audioLimiter.IsEnabled = true;
        //    //}

        //    //// Disable Audio Codec Copy
        //    //AudioControls.AutoCopyAudioCodec(this);
        //}
        //private void tglAudioLimiter_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    //// Disable Limit TextBox
        //    //if (tglAudioLimiter.IsChecked == false)
        //    //{
        //    //    audioLimiter.IsEnabled = false;
        //    //}

        //    //// Enable Audio Codec Copy if InputExt / outputExt match
        //    //AudioControls.AutoCopyAudioCodec(this);
        //}


        /// <summary>
        ///    Batch Extension Textbox
        /// </summary>
        private void batchExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            //// Enabled Check
            //if (batchExtensionTextBox.IsEnabled == true)
            //{
            //    batchExt = batchExtensionTextBox.Text;

            //    // Add period to batchExt if user did not enter (This helps enable Copy)
            //    if (!string.IsNullOrWhiteSpace(batchExtensionTextBox.Text) &&
            //        !batchExt.StartsWith("."))
            //    {
            //        batchExt = "." + batchExt;
            //    }
            //}
            //else
            //{
            //    batchExt = string.Empty;
            //}

            //// Set Video and AudioCodec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
            //VideoControls.AutoCopyVideoCodec(this);
            //VideoControls.AutoCopySubtitleCodec(this);
            //AudioControls.AutoCopyAudioCodec(this);

            // Remove Default Value
            if (string.IsNullOrWhiteSpace(batchExtensionTextBox.Text) || 
                batchExtensionTextBox.Text == "extension"
                )
            {
                batchExt = string.Empty;
            }
            // Batch Extension Variable
            else
            {
                batchExt = batchExtensionTextBox.Text;
            }

            // Add period to batchExt if user did not enter (This helps enable Copy)
            if (!batchExt.StartsWith(".") &&
                !string.IsNullOrWhiteSpace(batchExtensionTextBox.Text) &&
                batchExtensionTextBox.Text != "extension")
            {
                batchExt = "." + batchExt;
            }

            // Set Video and AudioCodec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
            VideoControls.AutoCopyVideoCodec(this);
            VideoControls.AutoCopySubtitleCodec(this);
            AudioControls.AutoCopyAudioCodec(this);

        }


        /// <summary>
        /// Subtitle Sort Up
        /// </summary>
        private void btnSortSubtitleUp_Click(object sender, RoutedEventArgs e)
        {
            if (listViewSubtitles.SelectedItems.Count > 0)
            {
                var selectedIndex = listViewSubtitles.SelectedIndex;

                if (selectedIndex > 0)
                {
                    // ListView Items
                    var itemlsvFileNames = listViewSubtitles.Items[selectedIndex];
                    listViewSubtitles.Items.RemoveAt(selectedIndex);
                    listViewSubtitles.Items.Insert(selectedIndex - 1, itemlsvFileNames);

                    // List File Paths
                    string itemFilePaths = Video.subtitleFilePathsList[selectedIndex];
                    Video.subtitleFilePathsList.RemoveAt(selectedIndex);
                    Video.subtitleFilePathsList.Insert(selectedIndex - 1, itemFilePaths);

                    // List File Names
                    string itemFileNames = Video.subtitleFileNamesList[selectedIndex];
                    Video.subtitleFileNamesList.RemoveAt(selectedIndex);
                    Video.subtitleFileNamesList.Insert(selectedIndex - 1, itemFileNames);

                    // Highlight Selected Index
                    listViewSubtitles.SelectedIndex = selectedIndex - 1;
                }
            }
        }

        /// <summary>
        /// Subtitle Sort Down
        /// </summary>
        private void btnSortSubtitleDown_Click(object sender, RoutedEventArgs e)
        {
            if (listViewSubtitles.SelectedItems.Count > 0)
            {
                var selectedIndex = listViewSubtitles.SelectedIndex;

                if (selectedIndex + 1 < listViewSubtitles.Items.Count)
                {
                    // ListView Items
                    var itemlsvFileNames = listViewSubtitles.Items[selectedIndex];
                    listViewSubtitles.Items.RemoveAt(selectedIndex);
                    listViewSubtitles.Items.Insert(selectedIndex + 1, itemlsvFileNames);

                    // List FilePaths
                    string itemFilePaths = Video.subtitleFilePathsList[selectedIndex];
                    Video.subtitleFilePathsList.RemoveAt(selectedIndex);
                    Video.subtitleFilePathsList.Insert(selectedIndex + 1, itemFilePaths);

                    // List File Names
                    string itemFileNames = Video.subtitleFileNamesList[selectedIndex];
                    Video.subtitleFileNamesList.RemoveAt(selectedIndex);
                    Video.subtitleFileNamesList.Insert(selectedIndex + 1, itemFileNames);

                    // Highlight Selected Index
                    listViewSubtitles.SelectedIndex = selectedIndex + 1;
                }
            }
        }

        /// <summary>
        /// Subtitle Add
        /// </summary>
        private void btnAddSubtitles_Click(object sender, RoutedEventArgs e)
        {
            // Open Select File Window
            Microsoft.Win32.OpenFileDialog selectFiles = new Microsoft.Win32.OpenFileDialog();

            // Defaults
            selectFiles.Multiselect = true;
            selectFiles.Filter = "All files (*.*)|*.*|SRT (*.srt)|*.srt|SUB (*.sub)|*.sub|SBV (*.sbv)|*.sbv|ASS (*.ass)|*.ass|SSA (*.ssa)|*.ssa|MPSUB (*.mpsub)|*.mpsub|LRC (*.lrc)|*.lrc|CAP (*.cap)|*.cap";

            // Remember Last Dir
            //
            //string previousPath = Settings.Default.subsDir.ToString();
            //// Use Previous Path if Not Null
            //if (!string.IsNullOrEmpty(previousPath))
            //{
            //    selectFiles.InitialDirectory = previousPath;
            //}

            // Process Dialog Box
            Nullable<bool> result = selectFiles.ShowDialog();
            if (result == true)
            {
                // Reset
                //SubtitlesClear();

                // Add Selected Files to List
                for (var i = 0; i < selectFiles.FileNames.Length; i++)
                {
                    // Wrap in quotes for ffmpeg -i
                    Video.subtitleFilePathsList.Add("\"" + selectFiles.FileNames[i] + "\"");
                    //MessageBox.Show(Video.subtitleFiles[i]); //debug

                    Video.subtitleFileNamesList.Add(Path.GetFileName(selectFiles.FileNames[i]));

                    // ListView Display File Names + Ext
                    listViewSubtitles.Items.Add(Path.GetFileName(selectFiles.FileNames[i]));
                }
            }
        }

        /// <summary>
        /// Subtitle Remove
        /// </summary>
        private void btnRemoveSubtitle_Click(object sender, RoutedEventArgs e)
        {
            if (listViewSubtitles.SelectedItems.Count > 0)
            {
                var selectedIndex = listViewSubtitles.SelectedIndex;

                // ListView Items
                var itemlsvFileNames = listViewSubtitles.Items[selectedIndex];
                listViewSubtitles.Items.RemoveAt(selectedIndex);

                // List File Paths
                string itemFilePaths = Video.subtitleFilePathsList[selectedIndex];
                Video.subtitleFilePathsList.RemoveAt(selectedIndex);

                // List File Names
                string itemFileNames = Video.subtitleFileNamesList[selectedIndex];
                Video.subtitleFileNamesList.RemoveAt(selectedIndex);
            }
        }

        /// <summary>
        /// Subtitle Clear All
        /// </summary>
        private void btnClearSubtitles_Click(object sender, RoutedEventArgs e)
        {
            SubtitlesClear();
        }

        /// <summary>
        /// Subtitle Clear (Method)
        /// </summary>
        public void SubtitlesClear()
        {
            // Clear List View
            listViewSubtitles.Items.Clear();

            // Clear Paths List
            if (Video.subtitleFilePathsList != null && 
                Video.subtitleFilePathsList.Count > 0)
            {
                Video.subtitleFilePathsList.Clear();
                Video.subtitleFilePathsList.TrimExcess();
            }

            // Clear Names List
            if (Video.subtitleFileNamesList != null &&
                Video.subtitleFileNamesList.Count > 0)
            {
                Video.subtitleFileNamesList.Clear();
                Video.subtitleFileNamesList.TrimExcess();
            }
        }

        /// <summary>
        ///    Batch Toggle
        /// </summary>
        // Checked
        private void tglBatch_Checked(object sender, RoutedEventArgs e)
        {
            // Enable / Disable batch extension textbox
            if (tglBatch.IsChecked == true)
            {
                batchExtensionTextBox.IsEnabled = true;
                batchExtensionTextBox.Text = string.Empty;
            }

            // Clear Browse Textbox, Input Filename, Dir, Ext
            if (!string.IsNullOrWhiteSpace(tbxInput.Text))
            {
                tbxInput.Text = string.Empty;
                inputFileName = string.Empty;
                inputDir = string.Empty;
                inputExt = string.Empty;
            }

            // Clear Output Textbox, Output Filename, Dir, Ext
            if (!string.IsNullOrWhiteSpace(tbxOutput.Text))
            {
                tbxOutput.Text = string.Empty;
                outputFileName = string.Empty;
                outputDir = string.Empty;
                outputExt = string.Empty;
            }

        }
        // Unchecked
        private void tglBatch_Unchecked(object sender, RoutedEventArgs e)
        {
            // Enable / Disable batch extension textbox
            if (tglBatch.IsChecked == false)
            {
                batchExtensionTextBox.IsEnabled = false;
                batchExtensionTextBox.Text = "extension";
            }

            // Clear Browse Textbox, Input Filename, Dir, Ext
            if (!string.IsNullOrWhiteSpace(tbxInput.Text))
            {
                tbxInput.Text = string.Empty;
                inputFileName = string.Empty;
                inputDir = string.Empty;
                inputExt = string.Empty;
            }

            // Clear Output Textbox, Output Filename, Dir, Ext
            if (!string.IsNullOrWhiteSpace(tbxOutput.Text))
            {
                tbxOutput.Text = string.Empty;
                outputFileName = string.Empty;
                outputDir = string.Empty;
                outputExt = string.Empty;
            }

            // Set Video and AudioCodec Combobox to "Copy" if Input Extension is Same as Output Extension and Video Quality is Auto
            VideoControls.AutoCopyVideoCodec(this);
            VideoControls.AutoCopySubtitleCodec(this);
            AudioControls.AutoCopyAudioCodec(this);
        }


        /// --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///    Preview Button
        /// </summary>
        /// --------------------------------------------------------------------------------------------------------
        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            FFplay.Preview(this);
        }




        /// --------------------------------------------------------------------------------------------------------
        /// <summary>
        ///    Convert Button
        /// </summary>
        /// --------------------------------------------------------------------------------------------------------
        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            // -------------------------
            // Check if Script has been Edited
            // -------------------------
            if (CheckScriptEdited(this) == true)
            {
                // Halt
                return;
            }

            // -------------------------
            // Clear Variables before Run
            // -------------------------
            ClearVariables(this);

            // -------------------------
            // Enable Script
            // -------------------------
            script = true;

            // -------------------------
            // Batch Extention Period Check
            // -------------------------
            BatchExtCheck(this);

            // -------------------------
            // Set FFprobe Path
            // -------------------------
            FFprobePath();

            // -------------------------
            // Ready Halts
            // -------------------------
            ReadyHalts(this);


            // Log Console Message /////////
            if (ready == true)
            {
                // Log Console Message /////////
                Log.WriteAction = () =>
                {
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Bold(new Run("...............................................")) { Foreground = Log.ConsoleAction });

                    // Log Console Message /////////
                    DateTime localDate = DateTime.Now;

                    // Log Console Message /////////
                    
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Bold(new Run(Convert.ToString(localDate))) { Foreground = Log.ConsoleAction });
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Bold(new Run("Starting Conversion...")) { Foreground = Log.ConsoleTitle });
                };
                Log.LogActions.Add(Log.WriteAction);
            }


            // --------------------------------------------------------------------
            // Ready Check
            // If Ready, start conversion process
            // --------------------------------------------------------------------
            if (ready == true)
            {
                // -------------------------
                // Single
                // -------------------------
                if (tglBatch.IsChecked == false)
                {
                    // -------------------------
                    // FFprobe Detect Metadata
                    // -------------------------
                    FFprobe.Metadata(this);

                    // -------------------------
                    // FFmpeg Generate Arguments (Single)
                    // -------------------------
                    //disabled if batch
                    FFmpeg.FFmpegSingleGenerateArgs(this);
                }

                // -------------------------
                // Batch
                // -------------------------
                else if (tglBatch.IsChecked == true)
                {
                    // -------------------------
                    // FFprobe Video Entry Type Containers
                    // -------------------------
                    FFprobe.VideoEntryType(this);

                    // -------------------------
                    // FFprobe Video Entry Type Containers
                    // -------------------------
                    FFprobe.AudioEntryType(this);

                    // -------------------------
                    // FFmpeg Generate Arguments (Batch)
                    // -------------------------
                    //disabled if single file
                    FFmpeg.FFmpegBatchGenerateArgs(this);
                }

                // -------------------------
                // FFmpeg Convert
                // -------------------------
                FFmpeg.FFmpegConvert(this);

                // -------------------------
                // Sort Script
                // -------------------------
                // Only if Auto Sort is enabled
                if (tglAutoSortScript.IsChecked == true)
                {
                    ScriptView.sort = false;
                    Sort();
                }

                // -------------------------
                // Reset Sort
                // -------------------------
                // Auto Sort enabled
                //if (tglAutoSortScript.IsChecked == true)
                //{
                //    ScriptView.sort = false;
                //    txblScriptSort.Text = "Inline";
                //}
                //// Auto Sort disabled
                //else if (tglAutoSortScript.IsChecked == false)
                //{
                //    ScriptView.sort = true;
                //    txblScriptSort.Text = "Sort";
                //}

                // Log Console Message /////////
                Log.WriteAction = () =>
                {
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Run("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~") { Foreground = Log.ConsoleAction });
                };
                Log.LogActions.Add(Log.WriteAction);


                // -------------------------
                // Write All Log Actions to Console
                // -------------------------
                Log.LogWriteAll(this);

                // -------------------------
                // Generate Script
                // -------------------------
                //FFmpeg.FFmpegScript(this); // moved to FFmpegConvert()

                // -------------------------
                // Clear Strings for next Run
                // -------------------------
                ClearVariables(this);
                GC.Collect();
            }
            else
            {
                //debug
                //MessageBox.Show("Not Ready");

                // Log Console Message /////////
                Log.WriteAction = () =>
                {
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new LineBreak());
                    Log.logParagraph.Inlines.Add(new Run("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~") { Foreground = Log.ConsoleAction });
                };
                Log.LogActions.Add(Log.WriteAction);


                /// <summary>
                ///    Write All Log Actions to Console
                /// </summary> 
                Log.LogWriteAll(this);

                /// <summary>
                ///    Restart
                /// </summary> 
                /* unlock */
                ready = true;

                // -------------------------
                // Write Variables to Debug Window (Method)
                // -------------------------
                //DebugConsole.DebugWrite(debugconsole, this);

                // -------------------------
                // Clear Variables for next Run
                // -------------------------
                ClearVariables(this);
                GC.Collect();

            }

        } //end convert button




        /// <summary>
        ///     Save Script
        /// </summary>
        private void btnScriptSave_Click(object sender, RoutedEventArgs e)
        {
            // Open 'Save File'
            Microsoft.Win32.SaveFileDialog saveFile = new Microsoft.Win32.SaveFileDialog();

            //saveFile.InitialDirectory = inputDir;
            saveFile.RestoreDirectory = true;
            saveFile.Filter = "Text file (*.txt)|*.txt";
            saveFile.DefaultExt = ".txt";
            saveFile.FileName = "Script";

            // Show save file dialog box
            Nullable<bool> result = saveFile.ShowDialog();

            // Process dialog box
            if (result == true)
            {
                // Save document
                File.WriteAllText(saveFile.FileName, ScriptView.GetScriptRichTextBoxContents(this), Encoding.Unicode);
            }
        }

        /// <summary>
        ///     Copy All Button
        /// </summary>
        private void btnScriptCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ScriptView.GetScriptRichTextBoxContents(this), TextDataFormat.UnicodeText);
        }

        /// <summary>
        ///     Sort Button
        /// </summary>
        private void btnScriptSort_Click(object sender, RoutedEventArgs e)
        {
            Sort();
        }

        /// <summary>
        ///     Sort (Method)
        /// </summary>
        public void Sort()
        {
            // Check if Rich TextBox is Empty
            //TextRange textRange = new TextRange(rtbScriptView.Document.ContentStart, rtbScriptView.Document.ContentEnd);
            //string rtb = textRange.Text;
            //if (string.IsNullOrWhiteSpace(rtb))
            //{
            //    //MessageBox.Show("Empty"); //debug
            //    return;
            //}

            // Debug Sort
            //if (ScriptView.sort == false)
            //{
            //    MessageBox.Show("sort false");
            //}
            //else if(ScriptView.sort == true)
            //{
            //    MessageBox.Show("sort true");
            //}
            

            // Only if Script not empty
            if (!string.IsNullOrWhiteSpace(ScriptView.GetScriptRichTextBoxContents(this)))
            {
                // -------------------------
                // Has Not Been Edited
                // -------------------------
                if (ScriptView.sort == false
                    && RemoveLineBreaks(ScriptView.GetScriptRichTextBoxContents(this))
                                 //.Replace(Environment.NewLine, "")
                                 //.Replace("\r\n", "")
                                 //.Replace("\u2028", "")
                                 //.Replace("\u000A", "")
                                 //.Replace("\u000B", "")
                                 //.Replace("\u000C", "")
                                 //.Replace("\u000D", "")
                                 //.Replace("\u0085", "")
                                 //.Replace("\u2028", "")
                                 //.Replace("\u2029", "")

                                 == FFmpeg.ffmpegArgs)
                {
                    // Clear Old Text
                    //ScriptView.scriptParagraph.Inlines.Clear();
                    ScriptView.ClearScriptView(this);

                    // Write FFmpeg Args Sort
                    rtbScriptView.Document = new FlowDocument(ScriptView.scriptParagraph);
                    rtbScriptView.BeginChange();
                    ScriptView.scriptParagraph.Inlines.Add(new Run(FFmpeg.ffmpegArgsSort));
                    rtbScriptView.EndChange();

                    // Sort is Off
                    ScriptView.sort = true;
                    // Change Button Back to Inline
                    txblScriptSort.Text = "Inline";
                }

                // -------------------------
                // Has Been Edited
                // -------------------------
                else if (ScriptView.sort == false
                      && RemoveLineBreaks(ScriptView.GetScriptRichTextBoxContents(this))
                                   //.Replace(Environment.NewLine, "")
                                   //.Replace("\r\n", "")
                                   //.Replace("\u2028", "")
                                   //.Replace("\u000A", "")
                                   //.Replace("\u000B", "")
                                   //.Replace("\u000C", "")
                                   //.Replace("\u000D", "")
                                   //.Replace("\u0085", "")
                                   //.Replace("\u2028", "")
                                   //.Replace("\u2029", "")

                                   != FFmpeg.ffmpegArgs)
                {
                    MessageBox.Show("Cannot sort edited text.",
                                    "Notice",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);

                    return;
                }


                // -------------------------
                // Inline
                // -------------------------
                else if (ScriptView.sort == true)
                {
                    // CMD Arguments are from Script TextBox
                    FFmpeg.ffmpegArgs = RemoveLineBreaks(ScriptView.GetScriptRichTextBoxContents(this));
                    //.Replace(Environment.NewLine, "") //Remove Linebreaks
                    //.Replace("\n", "")
                    //.Replace("\r\n", "")
                    //.Replace("\u2028", "")
                    //.Replace("\u000A", "")
                    //.Replace("\u000B", "")
                    //.Replace("\u000C", "")
                    //.Replace("\u000D", "")
                    //.Replace("\u0085", "")
                    //.Replace("\u2028", "")
                    //.Replace("\u2029", "");

                    // Clear Old Text
                    ScriptView.ClearScriptView(this);
                    //ScriptView.scriptParagraph.Inlines.Clear();

                    // Write FFmpeg Args
                    rtbScriptView.Document = new FlowDocument(ScriptView.scriptParagraph);
                    rtbScriptView.BeginChange();
                    ScriptView.scriptParagraph.Inlines.Add(new Run(FFmpeg.ffmpegArgs));
                    rtbScriptView.EndChange();

                    // Sort is On
                    ScriptView.sort = false;
                    // Change Button Back to Sort
                    txblScriptSort.Text = "Sort";
                }
            }
        }


        /// <summary>
        ///     Clear Button
        /// </summary>
        private void btnScriptClear_Click(object sender, RoutedEventArgs e)
        {
            //ScriptView.scriptParagraph.Inlines.Clear();

            ScriptView.ClearScriptView(this);
        }


        /// <summary>
        ///     Run Button
        /// </summary>
        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            // CMD Arguments are from Script TextBox
            FFmpeg.ffmpegArgs = RemoveLineBreaks(ScriptView.GetScriptRichTextBoxContents(this));
                                //.Replace(Environment.NewLine, "") //Remove Linebreaks
                                //.Replace("\r\n", "")
                                //.Replace("\n", "")
                                //.Replace("\u2028", "")
                                //.Replace("\u000A", "")
                                //.Replace("\u000B", "")
                                //.Replace("\u000C", "")
                                //.Replace("\u000D", "")
                                //.Replace("\u0085", "")
                                //.Replace("\u2028", "")
                                //.Replace("\u2029", "")
                                //;

            // Run FFmpeg Arguments
            FFmpeg.FFmpegConvert(this);
        }

    }

}