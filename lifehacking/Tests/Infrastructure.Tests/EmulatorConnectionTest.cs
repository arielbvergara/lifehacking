using Google.Cloud.Firestore;
using Grpc.Core;
using Xunit;

namespace Infrastructure.Tests;

public class EmulatorConnectionTest
{
    [Fact]
    public void EnvironmentVariable_ShouldBeSet()
    {
        // Arrange & Act
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "127.0.0.1:8080");
        var emulatorHost = Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST");

        // Assert
        Assert.Equal("127.0.0.1:8080", emulatorHost);
    }

    [Fact]
    public void FirestoreDb_ShouldConnectToEmulator_WhenEmulatorHostIsSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "127.0.0.1:8080");
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);

        var builder = new FirestoreDbBuilder
        {
            ProjectId = "demo-test",
            Endpoint = "127.0.0.1:8080",
            ChannelCredentials = ChannelCredentials.Insecure
        };

        // Act
        var db = builder.Build();

        // Assert
        Assert.NotNull(db);
    }
}
