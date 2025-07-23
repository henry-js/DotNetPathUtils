using DotNetPathUtils;
using Velopack;

VelopackApp.Build().Run();

IEnvironmentService service = new SystemEnvironmentService();
Console.WriteLine("Created SystemEnvironmentService instance.\n");

if (service.IsWindows())
{
    Console.WriteLine("Running on Windows.\n");
}
else
{
    Console.WriteLine("Running on non-Windows platform.\n");
}

Console.WriteLine($"App name: {service.GetApplicationName()}\n");

Console.WriteLine($"XDG Config Home: {service.GetXdgConfigHome()}\n");

Console.WriteLine(
    $"Path: {service.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)}\n"
);

var envHelper = new PathEnvironmentHelper(service);

Console.WriteLine("Created PathEnvironmentHelper instance.\n");

var result = envHelper.EnsureApplicationXdgConfigDirectoryIsInPath();

Console.WriteLine($"EnsureApplicationXdgConfigDirectoryIsInPath result: {result}\n");

var removeResult = envHelper.RemoveApplicationXdgConfigDirectoryFromPath();
