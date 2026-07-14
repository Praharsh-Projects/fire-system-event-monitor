#!/usr/bin/env sh
set -eu

if command -v dotnet >/dev/null 2>&1; then
  dotnet run --project ../src/FireSystemEventMonitor.Api/FireSystemEventMonitor.Api.csproj
else
  "$HOME/.dotnet/dotnet" run --project ../src/FireSystemEventMonitor.Api/FireSystemEventMonitor.Api.csproj
fi

