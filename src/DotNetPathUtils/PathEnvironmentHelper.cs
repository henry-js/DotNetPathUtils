using System.Security;
using Microsoft.Extensions.Logging;

namespace DotNetPathUtils;

public class PathEnvironmentHelper
{
    private readonly IEnvironmentService _service;
    private readonly string _pathVariableName;
    private readonly PathUtilsOptions _options;
    private readonly ILogger<PathEnvironmentHelper>? _logger;

    public PathEnvironmentHelper(
        PathUtilsOptions? options = null,
        ILogger<PathEnvironmentHelper>? logger = null
    )
        : this(new SystemEnvironmentService(), "PATH", options, logger) { }

    public PathEnvironmentHelper(
        IEnvironmentService service,
        PathUtilsOptions? options = null,
        ILogger<PathEnvironmentHelper>? logger = null
    )
        : this(service, "PATH", options, logger) { }

    internal PathEnvironmentHelper(
        IEnvironmentService service,
        string pathVariableName,
        PathUtilsOptions? options = null,
        ILogger<PathEnvironmentHelper>? logger = null
    )
    {
        if (string.IsNullOrWhiteSpace(pathVariableName))
            throw new ArgumentNullException(nameof(pathVariableName));

        _service = service ?? throw new ArgumentNullException(nameof(service));
        _pathVariableName = pathVariableName;
        _options = options ?? PathUtilsOptions.Default;
        _logger = logger;
    }

    public PathModificationResult EnsureApplicationXdgConfigDirectoryIsInPath(
        string? appName = null,
        PathUtilsOptions? methodOptions = null, // Renamed for clarity
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        var effectiveOptions = methodOptions ?? _options;

        string formattedName = GetFormattedApplicationName(appName, effectiveOptions);
        if (string.IsNullOrWhiteSpace(formattedName))
            return new PathModificationResult(PathUpdateStatus.Error);

        string configHome = _service.GetXdgConfigHome();
        if (string.IsNullOrWhiteSpace(configHome))
            return new PathModificationResult(PathUpdateStatus.Error);

        string appConfigPath = Path.Combine(configHome, formattedName);
        return EnsureDirectoryIsInPath(appConfigPath, target);
    }

    public PathModificationResult EnsureDirectoryIsInPath(
        string directoryPath,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));

        if (!Path.IsPathRooted(directoryPath))
        {
            throw new ArgumentException(
                "The directory path must be a fully rooted, absolute path to avoid ambiguity.",
                nameof(directoryPath)
            );
        }

        try
        {
            _service.CreateDirectory(directoryPath);
        }
        catch (Exception ex)
        {
            _logger?.DirectoryCreationFailed(directoryPath, ex.Message);
            return new PathModificationResult(PathUpdateStatus.Error);
        }

        if (
            target == EnvironmentVariableTarget.Process
            && _pathVariableName.Equals("PATH", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new ArgumentException(
                "Process target is not supported for persistent PATH changes. Use User or Machine for persistence.",
                nameof(target)
            );
        }

        string normalizedDirectoryToAdd = _service
            .GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string? currentPathVariable = _service.GetEnvironmentVariable(_pathVariableName, target);
        List<string> paths =
        [
            .. currentPathVariable
                ?.Split(Path.PathSeparator)
                ?.Where(p => !string.IsNullOrWhiteSpace(p)) ?? [],
        ];

        bool pathExists = paths.Any(p =>
        {
            try
            {
                string normalizedExisting = _service
                    .GetFullPath(p)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return normalizedExisting.Equals(
                    normalizedDirectoryToAdd,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch
            {
                return false;
            }
        });

        if (pathExists)
        {
            return new PathModificationResult(
                PathUpdateStatus.PathAlreadyExists,
                normalizedDirectoryToAdd
            );
        }

        paths.Add(normalizedDirectoryToAdd);
        string newPathVariable = string.Join(Path.PathSeparator.ToString(), paths);

        try
        {
            _service.SetEnvironmentVariable(_pathVariableName, newPathVariable, target);
            if (_service.IsWindows())
            {
                _service.BroadcastEnvironmentChange();
            }
            return new PathModificationResult(PathUpdateStatus.PathAdded, normalizedDirectoryToAdd);
        }
        catch (SecurityException ex)
        {
            throw new SecurityException(
                $"Failed to set {target} PATH variable. Administrator privileges may be required.",
                ex
            );
        }
    }

    public PathRemovalResult RemoveApplicationXdgConfigDirectoryFromPath(
        string? appName = null,
        PathUtilsOptions? methodOptions = null,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        var effectiveOptions = methodOptions ?? _options;

        string formattedName = GetFormattedApplicationName(appName, effectiveOptions);
        if (string.IsNullOrWhiteSpace(formattedName))
            return new PathRemovalResult(PathRemoveStatus.Error);

        string configHome = _service.GetXdgConfigHome();
        if (string.IsNullOrWhiteSpace(configHome))
            return new PathRemovalResult(PathRemoveStatus.Error);

        string appConfigPath = Path.Combine(configHome, formattedName);
        return RemoveDirectoryFromPath(appConfigPath, target);
    }

    public PathRemovalResult RemoveDirectoryFromPath(
        string directoryPath,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));

        string normalizedPathToRemove = _service
            .GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string? currentPathVariable = _service.GetEnvironmentVariable(_pathVariableName, target);
        if (string.IsNullOrEmpty(currentPathVariable))
            return new PathRemovalResult(PathRemoveStatus.PathNotFound, normalizedPathToRemove);

        List<string> paths =
        [
            .. currentPathVariable
                ?.Split(Path.PathSeparator)
                ?.Where(p => !string.IsNullOrWhiteSpace(p)) ?? [],
        ];

        int itemsRemoved = paths.RemoveAll(p =>
        {
            try
            {
                return _service
                    .GetFullPath(p)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Equals(normalizedPathToRemove, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        });

        if (itemsRemoved == 0)
            return new PathRemovalResult(PathRemoveStatus.PathNotFound, normalizedPathToRemove);

        string newPathVariable = string.Join(Path.PathSeparator.ToString(), paths);
        _service.SetEnvironmentVariable(_pathVariableName, newPathVariable, target);

        if (_service.IsWindows())
            _service.BroadcastEnvironmentChange();

        return new PathRemovalResult(PathRemoveStatus.PathRemoved, normalizedPathToRemove);
    }

    private string GetFormattedApplicationName(string? appName, PathUtilsOptions options)
    {
        string name = appName ?? _service.GetApplicationName();
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        if (options.DirectoryNameCase == DirectoryNameCase.CamelCase)
        {
            name = name.ToCamelCase();
        }
        var notAllowedChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(notAllowedChars) != -1)
        {
            throw new ArgumentException(
                "The application name contains invalid characters.",
                nameof(appName)
            );
        }

        if (options.PrefixWithPeriod && !name.StartsWith("."))
        {
            name = '.' + name;
        }

        return name;
    }
}
