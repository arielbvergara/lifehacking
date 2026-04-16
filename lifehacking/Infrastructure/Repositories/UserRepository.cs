using Application.Dtos;
using Application.Dtos.User;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObject;
using Infrastructure.Data;
using Infrastructure.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserRepository(LifehackingDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var row = await db.Users
            .Where(u => u.Id == id.Value && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDomain(row);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var row = await db.Users
            .Where(u => u.Email == email.Value && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDomain(row);
    }

    public async Task<User?> GetByExternalAuthIdAsync(
        ExternalAuthIdentifier externalAuthId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Users
            .Where(u => u.ExternalAuthId == externalAuthId.Value && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDomain(row);
    }

    public async Task<(IReadOnlyCollection<User> Items, int TotalCount)> GetPagedAsync(
        UserQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsQueryable();

        if (criteria.IsDeletedFilter.HasValue)
        {
            query = query.Where(u => u.IsDeleted == criteria.IsDeletedFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var term = $"%{criteria.SearchTerm.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, term) ||
                EF.Functions.ILike(u.Name, term));
        }

        query = ApplyOrdering(query, criteria);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return (rows.Select(MapToDomain).ToList(), totalCount);
    }

    public async Task<IReadOnlyCollection<User>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Users
            .Where(u => !u.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDomain).ToList();
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var row = MapToRow(user);
        db.Users.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var row = await db.Users
            .Where(u => u.Id == user.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return;
        }

        row.Name = user.Name.Value;
        row.Role = user.Role;
        row.UpdatedAt = user.UpdatedAt;
        row.IsDeleted = user.IsDeleted;
        row.DeletedAt = user.DeletedAt;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var row = await db.Users
            .Where(u => u.Id == id.Value && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return;
        }

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<UserRow> ApplyOrdering(IQueryable<UserRow> query, UserQueryCriteria criteria)
    {
        return (criteria.SortField, criteria.SortDirection) switch
        {
            (UserSortField.Email, SortDirection.Ascending) =>
                query.OrderBy(u => u.Email).ThenBy(u => u.Id),
            (UserSortField.Email, SortDirection.Descending) =>
                query.OrderByDescending(u => u.Email).ThenByDescending(u => u.Id),
            (UserSortField.Name, SortDirection.Ascending) =>
                query.OrderBy(u => u.Name).ThenBy(u => u.Id),
            (UserSortField.Name, SortDirection.Descending) =>
                query.OrderByDescending(u => u.Name).ThenByDescending(u => u.Id),
            (UserSortField.CreatedAt, SortDirection.Descending) =>
                query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id),
            _ =>
                query.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id)
        };
    }

    private static User MapToDomain(UserRow row)
    {
        return User.FromPersistence(
            UserId.Create(row.Id),
            Email.Create(row.Email),
            UserName.Create(row.Name),
            ExternalAuthIdentifier.Create(row.ExternalAuthId),
            row.Role,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);
    }

    private static UserRow MapToRow(User user)
    {
        return new UserRow
        {
            Id = user.Id.Value,
            Email = user.Email.Value,
            Name = user.Name.Value,
            ExternalAuthId = user.ExternalAuthId.Value,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt
        };
    }
}
