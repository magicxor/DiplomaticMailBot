using Moq;
using DiplomaticMailBot.Services;
using DiplomaticMailBot.Infra.Repositories.Contracts;

namespace DiplomaticMailBot.Tests.Unit.Services;

[TestFixture]
public class SeedServiceTests
{
    private Mock<ISeedRepository> _seedRepositoryMock;
    private SeedService _seedService;

    [SetUp]
    public void SetUp()
    {
        _seedRepositoryMock = new Mock<ISeedRepository>();
        _seedService = new SeedService(_seedRepositoryMock.Object);
    }

    [Test]
    public async Task InitializeDbAsync_CallsMigrateAndSeed()
    {
        await _seedService.InitializeDbAsync();

        _seedRepositoryMock.Verify(repo => repo.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
        _seedRepositoryMock.Verify(repo => repo.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitializeDbAsync_CanBeCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _seedService.InitializeDbAsync(cts.Token);

        _seedRepositoryMock.Verify(repo => repo.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
        _seedRepositoryMock.Verify(repo => repo.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
