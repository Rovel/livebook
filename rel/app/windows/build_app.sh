#!/bin/sh
set -euo pipefail

export MIX_ENV=prod
export MIX_TARGET=app

build_args="--configuration Release"
dotnet publish $build_args
target_dir="$PWD/bin/Release/net6.0-windows"
(cd ../../.. && mix release app --overwrite --path=${target_dir}/rel)
