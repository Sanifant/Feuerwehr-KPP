namespace de.openelp.feuerwehr.Tests.Backend.Controller
{
    using System.ComponentModel.DataAnnotations;
    using System.Reflection;
    using System.Security.Claims;
    using de.openelp.feuerwehr.Common.Models;
    using de.openelp.feuerwehr.Common.Models.HydrantInspections;
    using de.openelp.feuerwehr.Server.Authorization;
    using de.openelp.feuerwehr.Server.Controller;
    using de.openelp.feuerwehr.Server.Services;
    using de.openelp.feuerwehr.Server.Services.HydrantModules.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;

    public sealed class HydrantInspectionControllerTests
    {
        [Theory]
        [InlineData("GetInspections", "Admin", true)]
        [InlineData("GetInspections", "Commander", true)]
        [InlineData("GetInspections", "Firefighter", false)]
        [InlineData("GetInspections", "Viewer", false)]
        [InlineData("GetInspections", null, false)]
        [InlineData("Inspect", "Admin", true)]
        [InlineData("Inspect", "Commander", true)]
        [InlineData("Inspect", "Firefighter", false)]
        [InlineData("Inspect", "Viewer", false)]
        [InlineData("Inspect", null, false)]
        public async Task OnlyAdminAndCommanderCanReadOrCreateInspectionsAsync(string action, string? role, bool expected)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorization(options => options.AddFeuerwehrPolicies());
            using ServiceProvider provider = services.BuildServiceProvider();
            MethodInfo method = typeof(HydrantController).GetMethod(action)!;
            AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(provider.GetRequiredService<IAuthorizationPolicyProvider>(),
                typeof(HydrantController).GetCustomAttributes<AuthorizeAttribute>().Concat(method.GetCustomAttributes<AuthorizeAttribute>()));
            Assert.NotNull(policy);
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
            ClaimsPrincipal principal = new ClaimsPrincipal(role is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));
            AuthorizationResult result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, policy);
            Assert.Equal(expected, result.Succeeded);
        }

        [Fact]
        public void PostUsesTheRouteHydrantAndMapsTheServiceResult()
        {
            CreateHydrantInspectionRequest request = new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Blocked };
            Mock<IHydrantService> service = new Mock<IHydrantService>();
            service.Setup(x => x.Inspect(42, request)).Returns(ServiceResult.StatusCode(201, "created"));
            ObjectResult response = Assert.IsAssignableFrom<ObjectResult>(new HydrantController(service.Object).Inspect(42, request));
            Assert.Equal(201, response.StatusCode);
            Assert.Equal("created", response.Value);
            Assert.Equal("{id}/inspections", typeof(HydrantController).GetMethod("Inspect")!.GetCustomAttribute<HttpPostAttribute>()!.Template);
        }

        [Fact]
        public void RequestValidationRejectsInvalidStatusAndOverlongCommentButAllowsNoComment()
        {
            List<ValidationResult> results = [];
            CreateHydrantInspectionRequest request = new CreateHydrantInspectionRequest();
            Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
            request.NewStatus = HydrantStatus.Operational;
            Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
            request.Comment = new string('a', 501);
            Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
        }
    }
}
