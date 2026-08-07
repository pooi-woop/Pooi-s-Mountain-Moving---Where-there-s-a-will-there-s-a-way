@echo off
setlocal EnableExtensions
chcp 65001 >nul

REM ============================================================
REM  Pooi's Mountain Moving - 一键构建脚本
REM  作用：编译 C# 并把 PMM.MountainMoving.dll 拷到 Assemblies\
REM  前提：本机装有 RimWorld（用于引用其 DLL），以及 .NET SDK 或 Visual Studio
REM ============================================================

set "PROJ=%~dp0Source\MountainMoving\MountainMoving.csproj"
set "OUTDLL=%~dp0Source\MountainMoving\bin\Release\net472\PMM.MountainMoving.dll"
set "ASMDIR=%~dp0Assemblies"

REM ---- 1) 定位 RimWorld 安装目录 ----
set "RW=%RIMWORLD_PATH%"
if not defined RW (
  for %%P in (
    "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
    "C:\Program Files\Steam\steamapps\common\RimWorld"
    "D:\Steam\steamapps\common\RimWorld"
    "D:\steam\steamapps\common\RimWorld"
    "E:\Steam\steamapps\common\RimWorld"
    "E:\steam\steamapps\common\RimWorld"
    "F:\Steam\steamapps\common\RimWorld"
    "G:\Steam\steamapps\common\RimWorld"
  ) do (
    if not defined RW if exist "%%~P\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" set "RW=%%~P"
  )
)

if not defined RW (
  echo [错误] 找不到 RimWorld 安装目录。
  echo        请设置环境变量 RIMWORLD_PATH 为你的 RimWorld 根目录后重试。
  echo        例如: set RIMWORLD_PATH=D:\Steam\steamapps\common\RimWorld
  pause
  exit /b 1
)
echo [信息] RimWorld 目录: %RW%

REM ---- 2) 编译 ----
echo [信息] 开始编译 (Release, net472) ...
where dotnet >nul 2>nul
if %errorlevel%==0 (
  dotnet build "%PROJ%" -c Release /p:RimWorldPath="%RW%"
) else (
  echo [信息] 未找到 dotnet，尝试使用 Visual Studio 的 msbuild ...
  for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do set "MSB=%%i"
  if not defined MSB (
    echo [错误] 既找不到 dotnet 也找不到 msbuild。请安装 .NET SDK 或 Visual Studio 2022。
    pause
    exit /b 1
  )
  "%MSB%" "%PROJ%" /p:Configuration=Release /p:RimWorldPath="%RW%" /restore
)
if errorlevel 1 (
  echo [错误] 编译失败，请查看上方报错。
  pause
  exit /b 1
)

REM ---- 3) 拷贝 DLL 到 Assemblies ----
if not exist "%ASMDIR%" mkdir "%ASMDIR%"
if not exist "%OUTDLL%" (
  echo [错误] 未找到编译产物: %OUTDLL%
  pause
  exit /b 1
)
copy /Y "%OUTDLL%" "%ASMDIR%\" >nul

echo.
echo [成功] 已生成 %ASMDIR%\PMM.MountainMoving.dll
echo        把整个 "Pooi-s-Mountain-Moving" 文件夹放进 RimWorld 的 Mods 目录即可在游戏里启用。
echo.
pause
endlocal
