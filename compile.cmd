@echo off
SET MSBuildPath="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"

echo 1. Pack danmaku-show-server.
cd node_server
call npm pack
cd ..
mkdir dist
cd dist
del /f /s /q *
cd ..

echo 2. Build Client.
nuget.exe restore danmaku-show/danmaku-show.sln
%MSBuildPath%\MSBuild.exe danmaku-show/danmaku-show.sln /t:Rebuild /p:Configuration=Release

echo 3. Copy to Dest.
xcopy danmaku-show\danmaku-show\bin\Release dist /e /i /y

cd node_server
for %%F in (danmaku-show-server-*.tgz) do move /Y %%F ../dist/danmaku-show-server.tgz
cd ..

pause