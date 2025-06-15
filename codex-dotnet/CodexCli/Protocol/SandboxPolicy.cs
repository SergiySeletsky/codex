namespace CodexCli.Protocol;

public record SandboxPolicy
{
    public bool Unrestricted { get; init; }

    public bool IsUnrestricted() => Unrestricted;
}
