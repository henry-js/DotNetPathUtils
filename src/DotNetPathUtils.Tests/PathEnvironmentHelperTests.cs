using System.Security;
using System.Threading.Tasks;
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
        var rootDir = Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/";
        var directoryToAdd = Path.Combine(rootDir, "MyTool");
        var existingDir = Path.Combine(rootDir, "ExistingPath");
        var expectedNewPath = $"{existingDir}{Path.PathSeparator}{directoryToAdd}";

        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]);
        _service
            .GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
            .Returns(existingDir);
        _service.IsWindows().Returns(OperatingSystem.IsWindows()); // Use the real OS for the test

        // Act
        var result = _helper.EnsureDirectoryIsInPath(
            directoryToAdd,
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);
        _service
            .Received(1)
            .SetEnvironmentVariable("PATH", expectedNewPath, EnvironmentVariableTarget.User);
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Path_Already_Exists_Returns_AlreadyExists()
    {
        // Arrange
        // 1. Build platform-agnostic paths
        var rootDir = OperatingSystem.IsWindows() ? @"C:\" : "/";
        var directoryToAdd = Path.Combine(rootDir, "MyTool");
        var otherExistingDir = Path.Combine(rootDir, "ExistingPath");
        var existingPath = $"{otherExistingDir}{Path.PathSeparator}{directoryToAdd}";

        // 2. Setup mocks
        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]);
        _service
            .GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>())
            .Returns(existingPath);

        // Act
        // 3. THIS IS THE FIX: Ensure we are calling the correct method.
        var result = _helper.EnsureDirectoryIsInPath(
            directoryToAdd,
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAlreadyExists);
        _service.DidNotReceiveWithAnyArgs().SetEnvironmentVariable(default!, default, default);
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
        var result = _helper.EnsureApplicationXdgConfigDirectoryIsInPath(
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);

        _service.Received(1).CreateDirectory(expectedPath);
        _service
            .Received(1)
            .SetEnvironmentVariable("PATH", expectedPath, EnvironmentVariableTarget.User);
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Given_Null_Directory_Throws_ArgumentNullException()
    {
        // Act & Assert
        await Assert
            .That(() => _helper.EnsureDirectoryIsInPath(null!, EnvironmentVariableTarget.User))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Target_Is_Process_Throws_ArgumentException()
    {
        // Act & Assert
        await Assert
            .That(() =>
                _helper.EnsureDirectoryIsInPath("some-path", EnvironmentVariableTarget.Process)
            )
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Equivalent_Path_With_Trailing_Slash_Exists_Returns_AlreadyExists()
    {
        // Arrange
        // 1. Create a platform-agnostic base path.
        var rootDir =
            Path.GetPathRoot(Directory.GetCurrentDirectory())
            ?? (OperatingSystem.IsWindows() ? @"C:\" : "/");
        var directoryWithoutSlash = Path.Combine(rootDir, "MyTool");

        // 2. THIS IS THE FIX: Create the version with the correct trailing slash for the current OS.
        var directoryWithSlash = directoryWithoutSlash + Path.DirectorySeparatorChar;

        // 3. Set up the mocks to return these well-defined paths.
        _service.GetFullPath(directoryWithoutSlash).Returns(directoryWithoutSlash);
        _service.GetFullPath(directoryWithSlash).Returns(directoryWithSlash);
        _service
            .GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>())
            .Returns(directoryWithSlash);

        // Act
        // We try to add the version WITHOUT the slash.
        var result = _helper.EnsureDirectoryIsInPath(
            directoryWithoutSlash,
            EnvironmentVariableTarget.User
        );

        // Assert
        // The code should correctly identify it as a duplicate and do nothing.
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAlreadyExists);
        _service.DidNotReceiveWithAnyArgs().SetEnvironmentVariable(default!, default, default);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task EnsureDirectoryIsInPath_When_Current_Path_Is_Null_Or_Empty_Adds_Path_Correctly(
        string? currentPath
    )
    {
        // Arrange
        var rootDir = Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/";
        var directoryToAdd = Path.Combine(rootDir, "MyNewTool");

        _service
            .GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>())
            .Returns(currentPath);
        _service.GetFullPath(directoryToAdd).Returns(directoryToAdd);
        _service.IsWindows().Returns(OperatingSystem.IsWindows()); // Use the real OS

        // Act
        var result = _helper.EnsureDirectoryIsInPath(
            directoryToAdd,
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);
        _service
            .Received(1)
            .SetEnvironmentVariable("PATH", directoryToAdd, EnvironmentVariableTarget.User);
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Existing_Path_Contains_Invalid_Entry_Does_Not_Crash()
    {
        // Arrange
        var rootDir = Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/";
        var directoryToAdd = Path.Combine(rootDir, "GoodPath");
        var otherExistingDir = Path.Combine(rootDir, "AnotherPath");
        // We can still use a known invalid character for the test's purpose.
        var invalidEntry = otherExistingDir + "<";
        var existingPath = $"{otherExistingDir}{Path.PathSeparator}{invalidEntry}";

        _service
            .GetEnvironmentVariable("PATH", Arg.Any<EnvironmentVariableTarget>())
            .Returns(existingPath);
        _service.GetFullPath(directoryToAdd).Returns(directoryToAdd);
        _service.GetFullPath(otherExistingDir).Returns(otherExistingDir);
        _service.GetFullPath(invalidEntry).Throws<ArgumentException>(); // Mock the failure

        // Act
        var result = _helper.EnsureDirectoryIsInPath(
            directoryToAdd,
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathUpdateResult.PathAdded);
        var expectedNewPath = $"{existingPath}{Path.PathSeparator}{directoryToAdd}";
        _service
            .Received(1)
            .SetEnvironmentVariable("PATH", expectedNewPath, Arg.Any<EnvironmentVariableTarget>());
    }

    [Test]
    public async Task EnsureDirectoryIsInPath_When_Set_Fails_With_SecurityException_Rethrows_With_Custom_Message()
    {
        // Arrange
        _service.GetEnvironmentVariable(default!, default).ReturnsForAnyArgs("");
        _service.GetFullPath(Arg.Any<string>()).Returns(x => (string)x[0]);

        var originalException = new SecurityException("Access Denied.");
        _service
            .When(s =>
                s.SetEnvironmentVariable(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<EnvironmentVariableTarget>()
                )
            )
            .Throw(originalException);

        // Act & Assert
        var ex = await Assert
            .That(() =>
                _helper.EnsureDirectoryIsInPath("any_path", EnvironmentVariableTarget.Machine)
            )
            .ThrowsExactly<SecurityException>();

        // Verify the message and that the original exception is wrapped
        await Assert
            .That(ex!.Message)
            .IsEqualTo(
                "Failed to set Machine PATH variable. Administrator privileges may be required."
            );
        await Assert.That(ex.InnerException).IsEquivalentTo(originalException);
    }

    // In PathEnvironmentHelperTests.cs

    [Test]
    public async Task RemoveApplicationXdgConfigDirectoryFromPath_When_Path_Exists_Removes_It()
    {
        // Arrange
        var appName = "MyCoolApp";
        var xdgHome = "/home/user/.config";
        var pathToRemove = Path.Combine(xdgHome, appName);
        var existingPath =
            $"/usr/bin{Path.PathSeparator}{pathToRemove}{Path.PathSeparator}/usr/local/bin";
        var expectedNewPath = $"/usr/bin{Path.PathSeparator}/usr/local/bin";

        _service.GetApplicationName().Returns(appName);
        _service.GetXdgConfigHome().Returns(xdgHome);
        _service.GetFullPath(pathToRemove).Returns(pathToRemove); // Keep it simple for the test
        _service
            .GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
            .Returns(existingPath);

        // Act
        var result = _helper.RemoveApplicationXdgConfigDirectoryFromPath(
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathRemoveResult.PathRemoved);
        _service
            .Received(1)
            .SetEnvironmentVariable("PATH", expectedNewPath, EnvironmentVariableTarget.User);
    }

    [Test]
    public async Task RemoveApplicationXdgConfigDirectoryFromPath_When_Path_Does_Not_Exist_Returns_NotFound()
    {
        // Arrange
        var appName = "MyCoolApp";
        var xdgHome = "/home/user/.config";
        var pathThatShouldBeRemoved = Path.Combine(xdgHome, appName);
        var existingPath = "/usr/bin:/usr/local/bin"; // Path does not contain the target

        _service.GetApplicationName().Returns(appName);
        _service.GetXdgConfigHome().Returns(xdgHome);
        _service.GetFullPath(pathThatShouldBeRemoved).Returns(pathThatShouldBeRemoved);
        _service
            .GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
            .Returns(existingPath);

        // Act
        var result = _helper.RemoveApplicationXdgConfigDirectoryFromPath(
            EnvironmentVariableTarget.User
        );

        // Assert
        await Assert.That(result).IsEqualTo(PathRemoveResult.PathNotFound);
        _service.DidNotReceiveWithAnyArgs().SetEnvironmentVariable(default!, default, default);
    }

    [Test]
    public async Task RemoveApplicationXdgConfigDirectoryFromPath_When_AppName_Is_Unknown_Returns_Error()
    {
        // Arrange
        _service.GetApplicationName().Returns((string?)null);

        // Act
        var result = _helper.RemoveApplicationXdgConfigDirectoryFromPath();

        // Assert
        await Assert.That(result).IsEqualTo(PathRemoveResult.Error);
        _service.DidNotReceiveWithAnyArgs().SetEnvironmentVariable(default!, default, default);
    }
}
