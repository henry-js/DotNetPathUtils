namespace DotNetPathUtils;

public class PathRemovalResult
{
    public PathRemoveStatus Status { get; }

    /// <summary>
    /// The full, normalized path that was targeted for removal.
    /// </summary>
    public string? FullPath { get; }

    public bool Succeeded => Status == PathRemoveStatus.PathRemoved;

    internal PathRemovalResult(PathRemoveStatus status, string? fullPath = null)
    {
        Status = status;
        FullPath = fullPath;
    }
}
