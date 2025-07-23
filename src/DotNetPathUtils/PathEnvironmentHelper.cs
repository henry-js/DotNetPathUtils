using System.Security;

namespace DotNetPathUtils;

public class PathEnvironmentHelper
{
    private readonly IEnvironmentService _service;
    private readonly string _pathVariableName;

    public PathEnvironmentHelper(IEnvironmentService service)
        : this(service, "PATH") { }

    internal PathEnvironmentHelper(IEnvironmentService service, string pathVariableName)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _pathVariableName =
            pathVariableName ?? throw new ArgumentNullException(nameof(pathVariableName));
    }

    public PathUpdateResult EnsureApplicationXdgConfigDirectoryIsInPath(
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        string? appName = _service.GetApplicationName();
        if (string.IsNullOrWhiteSpace(appName))
            return PathUpdateResult.Error;

        string configHome = _service.GetXdgConfigHome();
        if (string.IsNullOrWhiteSpace(configHome))
            return PathUpdateResult.Error;

        string appConfigPath = Path.Combine(configHome, appName);
        _service.CreateDirectory(appConfigPath);

        return EnsureDirectoryIsInPath(appConfigPath, target);
    }

    public PathUpdateResult EnsureDirectoryIsInPath(
        string directoryPath,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (
            target == EnvironmentVariableTarget.Process
            && _pathVariableName.Equals("PATH", StringComparison.OrdinalIgnoreCase)
        )
            throw new ArgumentException(
                "Process target is not supported for persistent PATH changes. Use User or Machine for persistence.",
                nameof(target)
            );

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
            return PathUpdateResult.PathAlreadyExists;
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
            return PathUpdateResult.PathAdded;
        }
        catch (SecurityException ex)
        {
            throw new SecurityException(
                $"Failed to set {target} PATH variable. Administrator privileges may be required.",
                ex
            );
        }
    }

    public PathRemoveResult RemoveApplicationXdgConfigDirectoryFromPath(
        EnvironmentVariableTarget target = EnvironmentVariableTarget.User
    )
    {
        // This method should also be updated to use the generic RemoveDirectoryFromPath
        string? appName = _service.GetApplicationName();
        if (string.IsNullOrWhiteSpace(appName))
            return PathRemoveResult.Error;

        string configHome = _service.GetXdgConfigHome();
        if (string.IsNullOrWhiteSpace(configHome))
            return PathRemoveResult.Error;

        string appConfigPath = Path.Combine(configHome, appName);
        return RemoveDirectoryFromPath(appConfigPath, target);
    }

    public PathRemoveResult RemoveDirectoryFromPath(
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
            return PathRemoveResult.PathNotFound;

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
            return PathRemoveResult.PathNotFound;

        string newPathVariable = string.Join(Path.PathSeparator.ToString(), paths);
        _service.SetEnvironmentVariable(_pathVariableName, newPathVariable, target);

        if (_service.IsWindows())
            _service.BroadcastEnvironmentChange();

        return PathRemoveResult.PathRemoved;
    }
}
