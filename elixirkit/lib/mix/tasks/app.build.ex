defmodule Mix.Tasks.App.Build do
  use Mix.Task

  @impl true
  def run([]) do
    case :os.type() do
      {:unix, :darwin} ->
        ElixirKit.MacOS.build_app()
    end

    :ok
  end
end
