
@echo off
set DEFHOMEDRIVE=d:
set DEFHOMEDIR=%DEFHOMEDRIVE%%HOMEPATH%
set HOMEDIR=
set HOMEDRIVE=%CD:~0,2%

set RELEASEDIR=d:\Users\jbb\release
set ZIP="c:\Program Files\7-zip\7z.exe"
echo Default homedir: %DEFHOMEDIR%

rem set /p HOMEDIR= "Enter Home directory, or <CR> for default: "

if "%HOMEDIR%" == "" (
set HOMEDIR=%DEFHOMEDIR%
)
echo %HOMEDIR%

SET _test=%HOMEDIR:~1,1%
if "%_test%" == ":" (
set HOMEDRIVE=%HOMEDIR:~0,2%
)

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

rem set /p newVERSION= "Enter version: "
rem if "%newVERSION" NEQ "" set VERSION=%newVERSION%

mkdir %HOMEDIR%\install\GameData\JanitorsCloset
mkdir %HOMEDIR%\install\GameData\JanitorsCloset\Textures
mkdir %HOMEDIR%\install\GameData\JanitorsCloset\PluginData
mkdir %HOMEDIR%\install\GameData\JanitorsCloset\Plugins

copy bin\Release\JanitorsCloset.dll %HOMEDIR%\install\GameData\JanitorsCloset\Plugins
copy ..\GameData\JanitorsCloset\Textures\* %HOMEDIR%\install\GameData\JanitorsCloset\Textures

copy /Y "License.txt" "%HOMEDIR%\install\GameData\JanitorsCloset"
copy /Y "..\README.md" "%HOMEDIR%\install\GameData\JanitorsCloset"
copy /Y MiniAVC.dll  "%HOMEDIR%\install\GameData\JanitorsCloset"
copy /Y JanitorsCloset.version  "%HOMEDIR%\install\GameData\JanitorsCloset"
copy /y ..\Gamedata\JanitorsCloset\PluginData\JCBlacklist.cfg %HOMEDIR%\install\GameData\JanitorsCloset\PluginData
copy /y ..\Gamedata\JanitorsCloset\PluginData\JanitorsClosetDefault.cfg %HOMEDIR%\install\GameData\JanitorsCloset\PluginData

%HOMEDRIVE%
cd %HOMEDIR%\install

set FILE="%RELEASEDIR%\JanitorsCloset-%VERSION%.zip"
IF EXIST %FILE% del /F %FILE%
%ZIP% a -tzip %FILE% Gamedata\JanitorsCloset
