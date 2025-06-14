using System.Diagnostics;

namespace CodexCli.Util;

public static class NotifyUtils
{
    public static void RunNotify(string[] command, string arg, IDictionary<string,string>? env = null)
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
            if (env != null)
            {
                foreach (var (k,v) in env)
                    psi.Environment[k] = v;
            }
            Process.Start(psi);
        }
        catch
        {
            // Ignore failures
        }
    }
}
