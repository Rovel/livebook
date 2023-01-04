#!/bin/bash
#
# Usage:
#
#     $ sh .github/scripts/app/build_mac.sh
#     $ open _build/app_prod/Livebook.app
#     $ open livebook://github.com/livebook-dev/livebook/blob/main/test/support/notebooks/basic.livemd
#     $ open ./test/support/notebooks/basic.livemd
set -e

. .github/scripts/app/bootstrap_mac.sh
mix local.hex --force --if-missing
mix local.rebar --force --if-missing

export MIX_ENV=prod MIX_TARGET=app
mix deps.get --only prod

cd rel/app/macos
if [ -n "${ELIXIRKIT_CODESIGN_IDENTITY}" ]; then
  ./build_dmg.sh
else
  ./build_app.sh
fi
