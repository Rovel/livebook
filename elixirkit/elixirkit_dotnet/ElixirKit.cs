using System.Diagnostics;
using System.IO.Pipes;

#if DEBUG
using System.Runtime.InteropServices;
#endif

namespace ElixirKit;

public class API
{
    public bool mainInstance;
    private Mutex? mutex;
    private string? id;
    private Release? release;

    public bool MainInstance {
        get {
            return mainInstance;
        }
    }

    // On Windows we need to manually handle the app being launched multiple times.
    // It can be opened directly or via its associated file types and URL schemes.
    // To help with this, we can initialize the API with a unique `id` and we'll
    // ensure only the "main instance" interacts with the release process.
    public API(string? id = null)
    {
        if (id == null)
        {
            mainInstance = true;
        }
        else
        {
            this.id = id;
            mutex = new Mutex(true, id, out mainInstance);
        }
    }

    public void Start(string name, EventHandler? exited = null)
    {
        if (!mainInstance)
        {
            throw new Exception("Not on main instance");
        }

        release = new Release(name, exited);

        if (id != null)
        {
            Task.Run(() => {
                while (true) {
                    var line = PipeReadLine();

                    if (line != null)
                    {
                        release!.DoPublish(line);
                    }
                }
            });
        }
    }

    public void Stop()
    {
        if (!mainInstance)
        {
            throw new Exception("Not on main instance");
        }

        release!.Stop();
    }

    public void WaitForExit()
    {
        if (!mainInstance)
        {
            throw new Exception("Not on main instance");
        }

        release!.WaitForExit();
    }

    public void Publish(string name, string data)
    {
        if (mainInstance)
        {
            release!.Publish(name, data);
        }
        else
        {
            var message = Release.EncodeEventMessage(name, data);
            PipeWriteLine(message);
        }
    }

    private string? PipeReadLine()
    {
        using var pipe = new NamedPipeServerStream(id!);
        pipe.WaitForConnection();
        using var reader = new StreamReader(pipe);
        var line = reader.ReadLine()!;
        pipe.Disconnect();
        return line;
    }

    private void PipeWriteLine(string line)
    {
        using var pipe = new NamedPipeClientStream(id!);
        pipe.Connect();
        using var writer = new StreamWriter(pipe);
        writer.WriteLine(line);
    }
}

internal class Release
{
    Process? startProcess;
    AnonymousPipeServerStream? pipe;
    StreamWriter? writer;
    string? relScript;

    public Release(string name, EventHandler? exited = null)
    {
        relScript = releaseScript(name);

        pipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        writer = new StreamWriter(pipe);

        var fd = pipe.GetClientHandleAsString();

        startProcess = newReleaseProcess();
        startProcess.StartInfo.Arguments = "start";
        startProcess.StartInfo.Environment.Add("ELIXIRKIT_PIPE_FD", fd);

        if (exited != null)
        {
            startProcess.EnableRaisingEvents = true;
            startProcess.Exited += exited;
        }

        startProcess.Start();
        startProcess.BeginOutputReadLine();
        startProcess.BeginErrorReadLine();
        pipe.DisposeLocalCopyOfClientHandle();
    }

    internal static string EncodeEventMessage(string name, string data)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var encoded = System.Convert.ToBase64String(bytes);
        return $"event:{name}:{encoded}";
    }

    public void Publish(string name, string data) {
        DoPublish(EncodeEventMessage(name, data));
    }

    internal void DoPublish(string message)
    {
        writer!.WriteLine(message);
        writer!.Flush();
    }

    public void Stop()
    {
        if (startProcess!.HasExited)
        {
            return;
        }

        var process = newReleaseProcess();
        process.StartInfo.Arguments = "stop";
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }

    private Process newReleaseProcess()
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = relScript,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.OutputDataReceived += (sender, args) => {
            if (args.Data != null)
            {
                Console.WriteLine(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) => {
            if (args.Data != null)
            {
                Console.Error.WriteLine(args.Data);
            }
        };

        return process;
    }

    public void WaitForExit()
    {
        startProcess!.WaitForExit();
    }

    private string releaseScript(string name)
    {
        var exe = Process.GetCurrentProcess().MainModule!.FileName;
        var dir = Path.GetDirectoryName(exe)!;

        if (Path.GetExtension(exe) == ".exe")
        {
            return Path.Combine(dir, "rel", "bin", name + ".bat");
        }
        else
        {
            return Path.Combine(dir, "rel", "bin", name);
        }
    }
}

public static class Utils
{

#if DEBUG
    public static void DebugAttachConsole()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
    }

    [DllImport("kernel32.dll")]
    static extern bool AttachConsole( int dwProcessId );
    private const int ATTACH_PARENT_PROCESS = -1;
#else
    public static void DebugAttachConsole() {}
#endif
}
