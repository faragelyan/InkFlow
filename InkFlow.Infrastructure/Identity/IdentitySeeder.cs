using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InkFlow.Infrastructure.Persistence.Context;
using InkFlow.Domain;

namespace InkFlow.Infrastructure.Identity;

public sealed class IdentitySeeder
{
    private const string UserRole = "User";
    private const string AdminRole = "Admin";

    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        AppDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        ILogger<IdentitySeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        foreach (var roleName in new[] { UserRole, AdminRole })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Unable to create role '{roleName}'. " +
                        string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        var adminEmail = _configuration["Identity:InitialAdminEmail"]?.Trim();
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("Initial Admin email is not configured; skipping Admin promotion.");
            return;
        }

        var admin = await _userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            _logger.LogWarning(
                "Initial Admin account '{Email}' does not exist yet. Register it normally, then restart the API.",
                adminEmail);
            return;
        }

        if (!await _userManager.IsInRoleAsync(admin, AdminRole))
        {
            var result = await _userManager.AddToRoleAsync(admin, AdminRole);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Unable to assign '{AdminRole}' to '{adminEmail}'. " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));

            _logger.LogInformation("Assigned '{Role}' role to '{Email}'.", AdminRole, adminEmail);
        }
        else
        {
            _logger.LogInformation("'{Email}' is already assigned to '{Role}'.", adminEmail, AdminRole);
        }

        if (!await _userManager.IsInRoleAsync(admin, UserRole))
            await _userManager.AddToRoleAsync(admin, UserRole);
    }
}
