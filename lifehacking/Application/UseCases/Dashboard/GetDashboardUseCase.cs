using Application.Dtos.Dashboard;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;

namespace Application.UseCases.Dashboard;

public class GetDashboardUseCase(
    IUserRepository userRepository,
    ICategoryRepository categoryRepository,
    ITipRepository tipRepository)
{
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    private readonly ICategoryRepository _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    private readonly ITipRepository _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));

    public virtual async Task<Result<DashboardResponse, AppException>> ExecuteAsync(
        GetDashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // Calculate all time ranges
            var (thisDayStart, thisDayEnd) = GetCurrentDayRange(now);
            var (lastDayStart, lastDayEnd) = GetPreviousDayRange(now);
            var (thisWeekStart, thisWeekEnd) = GetCurrentWeekRange(now);
            var (lastWeekStart, lastWeekEnd) = GetPreviousWeekRange(now);
            var (thisMonthStart, thisMonthEnd) = GetCurrentMonthRange(now);
            var (lastMonthStart, lastMonthEnd) = GetPreviousMonthRange(now);
            var (thisYearStart, thisYearEnd) = GetCurrentYearRange(now);
            var (lastYearStart, lastYearEnd) = GetPreviousYearRange(now);

            // Fetch all entities in parallel
            var usersTask = _userRepository.GetAllActiveAsync(cancellationToken);
            var categoriesTask = _categoryRepository.GetAllAsync(cancellationToken);
            var tipsTask = _tipRepository.GetAllAsync(cancellationToken);

            await Task.WhenAll(usersTask, categoriesTask, tipsTask);

            var users = await usersTask;
            var categories = await categoriesTask;
            var tips = await tipsTask;

            // Calculate statistics
            var userStats = CalculateStatistics(
                users.Select(u => u.CreatedAt),
                thisDayStart, thisDayEnd,
                lastDayStart, lastDayEnd,
                thisWeekStart, thisWeekEnd,
                lastWeekStart, lastWeekEnd,
                thisMonthStart, thisMonthEnd,
                lastMonthStart, lastMonthEnd,
                thisYearStart, thisYearEnd,
                lastYearStart, lastYearEnd);

            var categoryStats = CalculateStatistics(
                categories.Where(c => !c.IsDeleted).Select(c => c.CreatedAt),
                thisDayStart, thisDayEnd,
                lastDayStart, lastDayEnd,
                thisWeekStart, thisWeekEnd,
                lastWeekStart, lastWeekEnd,
                thisMonthStart, thisMonthEnd,
                lastMonthStart, lastMonthEnd,
                thisYearStart, thisYearEnd,
                lastYearStart, lastYearEnd);

            var tipStats = CalculateStatistics(
                tips.Where(t => !t.IsDeleted).Select(t => t.CreatedAt),
                thisDayStart, thisDayEnd,
                lastDayStart, lastDayEnd,
                thisWeekStart, thisWeekEnd,
                lastWeekStart, lastWeekEnd,
                thisMonthStart, thisMonthEnd,
                lastMonthStart, lastMonthEnd,
                thisYearStart, thisYearEnd,
                lastYearStart, lastYearEnd);

            var response = new DashboardResponse
            {
                Users = userStats,
                Categories = categoryStats,
                Tips = tipStats
            };

            return Result<DashboardResponse, AppException>.Ok(response);
        }
        catch (Exception ex)
        {
            return Result<DashboardResponse, AppException>.Fail(
                new InfraException("Failed to retrieve dashboard statistics", ex));
        }
    }

    private static EntityStatistics CalculateStatistics(
        IEnumerable<DateTime> createdDates,
        DateTime thisDayStart,
        DateTime thisDayEnd,
        DateTime lastDayStart,
        DateTime lastDayEnd,
        DateTime thisWeekStart,
        DateTime thisWeekEnd,
        DateTime lastWeekStart,
        DateTime lastWeekEnd,
        DateTime thisMonthStart,
        DateTime thisMonthEnd,
        DateTime lastMonthStart,
        DateTime lastMonthEnd,
        DateTime thisYearStart,
        DateTime thisYearEnd,
        DateTime lastYearStart,
        DateTime lastYearEnd)
    {
        var datesList = createdDates.ToList();

        return new EntityStatistics
        {
            Total = datesList.Count,
            ThisDay = datesList.Count(d => d >= thisDayStart && d <= thisDayEnd),
            LastDay = datesList.Count(d => d >= lastDayStart && d <= lastDayEnd),
            ThisWeek = datesList.Count(d => d >= thisWeekStart && d <= thisWeekEnd),
            LastWeek = datesList.Count(d => d >= lastWeekStart && d <= lastWeekEnd),
            ThisMonth = datesList.Count(d => d >= thisMonthStart && d <= thisMonthEnd),
            LastMonth = datesList.Count(d => d >= lastMonthStart && d <= lastMonthEnd),
            ThisYear = datesList.Count(d => d >= thisYearStart && d <= thisYearEnd),
            LastYear = datesList.Count(d => d >= lastYearStart && d <= lastYearEnd)
        };
    }

    private static (DateTime Start, DateTime End) GetCurrentMonthRange(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = now;
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetPreviousMonthRange(DateTime now)
    {
        var lastMonth = now.AddMonths(-1);
        var start = new DateTime(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month), 23, 59, 59, DateTimeKind.Utc);
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetCurrentDayRange(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var end = now;
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetPreviousDayRange(DateTime now)
    {
        var yesterday = now.AddDays(-1);
        var start = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 23, 59, 59, DateTimeKind.Utc);
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetCurrentWeekRange(DateTime now)
    {
        // ISO 8601: Monday is the first day of the week
        var dayOfWeek = (int)now.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday = 0, adjust to Monday = 0
        var monday = now.AddDays(-daysFromMonday);
        var start = new DateTime(monday.Year, monday.Month, monday.Day, 0, 0, 0, DateTimeKind.Utc);
        var end = now;
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetPreviousWeekRange(DateTime now)
    {
        // ISO 8601: Monday is the first day of the week
        var dayOfWeek = (int)now.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var thisMonday = now.AddDays(-daysFromMonday);
        var lastMonday = thisMonday.AddDays(-7);
        var lastSunday = lastMonday.AddDays(6);
        
        var start = new DateTime(lastMonday.Year, lastMonday.Month, lastMonday.Day, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(lastSunday.Year, lastSunday.Month, lastSunday.Day, 23, 59, 59, DateTimeKind.Utc);
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetCurrentYearRange(DateTime now)
    {
        var start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = now;
        return (start, end);
    }

    private static (DateTime Start, DateTime End) GetPreviousYearRange(DateTime now)
    {
        var lastYear = now.Year - 1;
        var start = new DateTime(lastYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(lastYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        return (start, end);
    }
}
