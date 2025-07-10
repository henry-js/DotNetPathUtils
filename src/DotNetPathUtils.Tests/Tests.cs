using System.Security;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TUnit.Assertions.AssertConditions.Throws;

namespace DotNetPathUtils.Tests;

public class PathEnvironmentHelperTests
{
    private readonly IEnvironmentService _service;
    private readonly PathEnvironmentHelper _helper;

    public PathEnvironmentHelperTests()
    {
        _service = Substitute.For<IEnvironmentService>();
        _helper = new PathEnvironmentHelper(_service);
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Path_Does_Not_Exist_Adds_It()
    {
        // Arrange
        var directoryToAdd = @"C:\MyTool";
        var existingPath = @"C:\ExistingPath";
        var expectedNewPath = $"{existingPath}{Path.PathSeparator}{directoryToAdd}";

        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]); // Simple pass-through
        _service.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User).Returns(existingPath);
        _service.IsWindows().Returns(true);

        // Act
        var result = _helper.EnsureDirectoryIsInPath(directoryToAdd, EnvironmentVariableTarget.User);

        // Assert
        // await Assert.That(result).IsEqualTo(PathUpdateResult.PathAlreadyExists);
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);

        _service.Received(1).SetEnvironmentVariable("PATH", expectedNewPath, EnvironmentVariableTarget.User);
        _service.Received(1).BroadcastEnvironmentChange();
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Path_Already_Exists_Returns_AlreadyExists()
    {
        // Arrange
        var directoryToAdd = @"C:\MyTool";
        var existingPath = $@"C:\ExistingPath{Path.PathSeparator}C:\MyTool";

        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]);
        _service.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User).Returns(existingPath);

        // Act
        var result = _helper.EnsureDirectoryIsInPath(directoryToAdd, EnvironmentVariableTarget.User);

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAlreadyExists);

        _service.DidNotReceive().SetEnvironmentVariable(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EnvironmentVariableTarget>());
        _service.DidNotReceive().BroadcastEnvironmentChange();
    }

    [Test]
    public async Task EnsureApplicationXdgConfigDirectoryIsInPath_Constructs_Correct_Path_On_Linux()
    {
        // Arrange
        var appName = "MyCoolApp";
        var xdgHome = "/home/user/.config";
        var expectedPath = Path.Combine(xdgHome, appName);

        _service.GetApplicationName().Returns(appName);
        _service.GetXdgConfigHome().Returns(xdgHome);
        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]);
        _service.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User).Returns(""); // Start with empty path
        _service.IsWindows().Returns(false); // Simulate running on Linux

        // Act
        var result = _helper.EnsureApplicationXdgConfigDirectoryIsInPath(EnvironmentVariableTarget.User);

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);

        _service.Received(1).CreateDirectory(expectedPath);
        _service.Received(1).SetEnvironmentVariable("PATH", expectedPath, EnvironmentVariableTarget.User);
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Given_Null_Directory_Throws_ArgumentNullException()
    {
        // Act & Assert
        await Assert.That(() => _helper.EnsureDirectoryIsInPath(null!, EnvironmentVariableTarget.User)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Target_Is_Process_Throws_ArgumentException()
    {
        // Act & Assert
        await Assert.That(() => _helper.EnsureDirectoryIsInPath("some-path", EnvironmentVariableTarget.Process))
              .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Equivalent_Path_With_Trailing_Slash_Exists_Returns_AlreadyExists()
    {
        // Arrange
        var directoryToAdd = @"C:\MyTool";
        var existingPathWithSlash = @"C:\MyTool\";

        _service.GetFullPath(directoryToAdd).Returns(directoryToAdd);
        _service.GetFullPath(existingPathWithSlash).Returns(existingPathWithSlash);
        _service.GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>()).Returns(existingPathWithSlash);

        // Act
        var result = _helper.EnsureDirectoryIsInPath(directoryToAdd, EnvironmentVariableTarget.User);

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAlreadyExists);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task EnsureDirectoryIsInPath_When_Current_Path_Is_Null_Or_Empty_Adds_Path_Correctly(string? currentPath)
    {
        // Arrange
        var directoryToAdd = @"C:\MyNewTool";

        // Setup the service to return the specified input (null or empty) for the current PATH
        _service.GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>()).Returns(currentPath);

        // Standard setup for mocks
        _service.GetFullPath(directoryToAdd).Returns(directoryToAdd);
        _service.IsWindows().Returns(true);

        // Act
        var result = _helper.EnsureDirectoryIsInPath(directoryToAdd, EnvironmentVariableTarget.User);

        // Assert
        // 1. Verify the operation reported success
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);

        // 2. Verify SetEnvironmentVariable was called with *only the new path*,
        //    since the original was empty. No leading path separator should be present.
        _service.Received(1).SetEnvironmentVariable(
            "PATH",
            directoryToAdd,
            EnvironmentVariableTarget.User
        );

        // 3. Verify the environment change was broadcast (since IsWindows is true)
        _service.Received(1).BroadcastEnvironmentChange();
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Existing_Path_Contains_Invalid_Entry_Does_Not_Crash()
    {
        // Arrange
        var directoryToAdd = @"C:\GoodPath";
        var invalidEntry = @"C:\Bad<Path";
        var existingPath = $@"C:\AnotherPath{Path.PathSeparator}{invalidEntry}";

        _service.GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>()).Returns(existingPath);
        _service.GetFullPath(directoryToAdd).Returns(directoryToAdd); // Normal behavior for the good path
        _service.GetFullPath(@"C:\AnotherPath").Returns(@"C:\AnotherPath");

        // Make GetFullPath throw *only* for the invalid entry
        _service.GetFullPath(invalidEntry).Throws<ArgumentException>();
        // Act
        var result = _helper.EnsureDirectoryIsInPath(directoryToAdd, EnvironmentVariableTarget.User);

        // Assert
        // The helper should have gracefully ignored the bad entry and added the new one
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);

        var expectedNewPath = $"{existingPath}{Path.PathSeparator}{directoryToAdd}";
        _service.Received(1).SetEnvironmentVariable("PATH", expectedNewPath, Arg.Any<EnvironmentVariableTarget>());
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Set_Fails_With_SecurityException_Rethrows_With_Custom_Message()
    {
        // Arrange
        _service.GetEnvironmentVariable(default!, default).ReturnsForAnyArgs("");
        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]);

        var originalException = new SecurityException("Access Denied.");
        _service.When(s => s.SetEnvironmentVariable(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EnvironmentVariableTarget>()))
                .Throw(originalException);

        // Act & Assert
        var ex = await Assert.That(() => _helper.EnsureDirectoryIsInPath("any_path", EnvironmentVariableTarget.Machine))
                       .ThrowsExactly<SecurityException>();

        // Verify the message and that the original exception is wrapped
        await Assert.That(ex!.Message).IsEqualTo("Failed to set Machine PATH variable. Administrator privileges may be required.");
        await Assert.That(ex.InnerException).IsEquivalentTo(originalException);
    }
}