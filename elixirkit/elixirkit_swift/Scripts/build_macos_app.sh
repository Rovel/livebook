#!/bin/sh
set -euo pipefail

app_name=$ELIXIRKIT_APP_NAME
release_name="${ELIXIRKIT_RELEASE_NAME:-}"
app_dir=$PWD/.build/${app_name}.app
build_args="${ELIXIRKIT_BUILD_ARGS:-}"

rm -rf $app_dir
swift build $build_args
target_dir=`swift build --show-bin-path $build_args`
rel_dir=$app_dir/Contents/Resources/rel

mkdir -p $app_dir/Contents/{MacOS,Resources}

cp Info.plist $app_dir/Contents/Info.plist

cp $target_dir/$app_name $app_dir/Contents/MacOS/$app_name

for i in Resources/*; do
  cp $i $app_dir/Contents/Resources/
done

(
  cd $ELIXIRKIT_PROJECT_DIR
  mix release $release_name --overwrite --path=$rel_dir
)
