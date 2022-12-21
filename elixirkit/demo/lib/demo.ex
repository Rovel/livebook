defmodule Demo.Application do
  @moduledoc false
  use Application

  @impl true
  def start(_type, _args) do
    children = [
      Demo.Server,
      {Task, fn ->
          for i <- 10..1//-1 do
            IO.puts "#{i}..."
            Process.sleep(1000)
          end
          System.stop()
        end
      }
    ]

    opts = [strategy: :one_for_one, name: Demo.Supervisor]
    Supervisor.start_link(children, opts)
  end
end

defmodule Demo.Server do
  use GenServer

  def start_link(arg) do
    GenServer.start_link(__MODULE__, arg, name: __MODULE__)
  end

  @impl true
  def init(_) do
    dbg(:starting)
    Process.flag(:trap_exit, true)
    ElixirKit.subscribe()
    {:ok, nil}
  end

  @impl true
  def terminate(_reason, _state) do
    dbg(:terminating)
    nil
  end

  @impl true
  def handle_info({:dbg, data}, state) do
    dbg(data)
    {:noreply, state}
  end
end
