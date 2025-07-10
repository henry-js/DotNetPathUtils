// --- File: PathEnvironmentHelper.cs ---

public enum PathUpdateResult
{
    /// <summary>
    /// The directory was successfully added to the PATH.
    /// </summary>
    PathAdded,
    /// <summary>
    /// The directory was already present in the PATH; no changes were made.
    /// </summary>
    PathAlreadyExists,
    /// <summary>
    /// An error occurred, and the operation could not be completed.
    /// </summary>
    Error
}