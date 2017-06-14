
@echo off

set RELEASEDIR=d:\Users\jbb\release
set ZIP="c:\Program Files\7-zip\7z.exe"


set VERSIONFILE=JanitorsCloset.version
rem The following requires the JQ program, available here: https://stedolan.github.io/jq/download/
c:\local\jq-win64  ".VERSION.MAJOR" %VERSIONFILE% >tmpfile
set /P major=<tmpfile

c:\local\jq-win64  ".VERSION.MINOR"  %VERSIONFILE% >tmpfile
set /P minor=<tmpfile

c:\local\jq-win64  ".VERSION.PATCH"  %VERSIONFILE% >tmpfile
set /P patch=<tmpfile

c:\local\jq-win64  ".VERSION.BUILD"  %VERSIONFILE% >tmpfile
set /P build=<tmpfile
del tmpfile
set VERSION=%major%.%minor%.%patch%
if "%build%" NEQ "0"  set VERSION=%VERSION%.%build%

type JanitorsCloset.version

echo Version:  %VERSION%


copy JanitorsCloset\bin\Release\JanitorsCloset.dll GameData\JanitorsCloset\Plugins

copy /Y "License.txt" "GameData\JanitorsCloset"
copy /Y "README.md" "GameData\JanitorsCloset"
copy /Y ..\MiniAVC.dll  "GameData\JanitorsCloset"
copy /Y JanitorsCloset.version  "GameData\JanitorsCloset"

set FILE="%RELEASEDIR%\JanitorsCloset-%VERSION%.zip"
IF EXIST %FILE% del /F %FILE%
%ZIP% a -tzip %FILE% Gamedata\JanitorsCloset
