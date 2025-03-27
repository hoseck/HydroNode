# install-service.ps1
# dotnet publish -c Release -r win-x64 로 게시

$servicePath = "C:\경로\HydroNode.exe"
sc.exe create "HydroNodeService" binPath= "$servicePath" start= auto
Start-Service "HydroNodeService"