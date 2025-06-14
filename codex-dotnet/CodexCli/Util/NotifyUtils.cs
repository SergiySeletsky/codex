using System.Diagnostics;

namespace CodexCli.Util;

public static class NotifyUtils
{
    public static void RunNotify(string[] command, string arg)
    {
        if (command.Length == 0) return;
        try
        {
            var psi = new ProcessStartInfo(command[0])
            {
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            foreach (var c in command.Skip(1))
                psi.ArgumentList.Add(c);
            psi.ArgumentList.Add(arg);
            Process.Start(psi);
        }
        catch
        {
            // Ignore failures
        }
    }
}
