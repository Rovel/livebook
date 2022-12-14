defmodule ElixirKit.Server do
  @moduledoc false
  use GenServer

  def start_link(arg) do
    GenServer.start_link(__MODULE__, arg, name: __MODULE__)
  end

  @impl true
  def init(_) do
    {:ok, nil}
  end

  @impl true
  def handle_info({:publish, name, data}, state) do
    Registry.dispatch(ElixirKit.Registry, "subscribers", fn entries ->
      for {pid, _} <- entries, do: send(pid, {name, data})
    end)

    {:noreply, state}
  end
end
