defmodule ElixirKit.Application do
  @moduledoc false
  use Application

  @impl true
  def start(_type, _args) do
    children = [
      {Registry,
       name: ElixirKit.Registry, keys: :duplicate, partitions: System.schedulers_online()},
      ElixirKit.Server,
      {Task, fn -> ElixirKit.__gets__() end},
    ]

    opts = [strategy: :one_for_one, name: ElixirKit.Supervisor]
    Supervisor.start_link(children, opts)
  end
end
