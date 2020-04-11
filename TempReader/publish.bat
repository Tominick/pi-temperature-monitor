dotnet publish -c Release -r linux-arm
rd /S /Q .\bin\Release\netcoreapp3.1\linux-arm\publishTempReader
move .\bin\Release\netcoreapp3.1\linux-arm\publish .\bin\Release\netcoreapp3.1\linux-arm\publishTempReader