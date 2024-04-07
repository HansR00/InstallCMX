/*
 * Support - Part of InstallCMX
 *
 * © Copyright 2021 Hans Rottier <hans.rottier@gmail.com>
 *
 * Published under:
 * GNU General Public License as published by the Free Software Foundation;
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate : 13 januari 2021
 *              Initial release: version 1.0 
 *              
 * Environment: PC and Raspberry
 *              Windows and Linux (Raspbian)
 *              
 * Platform:    C# / Visual Studio
 *              
 */
using System;
using System.Diagnostics;
using System.IO;

namespace InstallCMX
{
    #region Support
    public class Support : IDisposable
    {
        #region declarations

        private readonly IniFile MyIni;         // that is: InstallCMX.ini

        private const string PressAnyKey = "Press any key to conitnue or Ctrl-C to abort...";
        private const string ChooseOrCtrlC = "Choose or press Ctrl-C to cancel...";

        #endregion

        #region Initialisation
        public Support()
        {
            if ( !File.Exists( "InstallCMX.ini" ) )
            {
                // All entries will be created when called for because I changed the IniFiles library. Search for: HAR
                StreamWriter of = new StreamWriter( "InstallCMX.ini" );
                of.Dispose();
            }

            MyIni = new IniFile( "InstallCMX.ini", this );

            // Init the logging
            //
            InitLogging();
        }

        #endregion

        #region Methods
        public string GetInstallCMXIniValue( string section, string key, string def ) => MyIni.GetValue( section, key, def );

        public void SetInstallCMXIniValue( string section, string key, string def ) => MyIni.SetValue( section, key, def );

        public static string Version()
        {
            string _ver;

            _ver = typeof( Support ).Assembly.GetName().Version.Major.ToString() + "." +
                          typeof( Support ).Assembly.GetName().Version.Minor.ToString() + "." +
                          typeof( Support ).Assembly.GetName().Version.Build.ToString();

            return $"Version {_ver} ";
        }

        public static string Copyright() => "(c) Hans Rottier";

        public void PressAnyKeyToContinue()
        {
            Console.WriteLine( PressAnyKey );
            Console.ReadKey();
        }

        public void PressChooseOrCtrlC() => Console.WriteLine( ChooseOrCtrlC );

        private void EndMyIniFile() { if ( MyIni != null ) { MyIni.Flush(); MyIni.Refresh(); } } // if (StringIni != null) { StringIni.Flush(); StringIni.Refresh(); } }

        #endregion


        #region Diagnostics

        TextWriterTraceListener ThisListener;
        TraceSwitch TraceSwitch;
        bool NormalMessageToConsole;

        public void InitLogging()
        {
            //ThisListener = new TextWriterTraceListener($"./InstallCMX.log");

            FileStream traceStream;

            try
            {
                traceStream = new FileStream( "./InstallCMX.log", FileMode.Create, FileAccess.ReadWrite );

                ThisListener = new TextWriterTraceListener( traceStream );
                Trace.Listeners.Add( ThisListener );  // Used for messages under the conditions of the Switch: None, Error, Warning, Information, Verbose
                Trace.AutoFlush = true;
            }
            catch ( Exception ex )
            {
                Console.WriteLine( $"Fatal - Can't create logfile, exiting - {ex.Message}" );
                PressAnyKeyToContinue();
                Environment.Exit( 0 );
            }

            TraceSwitch = new TraceSwitch( "CUTraceSwitch", "Tracing switch for CumulusUtils" )
            {
                Level = TraceLevel.Verbose
            };

            //LogTraceInfoMessage($"Initial {TraceSwitch} => Error: {TraceSwitch.TraceError}, Warning: {TraceSwitch.TraceWarning}, Info: {TraceSwitch.TraceInfo}, Verbose: {TraceSwitch.TraceInfo}");

            NormalMessageToConsole = GetInstallCMXIniValue( "InstallCMX", "NormalMessageToConsole", "false" ).Equals( "true" );   // Verbose, Information, Warning, Error, Off
            string thisTrace = GetInstallCMXIniValue( "InstallCMX", "TraceInfoLevel", "Warning" );   // Verbose, Information, Warning, Error, Off

            try
            {
                TraceSwitch.Level = (TraceLevel) Enum.Parse( typeof( TraceLevel ), thisTrace, true );
            }
            catch ( Exception e ) when ( e is ArgumentException || e is ArgumentNullException )
            {
                LogTraceErrorMessage( $"Initial: Exception parsing the TraceLevel - {e.Message}" );
                LogTraceErrorMessage( $"Initial: Setting level to Warning (default)." );
                TraceSwitch.Level = TraceLevel.Warning;
            }

            LogTraceInfoMessage( $"According to Inifile {thisTrace} => Error: {TraceSwitch.TraceError}, Warning: {TraceSwitch.TraceWarning}, Info: {TraceSwitch.TraceInfo}, Verbose: {TraceSwitch.TraceVerbose}, " );
        }

        public void LogDebugMessage( string message )
        {
            if ( NormalMessageToConsole ) Console.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss " ) + message );
            Debug.WriteLine( DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss " ) + message );
        }

        public void LogTraceErrorMessage( string message ) => Trace.WriteLineIf( TraceSwitch.TraceError, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss " ) + "Error " + message );
        public void LogTraceWarningMessage( string message ) => Trace.WriteLineIf( TraceSwitch.TraceWarning, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss " ) + "Warning " + message );
        public void LogTraceInfoMessage( string message ) => Trace.WriteLineIf( TraceSwitch.TraceInfo, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss " ) + "Information " + message );
        public void LogTraceVerboseMessage( string message ) => Trace.WriteLineIf( TraceSwitch.TraceVerbose, DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss " ) + "Verbose " + message );

        #endregion

        #region IDisposable CuSupport

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose( bool disposing )
        {
            if ( !disposedValue )
            {
                if ( disposing )
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                EndMyIniFile();
                ThisListener.Dispose();
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Support()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( false );
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose( true );
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable CuSupport
    }

    #endregion
} // Namespace