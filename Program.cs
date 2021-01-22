/*
 * InstallCMX - A multiplatform installer for CumulusMX
 *
 * © Copyright 2021 Hans Rottier <hans.rottier@gmail.com>
 *
 * License: GNU General Public License as published by the Free Software Foundation;
 * 
 * Author:      Hans Rottier <hans.rottier@gmail.com>
 * Project:     CumulusUtils meteo-wagenborgen.nl
 * Date:        21 january 2021
 *              
 * Environment: PC and Raspberry Pi
 *              Windows / Linux 
 * Platform:    C# / Visual Studio
 * 
 * Files:       Main.cs
 *              IniFile.cs
 *              Support.cs
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;

namespace InstallCMX
{
  public class Program
  {
    Support Sup;

    const string DefaultUnixInstallDir = "/home";
    const string DefaultWindowsInstallDir = "c:\\";
    const string CMXstring = "CumulusMX.exe";
    const string CMXDirstring = "CumulusMX/";

    bool FirstInstall = false;
    bool FailedExtraction = false;

    string InstallDirectory;
    string BuildToInstall;
    string ArchiveToInstall;

    string entryName = "";
    string destinationPath = "";

    static void Main(string[] args)
    {
      Program p = new Program();
      p.RealMain(args);
    }

    private void RealMain(string[] args)
    {
      // Initialise, setup logging
      Sup = new Support();
      Sup.LogDebugMessage($"CMX multiplatform installer version {Support.Version()} - {Support.Copyright()}");

      CommandLineArgs(args);        // Set the version to install
      ArchiveToInstall = SetArchiveToInstall();
      SetInstallationDir();

      if (string.IsNullOrEmpty(ArchiveToInstall) )
      {
        Sup.LogDebugMessage("No Archive to install found... nothing to do... Exiting.");
        Environment.Exit(0); // Nothing to Do
      }

      //
      // We know the installation directory and the build number. We found the Archive (if it is empty => Exit!!)
      // So we ask for confirmation of the user and do it.
      //
      Console.WriteLine($"The current install directory is : {InstallDirectory} [Y]/N :");
      Sup.PressChooseOrCtrlC();

      ConsoleKeyInfo thisKey = Console.ReadKey(); Console.WriteLine("");

      if (thisKey.Key != ConsoleKey.Y && thisKey.Key != ConsoleKey.Enter)
      {
        Console.Write("Please Enter the destination directory : ");
        InstallDirectory = Console.ReadLine();
      }

      InstallDirectory = Path.GetFullPath(InstallDirectory);
      ArchiveToInstall = Path.GetFullPath(ArchiveToInstall);

      if (!InstallDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())) InstallDirectory += Path.DirectorySeparatorChar;

      try
      {
        using (ZipArchive archive = ZipFile.OpenRead(ArchiveToInstall))
        {
          foreach (ZipArchiveEntry entry in archive.Entries)
          {
            entryName = entry.FullName;
            destinationPath = Path.Combine(InstallDirectory, entryName.Remove(0, CMXDirstring.Length));

            if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT))
              if (destinationPath[destinationPath.Length - 1] == '/') destinationPath = destinationPath.Substring(0, destinationPath.Length - 1) + "\\";

            if (destinationPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
              // A directory. Does it exist or does it have to be made?
              if (Directory.Exists(destinationPath))
              {
                continue;
              }
              else
              {
                Sup.LogDebugMessage($"Creating Directory : {destinationPath}");
                Directory.CreateDirectory(destinationPath);
                continue;
              }
            }
            else // Is file
            {
              Sup.LogDebugMessage($"Extracting : {entry.FullName} => {destinationPath}");
              entry.ExtractToFile(destinationPath, true);
            }
          } // Loop over the archive entries
        } // Use the Archive to extract
      } // try block
      catch(Exception e) when (e is SecurityException || e is NotSupportedException || e is ObjectDisposedException || e is ArgumentException || e is ArgumentNullException || e is PathTooLongException || 
                                e is DirectoryNotFoundException || e is IOException || e is UnauthorizedAccessException || e is FileNotFoundException || e is NotSupportedException || 
                                e is InvalidDataException)
      {
        Sup.LogTraceErrorMessage($"Failed Extracting : {entryName} to {destinationPath}");
        Sup.LogTraceErrorMessage($"Exception message : {e.Message}");
        FailedExtraction = true;
      } // End of catching exceptions, fall through and get to the end of program

      // Do all kind of other things e.g.
      //    1) Setup CMX elementary if it is a CMX initial install
      //    2) Setup the start/stop systemd when on Linux
      //    3) ??? etc...
      //

      // Done
      if (FailedExtraction)
      {
        Sup.LogDebugMessage("Installation of CMX Failed.");
        Sup.LogDebugMessage("Please check your logfile, destination directory and/or user rights.");
      }
      else 
      {
        Sup.LogDebugMessage("Installation of CMX completed. ");
        Sup.LogDebugMessage("If it is your first installation, please setup CumulusMX according to the instructions and complete the startup/shutdown setup");
        Sup.LogDebugMessage("If it is an Update of your installation, just restart CumulusMX.");
      }

      Console.WriteLine("Done");
      Sup.LogDebugMessage("Done");
      Sup.Dispose();
    }

    void SetInstallationDir()
    {
      List<string> InstallDirs;

      // Find the OS we are dealing with
      //
      // Now do the OS dependent stuff
      //
      if (Environment.OSVersion.Platform.Equals(PlatformID.Unix))
      {
        InstallDirectory = DefaultUnixInstallDir;
        Sup.LogDebugMessage($"Running on : {PlatformID.Unix}");
      }
      else if (Environment.OSVersion.Platform.Equals(PlatformID.Win32NT))
      {
        InstallDirectory = DefaultWindowsInstallDir;
        Sup.LogDebugMessage($"Running on : {PlatformID.Win32NT}");
      }
      else
      {
        //other OSs not implemented yet
        Sup.LogDebugMessage($" Your OS {Environment.OSVersion.Platform} is not yet supported.");
        Sup.LogDebugMessage($" Please file a request - exiting.");
        Sup.PressAnyKeyToContinue();
        Environment.Exit(0);
      }

      // Do some Install init stuff
      //
      // Check where CMX is installed. If not installed then set FirstInstall = true; otherwise choose one of the installs or a new one

      InstallDirs = SearchForCMX();
      InstallDirectory = ConfirmInstallDir(InstallDirs);

      Sup.LogDebugMessage($"{(FirstInstall ? "Installing" : "Updating")} CMX in {InstallDirectory}\n");
    } // Initialise

    List<string> SearchForCMX()
    {
      List<string> resultDirs = new List<string>();

      string[] tmpDirs;
      string[] resultFiles = null;

      // Path.DirectorySeparatorChar.ToString()
      tmpDirs = Directory.GetDirectories("/");

      for (int i = 0; i < tmpDirs.Length; i++)
      {
        try
        {
          // Apparently traversing the /sys directory on Linux does not work but it does not generate an exception.
          // For the moment, just skip it: CMX is not allowed to be installed in /sys
          //
          if (tmpDirs[i].Contains("/sys") || tmpDirs[i].Contains("/proc")) continue;

          resultFiles = Directory.GetFiles(tmpDirs[i], "CumulusMX.exe", SearchOption.AllDirectories);

          if (resultFiles.Length > 0)
          {
            // Remove the top CumulusMX string from the Path. On Windows it does not matter but on Linux
            // the Unzip will create /CumulusMX/CumulusMX iso /CumulusMX. This is of course undesirable
            //
            for (int j = 0; j < resultFiles.Length; j++)
            {
              string tmp = Path.GetFullPath(resultFiles[j]).Remove(Path.GetFullPath(resultFiles[j]).IndexOf(CMXstring));
              resultDirs.Add(tmp);
            }
          }
        }
        catch (Exception e) when (e is InvalidOperationException)
        {
          // We need to conitnue to search all parts of the drive where we have access
          Sup.LogTraceErrorMessage($"Invalid Operation reading Directory {tmpDirs[i]}");
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          // We need to conitnue to search all parts of the drive where we have access
          Sup.LogTraceErrorMessage($"No authorisation reading Directory {tmpDirs[i]}");
        }
        catch (Exception e) when (e is ArgumentException || e is ArgumentNullException || e is DirectoryNotFoundException || e is PathTooLongException || e is IOException)
        {
          // We're done
          Sup.LogTraceErrorMessage($"Something Wrong while reading directory {tmpDirs[i]}");
          Sup.LogTraceErrorMessage($"May noy be able to install correctly.");
          break;
        }
      }

      return resultDirs;
    } // SearchForCMX

    string ConfirmInstallDir(List<string> InstallDirs)
    {
      string thisInstallDir = "";

      if (InstallDirs.Count > 1)
      {
        int MaxSelect = -1, Selection = -1;

        // We have multiple installations, ask which to install/upgrade
        Console.WriteLine($"I found multiple installations:");
        for (int i = 0; i < InstallDirs.Count; i++)
        {
          Console.WriteLine($"  {i + 1}) {InstallDirs[i]}");
          MaxSelect = i;
        }

        while (Selection < 0 || Selection > MaxSelect)
        {
          string thisLine;

          Console.WriteLine("Which Installation do you wish to Update : ");
          Sup.PressChooseOrCtrlC();
          thisLine = Console.ReadLine(); // Console.WriteLine("");

          try
          {
            Selection = Convert.ToInt32(thisLine) - 1;
            thisInstallDir = InstallDirs[Selection];
          }
          catch (Exception e) when (e is OverflowException || e is ArgumentNullException || e is FormatException || e is IndexOutOfRangeException || e is ArgumentOutOfRangeException)
          {
            Console.WriteLine("Error in number...  Try again.");
          }
        } // While selection
      }
      else if (InstallDirs.Count == 1)
      {
        thisInstallDir = InstallDirs[0];
        Console.Write($"Found an installation on: {InstallDirectory}");
      }
      else
      {
        // Nothing found, First installation, use the default installdirectory
        thisInstallDir = InstallDirectory;
        FirstInstall = true;
      }

      return thisInstallDir;
    } // ConfirmInstallDir

    string SetArchiveToInstall()
    {
      string[] Archives;
      string thisArchive;

      // Find the zip in the current directory 
      //   If multiple finds then propose the newest, modifiable by the user
      //   In later instance one could download the zip (version specific or current) if none is found
      //   Return string to store in ArchiveToInstall

      if (string.IsNullOrEmpty(BuildToInstall))
      {
        Archives = Directory.GetFiles(".", "CumulusMXDist*.zip");

        thisArchive = SelectArchive(Archives);
      }
      else
      {
        string ArchiveName = $"CumulusMXDist{BuildToInstall}.zip";
        Archives = Directory.GetFiles(".", ArchiveName);

        thisArchive = Archives[0];
      }

      return thisArchive;
    }

    string SelectArchive(string[] Archives)
    {
      string thisArchive = "";

      if (Archives.Length > 1)
      {
        int MaxSelect = -1, Selection = -1;

        // We have multiple installations, ask which to install/upgrade
        Console.WriteLine($"I found multiple Archives :");
        for (int i = 0; i < Archives.Length; i++)
        {
          Console.WriteLine($"  {i + 1}) {Archives[i]}");
          MaxSelect = i;
        }

        while (Selection < 0 || Selection > MaxSelect)
        {
          string thisLine;

          Console.WriteLine("Which Archive do you wish to Install : ");
          Sup.PressChooseOrCtrlC();
          thisLine = Console.ReadLine(); // Console.WriteLine("");

          try
          {
            Selection = Convert.ToInt32(thisLine) - 1;
            thisArchive = Archives[Selection];
          }
          catch (Exception e) when (e is OverflowException || e is ArgumentNullException || e is FormatException || e is IndexOutOfRangeException || e is ArgumentOutOfRangeException)
          {
            Console.WriteLine("Error in number...  Try again.");
          }
        } // While selection
      }
      else if (Archives.Length == 1)
      {
        thisArchive = Archives[0];
        Console.Write($"Found one Archive to install: {thisArchive}");
      }

      return thisArchive;
    }


    private void CommandLineArgs(string[] args)
    {
      Sup.LogDebugMessage("CommandLineArgs : starting");

      if (args.Length > 0)
      {
        foreach (string s in args)
        {
          // So if you give 10 arguments, tha last is taken as the build number. No checks at all (so far)
          Sup.LogDebugMessage($"CommandLineArgs : Build number to install - {s}");
          BuildToInstall = s;
        }
      }
      else
      {
        BuildToInstall = "";
      }
    } // Commandline Argument(s)
  } // Class Program
} // Namespace
