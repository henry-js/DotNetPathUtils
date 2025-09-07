namespace DotNetPathUtils;

public record PathUtilsOptions
{
    public bool PrefixWithPeriod { get; set; } = true;
    public DirectoryNameCase DirectoryNameCase { get; set; }
    public static readonly PathUtilsOptions Default = new();
}

public enum DirectoryNameCase
{
    CamelCase,
}
