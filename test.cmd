@echo off
rem Pokretanje svih testova nad solution-om.
pushd "%~dp0"
"%USERPROFILE%\.dotnet\dotnet.exe" test CableTest.sln %*
set EXITCODE=%ERRORLEVEL%
popd
exit /b %EXITCODE%
