using Microsoft.EntityFrameworkCore;
using WSV.Api.Data;

namespace WSV.Api.Services.History;

public class AggregatedHistoryStrategy : IHistoryStrategy
{
    private readonly AppDbContext _context;

    public AggregatedHistoryStrategy(
        AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ReadingDto>> GetAsync(int sourceId, DateTimeOffset from, DateTimeOffset to, int limit)
    {
        var spanSeconds = (to - from).TotalSeconds;
        var bucketSeconds = (int)(spanSeconds / limit);

        var sql = @"
            SELECT
                {0} as ""SourceId"",
                TO_TIMESTAMP(
                    FLOOR(EXTRACT(EPOCH FROM ""Timestamp"") / {1}) *{1}
                ) as ""Timestamp"",
                'Aggregated' as ""Status"",
                CAST(AVG(""RPM"") AS INTEGER) as ""RPM"",
                CAST(AVG(""Power"") AS INTEGER) as ""Power"",
                AVG(""Temperature"") as ""Temperature""
            FROM ""SourceReadings""
            WHERE ""SourceId"" = {0}
            AND ""Timestamp"" >= {2}
            AND ""Timestamp"" < {3}
            GROUP BY FLOOR(EXTRACT(EPOCH FROM ""Timestamp"") / {1})
            ORDER BY ""Timestamp""";

        var results = await _context.Database
            .SqlQueryRaw<ReadingDto>(sql,
                sourceId,
                bucketSeconds,
                from,
                to)
            .ToListAsync();

        return results;
    }
}