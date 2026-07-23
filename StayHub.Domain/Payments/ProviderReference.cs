namespace StayHub.Domain.Payments;

public record ProviderReference(string Value)
{
    public static implicit operator string(ProviderReference reference)
    {
        return reference.Value;
    }

    public static implicit operator ProviderReference(string value)
    {
        return new ProviderReference(value);
    }
}