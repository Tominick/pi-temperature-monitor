# Performance note
To squeeze performance:

## Before optimization
``` csharp
public async Task<ActionResult<IEnumerable<object>>> GetDateByHourMinMax(int id, DateTime date)
{
    return await _context.Measures
                .Where(x => x.SensorId == id && x.DateTime.Date == date)
                .GroupBy(x => x.DateTime.Hour).Select(g => new
                {
                    SensorId = g.First().SensorId,
                    Hour = g.Key,
                    MinTemperature = g.Min(x => x.Temperature),
                    MaxTemperature = g.Max(x => x.Temperature),
                    MinHumidity = g.Min(x => x.Humidity),
                    MaxHumidity = g.Max(x => x.Humidity)
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();
}
```
SQLite generated query:
``` sql
.param set @__id_0 1
.param set @__date_1 '2025-04-25 00:00:00'

EXPLAIN QUERY PLAN SELECT (
    SELECT "t0"."SensorId"
    FROM (
        SELECT "m0"."SensorId", "m0"."DateTime", "m0"."Humidity", "m0"."Temperature", CAST(strftime('%H', "m0"."DateTime") AS INTEGER) AS "Key"
        FROM "Measures" AS "m0"
        WHERE "m0"."SensorId" = @__id_0 AND rtrim(rtrim(strftime('%Y-%m-%d %H:%M:%f', "m0"."DateTime", 'start of day'), '0'), '.') = @__date_1
    ) AS "t0"
    WHERE "t"."Key" = "t0"."Key" OR (("t"."Key" IS NULL) AND ("t0"."Key" IS NULL))
    LIMIT 1) AS "SensorId", "t"."Key" AS "Hour", MIN("t"."Temperature") AS "MinTemperature", MAX("t"."Temperature") AS "MaxTemperature", MIN("t"."Humidity") AS "MinHumidity", MAX("t"."Humidity") AS "MaxHumidity"
FROM (
    SELECT "m"."Humidity", "m"."Temperature", CAST(strftime('%H', "m"."DateTime") AS INTEGER) AS "Key"
    FROM "Measures" AS "m"
    WHERE "m"."SensorId" = @__id_0 AND rtrim(rtrim(strftime('%Y-%m-%d %H:%M:%f', "m"."DateTime", 'start of day'), '0'), '.') = @__date_1
) AS "t"
GROUP BY "t"."Key"
ORDER BY "t"."Key"
```

## After optimization
``` csharp
public async Task<ActionResult<IEnumerable<object>>> GetDateByHourMinMax(int id, DateTime date)
{
    return await _context.Measures
                .Where(x => x.SensorId == id && x.DateTime >= date && x.DateTime < date.AddDays(1))
                    .GroupBy(x => x.DateTime.Hour).Select(g => new
                    {
                        SensorId = g.First().SensorId,
                        Hour = g.Key,
                        MinTemperature = g.Min(x => x.Temperature),
                        MaxTemperature = g.Max(x => x.Temperature),
                        MinHumidity = g.Min(x => x.Humidity),
                        MaxHumidity = g.Max(x => x.Humidity)
                    })
                    .OrderBy(x => x.Hour).ToListAsync();
}
```
SQLite generated query:
``` sql
.param set @__id_0 1
.param set @__date_1 '2025-04-25 00:00:00'
.param set @__AddDays_2 '2025-04-26 00:00:00'

SELECT (
    SELECT "t0"."SensorId"
    FROM (
        SELECT "m0"."SensorId", "m0"."DateTime", "m0"."Humidity", "m0"."Temperature", CAST(strftime('%H', "m0"."DateTime") AS INTEGER) AS "Key"
        FROM "Measures" AS "m0"
        WHERE "m0"."SensorId" = @__id_0 AND "m0"."DateTime" >= @__date_1 AND "m0"."DateTime" < @__AddDays_2
    ) AS "t0"
    WHERE "t"."Key" = "t0"."Key" OR (("t"."Key" IS NULL) AND ("t0"."Key" IS NULL))
    LIMIT 1) AS "SensorId", "t"."Key" AS "Hour", MIN("t"."Temperature") AS "MinTemperature", MAX("t"."Temperature") AS "MaxTemperature", MIN("t"."Humidity") AS "MinHumidity", MAX("t"."Humidity") AS "MaxHumidity"
FROM (
    SELECT "m"."Humidity", "m"."Temperature", CAST(strftime('%H', "m"."DateTime") AS INTEGER) AS "Key"
    FROM "Measures" AS "m"
    WHERE "m"."SensorId" = @__id_0 AND "m"."DateTime" >= @__date_1 AND "m"."DateTime" < @__AddDays_2
) AS "t"
GROUP BY "t"."Key"
ORDER BY "t"."Key"
```