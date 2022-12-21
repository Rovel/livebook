defmodule Demo.Application do
  @moduledoc false

  use Application

  @impl true
  def start(_type, _args) do
    children = [Demo.Server]
    opts = [strategy: :one_for_one, name: Demo.Supervisor]
    Supervisor.start_link(children, opts)
  end
end

defmodule Demo.Server do
  @moduledoc false

  use GenServer

  def start_link(arg) do
    GenServer.start_link(__MODULE__, arg, name: __MODULE__)
  end

  @impl true
  def init(_) do
    log("init")
    Process.flag(:trap_exit, true)
    ElixirKit.subscribe()

    Task.start(fn ->
      for i <- 5..1//-1 do
        data = "Terminating in #{i}..."
        log(data)
        ElixirKit.publish("log", data)
        Process.sleep(1000)
      end

      System.stop()
    end)

    {:ok, nil}
  end

  @impl true
  def terminate(_reason, _state) do
    log("Terminating")
    nil
  end

  @impl true
  def handle_info({:event, "log", data}, state) do
    log(data)
    {:noreply, state}
  end

  defp log(data) do
    timestamp = Time.utc_now() |> Time.truncate(:millisecond) |> Time.to_string()
    IO.puts([timestamp, "Z [server] ", data])
  end
end
