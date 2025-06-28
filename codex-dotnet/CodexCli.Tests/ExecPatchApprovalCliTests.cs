using CodexCli.Commands;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;

public class ExecPatchApprovalCliTests
{
    [Fact(Skip="Requires mock provider environment")]
    public async Task PatchApprovedProducesApplyEvents()
    {
        var root = new RootCommand();
        var cfgOpt = new Option<string?>("--config");
        var cdOpt = new Option<string?>("--cd");
        root.AddOption(cfgOpt);
        root.AddOption(cdOpt);
        root.AddCommand(ExecCommand.Create(cfgOpt, cdOpt));
        var parser = new Parser(root);

        var oldIn = Console.In;
        var oldOut = Console.Out;
        try
        {
            Console.SetIn(new StringReader("y\ny\n"));
            var output = new StringWriter();
            Console.SetOut(output);
            await parser.InvokeAsync("exec hi --model-provider mock --ask-for-approval OnFailure --json");
            var text = output.ToString();
            Assert.Contains("patch_apply_begin", text);
            Assert.Contains("patch_apply_end", text);
        }
        finally
        {
            Console.SetIn(oldIn);
            Console.SetOut(oldOut);
        }
    }
}
