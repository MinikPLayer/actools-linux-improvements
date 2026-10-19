#!/bin/bash
# Dumps public properties/events of Avalonia (and optionally project) assemblies to PortTools/api.json,
# the XAML converter uses it to drop setters/attributes which don't exist in Avalonia.
# usage: gen_api_json.sh [extra assembly ...]
set -e
cd "$(dirname "$0")"
N=$HOME/.nuget/packages
(cd apidump && dotnet build -o out 2>&1 | tail -1)
dotnet apidump/out/apidump.dll "$PWD/api.json" $N/avalonia/11.3.22/lib/net8.0 $N/avalonia.controls.datagrid/11.3.13/lib/net8.0 \
  $N/avalonia.themes.simple/11.3.22/lib/net8.0 "$@"
