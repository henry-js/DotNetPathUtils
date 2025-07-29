namespace DotNetPathUtils;

public interface IEnvironmentService
{
    string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target);
    void SetEnvironmentVariable(string variable, string? value, EnvironmentVariableTarget target);
    string GetFullPath(string path);
    void CreateDirectory(string path);
    string GetApplicationName();
    string GetXdgConfigHome();
    void BroadcastEnvironmentChange();
    bool IsWindows();
}
