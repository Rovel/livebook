defmodule ElixirKit.Windows do
  @moduledoc false

  import ElixirKit.Utils

  @doc """
  Bundle the release.
  """
  def bundle(release) do
    app_source_dir =
      release.options
      |> Keyword.get(:windows, [])
      |> Keyword.validate!([:app_dir])
      |> Keyword.get(:app_dir, "rel/windows")

    log(:green, :killing, "epmd.exe")
    System.cmd("taskkill.exe", ~w(/F /IM epmd.exe))

    log(:green, "using", app_source_dir)

    %{
      app_source_dir: app_source_dir,
      app_target_dir: nil,
      release_path: release.path
    }
    |> build_app_dir()
    |> copy_release()

    release
  end

  defp build_app_dir(context) do
    cmd("dotnet", ~w(build --configuration #{configuration()}), cd: context.app_source_dir)
    [exe | _] = Path.wildcard("#{context.app_source_dir}/bin/#{configuration()}/**/*.exe")
    app_target_dir = Path.dirname(exe)
    %{context | app_target_dir: app_target_dir}
  end

  defp copy_release(context) do
    source = Path.relative_to_cwd(context.release_path)
    target = Path.relative_to_cwd("#{context.app_target_dir}/rel")
    log(:green, "copying", "#{source} to #{target}")
    File.cp_r!(source, target)
    context
  end

  @doc """
  Bundle the app.
  """
  def build_app do
    Mix.Task.run("release", ~w(app --overwrite))
  end

  @doc """
  Run the app.
  """
  def run_app do
    cmd("dotnet", ~w(run --configuration #{configuration()} --no-build), cd: "rel/windows")
  end

  defp configuration do
    case Mix.env() do
      :prod -> "Release"
      _ -> "Debug"
    end
  end
end
