namespace DotNetPathUtils;

public enum PathUpdateStatus
{
    Error,
    PathAdded,
    PathAlreadyExists,
}

public enum PathRemoveStatus
{
    PathRemoved,
    PathNotFound,

    Error,
}
