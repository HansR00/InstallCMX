/* � Copyright 2021 Hans Rottier<hans.rottier@gmail.com>
 *
 * Published under:
 *GNU General Public License as published by the Free Software Foundation;
 *
 *Author:      Hans Rottier<hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Dates:       Startdate: 13 januari 2021
 * Initial release: version 1.0
 *
 *Environment: PC and Raspberry
 *              Windows and Linux (Raspbian)
 *
 *Platform:    C# / Visual Studio
 *
 */
// **************************
// *** IniFile class V1.0 *** // Modified
// **************************
// *** (C)2009 S.T.A. snc ***
// **************************
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace InstallCMX
{
    internal class IniFile
    {
        #region "Declarations"

        // *** Lock for thread-safe access to file and local cache ***
        private readonly object m_Lock = new object();

        // *** File name ***
        private string m_FileName;

        internal string FileName {
            get {
                return m_FileName;
            }
        }

        // *** Lazy loading flag ***
        private bool m_Lazy;

        // *** Local cache ***
        private readonly Dictionary<string, Dictionary<string, string>> m_Sections = [];    // new Dictionary<string, Dictionary<string, string>>();

        // *** Local cache modified flag ***
        private bool m_CacheModified;
        readonly Support Sup;

        #endregion "Declarations"

        #region "Methods"


        // *** Constructor ***
        public IniFile( string FileName, Support s )
        {
            Sup = s;
            Initialize( FileName, false );
        }

        public IniFile( string FileName, Support s, bool Lazy )
        {
            Sup = s;
            Initialize( FileName, Lazy );
        }

        // *** Initialization ***
        private void Initialize( string FileName, bool Lazy )
        {
            m_FileName = FileName;
            m_Lazy = Lazy;
            if ( !m_Lazy ) Refresh();
        }

        // *** Read file contents into local cache ***
        internal void Refresh()
        {
            lock ( m_Lock )
            {
                StreamReader sr = null;
                FileStream fs = null;
                try
                {
                    // *** Clear local cache ***
                    m_Sections.Clear();

                    // *** Open the INI file ***
                    try
                    {
                        fs = new FileStream( m_FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
                        sr = new StreamReader( fs );
                        //            sr = new StreamReader(m_FileName);
                    }
                    catch ( FileNotFoundException )
                    {
                        return;
                    }

                    // *** Read up the file content ***
                    Dictionary<string, string> CurrentSection = null;
                    string s;
                    while ( ( s = sr.ReadLine() ) != null )
                    {
                        s = s.Trim();

                        // *** Check for section names ***
                        if ( s.StartsWith( "[", StringComparison.OrdinalIgnoreCase ) && s.EndsWith( "]", StringComparison.OrdinalIgnoreCase ) )
                        {
                            if ( s.Length > 2 )
                            {
                                string SectionName = s.Substring( 1, s.Length - 2 );

                                // *** Only first occurrence of a section is loaded ***
                                if ( m_Sections.ContainsKey( SectionName ) )
                                {
                                    CurrentSection = null;
                                }
                                else
                                {
                                    CurrentSection = [];     // new Dictionary<string, string>();
                                    m_Sections.Add( SectionName, CurrentSection );
                                }
                            }
                        }
                        else if ( CurrentSection != null )
                        {
                            // *** Check for key+value pair ***
                            int i;
                            if ( ( i = s.IndexOf( '=' ) ) > 0 )
                            {
                                int j = s.Length - i - 1;
                                string Key = s.Substring( 0, i ).Trim();
                                if ( Key.Length > 0 )
                                {
                                    // *** Only first occurrence of a key is loaded ***
                                    if ( !CurrentSection.ContainsKey( Key ) )
                                    {
                                        string Value = ( j > 0 ) ? ( s.Substring( i + 1, j ).Trim() ) : ( "" );
                                        CurrentSection.Add( Key, Value );
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // *** Cleanup: close file ***
                    sr?.Dispose();
                    sr = null;
                    fs.Close();
                }
            }
        }

        // *** Flush local cache content ***
        internal void Flush()
        {
            Sup.LogTraceInfoMessage( $" Ini Flush cache modif:{m_CacheModified}, {m_FileName}" );

            lock ( m_Lock )
            {
                // *** If local cache was not modified, exit ***
                if ( !m_CacheModified ) return;
                m_CacheModified = false;

                // *** Open the file ***
                using ( StreamWriter sw = new StreamWriter( m_FileName, false, Encoding.UTF8 ) )
                {
                    // *** Cycle on all sections ***
                    bool First = false;
                    foreach ( KeyValuePair<string, Dictionary<string, string>> SectionPair in m_Sections )
                    {
                        Dictionary<string, string> Section = SectionPair.Value;
                        if ( First ) sw.WriteLine();
                        First = true;

                        // *** Write the section name ***
                        sw.Write( '[' );
                        sw.Write( SectionPair.Key );
                        sw.WriteLine( ']' );

                        // *** Cycle on all key+value pairs in the section ***
                        foreach ( KeyValuePair<string, string> ValuePair in Section )
                        {
                            Sup.LogTraceVerboseMessage( $" Ini Flush writing => {SectionPair.Key}: {ValuePair.Key}/{ValuePair.Value}" );
                            // *** Write the key+value pair ***
                            sw.Write( ValuePair.Key );
                            sw.Write( '=' );
                            sw.WriteLine( ValuePair.Value );
                        }
                    }
                }
            }
        }

        // *** Read a value from local cache ***
        internal string GetValue( string SectionName, string Key, string DefaultValue )
        {
            // *** Lazy loading ***
            if ( m_Lazy )
            {
                m_Lazy = false;
                Refresh();
            }

            lock ( m_Lock )
            {
                // *** Check if the section exists ***
                if ( !m_Sections.TryGetValue( SectionName, out Dictionary<string, string> Section ) )
                {
                    // HAR: Section/key does not exist so make it
                    SetValue( SectionName, Key, DefaultValue );
                    return DefaultValue;
                }

                // *** Check if the key exists ***
                if ( !Section.TryGetValue( Key, out string Value ) )
                {
                    // HAR:  Key does not exist so make it
                    SetValue( SectionName, Key, DefaultValue );
                    return DefaultValue;
                }

                // *** Return the found value ***
                return Value;
            }
        }

        // *** Insert or modify a value in local cache ***
        internal void SetValue( string SectionName, string Key, string Value )
        {
            // *** Lazy loading ***
            if ( m_Lazy )
            {
                m_Lazy = false;
                Refresh();
            }

            lock ( m_Lock )
            {
                // *** Flag local cache modification ***
                m_CacheModified = true;

                // *** Check if the section exists ***
                if ( !m_Sections.TryGetValue( SectionName, out Dictionary<string, string> Section ) )
                {
                    // *** If it doesn't, add it ***
                    Section = [];   // new Dictionary<string, string>();
                    m_Sections.Add( SectionName, Section );
                }

                // *** Modify the value ***
                _ = Section.Remove( Key );
                Section.Add( Key, Value );
            }
        }


        // *** Encode byte array ***
        private static string EncodeByteArray( byte[] Value )
        {
            if ( Value == null ) return null;

            StringBuilder sb = new StringBuilder();
            foreach ( byte b in Value )
            {
                string hex = Convert.ToString( b, 16 );
                int l = hex.Length;
                if ( l > 2 )
                {
                    sb.Append( hex.Substring( l - 2, 2 ) );
                }
                else
                {
                    if ( l < 2 ) sb.Append( '0' );
                    sb.Append( hex );
                }
            }
            return sb.ToString();
        }


        // *** Decode byte array ***
        private static byte[] DecodeByteArray( string Value )
        {
            if ( Value == null ) return null;

            int l = Value.Length;
            if ( l < 2 ) return [];    // Array.Empty<byte>();

            l /= 2;
            byte[] Result = new byte[ l ];
            for ( int i = 0; i < l; i++ ) Result[ i ] = Convert.ToByte( Value.Substring( i * 2, 2 ), 16 );
            return Result;
        }

        // *** Getters for various types ***
        internal bool GetValue( string SectionName, string Key, bool DefaultValue )
        {
            string StringValue = GetValue( SectionName, Key, DefaultValue.ToString( System.Globalization.CultureInfo.InvariantCulture ) );
            if ( int.TryParse( StringValue, out int Value ) ) return ( Value != 0 );
            return DefaultValue;
        }

        internal int GetValue( string SectionName, string Key, int DefaultValue )
        {
            string StringValue = GetValue( SectionName, Key, DefaultValue.ToString( CultureInfo.InvariantCulture ) );
            if ( int.TryParse( StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int Value ) ) return Value;
            return DefaultValue;
        }

        internal double GetValue( string SectionName, string Key, double DefaultValue )
        {
            string StringValue = GetValue( SectionName, Key, DefaultValue.ToString( CultureInfo.InvariantCulture ) );
            if ( double.TryParse( StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double Value ) ) return Value;
            return DefaultValue;
        }

        internal byte[] GetValue( string SectionName, string Key, byte[] DefaultValue )
        {
            string StringValue = GetValue( SectionName, Key, EncodeByteArray( DefaultValue ) );
            try
            {
                return DecodeByteArray( StringValue );
            }
            catch ( FormatException )
            {
                return DefaultValue;
            }
        }

        internal DateTime GetValue( string SectionName, string Key, DateTime DefaultValue )
        {
            string StringValue = GetValue( SectionName, Key, DefaultValue.ToString( CultureInfo.InvariantCulture ) );
            if ( DateTime.TryParse( StringValue, out DateTime Value ) ) return Value;
            return DefaultValue;
        }

        // *** Setters for various types ***
        internal void SetValue( string SectionName, string Key, bool Value )
        {
            SetValue( SectionName, Key, ( Value ) ? ( "1" ) : ( "0" ) );
        }

        internal void SetValue( string SectionName, string Key, int Value )
        {
            SetValue( SectionName, Key, Value.ToString( CultureInfo.InvariantCulture ) );
        }

        internal void SetValue( string SectionName, string Key, double Value )
        {
            SetValue( SectionName, Key, Value.ToString( CultureInfo.InvariantCulture ) );
        }

        internal void SetValue( string SectionName, string Key, byte[] Value )
        {
            SetValue( SectionName, Key, EncodeByteArray( Value ) );
        }

        internal void SetValue( string SectionName, string Key, DateTime Value )
        {
            // write datetimes in ISO 8601 ("sortable")
            SetValue( SectionName, Key, Value.ToString( "s" ) );
        }

        #endregion "Methods"
    }
}