namespace ElixirKit;

using System.Diagnostics;
using System.IO.Pipes;

public class Release
{
    private Process process;

    public Release(EventHandler? exited)
    {
        var pipe = new NamedPipeServerStream("ElixirKit.Demo");

        process = ReleaseCommand("start");
        process.EnableRaisingEvents = true;
        if (exited != null)
        {
            process.Exited += exited;
        }
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public Release() : this(exited: null) {}

    public void Publish(String name, String data)
    {
        if (System.Text.RegularExpressions.Regex.IsMatch(name.ToLower(), @"^[a-zA-Z0-9-_]+$")) {
            process = ReleaseCommand($"rpc ElixirKit.__rpc__(:{name})");
            process.StartInfo.RedirectStandardInput = true;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.WriteLine(data);
            process.WaitForExit();
        }
    }

    public void Terminate()
    {
        process = ReleaseCommand("stop");
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }

    private Process ReleaseCommand(String command)
    {
        Process process = new Process();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        });
        process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
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
        return process;
    }
}
