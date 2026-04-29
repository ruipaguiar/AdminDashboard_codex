using AdminDashBoard.Domain.Auth;
using AdminDashBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AdminDashBoard.Infrastructure.Auth;

public sealed class DefaultUserSeeder
{
    public const string DefaultEmail = "ruipaguiar@gmail.com";
    private const string DefaultPassword = "Password123!";

    private readonly AppDbContext _dbContext;
    private readonly PasswordHashService _passwordHashService;
    private readonly ILogger<DefaultUserSeeder> _logger;

    public DefaultUserSeeder(
        AppDbContext dbContext,
        PasswordHashService passwordHashService,
        ILogger<DefaultUserSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var normalizedEmail = UserAuthService.NormalizeEmail(DefaultEmail);
        var exists = await _dbContext.UserAccounts
            .AnyAsync(account => account.Email == normalizedEmail, cancellationToken);

        if (exists)
        {
            return;
        }

        _dbContext.UserAccounts.Add(new UserAccount
        {
            Email = normalizedEmail,
            PasswordHash = _passwordHashService.HashPassword(DefaultPassword)
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Default local user created: {Email}", normalizedEmail);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _logger.LogDebug("Default local user already exists: {Email}", normalizedEmail);
        }
    }
}
