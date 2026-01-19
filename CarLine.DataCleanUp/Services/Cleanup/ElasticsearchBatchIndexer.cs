using CarLine.Common.Models;
using Elastic.Clients.Elasticsearch;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal sealed class ElasticsearchBatchIndexer(ILogger logger, ElasticsearchClient client)
{
    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken)
    {
        await ElasticsearchHelper.EnsureIndexExistsAsync(client, cancellationToken);
    }

    public async Task CreateOnlyAsync(IReadOnlyCollection<CarDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
            return;

        var created = 0;
        var alreadyExists = 0;
        var failed = 0;

        // Keep concurrency bounded; this is "best effort" and avoids relying on bulk-create types.
        const int maxConcurrency = 16;
        using var throttler = new SemaphoreSlim(maxConcurrency);

        var tasks = documents.Select(async doc =>
        {
            await throttler.WaitAsync(cancellationToken);
            try
            {
                var response = await client.CreateAsync(doc, c => c
                    .Index(ElasticsearchHelper.CarsIndexName)
                    .Id(doc.Id), cancellationToken);

                if (response.IsValidResponse)
                {
                    Interlocked.Increment(ref created);
                    return;
                }

                // 409 conflict => doc already exists, which is fine for insert-only mode.
                if (response.ElasticsearchServerError?.Status == 409)
                {
                    Interlocked.Increment(ref alreadyExists);
                    return;
                }

                Interlocked.Increment(ref failed);
                logger.LogError("Elasticsearch create failed for {id}: {info}", doc.Id, response.DebugInformation);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                logger.LogError(ex, "Elasticsearch create threw for {id}", doc.Id);
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);

        logger.LogInformation("Elasticsearch create-only summary: {created} created, {exists} already existed, {failed} failed",
            created, alreadyExists, failed);
    }
}
