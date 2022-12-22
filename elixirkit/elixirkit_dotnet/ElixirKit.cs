namespace ElixirKit;

using System.Diagnostics;
using System.Collections;
using System.Threading.Tasks;
using System.IO.Pipes;

public delegate void StartHandler();

public delegate void EventHandler(string name, string data);

public delegate void ExitHandler(int ExitCode);

public class InvalidEventNameException : Exception
{
    public InvalidEventNameException(string name) : base($"'{name}' is invalid") {}
}

public class InvalidMessageException : Exception
{
    public InvalidMessageException(string message) : base($"'{message}' is invalid") {}
}

public class API
{
    private static Mutex? mutex;

    internal string pipeName;

    private bool isMainInstance;

    public bool IsMainInstance
    {
        get { return isMainInstance; }
    }

    public API(string id)
    {
        this.pipeName = $"elixirkit.mutex.{id}";
        var mutexName = $"elixirkit.pipe.{id}";

        if (mutex == null)
        {
            mutex = new Mutex(true, mutexName, out isMainInstance);
        }
    }

    public Release StartRelease(StartHandler startHandler, EventHandler eventHandler, ExitHandler exitHandler)
    {
        return new Release(this, startHandler, eventHandler, exitHandler);
    }

    internal static void DoPublish(StreamWriter writer, string name, string data)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(name.ToLower(), @"^[a-zA-Z0-9-_\\.]+$")) {
            throw new ElixirKit.InvalidEventNameException(name);
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var base64 = System.Convert.ToBase64String(bytes);
        writer.Write($"elixirkit:event:{name}:");
        writer.WriteLine(base64);
        writer.Flush();
    }

    public void Publish(string name, string data)
    {
        using var pipe = new NamedPipeClientStream(this.pipeName);
        pipe.Connect();
        using var writer = new StreamWriter(pipe);
        DoPublish(writer, name, data);
    }

    public void Stop()
    {
        Publish("elixirkit.stop", "");
    }
}

public class Release
{
    private StartHandler startHandler;
    private EventHandler eventHandler;
    private ExitHandler exitHandler;
    private Process process;
    private bool started = false;

    internal Release(API api, StartHandler startHandler, EventHandler eventHandler, ExitHandler exitHandler)
    {
        this.startHandler = startHandler;
        this.eventHandler = eventHandler;
        this.exitHandler = exitHandler;
        process = new Process();
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c \"{AppDomain.CurrentDomain.BaseDirectory}rel\\bin\\app.bat start\"";
        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => this.handleOutputLine(e.Data));
        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => this.handleErrorLine(e.Data));
        process.EnableRaisingEvents = true;
        process.Exited += this.handleProcessExited;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var t = Task.Run(() => {
            var pipe = new NamedPipeServerStream(api.pipeName);
            var reader = new StreamReader(pipe);
            var writer = new StreamWriter(pipe);

            while (true)
            {
                pipe.WaitForConnection();

                while (true)
                {
                    String? line = reader.ReadLine();
                    if (String.IsNullOrEmpty(line)) break;
                    process.StandardInput.WriteLine(line);
                }

                pipe.Disconnect();
            }
        });
    }

    public void Publish(string name, string data)
    {
        API.DoPublish(process.StandardInput, name, data);
    }

    public void Stop()
    {
        Publish("elixirkit.stop", "");
    }

    private void handleProcessExited(object? sender, System.EventArgs e)
    {
        this.exitHandler(this.process.ExitCode);
    }

    private void handleOutputLine(string? line)
    {
        if (!this.started)
        {
            this.started = true;
            this.startHandler();
        }

        if (!String.IsNullOrEmpty(line))
        {
            if (line.StartsWith("elixirkit:"))
            {
                // elixirkit:event:<name>:<data>
                string[] parts = line.Split(':', 4);
                String kind = parts[1];

                if (parts[1] == "event")
                {
                    var name = parts[2];
                    var bytes = System.Convert.FromBase64String(parts[3]);
                    var data = System.Text.Encoding.UTF8.GetString(bytes);
                    this.eventHandler(name, data);
                }
                else
                {
                    throw new ElixirKit.InvalidMessageException(line);
                }
            }
            else
            {
                Console.WriteLine(line);
            }
        }
    }

    private void handleErrorLine(string? line)
    {
        if (!String.IsNullOrEmpty(line))
        {
            Console.Error.WriteLine(line);
        }
    }
}
