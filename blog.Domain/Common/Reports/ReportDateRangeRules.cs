namespace blog.Domain.Common.Reports
{
    public static class ReportDateRangeRules
    {
        public const int MaxRangeDays = 30;

        public static bool IsWithinAllowedRange(DateOnly from, DateOnly to)
        {
            var rangeDays = to.DayNumber - from.DayNumber + 1;
            return rangeDays is >= 1 and <= MaxRangeDays;
        }

        // Resolves the effective report range: falls back to the last MaxRangeDays days
        // (inclusive of today) when no range is supplied, otherwise returns the supplied
        // range as-is. Callers must validate the supplied range with IsWithinAllowedRange
        // before calling this (see report query validators).
        public static ReportDateRange Resolve(DateOnly? from, DateOnly? to)
        {
            if (from is not null && to is not null)
                return new ReportDateRange(from.Value, to.Value);

            var resolvedTo = DateOnly.FromDateTime(DateTime.UtcNow);
            var resolvedFrom = resolvedTo.AddDays(-(MaxRangeDays - 1));

            return new ReportDateRange(resolvedFrom, resolvedTo);
        }
    }
}