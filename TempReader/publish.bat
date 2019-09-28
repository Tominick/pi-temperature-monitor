dotnet publish -c Release -r linux-arm
rd /S /Q .\bin\Release\netcoreapp2.2\linux-arm\publishTempReader
move .\bin\Release\netcoreapp2.2\linux-arm\publish .\bin\Release\netcoreapp2.2\linux-arm\publishTempReader