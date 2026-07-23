using StayHub.Domain.Shared;

namespace StayHub.Application.Shared;

public static class ValidationHelper
{
    public static bool BeValidCurrency(string code)
    {
        return Currency.All.Any(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}