namespace DotNetPathUtils;

public record PathUtilsOptions
{
    public bool PrefixWithPeriod { get; } = true;
    public DirectoryNameCase DirectoryNameCase { get; }
    public static readonly PathUtilsOptions Default = new();
}

public enum DirectoryNameCase
{
    CamelCase,
}
