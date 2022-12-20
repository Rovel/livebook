defmodule Mix.Tasks.App.Run do
  use Mix.Task

  @impl true
  def run([]) do
    case :os.type() do
      {:unix, :darwin} ->
        ElixirKit.MacOS.run_app()

      {:win32, _} ->
        ElixirKit.Windows.run_app()
    end

    :ok
  end
end
