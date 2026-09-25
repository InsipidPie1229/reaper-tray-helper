@echo off
setlocal EnableExtensions

set "COMPILER=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%COMPILER%" set "COMPILER=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%COMPILER%" (
  echo .NET Framework C# compiler was not found.
  exit /b 1
)

set "ROOT=%~dp0"
set "ARTIFACTS=%ROOT%artifacts"
set "SRC=%ROOT%src\ReaperTrayHelper"
set "TESTS=%ROOT%tests"
if exist "%ARTIFACTS%" rmdir /s /q "%ARTIFACTS%"
mkdir "%ARTIFACTS%"

"%COMPILER%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"%ARTIFACTS%\ReaperTrayHelper.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "%SRC%\Program.cs" "%SRC%\UiText.cs" "%SRC%\AppSettings.cs" "%SRC%\StartupShortcutManager.cs" "%SRC%\ReaperProcessSelector.cs" "%SRC%\ReaperTrayContext.cs" "%SRC%\TrackHotkeys.cs" "%SRC%\ReaperOscBridge.cs"
if errorlevel 1 exit /b 1

"%COMPILER%" /nologo /target:exe /main:ReaperTrayHelperTests /out:"%ARTIFACTS%\ReaperTrayHelper.Tests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "%SRC%\Program.cs" "%SRC%\UiText.cs" "%SRC%\AppSettings.cs" "%SRC%\StartupShortcutManager.cs" "%SRC%\ReaperProcessSelector.cs" "%SRC%\ReaperTrayContext.cs" "%SRC%\TrackHotkeys.cs" "%SRC%\ReaperOscBridge.cs" "%TESTS%\ReaperTrayHelper.Tests.cs"
if errorlevel 1 exit /b 1

"%ARTIFACTS%\ReaperTrayHelper.Tests.exe" "%ARTIFACTS%\test-fixtures"
if errorlevel 1 exit /b 1

powershell -NoProfile -ExecutionPolicy Bypass -Command "& '%ROOT%scripts\Package.ps1' -Root '%ROOT%' -Artifacts '%ARTIFACTS%' -Version '1.2.0-rc.1'"
exit /b %errorlevel%
