defmodule ElixirKit do
  def bundle(release) do
    case :os.type() do
      {:unix, :darwin} ->
        ElixirKit.MacOS.bundle(release)

      {:win32, _} ->
        ElixirKit.Windows.bundle(release)
    end
  end

  def subscribe do
    {:ok, _} = Registry.register(ElixirKit.Registry, "subscribers", [])
  end

  def __rpc__(name) do
    data = IO.read(:line) |> String.trim()
    send(ElixirKit.Server, {:publish, name, data})
  end
end
