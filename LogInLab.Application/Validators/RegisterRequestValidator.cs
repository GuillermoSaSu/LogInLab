using FluentValidation;
using LogInLab.Application.DTOs;
using LogInLab.Application.Interfaces;

namespace LogInLab.Application.Validators
{
    public class RegisterRequestValidator :AbstractValidator<RegisterRequest>
    {
        private const int MinPasswordLength = 12;
        private const int MaxPasswordLength = 128;

        public RegisterRequestValidator(IPasswordBreachChecker breachChecker)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(256).WithMessage("Email can not exceed 256 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(MinPasswordLength).WithMessage($"Password must be at least {MinPasswordLength} characters long.")
                .MaximumLength(MaxPasswordLength).WithMessage($"Password cannot exceed {MaxPasswordLength} characters.")
                .MustAsync(async (password, cancellation) => !await breachChecker.IsBreachedAsync(password))
                    .WithMessage("This password has recently appeared in known data breaches.");
        }

    }
}
