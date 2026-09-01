namespace blog.Domain.Posts.Common
{
    public static class DateRangeValidationRules
    {
        public const int MinRangeDays = 1;
        public const int MaxRangeDays = 30;

        public static bool IsWithinAllowedRange(DateOnly from, DateOnly to)
        {
            var rangeDays = to.DayNumber - from.DayNumber + 1;
            return rangeDays is >= MinRangeDays and <= MaxRangeDays;
        }
    }
}
