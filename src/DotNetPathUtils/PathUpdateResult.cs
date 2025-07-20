namespace DotNetPathUtils;

public enum PathUpdateResult
{
    PathAdded,
    PathAlreadyExists,

    Error
}

public enum PathRemoveResult
{
    PathRemoved,
    PathNotFound,

    Error
}