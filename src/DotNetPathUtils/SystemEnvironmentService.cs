using System.Reflection;
using System.Runtime.InteropServices;
using Xdg.Directories;

namespace DotNetPathUtils;

public class SystemEnvironmentService : IEnvironmentService
{
    public string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target) =>
        Environment.GetEnvironmentVariable(variable, target);

    public void SetEnvironmentVariable(
        string variable,
        string? value,
        EnvironmentVariableTarget target
    ) => Environment.SetEnvironmentVariable(variable, value, target);

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string? GetApplicationName() => Assembly.GetEntryAssembly()?.GetName().Name;

    public string GetXdgConfigHome() => BaseDirectory.ConfigHome;

    public bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public void BroadcastEnvironmentChange()
    {
        if (!IsWindows())
            return;

        try
        {
            // Use UIntPtr for the result out parameter, though we discard it with _.
            SendMessageTimeout(
                new IntPtr(HWND_BROADCAST),
                WM_SETTINGCHANGE,
                UIntPtr.Zero,
                "Environment",
                SMTO_ABORTIFHUNG | SMTO_NOTIMEOUTIFNOTHUNG,
                5000,
                out _
            );
        }
        catch (Exception ex)
        {
            // It's acceptable for a service to log a warning for a non-critical failure.
            Console.Error.WriteLine(
                $"Warning: Failed to broadcast environment variable change. A restart or re-login might be needed. Error: {ex.Message}"
            );
        }
    }

    #region P/Invoke Declarations
    private const int HWND_BROADCAST = 0xFFFF;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    private const uint SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        UIntPtr wParam,
        string lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult
    );
    #endregion
}
