using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.AI.Python;
using Xunit;

namespace Usm.Shared.AI.Tests;

public sealed class PythonProcessManagerTests
{
    [Fact]
    public async Task RejectsInvalidOperationNames()
    {
        var manager = CreateManager();
        var request = new PythonRequest(Guid.NewGuid().ToString("N"), "drop;table", null, new Dictionary<string, object?>());

        await Assert.ThrowsAsync<ArgumentException>(() => manager.InvokeAsync(request));
    }

    [Fact]
    public async Task InvokesCustomFunctionThroughPersistentWorker()
    {
        var pythonExecutable = FindPythonExecutable();
        if (pythonExecutable is null)
        {
            return;
        }

        var workerRoot = FindWorkerRoot();
        if (workerRoot is null)
        {
            return;
        }

        var manager = new PythonProcessManager(
            Options.Create(new PythonAIOptions
            {
                PythonExecutablePath = pythonExecutable,
                WorkerRootPath = workerRoot,
                WarmupModels = false,
                Pools =
                [
                    new PythonWorkerPoolOptions
                    {
                        Name = "cpu",
                        Role = PythonWorkerRole.Cpu,
                        WorkerCount = 1,
                        MaxConcurrentRequestsPerWorker = 2,
                        Models = new List<string>()
                    }
                ],
                CustomFunctions =
                [
                    new PythonCustomFunctionOptions
                    {
                        Operation = "math.add",
                        Module = "test_fixtures.math_ops",
                        Function = "add",
                        PreferredRole = PythonWorkerRole.Generic
                    }
                ]
            }),
            NullLogger<PythonProcessManager>.Instance,
            NullLoggerFactory.Instance);

        await manager.StartAsync();
        var result = await manager.InvokeCustomAsync<Dictionary<string, int>>(
            "math.add",
            new Dictionary<string, object?>
            {
                ["a"] = 2,
                ["b"] = 5
            });

        Assert.Equal(7, result["sum"]);
        await manager.DisposeAsync();
    }

    private static PythonProcessManager CreateManager()
    {
        return new PythonProcessManager(
            Options.Create(new PythonAIOptions
            {
                WarmupModels = false,
                Pools = [new PythonWorkerPoolOptions { WorkerCount = 1 }]
            }),
            NullLogger<PythonProcessManager>.Instance,
            NullLoggerFactory.Instance);
    }

    private static string? FindPythonExecutable()
    {
        foreach (var candidate in OperatingSystem.IsWindows()
            ? new[] { "python.exe", "python3.exe", "python" }
            : new[] { "python3", "python" })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process is not null && process.WaitForExit(3000))
                {
                    return candidate;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static string? FindWorkerRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Shared", "AI", "python_worker");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var sln = Path.Combine(directory.FullName, "Usm.Inventory.MonoRepo.slnx");
            if (File.Exists(sln))
            {
                var worker = Path.Combine(directory.FullName, "Shared", "AI", "python_worker");
                if (Directory.Exists(worker))
                {
                    return worker;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }
}
