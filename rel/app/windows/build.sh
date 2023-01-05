#!/bin/sh
set -euo pipefail

export MIX_ENV=prod
export MIX_TARGET=app
export ELIXIRKIT_APP_NAME=Livebook
export ELIXIRKIT_PROJECT_DIR=$PWD/../../..
export ELIXIRKIT_RELEASE_NAME=app
export ELIXIRKIT_CONFIGURATION=Release

configuration=$ELIXIRKIT_CONFIGURATION
target_dir="$PWD/bin/${ELIXIRKIT_APP_NAME}-${configuration}"
dotnet build --configuration $configuration --output $target_dir
# (cd ../../.. && mix release app --overwrite --path=${target_dir}/rel)
