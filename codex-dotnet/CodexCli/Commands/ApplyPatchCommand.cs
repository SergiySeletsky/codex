using System.CommandLine;
using CodexCli.ApplyPatch;
using System.IO;

namespace CodexCli.Commands;

public static class ApplyPatchCommand
{
    public static Command Create()
    {
        var patchArg = new Argument<string?>("patch", () => null, "Patch file or '-' for stdin");
        var cwdOpt = new Option<string?>("--cwd", "Working directory");
        var summaryOpt = new Option<bool>("--summary", () => false, "Print summary only");
        var cmd = new Command("apply_patch", "Apply a patch to the file system");
        cmd.AddArgument(patchArg);
        cmd.AddOption(cwdOpt);
        cmd.AddOption(summaryOpt);
        cmd.SetHandler((string? patchFile, string? cwd, bool summaryOnly) =>
        {
            string patchText;
            if (patchFile == null || patchFile == "-")
                patchText = Console.In.ReadToEnd();
            else
                patchText = File.ReadAllText(patchFile);
            cwd ??= Directory.GetCurrentDirectory();
            try
            {
                PatchApplier.ApplyAndReport(patchText, cwd, Console.Out, Console.Error);
            }
            catch (PatchParseException e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
        }, patchArg, cwdOpt, summaryOpt);
        return cmd;
    }
}
