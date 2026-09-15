using System.Reflection;
using UTH.Library.Api;
using UTH.Library.Application;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure;

namespace UTH.Library.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceApplicationInfrastructureOrApi()
    {
        Assert.DoesNotContain(typeof(Domain.Entities.TodoItem).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "UTH.Library.Application" or "UTH.Library.Infrastructure" or "UTH.Library.Api");
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrApi()
    {
        Assert.DoesNotContain(typeof(Application.DependencyInjection).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "UTH.Library.Infrastructure" or "UTH.Library.Api");
    }

    [Fact]
    public void Infrastructure_ReferencesApplicationAndDomain()
    {
        var references = typeof(Infrastructure.DependencyInjection).Assembly.GetReferencedAssemblies();
        Assert.Contains(references, reference => reference.Name == "UTH.Library.Application");
        Assert.Contains(references, reference => reference.Name == "UTH.Library.Domain");
    }
}