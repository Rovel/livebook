namespace ElixirKit;

using System.Diagnostics;

public delegate void EventHandler(String name, String data);

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
    private static Mutex? mutex = null;

    public API()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.memorymappedfiles.memorymappedfile.createnew?view=net-7.0
        mutex = new Mutex(true, "", out isMainInstance);
    }
}

public class ReleaseScript
{
    public Process? serverProcess;

    public void Start(EventHandler? handler = null)
    {
        serverProcess = ReleaseCommand("start", handler);
    }

    public void Publish(String name, String data)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(name.ToLower(), @"^[a-zA-Z0-9-_\\.]+$")) {
            throw new ElixirKit.InvalidEventNameException(name);
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var base64 = System.Convert.ToBase64String(bytes);
        serverProcess!.StandardInput.Write($"elixirkit:event:{name}:");
        serverProcess!.StandardInput.WriteLine(base64);
    }

    public void Stop()
    {
        Publish("elixirkit.stop", "");
    }

    private Process ReleaseCommand(String command, EventHandler? handler = null)
    {
        var process = new Process();
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("elixirkit:"))
                {
                    // elixirkit:event:<name>:<data>
                    string[] parts = e.Data.Split(':', 4);
                    String kind = parts[1];

                    if (handler != null && parts[1] == "event")
                    {
                        var name = parts[2];
                        var bytes = System.Convert.FromBase64String(parts[3]);
                        var data = System.Text.Encoding.UTF8.GetString(bytes);
                        handler!(name, data);
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
        });
        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                Console.Error.WriteLine(e.Data);
            }
        });
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c \"{AppDomain.CurrentDomain.BaseDirectory}rel\\bin\\app.bat {command}\"";
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }
}
