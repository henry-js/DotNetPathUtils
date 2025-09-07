namespace DotNetPathUtils;

public class PathModificationResult
{
    public PathUpdateStatus Status { get; }
    public string? FullPath { get; }

    public bool Succeeded =>
        Status == PathUpdateStatus.PathAdded || Status == PathUpdateStatus.PathAlreadyExists;

    internal PathModificationResult(PathUpdateStatus status, string? fullPath = null)
    {
        Status = status;
        FullPath = fullPath;
    }
}
