@echo off
setlocal 

call "%VS100COMNTOOLS%\vsvars32.bat"

set prj=ibbsearch

pushd ..\%prj%
  start /wait "vc" VCExpress.exe %prj%.sln /build "Debug"
  start /wait "vc" VCExpress.exe %prj%.sln /build "Release"
popd

set conf=Release
if "%~1" == "d" set conf=Debug

xcopy /y /d "..\%prj%\%conf%\*.dll" bin\debug
xcopy /y /d "..\%prj%\%conf%\*.dll" bin\release
xcopy /y /d "..\%prj%\%conf%\*.pdb" bin\debug
xcopy /y /d "..\%prj%\%conf%\*.pdb" bin\release

endlocal
