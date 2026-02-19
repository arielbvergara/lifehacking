using Application.Dtos.Dashboard;
using Application.Interfaces;
using Domain.Primitives;
using Application.Exceptions;

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
            var (thisMonthStart, thisMonthEnd) = GetCurrentMonthRange(now);
            var (lastMonthStart, lastMonthEnd) = GetPreviousMonthRange(now);

            // Fetch all entities in parallel
            var usersTask = _userRepository.GetAllActiveAsync(cancellationToken);
            var categoriesTask = _categoryRepository.GetAllAsync(cancellationToken);
            var tipsTask = _tipRepository.GetAllAsync(cancellationToken);

            await Task.WhenAll(usersTask, categoriesTask, tipsTask);

            var users = await usersTask;
            var categories = await categoriesTask;
            var tips = await tipsTask;

            // Calculate statistics
            var userStats = CalculateStatistics(users.Select(u => u.CreatedAt), thisMonthStart, thisMonthEnd, lastMonthStart, lastMonthEnd);
            var categoryStats = CalculateStatistics(categories.Where(c => !c.IsDeleted).Select(c => c.CreatedAt), thisMonthStart, thisMonthEnd, lastMonthStart, lastMonthEnd);
            var tipStats = CalculateStatistics(tips.Where(t => !t.IsDeleted).Select(t => t.CreatedAt), thisMonthStart, thisMonthEnd, lastMonthStart, lastMonthEnd);

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
        DateTime thisMonthStart,
        DateTime thisMonthEnd,
        DateTime lastMonthStart,
        DateTime lastMonthEnd)
    {
        var datesList = createdDates.ToList();

        return new EntityStatistics
        {
            Total = datesList.Count,
            ThisMonth = datesList.Count(d => d >= thisMonthStart && d <= thisMonthEnd),
            LastMonth = datesList.Count(d => d >= lastMonthStart && d <= lastMonthEnd)
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
}
