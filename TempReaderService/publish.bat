REM https://www.hanselman.com/blog/RemoteDebuggingWithVSCodeOnWindowsToARaspberryPiUsingNETCoreOnARM.aspx
dotnet publish -r linux-arm /p:ShowLinkerSizeComparison=true
pushd .\bin\Debug\netcoreapp3.1\linux-arm\publish
C:\"Program Files"\PuTTY\pscp -pw raspberry -v -r .\* pi@192.168.0.76:/home/pi/dotnet/TempReaderService
popd