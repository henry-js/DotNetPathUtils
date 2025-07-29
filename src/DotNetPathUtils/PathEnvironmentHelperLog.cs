using Microsoft.Extensions.Logging;

namespace DotNetPathUtils;

internal static partial class PathEnvironmentHelperLog
{
    [LoggerMessage(1000, LogLevel.Information, "Ensuring app config path '{Path}' is in PATH.")]
    public static partial void EnsuringAppConfigPath(this ILogger logger, string path);

    [LoggerMessage(1001, LogLevel.Warning, "The path '{Path}' already exists in PATH.")]
    public static partial void PathAlreadyExists(this ILogger logger, string path);

    [LoggerMessage(1002, LogLevel.Information, "Path '{Path}' was added to PATH.")]
    public static partial void PathAdded(this ILogger logger, string path);

    [LoggerMessage(1003, LogLevel.Error, "Failed to create directory '{Path}': {Message}")]
    public static partial void DirectoryCreationFailed(
        this ILogger logger,
        string path,
        string message
    );

    [LoggerMessage(1004, LogLevel.Error, "Exception setting environment variable: {Message}")]
    public static partial void SetEnvVarFailed(this ILogger logger, string message);

    [LoggerMessage(1005, LogLevel.Information, "Removing path '{Path}' from PATH.")]
    public static partial void RemovingPath(this ILogger logger, string path);

    [LoggerMessage(1006, LogLevel.Warning, "Path '{Path}' not found in PATH.")]
    public static partial void PathNotFound(this ILogger logger, string path);

    [LoggerMessage(1007, LogLevel.Information, "Path '{Path}' was removed from PATH.")]
    public static partial void PathRemoved(this ILogger logger, string path);
}
