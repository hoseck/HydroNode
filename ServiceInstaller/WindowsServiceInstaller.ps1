# install-service.ps1
# dotnet publish -c Release -r win-x64 �� �Խ�

$servicePath = "C:\���\HydroNode.exe"
sc.exe create "HydroNodeService" binPath= "$servicePath" start= auto
Start-Service "HydroNodeService"