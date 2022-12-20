defmodule ElixirKit.Utils do
  @moduledoc false

  def log(color, command, message, options \\ []) do
    unless options[:quiet] do
      Mix.shell().info([color, "* #{command} ", :reset, message])
    end
  end

  def cmd(cmd, args, opts \\ []) do
    opts =
      opts
      |> Keyword.put_new(:into, IO.stream())
      |> Keyword.put_new(:stderr_to_stdout, true)

    {out, result} = System.cmd(cmd, args, opts)

    if result != 0 do
      Mix.raise("""
      Command exited with #{result}

      cmd: #{cmd}
      args: #{inspect(args, pretty: true)}
      opts: #{inspect(opts, pretty: true)}
      """)
    end

    out
  end

  def sh(cmd) do
    {out, result} = System.shell(cmd)

    if result != 0 do
      Mix.raise("""
      Command exited with #{result}

      cmd: #{cmd}
      """)
    end

    out
  end
end
