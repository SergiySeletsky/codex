using System.CommandLine;
using CodexCli.ApplyPatch;

namespace CodexCli.Commands;

public static class ApplyPatchCommand
{
    public static Command Create()
    {
        var patchArg = new Argument<string?>("patch", () => null, "Patch file or '-' for stdin");
        var cwdOpt = new Option<string?>("--cwd", "Working directory");
        var cmd = new Command("apply_patch", "Apply a patch to the file system");
        cmd.AddArgument(patchArg);
        cmd.AddOption(cwdOpt);
        cmd.SetHandler((string? patchFile, string? cwd) =>
        {
            string patchText;
            if (patchFile == null || patchFile == "-")
                patchText = Console.In.ReadToEnd();
            else
                patchText = File.ReadAllText(patchFile);
            cwd ??= Directory.GetCurrentDirectory();
            try
            {
                var output = PatchApplier.Apply(patchText, cwd);
                Console.WriteLine(output);
            }
            catch (PatchParseException e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
        }, patchArg, cwdOpt);
        return cmd;
    }
}
