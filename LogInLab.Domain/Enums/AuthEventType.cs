namespace LogInLab.Domain.Enums
{
    public enum AuthEventType
    {
        RegisterSuccess,
        LoginSuccess,
        LoginFailedInvalidCredentials,
        LoginFailedAccountLocked,
        LoginFailedEmailNotVerified,
        LoginFailedMfaInvalid,
        AccountLocked,
        Logout,
        PasswordResetRequested,
        PasswordResetCompleted,
        MfaEnabled,
        MfaDisabled,
        EmailVerified
    }
}
