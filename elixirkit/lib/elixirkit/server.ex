defmodule ElixirKit.Server do
  @moduledoc false

  use GenServer

  def start_link(arg) do
    GenServer.start_link(__MODULE__, arg, name: __MODULE__)
  end

  def publish(name, data) do
    GenServer.cast(__MODULE__, {:publish, name, data})
  end

  @impl true
  def init({{:path, path}, pid}) do
    port = Port.open({:spawn, "cat #{path}"}, [:binary, line: 1024])
    {:ok, %{port: port, pid: pid}}
  end

  @impl true
  def init({{:fd, fd}, pid}) do
    port = Port.open({:fd, fd, fd}, [:binary, line: 1024])
    {:ok, %{port: port, pid: pid}}
  end

  @impl true
  def handle_info({port, {:data, {:eol, "event:" <> rest}}}, state) when port == state.port do
    [name, data] = String.split(rest, ":")
    data = Base.decode64!(data)
    send(state.pid, {:event, name, data})
    {:noreply, state}
  end
end
