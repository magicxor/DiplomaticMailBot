using DiplomaticMailBot.Cli.Ref;
using DiplomaticMailBot.Common.Ref;
using DiplomaticMailBot.Domain.Implementations.Ref;
using DiplomaticMailBot.Infra.Repositories.Implementations.Ref;
using DiplomaticMailBot.Services.Ref;
using NetArchTest.Rules;
using System.Reflection;
using DiplomaticMailBot.Infra.Entities.Ref;
using DiplomaticMailBot.Infra.ServiceModels.Ref;
using DiplomaticMailBot.Infra.Telegram.Implementations.Ref;

namespace DiplomaticMailBot.Tests.Unit;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class ArchitectureTests
{
    private static readonly Assembly CliAssembly = typeof(ICliRef).Assembly;
    private static readonly Assembly CommonAssembly = typeof(ICommonRef).Assembly;
    private static readonly Assembly EntitiesAssembly = typeof(IEntitiesRef).Assembly;
    private static readonly Assembly DomainAssembly = typeof(IDomainRef).Assembly;
    private static readonly Assembly ServicesAssembly = typeof(IServicesRef).Assembly;
    private static readonly Assembly RepositoriesAssembly = typeof(IRepositoriesRef).Assembly;
    private static readonly Assembly ServiceModelsAssembly = typeof(IServiceModelsRef).Assembly;
    private static readonly Assembly TelegramInteropAssembly = typeof(ITelegramInteropRef).Assembly;

    [Test]
    public void Common_ShouldNotHaveDependencyOn_AnyOtherProject()
    {
        var result = Types
            .InAssembly(CommonAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                CliAssembly.GetName().Name,
                DomainAssembly.GetName().Name,
                EntitiesAssembly.GetName().Name,
                RepositoriesAssembly.GetName().Name,
                ServiceModelsAssembly.GetName().Name,
                ServicesAssembly.GetName().Name,
                TelegramInteropAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Entities_ShouldNotHaveDependencyOn_SpecifiedProjects()
    {
        var result = Types
            .InAssembly(EntitiesAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                RepositoriesAssembly.GetName().Name,
                ServiceModelsAssembly.GetName().Name,
                ServicesAssembly.GetName().Name,
                DomainAssembly.GetName().Name,
                CliAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Domain_ShouldNotHaveDependencyOn_SpecifiedProjects()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                EntitiesAssembly.GetName().Name,
                RepositoriesAssembly.GetName().Name,
                ServicesAssembly.GetName().Name,
                CliAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Services_ShouldNotHaveDependencyOn_Cli()
    {
        var result = Types
            .InAssembly(ServicesAssembly)
            .Should()
            .NotHaveDependencyOnAny(CliAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Repositories_ShouldNotHaveDependencyOn_SpecifiedProjects()
    {
        var result = Types
            .InAssembly(RepositoriesAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                ServicesAssembly.GetName().Name,
                TelegramInteropAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void TelegramInterop_ShouldNotHaveDependencyOn_SpecifiedProjects()
    {
        var result = Types
            .InAssembly(TelegramInteropAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                ServicesAssembly.GetName().Name,
                RepositoriesAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }

    [Test]
    public void Interfaces_ShouldHaveNameStartingWith_I()
    {
        var result = Types
            .InAssemblies([CliAssembly, CommonAssembly, DomainAssembly, EntitiesAssembly, RepositoriesAssembly, ServiceModelsAssembly, ServicesAssembly, TelegramInteropAssembly])
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I", StringComparison.Ordinal)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True);
    }
}
