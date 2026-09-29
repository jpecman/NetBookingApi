using System.ComponentModel;
using System.Diagnostics;

namespace BookingApi.Tests.Fixtures;

/// <summary>
/// Points Testcontainers at a podman socket when no Docker socket is available.
/// Must run before the first Testcontainers type is used; see docs/testcontainers-podman.md.
/// </summary>
public static class ContainerRuntime
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(5);

    public static void Configure()
    {
        if (IsConfiguredByUser() || OperatingSystem.IsWindows() || File.Exists("/var/run/docker.sock"))
            return;

        var socket = OperatingSystem.IsMacOS() ? FindMacSocket() : FindLinuxSocket();

        if (socket is null)
        {
            if (TryRun("podman", "--version") is not null)
                Console.Error.WriteLine(
                    "Podman is installed but its socket was not found. Is the machine running? `podman machine start`");
            return;
        }

        Environment.SetEnvironmentVariable("DOCKER_HOST", $"unix://{socket}");
        // Ryuk bind-mounts the socket into its container, which rootless podman refuses.
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }

    private static bool IsConfiguredByUser()
    {
        string[] variables = ["DOCKER_HOST", "TESTCONTAINERS_HOST_OVERRIDE", "TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE"];
        if (variables.Any(v => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v))))
            return true;

        var properties = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".testcontainers.properties");

        return File.Exists(properties)
               && File.ReadLines(properties).Any(line => line.TrimStart().StartsWith("docker.host", StringComparison.Ordinal));
    }

    private static string? FindLinuxSocket()
    {
        var runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrEmpty(runtimeDir))
        {
            var socket = Path.Combine(runtimeDir, "podman", "podman.sock");
            if (File.Exists(socket))
                return socket;
        }

        if (TryRun("id", "-u") is not { } uid)
            return null;

        var fallback = $"/run/user/{uid}/podman/podman.sock";
        return File.Exists(fallback) ? fallback : null;
    }

    private static string? FindMacSocket()
    {
        // The reported directory comes from the current shell's $TMPDIR, which may differ from the one the
        // machine started under (e.g. inside `nix develop`), so only the file name is trusted.
        var reported = TryRun("podman", "machine", "inspect", "--format", "{{.ConnectionInfo.PodmanSocket.Path}}");
        var fileName = string.IsNullOrEmpty(reported) ? null : Path.GetFileName(reported);
        if (string.IsNullOrEmpty(fileName))
            return null;

        string?[] tempDirs = [Environment.GetEnvironmentVariable("TMPDIR"), TryRun("getconf", "DARWIN_USER_TEMP_DIR"), "/tmp"];

        return tempDirs
            .Where(dir => !string.IsNullOrEmpty(dir))
            .Select(dir => Path.Combine(dir!, "podman", fileName))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>Runs a command and returns its trimmed output, or null if it is missing, fails or times out.</summary>
    private static string? TryRun(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(CommandTimeout))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            return process.ExitCode == 0 ? output.Result.Trim() : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }
}
