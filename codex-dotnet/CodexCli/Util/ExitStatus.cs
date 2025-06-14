namespace CodexCli.Util;

public static class ExitStatus
{
    public static void ExitWith(System.Diagnostics.Process process)
    {
        process.WaitForExit();
#if NET6_0_OR_GREATER
        if (process.ExitCode != 0)
            Environment.Exit(process.ExitCode);
        else
            Environment.Exit(0);
#else
        Environment.Exit(process.ExitCode);
#endif
    }
}
