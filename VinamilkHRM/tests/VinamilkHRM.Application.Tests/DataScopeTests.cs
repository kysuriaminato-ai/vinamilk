using VinamilkHRM.Application.Common.Security;
using Xunit;

namespace VinamilkHRM.Application.Tests;

public class DataScopeTests
{
    [Fact]
    public void Roles_Definitions_ShouldIncludeAllTenStandardEnterpriseRoles()
    {
        Assert.Equal(10, Roles.All.Count);
        Assert.Contains(Roles.SystemAdmin, Roles.All);
        Assert.Contains(Roles.Executive, Roles.All);
        Assert.Contains(Roles.HRDirector, Roles.All);
        Assert.Contains(Roles.HRManager, Roles.All);
        Assert.Contains(Roles.HRSpecialist, Roles.All);
        Assert.Contains(Roles.ChiefAccountant, Roles.All);
        Assert.Contains(Roles.PayrollAccountant, Roles.All);
        Assert.Contains(Roles.UnitManager, Roles.All);
        Assert.Contains(Roles.TeamLeader, Roles.All);
        Assert.Contains(Roles.Employee, Roles.All);
    }

    [Fact]
    public void Permissions_Definitions_ShouldHaveModuleActionFormat()
    {
        Assert.NotEmpty(Permissions.All);
        foreach (var permission in Permissions.All)
        {
            Assert.Contains(".", permission);
            var parts = permission.Split('.');
            Assert.Equal(2, parts.Length);
            Assert.NotEmpty(parts[0]);
            Assert.NotEmpty(parts[1]);
        }
    }
}
