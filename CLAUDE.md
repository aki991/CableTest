# CableTest

## Prevođenje i testovi

Koristi **`%USERPROFILE%\.dotnet\dotnet.exe`**, a ne `dotnet` iz PATH-a.

`dotnet` iz PATH-a (`C:\Program Files\dotnet\dotnet.exe`) na ovoj mašini ima samo runtime,
bez SDK-a, pa svaki `build` i `test` puca sa „No .NET SDKs were found".

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" build CableTest.sln
& "$env:USERPROFILE\.dotnet\dotnet.exe" test CableTest.sln
```
