cd "c:\Users\alexey.evlampiev\source\repos\AlexeyEvlampiev\Solitons\src\Solitons.Postgres.PgUp.Native\"
cls
dotnet publish -c Release -r win-x64   --self-contained true /p:PublishSingleFile=true
#dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true