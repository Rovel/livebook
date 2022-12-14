defmodule ElixirKit.MacOS do
  @moduledoc false

  def bundle(release) do
    options =
      release.options
      |> Keyword.fetch!(:macos)
      |> Keyword.validate!([:app_dir, :build_dmg, :notarization])

    %{name: app_name, dir: target_app_dir} = app_config()
    source_app_dir = Keyword.fetch!(options, :app_dir)

    %{
      app_name: app_name,
      release_path: release.path,
      version: release.version,
      source_app_dir: source_app_dir,
      target_app_dir: target_app_dir,
      build_dmg?: options[:build_dmg] == true,
      notarization: options[:notarization]
    }
    |> build_app_dir()
    |> copy_release()
    |> copy_info_plist()
    |> build_launcher()
    |> copy_resources()
    |> build_dmg()

    release
  end

  defp build_app_dir(context) do
    log(:green, "creating", Path.relative_to_cwd(context.target_app_dir))
    File.mkdir_p!("#{context.target_app_dir}/Contents/Resources")
    context
  end

  defp copy_release(context) do
    source = Path.relative_to_cwd(context.release_path)
    target = Path.relative_to_cwd("#{context.target_app_dir}/Contents/Resources/rel")
    log(:green, "copying", "#{source} to #{target}")
    File.cp_r!(source, target)
    context
  end

  defp copy_info_plist(context) do
    source = Path.relative_to_cwd("#{context.source_app_dir}/Info.plist")
    target = Path.relative_to_cwd("#{context.target_app_dir}/Contents/Info.plist")

    if File.exists?(source) do
      data =
        source
        |> File.read!()
        |> String.replace("$(PRODUCT_VERSION)", context.version)

      File.write!(target, data)
    else
      log(:yellow, "skipping", "Info.plist (#{source} not found)")
    end

    context
  end

  defp copy_resources(context) do
    resources =
      Path.wildcard(
        "#{context.spm_release_dir}/#{context.app_name}_#{context.app_name}.bundle/Contents/Resources/*"
      )

    if resources != [] do
      log(:green, "copying resources", "")

      for path <- resources do
        basename = Path.basename(path)
        target = "#{context.target_app_dir}/Contents/Resources/#{basename}"
        log(:green, "copying", "#{basename} to #{Path.relative_to_cwd(target)}")
      end
    end

    context
  end

  defp build_launcher(context) do
    launcher_path = "#{context.target_app_dir}/Contents/MacOS/#{context.app_name}"
    File.mkdir_p!("#{context.target_app_dir}/Contents/MacOS")
    log(:green, "creating", Path.relative_to_cwd(launcher_path))

    args = ~w(build -c release --arch arm64 --arch x86_64)
    cmd("swift", args, cd: context.source_app_dir)

    spm_release_dir =
      cmd("swift", args ++ ["--show-bin-path"], cd: context.source_app_dir, into: "")
      |> String.trim()

    File.cp!("#{spm_release_dir}/#{context.app_name}", launcher_path)
    Map.put(context, :spm_release_dir, spm_release_dir)
  end

  defp build_dmg(context) when context.build_dmg? == false do
    context
  end

  defp build_dmg(context) do
    dmg_dir = "#{Mix.Project.build_path()}/dmg"
    app_dir = "#{dmg_dir}/#{context.app_name}.app"
    File.rm_rf!(dmg_dir)
    File.mkdir_p!(dmg_dir)
    File.ln_s!("/Applications", "#{dmg_dir}/Applications")
    File.cp_r!("#{Mix.Project.build_path()}/#{context.app_name}.app", app_dir)

    log(:green, "signing", Path.relative_to_cwd(app_dir))
    to_sign = find_executable_files(app_dir) ++ [app_dir]
    entitlements_path = "#{context.source_app_dir}/#{context.app_name}.entitlements"
    codesign(to_sign, context.notarization, entitlements_path)

    dmg_path = "#{Mix.Project.build_path()}/#{context.app_name}Install.dmg"
    log(:green, "creating", Path.relative_to_cwd(dmg_path))
    volname = "#{context.app_name}Install"
    cmd("hdiutil", ~w(create #{dmg_path} -ov -volname #{volname} -fs HFS+ -srcfolder #{dmg_dir}))

    log(:green, "notarizing", Path.relative_to_cwd(dmg_path))
    notarize(dmg_path, context.notarization)
    context
  end

  defp codesign(paths, options, entitlements_path) do
    identity = Keyword.fetch!(options, :identity)

    flags = [
      "--force",
      "--timestamp",
      "--verbose=4",
      "--options",
      "runtime",
      "--sign",
      identity,
      "--entitlements",
      entitlements_path
    ]

    cmd("codesign", flags ++ paths)
  end

  defp notarize(path, options) do
    team_id = Keyword.fetch!(options, :team_id)
    apple_id = Keyword.fetch!(options, :apple_id)
    password = Keyword.fetch!(options, :password)

    cmd("xcrun", [
      "notarytool",
      "submit",
      "--team-id",
      team_id,
      "--apple-id",
      apple_id,
      "--password",
      password,
      "--progress",
      "--wait",
      path
    ])
  end

  def build_app do
    %{dir: dir} = app_config()

    # build app
    Mix.Task.run("release", ~w(app --overwrite))

    # register app
    cmd(
      "/System/Library/Frameworks/CoreServices.framework" <>
        "/Versions/A/Frameworks/LaunchServices.framework" <>
        "/Versions/A/Support/lsregister",
      ["-f", dir]
    )
  end

  def open_app do
    %{name: name, dir: dir} = app_config()

    # open app
    ensure_not_running(name)
    tty = tty()
    forward_logs("#{System.user_home!()}/Library/Logs/#{name}.log")
    cmd("open", ~w(-W --stdout #{tty} --stderr #{tty} #{dir}))
  end

  defp forward_logs(path) do
    File.write!(path, "")
    {:ok, log_pid} = File.open(path, [:read])

    Task.start_link(fn ->
      gets(log_pid)
    end)
  end

  defp gets(pid) do
    data = IO.gets(pid, "")

    if data != :eof do
      IO.write(data)
    end

    gets(pid)
  end

  defp ensure_not_running(name, kill? \\ true) do
    case System.cmd("pgrep", ["^#{name}$"], into: "") do
      {"", 1} ->
        :ok

      {out, 0} ->
        [pid] = String.split(out, "\n", trim: true)

        if kill? do
          log(:green, "killing", "#{name} (pid=#{pid})")
          cmd("kill", [pid])
        end

        Process.sleep(100)
        ensure_not_running(name, false)
    end
  end

  defp app_config do
    config = Mix.Project.config()
    name = Macro.camelize(Atom.to_string(config[:app]))
    dir = Path.join(Mix.Project.build_path(), "#{name}.app")
    %{name: name, dir: dir}
  end

  defp log(color, command, message, options \\ []) do
    unless options[:quiet] do
      Mix.shell().info([color, "* #{command} ", :reset, message])
    end
  end

  defp find_executable_files(dir) do
    "find #{dir} -perm +111 -type f -exec sh -c \"file {} | grep --silent Mach-O\" \\; -print"
    |> sh()
    |> String.split("\n", trim: true)
  end

  defp tty do
    tty = sh("ps -p #{System.pid()} | tail -1 | awk '{ print $2 }'")
    "/dev/#{String.trim(tty)}"
  end

  defp cmd(cmd, args, opts \\ []) do
    opts = Keyword.put_new(opts, :into, IO.stream())
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

  defp sh(cmd) do
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
