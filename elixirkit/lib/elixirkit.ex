defmodule ElixirKit do
  def publish(name, data) do
    IO.puts(["elixirkit:event:", name, ":", Base.encode64(data)])
  end

  def __gets__ do
    case IO.gets("") do
      :eof ->
        System.stop()

      "elixirkit:event:" <> rest ->
        [name, data] = String.split(rest, ":", parts: 2)
        data = data |> String.trim_trailing() |> Base.decode64!()
        send(ElixirKit.Server, {:event, name, data})
        __gets__()
    end
  end

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

  def __publish__(name) do
    data = IO.read(:line) |> String.trim()
    send(ElixirKit.Server, {:event, name, data})
  end
end
