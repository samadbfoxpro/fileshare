# fileshare

FileShare is a small Windows file and message sharing app for a local network.

## Build

```powershell
dotnet build
```

## Publish single exe

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```
