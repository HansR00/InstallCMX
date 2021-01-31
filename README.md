# InstallCMX
 Multiplatform installer for CumulusMX

README 
InstallCMX, a commandline installer for CumulusMX.
Version 	: 	1.0.0
date		: 	22 januari 2021

Usage		:	InstallCMX [buildnumber]

How to  run: 

- Copy the InstallCMX.exe to any directory you want. This directory must contain the
  Archive(s) to install. The install procedure gives you the possibility to define 
  the Archive to install and the location where to install. 

- Copy the CumulusMX distribution zip(s) to that same directory. You may have more 
  than one distribution in the same directory. You can give the buildnumber to 
  install as commandline argument.
  
- Stop CumulusMX

- Run InstallCMX and confirm / fill in on the console where you wish to install (or 
  update) CMX. 

  * The default for Windows is C:\CumulusMX\ and for Linux it is : /home/CumulusMX. 
    The Installation directory can be modified.

  * NOTE: On Windows you run it as any other commandline executable and it is best
    to open a command window in which you start the installer. On Linux you run it
    on the commandline as "mono ./InstallCMX.exe". 

  * NOTE: In an existing installation with modified files, make sure they are in a 
    safe place. If they have the same name as files in the distribution they will 
    be overwritten.

- Start CumulusMX

- After the installation, there is a log file. Check the logfile to see everything
  has gone well.

- There is an ini file where you can control:
  * NormalMessageToConsole=true
  * TraceInfoLevel=Warning

Any issues and questions please direct at the CumulusUtils forum :
    https://cumulus.hosiene.co.uk/viewforum.php?f=44.

Modifications and additions on user request can be made (after approval).
