dotnet publish -c Release -r linux-arm  --self-contained
del .\bin\release\net7.0\linux-arm\publish\MyDatabase.db
