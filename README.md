# polygonstats

PolygonStats is a backend server for polygon# to connect to. 

### Requirement
.Net 7: https://dotnet.microsoft.com/download/dotnet/7.0

Microsoft Visual C++ Redistributable für Visual Studio 2015, 2017, 2019 und 2022: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170

## Run

Compile the code or for windows get the compiled binary from the release page.

Use the `Config.json` to change the settings.

Run the `PolygonStats.exe` as admin! (If it's not working, try to install the hosting bundle of .net 7)

Connect with polygon and open `http://{your ip}:8888`.

## For Map export
https://raw.githubusercontent.com/Map-A-Droid/MAD/master/scripts/SQL/rocketmap.sql
If you are not using mad, add a default event to trs_event
