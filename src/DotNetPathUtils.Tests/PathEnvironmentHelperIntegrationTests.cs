// --- File: PathEnvironmentHelperIntegrationTests.cs ---

using System;
using System.IO;
using System.Threading.Tasks;
using DotNetPathUtils;
using OnPath.Net;
using TUnit.Core;

namespace OnPath.Net.Tests
{
    [Property("Category", "Integration")]
    public class PathEnvironmentHelperIntegrationTests
    {
        private PathEnvironmentHelper _helper = null!; // Non-nullable, initialized in setup.

        // A unique name for our test variable to avoid conflicts with real system variables.
        private const string TestPathVariableName = "ONPATH_INTEGRATION_TEST_VAR";

        // This method runs before every single test in this class.
        [Before(HookType.Test)]
        public void Setup()
        {
            // For integration tests, we instantiate the REAL service.
            var realService = new SystemEnvironmentService();

            // We use the constructor overload to target our temporary, safe environment variable.
            _helper = new PathEnvironmentHelper(realService, TestPathVariableName);
        }

        // This method runs after every test, ensuring a clean state for the next run.
        [After(HookType.Test)]
        public async Task Cleanup()
        {
            // Clean up the process-level environment variable.
            Environment.SetEnvironmentVariable(TestPathVariableName, null, EnvironmentVariableTarget.Process);
            // Even though this is a synchronous call, we can return a completed task.
            await Task.CompletedTask;
        }

        [Test]
        public async Task AddAndRemove_Cycle_Works_Correctly_On_Live_Process_Environment()
        {
            // Arrange
            // Use the real AppDomain's BaseDirectory for a realistic test path.
            var directory = AppDomain.CurrentDomain.BaseDirectory!;

            // 2. THIS IS THE FIX: Create the normalized version that our code produces.
            var expectedPathInEnvironment = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Act: Part 1 - Add the path to the PROCESS environment
            var addResult = _helper.EnsureDirectoryIsInPath(directory, EnvironmentVariableTarget.Process);

            // Assert: Part 1 - Check the result and the real environment variable
            await Assert.That(addResult).IsEqualTo(PathUpdateResult.PathAdded);

            var currentPath = Environment.GetEnvironmentVariable(TestPathVariableName, EnvironmentVariableTarget.Process);
            await Assert.That(currentPath).IsNotNull().And.IsNotEmpty();
            // Use Contains because the GetFullPath normalization might slightly change the string format
            await Assert.That(currentPath!).Contains(expectedPathInEnvironment, StringComparison.OrdinalIgnoreCase);

            // Act: Part 2 - Remove the path from the PROCESS environment
            var removeResult = _helper.RemoveDirectoryFromPath(directory, EnvironmentVariableTarget.Process);

            // Assert: Part 2 - Check the result and the now-empty environment variable
            await Assert.That(removeResult).IsEqualTo(PathRemoveResult.PathRemoved);

            var finalPath = Environment.GetEnvironmentVariable(TestPathVariableName, EnvironmentVariableTarget.Process);
            await Assert.That(finalPath).IsEmpty();
        }
    }
}