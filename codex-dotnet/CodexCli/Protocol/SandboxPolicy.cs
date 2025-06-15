using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace CodexCli.Protocol;


public enum SandboxPermissionType
{
    DiskFullReadAccess,
    DiskWritePlatformUserTempFolder,
    DiskWritePlatformGlobalTempFolder,
    DiskWriteCwd,
    DiskWriteFolder,
    DiskFullWriteAccess,
    NetworkFullAccess,
}

public readonly record struct SandboxPermission(SandboxPermissionType Type, string? Path = null);

public class SandboxPolicy
{
    public List<SandboxPermission> Permissions { get; init; } = new();

    public static SandboxPolicy NewReadOnlyPolicy() => new()
    {
        Permissions = { new SandboxPermission(SandboxPermissionType.DiskFullReadAccess) }
    };

    public static SandboxPolicy NewReadOnlyPolicyWithWritableRoots(IEnumerable<string> roots)
    {
        var policy = NewReadOnlyPolicy();
        foreach (var r in roots)
            policy.Permissions.Add(new SandboxPermission(SandboxPermissionType.DiskWriteFolder, r));
        return policy;
    }

    public static SandboxPolicy NewFullAutoPolicy() => new()
    {
        Permissions =
        {
            new SandboxPermission(SandboxPermissionType.DiskFullReadAccess),
            new SandboxPermission(SandboxPermissionType.DiskWritePlatformUserTempFolder),
            new SandboxPermission(SandboxPermissionType.DiskWriteCwd)
        }
    };

    public bool HasFullDiskReadAccess() => Permissions.Any(p => p.Type == SandboxPermissionType.DiskFullReadAccess);
    public bool HasFullDiskWriteAccess() => Permissions.Any(p => p.Type == SandboxPermissionType.DiskFullWriteAccess);
    public bool HasFullNetworkAccess() => Permissions.Any(p => p.Type == SandboxPermissionType.NetworkFullAccess);

    public List<string> GetWritableRootsWithCwd(string cwd)
    {
        var list = new List<string>();
        foreach(var perm in Permissions)
        {
            switch (perm.Type)
            {
                case SandboxPermissionType.DiskWriteCwd:
                    list.Add(cwd);
                    break;
                case SandboxPermissionType.DiskWriteFolder when perm.Path != null:
                    list.Add(perm.Path);
                    break;
                case SandboxPermissionType.DiskWritePlatformUserTempFolder:
                    var tmp = Environment.GetEnvironmentVariable("TMPDIR");
                    if (!string.IsNullOrEmpty(tmp)) list.Add(tmp);
                    break;
                case SandboxPermissionType.DiskWritePlatformGlobalTempFolder:
                    list.Add(Path.GetTempPath());
                    break;
            }
        }
        return list;
    }

    public bool IsUnrestricted() => HasFullDiskReadAccess() && HasFullDiskWriteAccess() && HasFullNetworkAccess();
}
