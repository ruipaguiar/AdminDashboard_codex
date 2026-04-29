namespace AdminDashBoard.Application.Auth;

public interface IUserAuthService
{
    Task<bool> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<ChangePasswordResult> ChangePasswordAsync(
        string email,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);
}

public enum ChangePasswordResult
{
    Success,
    UserNotFound,
    InvalidCurrentPassword,
    InvalidNewPassword
}
