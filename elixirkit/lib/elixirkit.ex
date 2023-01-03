defmodule ElixirKit do
  def start do
    pipe =
      if path = System.get_env("ELIXIRKIT_PIPE_PATH") do
        {:path, path}
      else
        fd = System.get_env("ELIXIRKIT_PIPE_FD")
        {:fd, String.to_integer(fd)}
      end

    {:ok, _} = ElixirKit.Server.start_link({pipe, self()})
  end
end
