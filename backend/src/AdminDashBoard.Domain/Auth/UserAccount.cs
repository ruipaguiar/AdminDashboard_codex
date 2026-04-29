namespace AdminDashBoard.Domain.Auth;

public sealed class UserAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PasswordChangedAtUtc { get; set; }
}
