
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


type JanitorsCloset.version
set /p VERSION= "Enter version: "

mkdir %HOMEDIR%\install\GameData\JanitorsCloset
mkdir %HOMEDIR%\install\GameData\JanitorsCloset\Textures
mkdir %HOMEDIR%\install\GameData\JanitorsCloset\PluginData
mkdir %HOMEDIR%\install\GameData\JanitorsCloset\Plugins

copy bin\Release\JanitorsCloset.dll %HOMEDIR%\install\GameData\JanitorsCloset\Plugins
copy ..\GameData\JanitorsCloset\Textures\* %HOMEDIR%\install\GameData\JanitorsCloset\Textures

copy /Y "License.txt" "%HOMEDIR%\install\GameData\JanitorsCloset"
copy /Y "..\README.md" "%HOMEDIR%\install\GameData\JanitorsCloset"
copy /Y MiniAVC.dll  "%HOMEDIR%\install\GameData\JanitorsCloset"


%HOMEDRIVE%
cd %HOMEDIR%\install

set FILE="%RELEASEDIR%\JanitorsCloset-%VERSION%.zip"
IF EXIST %FILE% del /F %FILE%
%ZIP% a -tzip %FILE% Gamedata\JanitorsCloset
