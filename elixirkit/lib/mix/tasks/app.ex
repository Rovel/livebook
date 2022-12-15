defmodule Mix.Tasks.App do
  use Mix.Task

  @impl true
  def run([]) do
    case :os.type() do
      {:unix, :darwin} ->
        ElixirKit.MacOS.build_app()
        ElixirKit.MacOS.open_app()

      {:win32, _} ->
        ElixirKit.Windows.run_app()
    end

    :ok
  end
end
