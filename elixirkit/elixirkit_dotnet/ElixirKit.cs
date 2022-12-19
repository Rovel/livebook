namespace ElixirKit;

public class Release
{
    private System.Diagnostics.Process process;

    public Release()
    {
        Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
        process = new System.Diagnostics.Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c \"echo foo\"";
        process.EnableRaisingEvents = true;
        process.Exited += Exited;
        process.Start();
        process.WaitForExit();

        /* startProc.StartInfo.FileName = "cmd.exe" */
        /* startProc.StartInfo.Arguments = "/c """ & script & """ start" */
        /* startProc.StartInfo.UseShellExecute = false */
        /* startProc.StartInfo.CreateNoWindow = true */
        /* startProc.StartInfo.RedirectStandardError = true */
        /* startProc.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8 */
        /* startProc.Start() */
        /* Dim errorMessage = startProc.StandardError.ReadToEnd() */
        /* startProc.WaitForExit() */
    }

    private void Exited(object? sender, System.EventArgs e)
    {
        Console.WriteLine(
            $"Exit time    : {process.ExitTime}\n" +
            $"Exit code    : {process.ExitCode}\n" +
            $"Elapsed time : {Math.Round((process.ExitTime - process.StartTime).TotalMilliseconds)}"
        );
    }
}
