REM https://www.hanselman.com/blog/RemoteDebuggingWithVSCodeOnWindowsToARaspberryPiUsingNETCoreOnARM.aspx
dotnet publish -r linux-arm /p:ShowLinkerSizeComparison=true --self-contained
pushd .\bin\Debug\net7.0\linux-arm\publish
C:\"Program Files"\PuTTY\pscp -pw raspberry -v -r .\* stefano@192.168.0.76:/home/stefano/TempReaderService
popd