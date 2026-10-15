using System.Reflection;
using System.Text.Json;
using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Controller;
using de.openelp.feuerwehr.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Tests.Backend;

public class ArchitectureTests
{
    [Fact]
    public void Controllers_OnlyDependOnApplicationServiceInterfaces()
    {
        var controllers = typeof(HydrantController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
        foreach (var controller in controllers)
        {
            var dependency = Assert.Single(Assert.Single(controller.GetConstructors()).GetParameters()).ParameterType;
            Assert.True(dependency.IsInterface, controller.FullName);
            Assert.StartsWith("de.openelp.feuerwehr.Server.Services", dependency.Namespace);
        }
    }

    [Fact]
    public void Common_ContainsContractsWithoutServerOrPersistenceDependencies()
    {
        var common = typeof(LoginRequest).Assembly;
        Assert.DoesNotContain(common.GetReferencedAssemblies(), assembly =>
            assembly.Name == "Feuerwehr.Server" || assembly.Name!.StartsWith("Microsoft.EntityFrameworkCore"));
        using var db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Assert.All(db.Model.GetEntityTypes().Where(entity => !entity.ClrType.Namespace!.StartsWith("Microsoft.")),
            entity => Assert.Equal(typeof(FeuerwehrDbContext).Assembly, entity.ClrType.Assembly));
    }

    [Fact]
    public void SharedResponses_KeepExistingJsonFieldNames()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var response = JsonSerializer.SerializeToElement(new DiaryEntryUpdatedResponse(Guid.NewGuid(), 2, 8, DateTime.UtcNow), options);
        Assert.Equal(2, response.GetProperty("currentRevision").GetInt32());
        Assert.Equal(8, response.GetProperty("incidentRevision").GetInt64());
        var error = JsonSerializer.SerializeToElement(new IdentityErrorResponse("Failed", [new IdentityErrorDto("code", "description")]), options);
        Assert.Equal("Failed", error.GetProperty("message").GetString());
        Assert.Equal("code", error.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public void NamespaceAndEntitySeparation_DoNotRequireSchemaChanges()
    {
        using var db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseNpgsql("Host=localhost;Database=architecture;Username=postgres;Password=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
