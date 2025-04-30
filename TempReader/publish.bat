dotnet publish -c Release -r linux-arm
rd /S /Q .\bin\Release\net7.0\linux-arm\publishTempReader
move .\bin\Release\net7.0\linux-arm\publish .\bin\Release\net7.0\linux-arm\publishTempReader