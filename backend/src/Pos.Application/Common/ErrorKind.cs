namespace Pos.Application.Common;

public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    IntegrationUnavailable,
    IntegrationRejected,
    VerificationFailed,
    Uncertain,
    Unexpected
}
