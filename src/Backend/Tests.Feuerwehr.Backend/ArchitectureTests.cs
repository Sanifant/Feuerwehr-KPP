using System.Reflection;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Controller;
using De.Openelp.Feuerwehr.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Tests.Backend;

public class ArchitectureTests
{
    [Fact]
    /// <summary>
    /// Executes controllers Only Depend On Application Service Interfaces.
    /// </summary>
    public void Controllers_OnlyDependOnApplicationServiceInterfaces()
    {
        var controllers = typeof(HydrantController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
        foreach (var controller in controllers)
        {
            var dependency = Assert.Single(Assert.Single(controller.GetConstructors()).GetParameters()).ParameterType;
            Assert.True(dependency.IsInterface, controller.FullName);
            Assert.StartsWith("De.Openelp.Feuerwehr.Server.Services", dependency.Namespace);
        }
    }

    [Fact]
    /// <summary>
    /// Executes common Contains Contracts Without Server Or Persistence Dependencies.
    /// </summary>
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
    /// <summary>
    /// Executes shared Responses Keep Existing Json Field Names.
    /// </summary>
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
    /// <summary>
    /// Executes namespace And Entity Separation Do Not Require Schema Changes.
    /// </summary>
    public void NamespaceAndEntitySeparation_DoNotRequireSchemaChanges()
    {
        using var db = new FeuerwehrDbContext(new DbContextOptionsBuilder<FeuerwehrDbContext>()
            .UseNpgsql("Host=localhost;Database=architecture;Username=postgres;Password=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}

