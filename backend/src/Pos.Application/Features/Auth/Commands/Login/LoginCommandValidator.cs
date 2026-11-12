using FluentValidation;

namespace Pos.Application.Features.Auth.Commands.Login;

/// <summary>Valida que lleguen usuario y contraseña antes de consultar la cuenta.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Define las reglas del login.</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithErrorCode("AUTH_USERNAME_REQUIRED").WithMessage("Ingrese el usuario.")
            .MaximumLength(100).WithErrorCode("AUTH_USERNAME_TOO_LONG").WithMessage("El usuario es demasiado largo.");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("AUTH_PASSWORD_REQUIRED").WithMessage("Ingrese la contraseña.")
            .MaximumLength(200).WithErrorCode("AUTH_PASSWORD_TOO_LONG").WithMessage("La contraseña es demasiado larga.");
    }
}
