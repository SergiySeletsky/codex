using CodexCli;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        var newArgs = new string[args.Length + 1];
        newArgs[0] = "interactive";
        Array.Copy(args, 0, newArgs, 1, args.Length);
        return await CodexCli.Program.Main(newArgs);
    }
}
