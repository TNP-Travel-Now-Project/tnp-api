namespace AuthApi.Application.Abstractions.Interfaces.UnitOfWork;

/// <summary>
/// Đơn vị công việc - gom mọi thay đổi trong 1 business use case
/// và commit một lần duy nhất để đảm bảo tính nguyên tử (atomic).
///
/// Lợi ích:
///   - Tránh nhiều lần gọi SaveChanges rời rạc trong 1 use case
///   - Nếu 1 bước lỗi giữa chừng -> rollback toàn bộ, dữ liệu không lệch
///   - Application layer không phụ thuộc vào EF Core (chỉ biết interface)
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Lưu MỌI thay đổi đang được theo dõi (change tracking) xuống DB trong 1 transaction.
    /// Trả về số dòng bị ảnh hưởng.
    ///
    /// Gọi 1 lần DUY NHẤT ở cuối use case.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // =====================================================================
    // CÁC KHẢ NĂNG UnitOfWork CÓ THỂ MỞ RỘNG (chưa implement - mở rộng khi cần)
    // =====================================================================

    // ---------------------------------------------------------------------
    // 1. QUẢN LÝ TRANSACTION THỦ CÔNG
    // Dùng khi 1 use case cần control rõ ràng giữa các bước,
    // hoặc cần phối hợp EF Core + Dapper trong cùng 1 giao dịch.
    // ---------------------------------------------------------------------
    // Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    // Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    // Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------------
    // 2. PUBLISH DOMAIN EVENTS SAU KHI COMMIT
    // Thu thập các domain event từ entity đã thay đổi,
    // chỉ publish SAU khi SaveChanges thành công (tránh gửi email/thông báo
    // khi dữ liệu thực tế chưa được lưu).
    // ---------------------------------------------------------------------
    // Task<int> SaveChangesAndPublishEventsAsync(CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------------
    // 3. OUTBOX PATTERN (CHỐNG MẤT SỰ KIỆN)
    // Ghi event vào bảng Outbox CÙNG transaction với dữ liệu,
    // background worker đọc outbox -> publish -> xóa.
    // Đảm bảo event KHÔNG BAO GIỜ bị mất dù app crash sau commit.
    // ---------------------------------------------------------------------
    // Task AddOutboxMessageAsync(object message, CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------------
    // 4. AUDIT LOG TỰ ĐỘNG
    // Ghi nhật ký (ai, làm gì, lúc nào) cho mọi thay đổi trước khi SaveChanges.
    // ---------------------------------------------------------------------
    // Task AddAuditEntryAsync(string action, string entityName, string? entityId,
    //     string? oldValues = null, string? newValues = null,
    //     CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------------
    // 5. RETRY / RESILIENCY
    // Tự động thử lại khi DB tạm thời lỗi (deadlock, timeout) nhờ
    // execution strategy của EF Core.
    // ---------------------------------------------------------------------
    // Task<int> SaveChangesWithRetryAsync(CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------------
    // 6. TRACK THAY ĐỔI (KIỂM TRA)
    // Cho biết có thay đổi nào đang chờ lưu không - hữu ích khi cần
    // tránh gọi SaveChanges vô ích khi không có gì thay đổi.
    // ---------------------------------------------------------------------
    bool HasChanges();
}