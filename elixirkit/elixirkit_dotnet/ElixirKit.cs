namespace ElixirKit;

using System.Diagnostics;
using System.Collections;
using System.Threading.Tasks;
using System.IO.Pipes;

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

    public Release StartRelease(EventHandler eventHandler, ExitHandler exitHandler)
    {
        return new Release(this, eventHandler, exitHandler);
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
    private EventHandler eventHandler;
    private ExitHandler exitHandler;
    private Process process;

    internal Release(API api, EventHandler eventHandler, ExitHandler exitHandler)
    {
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
        process.OutputDataReceived += new DataReceivedEventHandler(this.outputHandler);
        process.ErrorDataReceived += new DataReceivedEventHandler(this.errorHandler);
        process.EnableRaisingEvents = true;
        process.Exited += ProcessExited;
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

    private void ProcessExited(object? sender, System.EventArgs e)
    {
        this.exitHandler(this.process.ExitCode);
    }

    private void outputHandler(object? sender, DataReceivedEventArgs e)
    {
        if (!String.IsNullOrEmpty(e.Data))
        {
            if (e.Data.StartsWith("elixirkit:"))
            {
                // elixirkit:event:<name>:<data>
                string[] parts = e.Data.Split(':', 4);
                String kind = parts[1];

                if (parts[1] == "event")
                {
                    var name = parts[2];
                    var bytes = System.Convert.FromBase64String(parts[3]);
                    var data = System.Text.Encoding.UTF8.GetString(bytes);
                    eventHandler(name, data);
                }
                else
                {
                    throw new ElixirKit.InvalidMessageException(e.Data);
                }
            }
            else
            {
                Console.WriteLine(e.Data);
            }
        }
    }

    private void errorHandler(object? sender, DataReceivedEventArgs e)
    {
        if (!String.IsNullOrEmpty(e.Data))
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
