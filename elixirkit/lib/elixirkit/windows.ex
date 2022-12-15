defmodule ElixirKit.Windows do
  @moduledoc false

  import ElixirKit.Utils

  def run_app do
    cmd("dotnet", ~w(run), cd: "rel/windows")
  end
end
