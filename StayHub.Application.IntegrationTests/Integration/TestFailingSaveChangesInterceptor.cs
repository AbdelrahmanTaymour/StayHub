using Microsoft.EntityFrameworkCore.Diagnostics;

namespace StayHub.Application.IntegrationTests.Integration;

/// <summary>
/// Forces the next SaveChangesAsync call on this DbContext instance to throw,
/// without affecting any read that happens before it. Needed for tests
/// proving a compensating action runs when persistence fails AFTER other
/// real work (e.g. a real file upload) already succeeded — disposing the
/// underlying connection doesn't work for this, since it breaks every
/// subsequent DB call, including reads the handler needs before it ever
/// reaches SaveChangesAsync.
/// </summary>
public sealed class TestFailingSaveChangesInterceptor : SaveChangesInterceptor
{
    public Exception? FailNextSave { get; set; }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ThrowIfArmed();
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfArmed();
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ThrowIfArmed()
    {
        if (FailNextSave is not { } exception) return;

        FailNextSave = null;
        throw exception;
    }
}