using System.Data;
using Dapper;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Data;
using StayHub.Domain.Abstractions;

namespace StayHub.Infrastructure.Outbox;

/// <summary>
/// Reads a batch of unprocessed <see cref="OutboxMessage"/> rows, republishes them
/// as domain events via <see cref="IPublisher"/>, and marks each as processed,
/// retried, or dead-lettered. Reschedules its own next run at the end, giving
/// true sub-minute polling without relying on cron's minute-level granularity.
///
/// Concurrency safety: the read-and-claim step happens inside a single DB
/// transaction using SELECT ... FOR UPDATE, so the claimed rows are row-locked
/// until the transaction commits. [DisableConcurrentExecution] additionally
/// stops two runs of this job from overlapping at the Hangfire level even if
/// a batch takes longer than IntervalInSeconds to process — together these
/// make it safe for a slow batch and the next scheduled run to coexist without
/// double-publishing any event.
///
/// AutomaticRetry(Attempts = 0): Hangfire's own retry-on-exception is disabled
/// here on purpose. The job already guarantees a next attempt via the
/// self-rescheduling in the `finally` block below regardless of outcome, and
/// per-message retry/dead-lettering already happens inside ProcessMessageAsync.
/// Combining Hangfire's job-level retry with the self-rescheduling caused every
/// failed run to spawn TWO follow-ups (one retry and one rescheduling) instead of
/// one, which is what produced the job-flood.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 60)]
internal sealed class ProcessOutboxMessagesJob(
    ISqlConnectionFactory sqlConnectionFactory,
    IPublisher publisher,
    IDateTimeProvider dateTimeProvider,
    IOptions<OutboxOptions> outboxOptions,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ProcessOutboxMessagesJob> logger)
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
    };

    private readonly OutboxOptions _outboxOptions = outboxOptions.Value;

    // Hangfire injects IJobCancellationToken automatically; ShutdownToken is
    // signaled when the server is shutting down, letting an in-flight
    // publication be canceled cleanly instead of abandoned mid-batch.
    public async Task ProcessAsync(IJobCancellationToken cancellationToken)
    {
        try
        {
            await ProcessBatchAsync(cancellationToken.ShutdownToken);
        }
        finally
        {
            // Always reschedule, even if this run threw or found nothing to do —
            // otherwise a single unhandled failure would silently kill the chain.
            // Uses the injected IBackgroundJobClient (service-based API) rather
            // than the static BackgroundJob class — the static reads the
            // process-wide JobStorage.Current, which is never set by
            // AddHangfire(...) and threw at startup. See ProcessOutboxMessagesJobSetup.
            backgroundJobClient.Schedule<ProcessOutboxMessagesJob>(
                job => job.ProcessAsync(JobCancellationToken.Null),
                TimeSpan.FromSeconds(_outboxOptions.IntervalInSeconds));
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Beginning to process outbox messages");

        using var connection = sqlConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        IReadOnlyList<OutboxMessageResponse> messages =
            await GetOutboxMessagesAsync(connection, transaction);

        if (messages.Count == 0)
        {
            transaction.Commit();
            logger.LogInformation("No outbox messages to process");
            return;
        }

        foreach (OutboxMessageResponse message in messages)
        {
            await ProcessMessageAsync(connection, transaction, message, cancellationToken);
        }

        transaction.Commit();

        logger.LogInformation("Completed processing {Count} outbox messages", messages.Count);
    }

    private async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxMessagesAsync(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
                           SELECT id AS Id, 
                                  content AS Content,
                                  retry_count AS RetryCount
                           FROM outbox_messages
                           WHERE processed_on_utc IS NULL AND retry_count < @MaxRetries
                           ORDER BY occurred_on_utc
                           LIMIT @BatchSize
                           FOR UPDATE
                           """;

        var messages = await connection.QueryAsync<OutboxMessageResponse>(
            sql,
            new { _outboxOptions.MaxRetries, _outboxOptions.BatchSize },
            transaction: transaction);

        return messages.ToList();
    }

    private async Task ProcessMessageAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        OutboxMessageResponse message,
        CancellationToken cancellationToken)
    {
        string? error = null;
        var retryCount = message.RetryCount;

        try
        {
            var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                message.Content,
                JsonSerializerSettings);

            if (domainEvent is null)
            {
                throw new InvalidOperationException(
                    $"Deserialization returned null for outbox message {message.Id}.");
            }

            await publisher.Publish(domainEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            retryCount++;
            error = ex.ToString();

            bool exhausted = retryCount >= _outboxOptions.MaxRetries;

            logger.LogError(
                ex,
                "Exception while processing outbox message {MessageId} " +
                "(attempt {RetryCount}/{MaxRetries}){DeadLetterNote}",
                message.Id,
                retryCount,
                _outboxOptions.MaxRetries,
                exhausted ? " — dead-lettered, no further retries" : "");
        }

        await UpdateOutboxMessageAsync(connection, transaction, message.Id, retryCount, error);
    }

    private async Task UpdateOutboxMessageAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid id,
        int retryCount,
        string? error)
    {
        // Terminal (ProcessedOnUtc set) on success, or once retries are
        // exhausted. Otherwise, ProcessedOnUtc stays null, so the next poll
        // picks the message up again — the fixed poll interval is the
        // backoff, no separate timer needed.
        bool terminal = error is null || retryCount >= _outboxOptions.MaxRetries;

        const string sql = """
                           UPDATE outbox_messages
                           SET processed_on_utc = @ProcessedOnUtc,
                               error = @Error,
                               retry_count = @RetryCount
                           WHERE id = @Id
                           """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                ProcessedOnUtc = terminal ? dateTimeProvider.UtcNow : (DateTime?)null,
                Error = error,
                RetryCount = retryCount
            },
            transaction: transaction);
    }

    internal sealed record OutboxMessageResponse(Guid Id, string Content, int RetryCount);
}