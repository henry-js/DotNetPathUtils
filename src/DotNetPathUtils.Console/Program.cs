using DotNetPathUtils;
using Velopack;

VelopackApp.Build().Run();

IEnvironmentService service = new SystemEnvironmentService();
Console.WriteLine("Created SystemEnvironmentService instance.");

var envHelper = new PathEnvironmentHelper(service);
Console.WriteLine("Created PathEnvironmentHelper instance.");
