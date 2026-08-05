namespace Shared.AI.Extensions;

using Shared.AI.Python;

file static class PythonAIOptionsCopyExtensions
{
    public static void CopyTo(this PythonAIOptions source, PythonAIOptions target)
    {
        target.PythonExecutablePath = source.PythonExecutablePath;
        target.VirtualEnvironmentPath = source.VirtualEnvironmentPath;
        target.WorkerModule = source.WorkerModule;
        target.WorkerScriptPath = source.WorkerScriptPath;
        target.WorkerRootPath = source.WorkerRootPath;
        target.Pools = source.Pools;
        target.StartupTimeout = source.StartupTimeout;
        target.RequestTimeout = source.RequestTimeout;
        target.HeartbeatInterval = source.HeartbeatInterval;
        target.RestartDelay = source.RestartDelay;
        target.MaxRestartAttempts = source.MaxRestartAttempts;
        target.MaxRequestRetries = source.MaxRequestRetries;
        target.Scheduling = source.Scheduling;
        target.ProtocolVersion = source.ProtocolVersion;
        target.WarmupModels = source.WarmupModels;
        target.CustomFunctions = source.CustomFunctions;
        target.AllowedOperations = source.AllowedOperations;
        target.MinimumPythonVersion = source.MinimumPythonVersion;
    }
}
