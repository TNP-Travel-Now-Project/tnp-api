using AuthApi.Application.Abstractions.Interfaces.UnitOfWork;
using DnsClient.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Persistence;

/// <summary>
/// Triển khai IUnitOfWork - bọc AppDbContext.
/// AppDbContext đã là 1 UnitOfWork sẵn (change tracking + SaveChanges),
/// wrapper này chỉ định hình rõ ràng API và là nơi tập trung
/// cho các logic tương lai (audit, events, outbox, retry...).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext db, ILogger<UnitOfWork> logger)
    {
        _db = db;
        _logger = logger;
    }

    public bool HasChanges() => _db.ChangeTracker.HasChanges();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        LogChangesBeforeSave();

        var result = await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved {Count} change(s) to database", result);

        return result;
    }

    private void LogChangesBeforeSave()
    {
        var entries = _db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added
                        or EntityState.Modified
                        or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            _logger.LogDebug("No changes detected in ChangeTracker");
            return;
        }

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);

            switch (entry.State)
            {
                case EntityState.Added:
                    _logger.LogDebug("[ADDED] {Entity} Id={Id}", entityName, entityId);
                    break;

                case EntityState.Modified:
                    var modifiedFields = entry.Properties
                        .Where(p => p.IsModified)
                        .Select(p => p.Metadata.Name);
                    _logger.LogDebug("[MODIFIED] {Entity} Id={Id} Fields=[{Fields}]",
                        entityName, entityId, string.Join(", ", modifiedFields));
                    break;

                case EntityState.Deleted:
                    _logger.LogDebug("[DELETED] {Entity} Id={Id}", entityName, entityId);
                    break;
            }
        }
    }

    private static string? GetEntityId(EntityEntry entry)
    {
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProp?.CurrentValue?.ToString();
    }

    #region suggestion code
    // =====================================================================
    // MỞ RỘNG KHI CẦN - bỏ comment và triển khai logic tương ứng
    // =====================================================================

    // ---------------------------------------------------------------------
    // 1. QUẢN LÝ TRANSACTION THỦ CÔNG
    // Dùng khi cần phối hợp EF Core + Dapper trong cùng 1 giao dịch,
    // hoặc khi 1 use case cần kiểm soát rõ ràng giữa các bước.
    // ---------------------------------------------------------------------
    // public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    // {
    //     await _db.Database.BeginTransactionAsync(cancellationToken);
    // }
    //
    // public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    // {
    //     await _db.Database.CommitTransactionAsync(cancellationToken);
    // }
    //
    // public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    // {
    //     await _db.Database.RollbackTransactionAsync(cancellationToken);
    // }

    // ---------------------------------------------------------------------
    // 2. PUBLISH DOMAIN EVENTS SAU KHI COMMIT
    // Ví dụ entity User có Danh sách DomainEvents; sau khi SaveChanges
    // thành công mới publish từng event ra ngoài (email, notification...).
    // ---------------------------------------------------------------------
    // public async Task<int> SaveChangesAndPublishEventsAsync(CancellationToken cancellationToken = default)
    // {
    //     var entities = _db.ChangeTracker.Entries<EntityBase>()
    //         .Where(e => e.Entity.DomainEvents.Count > 0)
    //         .Select(e => e.Entity)
    //         .ToList();
    //
    //     var events = entities.SelectMany(e => e.DomainEvents).ToList();
    //     entities.ForEach(e => e.ClearDomainEvents());
    //
    //     var result = await _db.SaveChangesAsync(cancellationToken);
    //
    //     foreach (var e in events)
    //         await _eventPublisher.PublishAsync(e);   // publish SAU commit
    //
    //     return result;
    // }

    // ---------------------------------------------------------------------
    // 3. OUTBOX PATTERN
    // Ghi event vào bảng Outbox trong CÙNG transaction với dữ liệu.
    // Background worker đọc outbox -> publish -> xóa. Chống mất event.
    // ---------------------------------------------------------------------
    // public async Task AddOutboxMessageAsync(object message, CancellationToken cancellationToken = default)
    // {
    //     _db.Set<OutboxMessage>().Add(new OutboxMessage
    //     {
    //         Id = Guid.NewGuid(),
    //         Type = message.GetType().FullName!,
    //         Data = JsonSerializer.Serialize(message),
    //         CreatedAt = DateTime.UtcNow
    //     });
    //     // KHÔNG SaveChanges ở đây - để gom chung với dữ liệu ở SaveChangesAsync
    // }

    // ---------------------------------------------------------------------
    // 4. AUDIT LOG TỰ ĐỘNG
    // Ghi nhật ký cho mọi thay đổi trước khi lưu - gom chung 1 transaction.
    // ---------------------------------------------------------------------
    // public async Task AddAuditEntryAsync(string action, string entityName, string? entityId,
    //     string? oldValues = null, string? newValues = null,
    //     CancellationToken cancellationToken = default)
    // {
    //     _db.Set<AuditLog>().Add(new AuditLog
    //     {
    //         Id = Guid.NewGuid(),
    //         Action = action,
    //         EntityName = entityName,
    //         EntityId = entityId,
    //         OldValues = oldValues,
    //         NewValues = newValues,
    //         CreatedAt = DateTime.UtcNow,
    //         UserId = _userContext.UserId   // ai thực hiện
    //     });
    // }

    // ---------------------------------------------------------------------
    // 5. RETRY / RESILIENCY
    // Tự động thử lại khi DB tạm thời lỗi (deadlock, timeout).
    // ---------------------------------------------------------------------
    // public async Task<int> SaveChangesWithRetryAsync(CancellationToken cancellationToken = default)
    // {
    //     var strategy = _db.Database.CreateExecutionStrategy();
    //     return await strategy.ExecuteAsync(async () =>
    //     {
    //         return await _db.SaveChangesAsync(cancellationToken);
    //     });
    // }

    // ---------------------------------------------------------------------
    // 6. TRACK THAY ĐỔI
    // Cho biết có thay đổi nào đang chờ lưu không - tránh SaveChanges vô ích.
    // ---------------------------------------------------------------------
    // public bool HasChanges()
    // {
    //     return _db.ChangeTracker.HasChanges();
    // }
    #endregion
}