@echo off
rem Pokretanje CableTest aplikacije u pretrazivacu (http://localhost:5080).
rem SDK je instaliran po korisniku u %USERPROFILE%\.dotnet, dok je u C:\Program Files\dotnet
rem samo runtime bez SDK-a, pa se poziva pun put do per-user dotnet.exe.
pushd "%~dp0"
rem "--" razdvaja argumente za dotnet od argumenata za samu aplikaciju,
rem pa "run-web.cmd --demo" pokrece server u demo rezimu.
"%USERPROFILE%\.dotnet\dotnet.exe" run --project CableTest.Web -- %*
set EXITCODE=%ERRORLEVEL%
popd
exit /b %EXITCODE%
