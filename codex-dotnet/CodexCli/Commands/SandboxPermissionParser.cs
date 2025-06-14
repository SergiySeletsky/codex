using System;

namespace CodexCli.Commands;

public static class SandboxPermissionParser
{
    public static SandboxPermission Parse(string raw, string basePath)
    {
        if (raw.StartsWith("disk-write-folder="))
        {
            var path = raw.Substring("disk-write-folder=".Length);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("--sandbox-permission disk-write-folder=<PATH> requires a non-empty PATH");
            var full = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(basePath, path));
            return new SandboxPermission(SandboxPermissionType.DiskWriteFolder, full);
        }

        return raw switch
        {
            "disk-full-read-access" => new SandboxPermission(SandboxPermissionType.DiskFullReadAccess),
            "disk-write-platform-user-temp-folder" => new SandboxPermission(SandboxPermissionType.DiskWritePlatformUserTempFolder),
            "disk-write-platform-global-temp-folder" => new SandboxPermission(SandboxPermissionType.DiskWritePlatformGlobalTempFolder),
            "disk-write-cwd" => new SandboxPermission(SandboxPermissionType.DiskWriteCwd),
            "disk-full-write-access" => new SandboxPermission(SandboxPermissionType.DiskFullWriteAccess),
            "network-full-access" => new SandboxPermission(SandboxPermissionType.NetworkFullAccess),
            _ => throw new ArgumentException($"`{raw}` is not a recognised permission. Run with --help to see the accepted values.")
        };
    }
}
