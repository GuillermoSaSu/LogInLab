using FluentValidation;
using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;

namespace LogInLab.Application.Validators
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        private const int MinPasswordLength = 12;
        private const int MaxPasswordLength = 128;

        public ResetPasswordRequestValidator(IPasswordBreachChecker breachChecker)
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(MinPasswordLength).WithMessage($"New password must be at least {MinPasswordLength} characters long.")
                .MaximumLength(MaxPasswordLength).WithMessage($"New password cannot exceed {MaxPasswordLength} characters.")
                .MustAsync(async (password, cancellation) => !await breachChecker.IsBreachedAsync(password))
                    .WithMessage("This password has recently appeared in known data breaches.");
        }
    }
}
