using AdminDashBoard.Application.Auth;
using AdminDashBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminDashBoard.Infrastructure.Auth;

public sealed class UserAuthService : IUserAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly PasswordHashService _passwordHashService;

    public UserAuthService(
        AppDbContext dbContext,
        PasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<bool> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.UserAccounts
            .SingleOrDefaultAsync(account => account.Email == normalizedEmail, cancellationToken);

        return user is not null &&
            _passwordHashService.VerifyPassword(password, user.PasswordHash);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        string email,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        if (!IsValidPassword(newPassword))
        {
            return ChangePasswordResult.InvalidNewPassword;
        }

        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.UserAccounts
            .SingleOrDefaultAsync(account => account.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        if (!_passwordHashService.VerifyPassword(currentPassword, user.PasswordHash))
        {
            return ChangePasswordResult.InvalidCurrentPassword;
        }

        user.PasswordHash = _passwordHashService.HashPassword(newPassword);
        user.PasswordChangedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ChangePasswordResult.Success;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static bool IsValidPassword(string password)
    {
        return password.Length >= 8;
    }
}
