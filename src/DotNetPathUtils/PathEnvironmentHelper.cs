using System.Security;

namespace DotNetPathUtils;

public class PathEnvironmentHelper
{
    private readonly IEnvironmentService _service;
    private const string PathVariableName = "PATH";

    public PathEnvironmentHelper(IEnvironmentService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public PathUpdateResult EnsureApplicationXdgConfigDirectoryIsInPath(EnvironmentVariableTarget target = EnvironmentVariableTarget.User)
    {
        string? appName = _service.GetApplicationName();
        if (string.IsNullOrWhiteSpace(appName)) return PathUpdateResult.Error;

        string configHome = _service.GetXdgConfigHome();
        if (string.IsNullOrWhiteSpace(configHome)) return PathUpdateResult.Error;

        string appConfigPath = Path.Combine(configHome, appName);
        _service.CreateDirectory(appConfigPath);

        return EnsureDirectoryIsInPath(appConfigPath, target);
    }

    public PathUpdateResult EnsureDirectoryIsInPath(string directoryPath, EnvironmentVariableTarget target = EnvironmentVariableTarget.User)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (target == EnvironmentVariableTarget.Process)
            throw new ArgumentException("Process target is not supported for persistent changes.", nameof(target));

        string normalizedDirectoryToAdd = _service.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string? currentPathVariable = _service.GetEnvironmentVariable(PathVariableName, target);
        var paths = new List<string>(currentPathVariable?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? []);

        bool pathExists = paths.Any(p =>
        {
            try
            {
                string normalizedExisting = _service.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return normalizedExisting.Equals(normalizedDirectoryToAdd, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        if (pathExists)
        {
            return PathUpdateResult.PathAlreadyExists;
        }

        paths.Add(normalizedDirectoryToAdd);
        string newPathVariable = string.Join(Path.PathSeparator.ToString(), paths);

        try
        {
            _service.SetEnvironmentVariable(PathVariableName, newPathVariable, target);
            if (_service.IsWindows())
            {
                _service.BroadcastEnvironmentChange();
            }
            return PathUpdateResult.PathAdded;
        }
        catch (SecurityException ex)
        {
            throw new SecurityException($"Failed to set {target} PATH variable. Administrator privileges may be required.", ex);
        }
    }
}