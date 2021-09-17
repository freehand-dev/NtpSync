# TimeSync
> TimeSync - windows service for synchronizing system clock  from NTP server

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-brightgreen.svg)](COPYING)
[![Build Status](https://dev.azure.com/oleksandr-nazaruk/openprocurement-agent/_apis/build/status/openprocurement-agent-CI)](https://dev.azure.com/oleksandr-nazaruk/openprocurement-agent/_apis/build/status/openprocurement-agent-CI)


## Compile and install
Once you have installed all the dependencies, get the code:

	git clone https://github.com/freehand-dev/NtpSync.git
	cd NtpSync

Then just use:

	mkdir "%ProgramData%\FreeHand\TimeSync\bin\"
	dotnet restore
	dotnet build
	dotnet publish --runtime win-x64 --output %ProgramData%\FreeHand\TimeSync\bin\ -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true .\TimeSync

Install as Windows Service
   
	sc create TimeSyncSvc 
    binPath= "%ProgramData%\FreeHand\TimeSync\bin\TimeSync.exe" 
    DisplayName= "FreeHand TimeSync" 
    start= auto
    description= "Service for synchronization system time from remote NTP server"


## Configure and start
To start the service, you can use the `TimeSync` executable as the application or `sc start TimeSync` as a Windows service. For configuration you can edit a configuration file:

	notepad.exe %ProgramData%\FreeHand\TimeSync\TimeSync.conf

The content of the file will be the following one

    [Logging:LogLevel]
    Default=Information
    Microsoft=Information
    Microsoft.Hosting.Lifetime=Information
    TimeSync.Services.TimeSyncService=Information

    [Global]
    # SystemClockOffset=<milliseconds>
    # Default value is 0
    SystemClockOffset=60000

    [NtpClient]
    # UpdateInterval=<seconds>
    # Default value is 300
    UpdateInterval=10

    #
    Peers:0=time.windows.com
    Peers:1=time.windows.com

    # MaxPosPhaseCorrection=<milliseconds>
    # Default value is 5000
    MaxPosPhaseCorrection=5000

    # MaxNegPhaseCorrection=<milliseconds>
    # Default value is 5000
    MaxNegPhaseCorrection=5000

    # MaxAllowedOffset=<milliseconds>
    # Default value is 40
    MaxAllowedPhaseOffset=0
