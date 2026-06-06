**Business Logic**

**Nguyên tắc thiết kế chung**

- **Trip as Context Container:** Mọi hoạt động chính (Itinerary, Expense, Chat, Members) đều phải thuộc về một Trip. Không có Trip → không có context.
- **Source of Truth vẫn là Transaction**, nhưng mở rộng:
  - Transaction có thể thuộc Personal (`user_id`) hoặc thuộc Trip (`trip_id`).
  - Khi expense thuộc Trip → áp dụng split logic (TripExpenseSplit / TripMemberShare).
- **Collaboration & Permission:** Mỗi Trip có roles (Organizer/Creator, Member, Viewer). Một số action chỉ Organizer thực hiện (xóa Trip, finalize settlement...).
- **Realtime-first cho Collaboration**: Thay đổi Itinerary, Expense, Debt → phải trigger realtime update qua SignalR (system message + push đến tất cả members).
- **Soft Delete & Audit** vẫn giữ, nhưng thêm TripMember table để track ai được invite, ai đã join, permission level.
- **Multi-Currency nâng cao**: Trip có `trip_currency` (default), nhưng cho phép expense dùng currency khác → tự động convert theo tỷ giá tại thời điểm `transaction_date` (cần bảng ExchangeRate hoặc gọi external API + cache).
- **Atomicity cao hơn:** Tạo Expense + Split + Update Debt + Send System Message phải trong transaction database hoặc Outbox Pattern + Hangfire.
- **Performance:** Snapshot cho Trip Summary (total spent, per person balance, debt matrix) để tránh query phức tạp realtime.

## TRAVEL MODULE

**Phần này đã bao gồm những gì cần thiết cho MVP** của Travel Module:

- Tạo chuyến đi & quản lý thành viên
- Lập lịch trình cơ bản (Itinerary)
- Ghi nhận và chia chi tiêu nhóm
- Tích hợp nhẹ với Chat (System Message)
- Realtime update (được đề cập ở mức logic)

### 1. Trip (Chuyến đi)

**Business Logic chính:**

- Mỗi Trip là một không gian riêng biệt chứa lịch trình, thành viên, chi tiêu và chat.
- Trip có thể ở nhiều trạng thái khác nhau để phản ánh giai đoạn thực tế của chuyến đi.
- `total_spent` là giá trị cache, nguồn dữ liệu thật luôn lấy từ tổng các TripExpense.
- Khi tạo Trip mới, hệ thống tự động tạo Trip Chat Room tương ứng.
- Organizer có thể chỉnh sửa thông tin Trip bất kỳ lúc nào (trừ khi Trip đã Completed).

**Luồng chính:**

- User tạo Trip (nhập tên, thời gian, địa điểm chính).
- Hệ thống tạo Trip + TripMember (role = Organizer) + TripChatRoom.
- Mời thành viên qua link hoặc email.
- Cập nhật thông tin Trip (tên, ngày đi, ngày về, mô tả…).
- Chuyển trạng thái Trip khi bắt đầu hoặc kết thúc chuyến đi.

### 2. TripMember (Thành viên chuyến đi)

**Business Logic chính:**

- Một User có thể tham gia nhiều Trip khác nhau với vai trò khác nhau.
- Mỗi TripMember có một Role xác định quyền hạn trong Trip.
- `balance` của TripMember là giá trị cache (tiền đã chi trừ đi phần phải chịu), nguồn thật được tính từ TripExpenseSplit.
- Khi thành viên rời Trip (nếu được phép), hệ thống phải xử lý các khoản nợ/chưa thanh toán hiện có.

**Luồng chính:**

- Organizer mời thành viên (Pending).
- Thành viên chấp nhận lời mời → trạng thái thành Accepted.
- Thành viên có thể xem lịch trình, chat, và thêm chi tiêu (tùy theo quyền).
- Organizer có quyền xóa thành viên khỏi Trip (soft delete TripMember).

### 3. TripActivity (Hoạt động / Lịch trình)

**Business Logic chính:**

- Lịch trình được tổ chức theo ngày (day) để dễ theo dõi “Hôm nay đi đâu?”.
- Hỗ trợ collaborative editing: nhiều thành viên cùng thêm, sửa, sắp xếp lại hoạt động.
- Mỗi hoạt động có thể có ước tính chi phí (`cost_estimate`) để tham chiếu sau với chi tiêu thực tế.
- Khi thêm/sửa/xóa Activity quan trọng → tự động tạo System Message trong Trip Chat.

**Luồng chính:**

- Thêm hoạt động mới vào một ngày cụ thể.
- Sắp xếp lại thứ tự hoạt động (drag & drop).
- Đánh dấu hoạt động đã hoàn thành (status = Done).
- Xóa hoạt động (soft delete).

### 4. TripExpense (Chi tiêu trong chuyến đi)

**Business Logic chính:**

- Đây là bảng ghi nhận chi tiêu thực tế của cả nhóm trong Trip.
- Mỗi khoản chi phải chỉ rõ `paid_by_id` (người trả tiền thật).
- Sau khi tạo TripExpense, hệ thống tự động thực hiện chia tiền theo quy tắc Split và cập nhật nợ giữa các thành viên.
- Tất cả TripExpense đều thuộc về một Trip cụ thể. Mặc định ghi nhận bằng VND, nhưng hỗ trợ multi-currency: mỗi expense có thể dùng currency khác (USD, THB, JPY...) và tự động convert về `trip_currency` theo tỷ giá tại `expense_date` (dùng bảng ExchangeRate).
- Khi tạo TripExpense phải trigger realtime System Message trong chat nhóm.

**Luồng chính:**

- Thành viên thêm chi phí (ví dụ: ăn tối, vé tham quan, taxi…).
- Chọn người đã trả tiền (`paid_by`).
- Chọn cách chia tiền (Chia đều / Chia tùy chỉnh).
- Hệ thống tạo TripExpense + các bản ghi TripExpenseSplit.
- Cập nhật cache balance của các TripMember và gửi thông báo realtime.

### 5. TripExpenseSplit (Phân bổ chi phí)

**Business Logic chính:**

- Chi tiết cách một khoản TripExpense được phân bổ cho từng thành viên.
- Tổng số tiền trong TripExpenseSplit của một khoản chi phải bằng đúng amount của TripExpense.
- Hỗ trợ hai cách chia chính: **Equal** (chia đều) và **Custom** (chia theo số tiền hoặc tỷ lệ tùy chỉnh).

**Luồng chính:**

- Hệ thống tự động tạo split khi người dùng chọn “Chia đều”.
- Người dùng có thể chỉnh sửa split theo từng người (Custom split).
- Dùng để tính toán “Ai nợ ai” trong chuyến đi.

### 6. TripSettlement (Thanh toán cuối chuyến)

**Business Logic chính:**

- Ghi nhận các khoản thanh toán thực tế giữa thành viên để dứt điểm nợ nần sau chuyến đi.
- Đây là bước cuối cùng giúp chuyến đi được coi là “settled” hoàn toàn.

**Luồng chính:**

- Sau khi Trip chuyển sang trạng thái Completed, thành viên xem ma trận nợ.
- Người nợ chuyển tiền cho người được nợ và ghi nhận khoản thanh toán vào TripSettlement.
- Khi tất cả nợ đã được thanh toán, Trip có thể được đánh dấu là đã hoàn tất tài chính.

## FINANCIAL MODULE

### 1. User (Người dùng)

**Business Logic chính:**

- Mỗi người dùng chỉ có **1 tài khoản** (1 email chi liên kết với 1 tài khoản).
- Email là thông tin duy nhất để đăng nhập và khôi phục mật khẩu.
- Người dùng có một **tiền tệ mặc định** (`currency_default`) – đây là đơn vị dùng để hiển thị báo cáo tổng hợp, tổng tài sản, biểu đồ…
- Khi tạo tài khoản mới, hệ thống tự động tạo một số dữ liệu mặc định:
  - Một vài Category hệ thống (Ăn uống, Lương, Di chuyển, Điện nước…)
  - Ít nhất 1 Wallet mặc định (ví dụ: “Tiền mặt”)
- Người dùng có thể cập nhật tên, avatar, và tiền tệ mặc định bất kỳ lúc nào.
- Soft delete: Khi người dùng xóa tài khoản → chỉ đánh dấu xóa, không xóa dữ liệu ngay (dành cho khôi phục sau này).

**Luồng chính:**

- Đăng ký → Tạo User + Wallet mặc định + Category hệ thống
- Đăng nhập bằng email + password
- Quên mật khẩu → Gửi link reset qua email
- Cập nhật profile (name, avatar, `currency_default`)

### 2. Wallet (Ví / Nguồn tiền)

**Business Logic chính:**

- Một User có thể có **nhiều Wallet**.
- Mỗi Wallet đại diện cho một nơi chứa tiền (Tiền mặt, Ngân hàng, Momo, Thẻ tín dụng, Ví USD…).
- Mỗi Wallet có **currency riêng** (hỗ trợ multi-currency).
- `balance` trong Wallet chỉ là **giá trị cache** (hiển thị nhanh), **không phải nguồn dữ liệu thật**. → Giá trị thật luôn được tính từ tổng các Transaction thuộc Wallet đó.
- Khi tạo Wallet mới, balance ban đầu = 0.

**Các case thực tế quan trọng:**

- User muốn theo dõi tiền nằm ở đâu → tạo nhiều Wallet (Cash, Bank, Credit Card…)
- Chi tiêu bằng tiền mặt → chọn Wallet “Tiền mặt”
- Chi bằng thẻ tín dụng → chọn Wallet “Visa Credit” (balance sẽ âm)

**Luồng chính với Wallet:**

- Tạo Wallet mới (name, currency, type)
- Xem danh sách Wallet + số dư từng ví
- Sửa tên/icon/type của Wallet
- Xóa Wallet (chỉ được xóa khi không còn Transaction nào thuộc ví đó, hoặc chuyển hết giao dịch sang ví khác)

### 3. Category (Danh mục thu/chi)

**Business Logic chính:**

- Có 2 loại Category:
  - **System default**: `user_id` = null → không cho phép xóa, dùng chung cho tất cả user.
  - **User custom**: `user_id` có giá trị → chỉ user đó dùng và quản lý.
- Mỗi Category thuộc loại **income** hoặc **expense**.
- Hỗ trợ phân cấp (hierarchical): Một category có thể có parent (ví dụ: Ăn uống > Quán ăn > Cà phê).
- Category dùng để phân loại giao dịch và lập ngân sách.

**Luồng chính:**

- User chọn Category khi tạo Transaction.
- User có thể tạo thêm Category cá nhân.
- System default Category không cho phép xóa/sửa tên&type.
- Dùng trong Budget và báo cáo theo danh mục.

### 4. Transaction (Giao dịch) – Core nghiệp vụ

**Business Logic chính:**

- Đây là bảng quan trọng nhất của toàn bộ ứng dụng.
- Mọi khoản thu/chi/transfer đều phải ghi vào **Transaction**.
- Mỗi Transaction **bắt buộc** phải thuộc:
  - 1 User
  - 1 Wallet
  - 1 Category
- `type` chỉ có 3 giá trị: income, expense, transfer
- `transaction_date` là ngày xảy ra thực tế (dùng để làm báo cáo tháng/năm)
- `created_at` là thời điểm ghi nhận vào hệ thống (dùng cho audit)

**Các rule quan trọng:**

- Khi tạo Transaction:
  - Nếu type = income hoặc expense → chỉ ảnh hưởng đến **1 Wallet**
  - Nếu type = transfer → phải xử lý **2 Wallet** (trừ ví nguồn, cộng ví đích)
- Amount luôn là số dương. Dấu (+/-) được quyết định bởi type.
- Sau khi tạo Transaction → cập nhật cache balance của Wallet liên quan.

**Flow thực tế thường gặp:**

- User ăn trưa → tạo expense 50.000 từ Wallet “Tiền mặt”
- Nhận lương → tạo income 15 triệu vào Wallet “Vietcombank”
- Rút ATM 2 triệu → tạo transfer: trừ 2 triệu ở Bank, cộng 2 triệu ở Cash
- Mua hàng bằng thẻ tín dụng → tạo expense trên Wallet “Visa Credit” (balance càng âm)

### 5. Budget (Ngân sách)

**Business Logic chính:**

- Budget chỉ áp dụng cho **expense** Category.
- Một Budget thuộc về 1 Category + 1 kỳ (monthly / weekly).
- Hệ thống tính **spent** = tổng amount của các Transaction expense thuộc Category đó trong kỳ Budget.
- Progress = spent / `amount_limit`
- Khi spent ≥ 80% `amount_limit` → tạo Notification cảnh báo.
- Budget sẽ tự reset theo period và start_date.

**Flow thực tế:**

- Đầu tháng user đặt “Ăn uống” tối đa 3 triệu/tháng
- Hệ thống tự tính đến ngày hiện tại đã chi bao nhiêu
- Gần cuối tháng nếu sắp vượt → gửi thông báo

### 6. Recurring Transaction (Giao dịch định kỳ)

**Business Logic chính:**

- Dùng để tự động tạo các khoản tiền lặp lại (lương, tiền nhà, Netflix, điện nước…).
- Mỗi Recurring có frequency (monthly, weekly…), `next_execution_date`.
- Hàng ngày (hoặc theo cron job), hệ thống quét các Recurring đến hạn → tự động tạo Transaction thật.
- User có thể tạm dừng (`is_active` = false) hoặc đặt ngày kết thúc.

**Flow thực tế:**

- User tạo “Lương tháng” recurring monthly → ngày 1 hàng tháng tự tạo income
- User tạo “Tiền nhà” recurring monthly → tự động trừ tiền

### 7. Saving Goal (Mục tiêu tiết kiệm)

**Business Logic chính:**

- User tạo mục tiêu cụ thể: “Mua xe máy 25 triệu”, “Du lịch 15 triệu”…
- Có thể liên kết với một Category hoặc một Wallet.
- `current_amount` tăng khi user chuyển tiền vào Goal (thông qua Transaction đặc biệt hoặc nút “Nạp vào Goal”).
- Hiển thị tiến độ phần trăm và số tiền còn thiếu.

### 8. Notification

**Business Logic chính:**

- Hệ thống tự động tạo thông báo trong các trường hợp:
  - Sắp vượt hoặc đã vượt Budget
  - Gần đến hạn trả nợ thẻ tín dụng
  - Có Recurring Transaction được tạo tự động
  - Nhắc nhở nhập chi tiêu nếu vài ngày chưa có Transaction
- Notification có trạng thái `is_read` để hiển thị badge số lượng chưa đọc.

**Tóm tắt thứ tự ưu tiên phát triển Business Logic:**

1. **User** + Authentication
2. **Wallet** + Transaction cơ bản (income, expense)
3. **Category** + Budget
4. **Transfer** logic (rất quan trọng)
5. **Recurring Transaction**
6. **Saving Goal**
7. **Notification & Alert**

## CHAT REALTIME MODULE

**Business Logic chính:**

- Mỗi Trip có đúng **một TripChatRoom** để tất cả thành viên trao đổi kế hoạch, đề xuất địa điểm và cập nhật thông tin.
- Chat là “glue” giúp giữ người dùng ở lại trong ứng dụng thay vì chuyển sang Zalo hoặc Messenger.
- Hỗ trợ hai loại tin nhắn chính:
  - **User Message**: Tin nhắn do thành viên gửi (text thông thường).
  - **System Message**: Tin nhắn tự động được hệ thống tạo khi có sự kiện quan trọng (thêm chi phí, thêm hoạt động, thành viên join…).
- Toàn bộ tin nhắn đều thuộc về một Trip cụ thể và chỉ thành viên của Trip đó mới xem được.
- Realtime là bắt buộc: tin nhắn mới phải xuất hiện ngay lập tức trên thiết bị của tất cả thành viên đang online.
- Soft Delete áp dụng cho tin nhắn (cho phép thu hồi hoặc xóa sau này).
- Audit Fields: Tất cả tin nhắn đều có thông tin tạo và cập nhật.

**Luồng chính:**

- Khi tạo Trip mới → hệ thống tự động tạo TripChatRoom tương ứng.
- Thành viên gửi tin nhắn text thông thường hoặc gửi “Card” (rich message) như địa điểm, hoạt động, chi phí.
- Khi có sự kiện quan trọng trong Trip (thêm TripExpense, thêm TripActivity, mời thành viên…) → hệ thống tự động tạo System Message và đẩy realtime cho tất cả thành viên.
- Thành viên có thể xem lịch sử chat theo thứ tự thời gian.
- Hỗ trợ gửi tin nhắn realtime qua SignalR với Redis Backplane.

**Entities - Reality workflow**

Nhóm theo **loại bảng** => **output:**

- Mô tả ngắn gọn
- Danh sách field chính (các field quan trọng nhất)
- **Ví dụ JSON data**
- **Flow / Case thực tế** người dùng thường làm với bảng đó

## TRAVEL MODULE

**Lưu ý khi implement:**

- `total_spent` và balance trong Trip / TripMember chỉ là **cache**. Luôn tính toán từ TripExpense + TripExpenseSplit khi cần độ chính xác cao.
- Nên thêm **composite index** quan trọng: `(trip_id, activity_date)`, `(trip_id, expense_date)`, `(trip_id, user_id)`.

### 1. Trip (Chuyến đi)

**Mô tả:**

Bảng đại diện cho một chuyến du lịch nhóm. Đây là **container chính** chứa tất cả lịch trình, thành viên, chi tiêu và chat. Mỗi Trip có một Organizer làm người quản lý chính.

**Field chính:**

- `id` (Ulid PK)
- `name`
- `description` (TEXT, NULL)
- `start_date` (DATE)
- `end_date` (DATE)
- `destination` (VARCHAR(255))
- `status` (Planning | Ongoing | Completed | Cancelled)
- `total_spent` (DECIMAL(18,2) – cache only)
- `created_by_id` (FK to User – Organizer)
- `created_at`, `created_by_id`, `updated_at`, `updated_by_id`, `deleted_at`

**Ví dụ JSON:**

```json
{
  "id": "trip_001",
  "name": "Đà Nẵng - Hội An hè 2026",
  "description": "Chuyến đi 4 ngày 3 đêm cùng nhóm bạn",
  "startDate": "2026-06-15",
  "endDate": "2026-06-18",
  "destination": "Đà Nẵng",
  "status": "Planning",
  "totalSpent": 12450000,
  "createdById": "user_001",
  "createdAt": "2026-04-20T10:00:00Z"
}
```

**Flow / Case thực tế:**

- User tạo chuyến đi mới → hệ thống tự động tạo Trip + thêm Organizer vào TripMember + tạo TripChatRoom.
- Organizer chỉnh sửa thông tin chuyến đi (tên, ngày giờ, mô tả).
- Khi chuyến đi bắt đầu → đổi status sang Ongoing.
- Khi kết thúc → đổi sang Completed và hỗ trợ xem tổng kết chi tiêu.

### 2. TripMember (Thành viên chuyến đi)

**Mô tả:**

Quản lý danh sách thành viên tham gia Trip và quyền hạn của họ trong chuyến đi.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK)
- `user_id` (FK)
- `role` (Organizer | Member | Viewer)
- `invitation_status` (Pending | Accepted | Declined)
- `joined_at` (TIMESTAMP)
- `balance` (DECIMAL(18,2) – cache: dương = được nhận, âm = còn nợ)
- `color` (VARCHAR(7) – màu đại diện trong UI)
- `created_at`, `updated_at`, `deleted_at`

**Ví dụ JSON:**

```json
[
  {
    "id": "tm_001",
    "tripId": "trip_001",
    "userId": "user_001",
    "role": "Organizer",
    "invitationStatus": "Accepted",
    "balance": 0,
    "color": "#FF5733"
  },
  {
    "id": "tm_002",
    "tripId": "trip_001",
    "userId": "user_002",
    "role": "Member",
    "invitationStatus": "Accepted",
    "balance": -1250000,
    "color": "#33FF57"
  }
]
```

**Flow / Case thực tế:**

- Organizer mời thành viên qua link hoặc email → tạo record với `invitation_status` = Pending.
- Thành viên xác nhận → cập nhật thành Accepted.
- Thành viên tham gia thêm chi tiêu hoặc lịch trình.
- Organizer có quyền xóa thành viên (soft delete).

### 2.1. TripInvitation (Lời mời tham gia chuyến đi)

**Mô tả:**

Bảng riêng để quản lý lời mời tham gia Trip, tách biệt khỏi TripMember để support: link mời có hạn, gửi email, tracking trạng thái gửi. TripMember chỉ được tạo khi invitation được chấp nhận.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK)
- `invited_email` (VARCHAR(255) – email người được mời)
- `invited_by_id` (FK to User – người gửi lời mời)
- `token` (VARCHAR(100) – unique token cho link mời)
- `invitation_status` (Pending | Accepted | Declined | Expired)
- `expires_at` (TIMESTAMP – thời gian hết hạn)
- `email_sent_at` (TIMESTAMP, NULL)
- `accepted_at` (TIMESTAMP, NULL)
- `created_at`, `updated_at`

**Ví dụ JSON:**

```json
{
  "id": "inv_001",
  "tripId": "trip_001",
  "invitedEmail": "nguyenvanb@gmail.com",
  "invitedById": "user_001",
  "token": "abc123xyz",
  "invitationStatus": "Pending",
  "expiresAt": "2026-07-01T00:00:00Z",
  "createdAt": "2026-06-10T10:00:00Z"
}
```

**Flow / Case thực tế:**

- Organizer mời thành viên → tạo TripInvitation + gửi email chứa link có token.
- Người nhận click link → hệ thống tìm TripInvitation theo token, kiểm tra còn hạn.
- Hết hạn → tự động chuyển Expired.
- Sau khi xác nhận → tạo TripMember record với role tương ứng + TripInvitation chuyển Accepted.
- Tách biệt TripInvitation khỏi TripMember giúp dễ dàng resend email, track ai gửi lúc nào, và audit.

### 3. TripActivity (Hoạt động / Lịch trình)

**Mô tả:**

Lưu trữ các hoạt động trong lịch trình chuyến đi, được tổ chức theo ngày để dễ quản lý và giảm tranh cãi về thứ tự.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK)
- `activity_date` (DATE)
- `title`
- `description` (TEXT, NULL)
- `location` (VARCHAR(500))
- `start_time` (TIME, NULL)
- `end_time` (TIME, NULL)
- `category` (Sightseeing | Food | Transport | Accommodation | Other)
- `cost_estimate` (DECIMAL(18,2), NULL)
- `status` (Planned | Done | Cancelled)
- `order` (INT – dùng để sắp xếp)
- `created_by_id`, `updated_by_id`
- `created_at`, `updated_at`, `deleted_at`

**Ví dụ JSON:**

```json
{
  "id": "act_001",
  "tripId": "trip_001",
  "activityDate": "2026-06-16",
  "title": "Thăm Bà Nà Hills",
  "location": "Bà Nà Hills, Đà Nẵng",
  "startTime": "08:00",
  "endTime": "16:00",
  "category": "Sightseeing",
  "costEstimate": 450000,
  "status": "Planned",
  "order": 1
}
```

**Flow / Case thực tế:**

- Thành viên thêm hoạt động mới vào một ngày cụ thể.
- Nhiều người cùng sắp xếp lại thứ tự (drag & drop).
- Đánh dấu Done khi hoạt động đã hoàn thành.
- Xóa hoạt động (soft delete).

### 4. TripExpense (Chi tiêu trong chuyến đi)

**Mô tả:**

Bảng ghi nhận các khoản chi tiêu thực tế của nhóm trong chuyến đi. Đây là phần quan trọng kết nối Travel Module với Finance Module.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK)
- `paid_by_id` (FK to TripMember – người trả tiền thật, luôn là 1 member trong Trip)
- `category_id` (FK – có thể dùng Category từ Finance)
- `amount` (DECIMAL(18,2) – số tiền trong currency gốc)
- `currency` (VARCHAR(3) – mã tiền tệ gốc, mặc định 'VND')
- `exchange_rate_to_trip_currency` (DECIMAL(18,6), NULL – tỷ giá quy đổi về `trip_currency` tại `expense_date`)
- `amount_in_trip_currency` (DECIMAL(18,2), NULL – amount đã quy đổi, NULL nếu currency == trip_currency)
- `note` (TEXT)
- `expense_date` (DATE)
- `location` (VARCHAR(255), NULL)
- `is_settled` (boolean – default false)
- `created_by_id`, `updated_by_id`
- `created_at`, `updated_at`, `deleted_at`

**Ví dụ JSON:**

```json
{
  "id": "texp_001",
  "tripId": "trip_001",
  "paidById": "tm_001",
  "categoryId": "cat_food",
  "amount": 1250000,
  "currency": "VND",
  "exchangeRateToTripCurrency": null,
  "amountInTripCurrency": null,
  "note": "Ăn tối hải sản tại Mỹ Khánh",
  "expenseDate": "2026-06-16",
  "isSettled": false,
  "createdById": "user_001"
}
```

**Flow / Case thực tế:**

- Thành viên thêm khoản chi tiêu → chọn người trả tiền thật (`paid_by`).
- Chọn cách chia tiền → hệ thống tạo TripExpenseSplit.
- Tạo System Message trong chat: “A đã thêm chi 1.250.000đ ăn tối, đã chia đều”.

### 5. TripExpenseSplit (Phân bổ chi phí)

**Mô tả:**

Chi tiết cách một khoản chi được phân bổ cho từng thành viên trong Trip.

**Field chính:**

- `id` (Ulid PK)
- `trip_expense_id` (FK)
- `trip_member_id` (FK)
- `share_amount` (DECIMAL(18,2))
- `note` (VARCHAR(255), NULL)
- `created_at`

**Ví dụ JSON:**

```json
[
  {
    "id": "split_001",
    "tripExpenseId": "texp_001",
    "tripMemberId": "tm_001",
    "shareAmount": 312500
  },
  {
    "id": "split_002",
    "tripExpenseId": "texp_001",
    "tripMemberId": "tm_002",
    "shareAmount": 312500
  }
]
```

**Flow / Case thực tế:**

- Khi thêm TripExpense và chọn “Chia đều” → hệ thống tự động tạo split bằng nhau.
- Người dùng có thể chỉnh sửa để chia tùy chỉnh.
- Dữ liệu này dùng để tính toán balance và ma trận nợ của từng thành viên.

### 6. TripSettlement (Thanh toán bù trừ)

**Mô tả:**

Ghi nhận các khoản thanh toán thực tế giữa thành viên để dứt điểm nợ sau chuyến đi.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK)
- `from_member_id` (FK)
- `to_member_id` (FK)
- `amount` (DECIMAL(18,2))
- `settled_date` (DATE)
- `note`
- `created_at`, `created_by_id`

**Flow / Case thực tế:**

- Sau khi Trip Completed, thành viên xem “Ai nợ ai”.
- Người nợ chuyển tiền và ghi nhận khoản thanh toán vào bảng này.
- Khi tất cả settlement hoàn tất → Trip được coi là đã dứt điểm tài chính.

### 7. TripDebt (Ma trận nợ giữa các thành viên)

**Mô tả:**

Bảng track chi tiết "ai nợ ai" (pairwise debt) trong Trip. Dữ liệu được tính toán từ TripExpenseSplit và cập nhật mỗi khi có expense/split thay đổi. Không phải cache thuần — đây là nguồn dữ liệu cho debt matrix, có thể được sync lại (recalculate) từ splits khi cần.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK)
- `from_member_id` (FK to TripMember – người nợ)
- `to_member_id` (FK to TripMember – người được nợ)
- `amount` (DECIMAL(18,2) – số tiền đang nợ)
- `last_calculated_at` (TIMESTAMP – thời điểm tính toán gần nhất)
- `created_at`, `updated_at`

**Ví dụ JSON:**

```json
{
  "id": "debt_001",
  "tripId": "trip_001",
  "fromMemberId": "tm_002",
  "toMemberId": "tm_001",
  "amount": 312500,
  "lastCalculatedAt": "2026-06-17T10:00:00Z"
}
```

**Flow / Case thực tế:**

- Sau mỗi lần tạo/sửa/xóa TripExpenseSplit → hệ thống recalculate TripDebt.
- UI hiển thị "A nợ B 312.500đ, C nợ A 150.000đ" thay vì chỉ balance tổng.
- Tính toán suggest tối ưu: "B thay vì nợ A, hãy trả C giúp A" (debt simplification).
- TripSettlement ghi nhận thanh toán thực tế → giảm số dư TripDebt tương ứng.

## FINANCIAL MODULE

### 1. User (Người dùng)

**Mô tả:**

Thông tin tài khoản người dùng, là trung tâm của hệ thống. Bảng này kế thừa từ ASP.NET Identity và chứa các thông tin cá nhân hóa.

**Field chính:**

- `id` (Ulid PK)
- `email` (unique)
- `password_hash`
- `full_name`
- `avatar_url`
- `currency_default` (VARCHAR(3) – ví dụ: VND, USD)
- `locale` (ví dụ: vi-VN, en-US)
- `time_zone` (ví dụ: Asia/Ho_Chi_Minh)
- `created_at`
- `created_by_id` (Ulid, NULL – NULL khi user tự đăng ký)
- `updated_at`
- `updated_by_id` (Ulid, NULL)
- `deleted_at`

**Ví dụ JSON:**

```json
{
  "id": "user_001",
  "email": "nguyenvana@gmail.com",
  "fullName": "Nguyễn Văn A",
  "avatarUrl": "https://cdn.example.com/avatar/a.png",
  "currencyDefault": "VND",
  "locale": "vi-VN",
  "timeZone": "Asia/Ho_Chi_Minh",
  "createdAt": "2026-04-01T10:00:00Z",
  "updatedAt": "2026-04-15T14:30:00Z"
}
```

**Flow / Case thực tế:**

- Đăng ký tài khoản mới (tự động tạo Wallet mặc định và Category hệ thống)
- Đăng nhập bằng email & password
- Cập nhật thông tin cá nhân, avatar, tiền tệ mặc định
- Quên mật khẩu → reset qua email
- Xóa tài khoản (soft delete)

### 2. Wallet (Ví / Tài khoản)

**Mô tả:**

Nơi lưu trữ tiền của người dùng (tiền mặt, ngân hàng, ví điện tử, thẻ tín dụng…). Đây là bảng quan trọng để theo dõi tài sản theo từng kênh.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `name`
- `balance` (DECIMAL – **cache only**)
- `currency` (VARCHAR(3))
- `type` (Cash | Bank | Credit | Ewallet | Savings)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`
- `deleted_at`

**Ví dụ JSON (một user có nhiều ví):**

```json
[
  {
    "id": "wallet_cash",
    "userId": "user_001",
    "name": "Tiền mặt",
    "balance": 1350000,
    "currency": "VND",
    "type": "Cash",
    "createdAt": "2026-04-01T10:00:00Z"
  },
  {
    "id": "wallet_bank",
    "userId": "user_001",
    "name": "Vietcombank",
    "balance": 27000000,
    "currency": "VND",
    "type": "Bank",
    "createdAt": "2026-04-01T10:00:00Z"
  },
  {
    "id": "wallet_credit",
    "userId": "user_001",
    "name": "Visa Credit",
    "balance": -4500000,
    "currency": "VND",
    "type": "Credit",
    "createdAt": "2026-04-02T09:00:00Z"
  }
]
```

**Flow / Case thực tế:**

- Tạo ví mới (Tiền mặt, Momo, Thẻ tín dụng, Ví USD…)
- Xem dashboard hiển thị số dư từng ví
- Rút tiền ATM (chuyển từ Bank sang Cash)
- Chi tiêu bằng thẻ tín dụng (balance âm)
- Xóa ví (chỉ cho phép khi không còn giao dịch hoặc đã chuyển hết)

### 3. Category (Danh mục)

**Mô tả:**

Phân loại thu/chi. Hỗ trợ cả danh mục hệ thống mặc định và danh mục do người dùng tự tạo, có hỗ trợ phân cấp.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK, NULL = system default)
- `name`
- `type` (Income | Expense)
- `icon`
- `color`
- `parent_category_id` (FK, NULL)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`
- `deleted_at`

**Ví dụ JSON:**

```json
[
  {
    "id": "cat_food",
    "userId": null,
    "name": "Ăn uống",
    "type": "Expense",
    "icon": "restaurant",
    "color": "#FF6B6B",
    "parentCategoryId": null
  },
  {
    "id": "cat_salary",
    "userId": null,
    "name": "Lương",
    "type": "Income",
    "icon": "payments",
    "color": "#4CAF50"
  },
  {
    "id": "cat_transport",
    "userId": "user_001",
    "name": "Xăng xe",
    "type": "Expense",
    "icon": "local_gas_station",
    "color": "#2196F3",
    "parentCategoryId": null
  }
]
```

**Flow / Case thực tế:**

- Chọn danh mục khi tạo giao dịch
- Tạo ngân sách cho một danh mục
- Xem báo cáo chi tiêu theo danh mục (hỗ trợ phân cấp)

### 4. Transaction (Giao dịch – quan trọng nhất)

**Mô tả:**

Bảng cốt lõi của hệ thống, ghi nhận mọi hoạt động thu chi và chuyển tiền. Đây là **Source of Truth** cho toàn bộ dữ liệu tài chính.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `wallet_id` (FK) – ví chính của giao dịch
- `from_wallet_id` (FK, NULL) – dùng khi chuyển tiền
- `to_wallet_id` (FK, NULL) – dùng khi chuyển tiền
- `transfer_group_id` (Ulid, NULL) – nhóm transaction của cùng một lần chuyển tiền
- `category_id` (FK)
- `amount` (DECIMAL)
- `currency` (VARCHAR(3))
- `type` (Income | Expense | Transfer)
- `note` (TEXT)
- `transaction_date` (DATE)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`
- `deleted_at`

**Ví dụ JSON:**

```json
[
  {
    "id": "txn_001",
    "userId": "user_001",
    "walletId": "wallet_cash",
    "fromWalletId": null,
    "toWalletId": null,
    "transferGroupId": null,
    "categoryId": "cat_food",
    "amount": 50000,
    "currency": "VND",
    "type": "Expense",
    "note": "Ăn trưa",
    "transactionDate": "2026-04-02",
    "createdAt": "2026-04-02T12:00:00Z"
  },
  {
    "id": "txn_002",
    "userId": "user_001",
    "walletId": "wallet_bank",
    "categoryId": "cat_salary",
    "amount": 15000000,
    "currency": "VND",
    "type": "Income",
    "note": "Lương tháng 4",
    "transactionDate": "2026-04-01",
    "createdAt": "2026-04-01T09:00:00Z"
  }
]
```

**Flow / Case thực tế:**

- Nhập chi tiêu và thu nhập hàng ngày
- Chuyển tiền giữa các ví (Transfer)
- Tìm kiếm, lọc giao dịch theo ngày, ví, danh mục, tag

**Lưu ý quan trọng:** Khi type = Transfer, hệ thống phải xử lý atomic và liên kết qua `transfer_group_id`.

### 5. TransactionSplit (Chia nhỏ giao dịch)

**Mô tả:**

Cho phép một giao dịch được phân bổ vào nhiều danh mục khác nhau (ví dụ: một hóa đơn siêu thị gồm ăn uống và đồ gia dụng).

**Field chính:**

- `id` (Ulid PK)
- `transaction_id` (FK)
- `category_id` (FK)
- `amount` (DECIMAL)
- `note`
- `created_at`
- `created_by_id`

**Ví dụ JSON:**

```json
[
  {
    "id": "split_001",
    "transactionId": "txn_001",
    "categoryId": "cat_food",
    "amount": 35000,
    "note": "Ăn trưa"
  },
  {
    "id": "split_002",
    "transactionId": "txn_001",
    "categoryId": "cat_household",
    "amount": 150000,
    "note": "Đồ dùng gia đình"
  }
]
```

**Flow / Case thực tế:**

Người dùng chụp một hóa đơn nhưng muốn phân bổ chi phí vào nhiều danh mục khác nhau.

### 6. Budget (Ngân sách)

**Mô tả:**

Quản lý giới hạn chi tiêu theo danh mục và kỳ thời gian.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `category_id` (FK)
- `amount_limit` (DECIMAL)
- `period` (Monthly | Weekly | Yearly)
- `start_date` (DATE)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`
- `deleted_at`

**Ví dụ JSON:**

```json
{
  "id": "budget_001",
  "userId": "user_001",
  "categoryId": "cat_food",
  "amountLimit": 3000000,
  "period": "Monthly",
  "startDate": "2026-04-01",
  "createdAt": "2026-04-01T09:00:00Z"
}
```

**Flow / Case thực tế:**

- Đặt ngân sách “Ăn uống” 3 triệu/tháng
- Xem tiến độ chi tiêu theo thời gian thực
- Nhận thông báo khi sắp vượt ngân sách

### 7. Notification (Thông báo)

**Mô tả:**

Lưu trữ các thông báo hệ thống gửi cho người dùng.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `title`
- `message`
- `type` (BudgetAlert | DueDateReminder | RecurringCreated | System | TripInvitation | TripEvent)
- `reference_type` (Transaction | Trip | TripExpense | Budget | Debt | TripActivity | ...)
- `reference_id` (Ulid – FK polymorphic, ID của entity liên quan để deep-link)
- `is_read` (boolean)
- `created_at`

**Ví dụ JSON:**

```json
[
  {
    "id": "noti_001",
    "userId": "user_001",
    "title": "Bạn sắp vượt ngân sách",
    "message": "Bạn đã dùng 80% ngân sách Ăn uống tháng này",
    "type": "BudgetAlert",
    "referenceType": "Budget",
    "referenceId": "budget_001",
    "isRead": false,
    "createdAt": "2026-04-10T14:30:00Z"
  }
]
```

**Flow / Case thực tế:**

- Cảnh báo vượt ngân sách
- Nhắc nhở nhập giao dịch khi lâu không hoạt động
- Thông báo khi thực hiện giao dịch định kỳ

### 8. Tag & TransactionTag

**Tag**

**Mô tả:** Tag do người dùng tự tạo để phân loại giao dịch linh hoạt.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `name`
- `created_at`
- `created_by_id`

**Ví dụ JSON:**

```json
[
  { "id": "tag_work", "userId": "user_001", "name": "Công việc" },
  { "id": "tag_personal", "userId": "user_001", "name": "Cá nhân" }
]
```

**TransactionTag (Junction Table)**

**Ví dụ JSON:**

```json
[
  { "transactionId": "txn_001", "tagId": "tag_work" }
]
```

**Flow:** Lọc giao dịch theo tag, phân tích chi tiêu theo tag tùy chỉnh.

### 9. TransactionAttachment (Đính kèm)

**Mô tả:** Lưu file đính kèm (hóa đơn, biên lai…) cho giao dịch.

**Field chính:**

- `id` (Ulid PK)
- `transaction_id` (FK)
- `file_url`
- `created_at`
- `created_by_id`

**Ví dụ JSON:**

```json
{
  "id": "attach_001",
  "transactionId": "txn_001",
  "fileUrl": "https://cdn.example.com/bills/hoa-don-20260402.jpg",
  "createdAt": "2026-04-02T12:05:00Z"
}
```

**Flow:** Chụp ảnh hóa đơn → đính kèm vào giao dịch.

### 10. RecurringTransaction (Giao dịch định kỳ)

**Mô tả:** Quản lý các khoản thu/chi lặp lại theo chu kỳ.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `wallet_id` (FK) – ví chính (dùng khi type = Income/Expense)
- `from_wallet_id` (FK, NULL) – ví nguồn (dùng khi type = Transfer)
- `to_wallet_id` (FK, NULL) – ví đích (dùng khi type = Transfer)
- `category_id` (FK)
- `amount` (DECIMAL)
- `type` (Income | Expense | Transfer)
- `note`
- `frequency` (Daily | Weekly | Monthly | Yearly)
- `start_date` (DATE)
- `end_date` (DATE, NULL = vô hạn)
- `next_execution_date` (DATE)
- `is_active` (boolean)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`

**Ví dụ JSON:**

```json
{
  "id": "rec_001",
  "userId": "user_001",
  "walletId": "wallet_bank",
  "categoryId": "cat_salary",
  "amount": 15000000,
  "type": "Income",
  "note": "Lương tháng",
  "frequency": "Monthly",
  "startDate": "2026-04-01",
  "endDate": null,
  "nextExecutionDate": "2026-05-01",
  "isActive": true
}
```

**Flow thực tế:**

- Tự động tạo giao dịch lương ngày 1 hàng tháng
- Tự động trừ tiền nhà, điện nước, Netflix…

### 11. SavingGoal (Mục tiêu tiết kiệm)

**Mô tả:** Theo dõi các mục tiêu tiết kiệm dài hạn.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `name`
- `target_amount` (DECIMAL)
- `current_amount` (DECIMAL – cache)
- `target_date` (DATE, NULL)
- `category_id` (FK, NULL)
- `wallet_id` (FK, NULL)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`
- `deleted_at`

**Ví dụ JSON:**

```json
{
  "id": "goal_001",
  "userId": "user_001",
  "name": "Du lịch Đà Nẵng",
  "targetAmount": 20000000,
  "currentAmount": 4500000,
  "targetDate": "2026-12-31",
  "walletId": "wallet_bank"
}
```

**Flow thực tế:**

- Tạo mục tiêu và theo dõi tiến độ
- Chuyển tiền dư vào mục tiêu

### 12. Debt (Nợ & Vay)

**Mô tả:** Quản lý nợ vay, đặc biệt là nợ thẻ tín dụng.

**Field chính:**

- `id` (Ulid PK)
- `user_id` (FK)
- `wallet_id` (FK)
- `name`
- `initial_amount` (DECIMAL)
- `current_balance` (DECIMAL – cache)
- `interest_rate` (DECIMAL)
- `due_date` (DATE)
- `type` (CreditCard | PersonalLoan | Mortgage)
- `created_at`
- `created_by_id`
- `updated_at`
- `updated_by_id`

**Ví dụ JSON:**

```json
{
  "id": "debt_001",
  "userId": "user_001",
  "walletId": "wallet_credit",
  "name": "Thẻ Visa",
  "initialAmount": 50000000,
  "currentBalance": -8200000,
  "interestRate": 2.5,
  "dueDate": "2026-04-25",
  "type": "CreditCard"
}
```

**Flow thực tế:**

- Chi tiêu bằng thẻ tín dụng → nợ tăng
- Trả nợ → nợ giảm
- Nhận nhắc nợ sắp đến hạn

### 13. ExchangeRate (Tỷ giá ngoại tệ)

**Mô tả:**

Lưu trữ tỷ giá quy đổi giữa các loại tiền tệ, phục vụ cho tính năng multi-currency trong cả Travel và Financial Module. Dữ liệu được đồng bộ từ external API (ex: exchangerate-api.com) theo lịch và cache lại để tránh gọi API liên tục.

**Field chính:**

- `id` (Ulid PK)
- `from_currency` (VARCHAR(3) – ví dụ: 'USD')
- `to_currency` (VARCHAR(3) – ví dụ: 'VND')
- `rate` (DECIMAL(18,6) – tỷ giá: 1 from_currency = ? to_currency)
- `date` (DATE – ngày áp dụng tỷ giá)
- `source` (VARCHAR(50) – nguồn dữ liệu: 'external_api', 'admin_manual')
- `created_at`

**Ví dụ JSON:**

```json
{
  "id": "er_001",
  "fromCurrency": "USD",
  "toCurrency": "VND",
  "rate": 25450.000000,
  "date": "2026-06-05",
  "source": "external_api"
}
```

**Flow / Case thực tế:**

- Cron job hàng ngày gọi external API, lưu tỷ giá USD/VND, THB/VND, JPY/VND...
- User tạo TripExpense bằng USD → system lookup ExchangeRate tại `expense_date` → convert sang VND (trip_currency).
- User xem báo cáo tổng hợp với multi-currency → tự động quy đổi về currency mặc định của user.
- Admin có thể nhập tay tỷ giá nếu API không available.

### Lưu ý quan trọng khi phát triển

1. **Soft Delete & Audit Fields**: Áp dụng cho hầu hết các bảng (`created_by_id`, `updated_by_id`, `deleted_at`).
2. **Wallet.Balance là Cache**: Luôn tính toán từ bảng Transaction khi cần độ chính xác cao.
3. **Index quan trọng cho Transaction**:
   - `(user_id, transaction_date)`
   - `(user_id, wallet_id)`
   - `(user_id, category_id, transaction_date)`
4. **ID Type thống nhất: Ulid cho toàn bộ hệ thống**:
   - **Tại sao chọn Ulid thay vì UUID?** Ulid (Universally Unique Lexicographically Sortable Identifier) sortable theo thời gian → Primary Key clustering trong database hiệu quả hơn, index insert không bị page split như UUID random. Ngoài ra, Ulid ngắn hơn UUID (26 ký tự base32 so với 36 ký tự hex), tốt cho URL, JSON, và debugging.
   - Travel Module đã dùng Ulid, Financial Module chuyển từ UUID sang Ulid để đồng bộ.
   - Lưu ý: ASP.NET Identity mặc định dùng string cho ID, cần cấu hình để dùng Ulid.

### Lưu ý khi dev:

#### 1. Thêm phần Soft Delete & Audit fields (created_by, updated_by, deleted_at) cho hầu hết bảng

**Tại sao nên thêm?**

- **Soft Delete** (`deleted_at`): Thay vì xóa thật (DELETE) một record (ví dụ: xóa một Transaction hoặc một Wallet), bạn chỉ cập nhật trường `deleted_at` = thời điểm hiện tại. → Dữ liệu vẫn còn trong database, dễ khôi phục nếu user xóa nhầm. → Quan trọng trong app tài chính vì phải giữ lịch sử để audit, báo cáo thuế, hoặc khôi phục sau này. → Tránh mất dữ liệu vĩnh viễn.
- **Audit fields**:
  - `created_by`: ID của user tạo record (thường là `user_id`).
  - `updated_by`: ID của user sửa lần cuối.
  - Kết hợp với `created_at`, `updated_at`, `deleted_at`.

#### 2. Nhấn mạnh: wallet.balance chỉ là cache → luôn tính từ Transaction

**Tại sao phải nhấn mạnh?**

- balance trong bảng Wallet chỉ lưu để **hiển thị nhanh** trên UI (dashboard, danh sách ví).
- **Nguồn dữ liệu thật (source of truth)** luôn là bảng **Transaction**. Balance thực tế = SUM tất cả Transaction thuộc Wallet đó (income thì +, expense thì -, transfer thì trừ/cộng tương ứng).

**Rủi ro nếu không nhấn mạnh**:

- Dễ bị sai lệch (drift) nếu code update balance sai.
- Khi có bug hoặc import dữ liệu, balance cache có thể không khớp với thực tế.

#### 3. Index quan trọng cho Transaction: (user_id, transaction_date), (user_id, wallet_id)

**Tại sao cần chỉ rõ index?**

- Bảng **Transaction** thường là bảng lớn nhất (hàng trăm nghìn đến hàng triệu record khi user dùng lâu năm).
- Các query phổ biến nhất:
  - Xem giao dịch theo tháng/năm → lọc theo `transaction_date`
  - Xem tất cả giao dịch của user → lọc theo `user_id`
  - Tính balance của một ví → lọc theo `user_id` + `wallet_id`
  - Báo cáo theo tháng, theo ví, theo category…

**Index giúp**:

- Query chạy nhanh hơn rất nhiều (giảm thời gian từ vài giây xuống mili giây).
- Tiết kiệm chi phí server khi app có nhiều user.

**Index khuyến nghị cho bảng Transaction** (rất quan trọng cho performance):

- Composite index: `(user_id, transaction_date)` → hỗ trợ báo cáo theo tháng/năm
- Composite index: `(user_id, wallet_id)` → tính balance ví, xem lịch sử theo ví
- Có thể thêm: `(user_id, category_id, transaction_date)` cho báo cáo theo danh mục

## CHAT REALTIME MODULE

### Lưu ý quan trọng khi triển khai Chat Module

1. **Realtime Mechanism**:
   - Sử dụng **SignalR Hub** với Group = `Trip_{tripId}`.
   - Khi có tin nhắn mới hoặc system event → gọi `Clients.Group(tripId).SendAsync(...)`.
2. **Performance**:
   - Bảng TripMessage sẽ lớn nhanh → cần index mạnh: `(trip_chat_room_id, created_at)`.
   - Nên có cơ chế phân trang (infinite scroll) khi load lịch sử chat.
3. **Rich Message (Card)**:
   - Khi gửi ActivityCard hoặc ExpenseCard, dùng trường `metadata` (JSONB) để lưu reference ID.
   - Khi click vào card trên chat → navigate đến màn hình chi tiết tương ứng.
4. **System Message**:
   - Không cho phép người dùng xóa System Message.
   - Nội dung System Message nên ngắn gọn, rõ ràng và có thể click được.

### 1. TripChatRoom (Phòng chat của chuyến đi)

**Mô tả:**

Bảng đại diện cho phòng chat của một Trip. Thường có quan hệ 1-1 với bảng Trip.

**Field chính:**

- `id` (Ulid PK)
- `trip_id` (FK – unique)
- `created_at`
- `created_by_id`

**Ví dụ JSON:**

```json
{
  "id": "chatroom_001",
  "tripId": "trip_001",
  "createdAt": "2026-04-20T10:15:00Z",
  "createdById": "user_001"
}
```

**Flow / Case thực tế:**

- Khi User tạo Trip → hệ thống tự động tạo TripChatRoom.
- Tất cả tin nhắn của chuyến đi đều được lưu vào phòng chat này.
- Khi Trip bị soft delete → phòng chat cũng bị ẩn theo.

### 2. TripMessage (Tin nhắn trong chuyến đi)

**Mô tả:**

Bảng lưu trữ tất cả tin nhắn (user message và system message) trong Trip Chat. Đây là bảng cốt lõi của Realtime Chat Module.

**Field chính:**

- `id` (Ulid PK)
- `trip_chat_room_id` (FK) hoặc `trip_id` (FK)
- `sender_id` (FK to User – NULL nếu là System Message)
- `message_type` (Text | System | ActivityCard | ExpenseCard | PlaceCard)
- `content` (TEXT – nội dung text)
- `metadata` (JSONB – lưu dữ liệu bổ sung cho rich message: `activity_id`, `expense_id`, `place_info`…)
- `reply_to_id` (FK – hỗ trợ trả lời tin nhắn, optional)
- `is_edited` (boolean)
- `created_at`
- `updated_at`
- `deleted_at`

**Ví dụ JSON:**

```json
[
  {
    "id": "msg_001",
    "tripChatRoomId": "chatroom_001",
    "senderId": "user_001",
    "messageType": "Text",
    "content": "Mọi người thấy sao nếu sáng mai đi Bà Nà Hills?",
    "createdAt": "2026-05-01T08:30:00Z"
  },
  {
    "id": "msg_002",
    "tripChatRoomId": "chatroom_001",
    "senderId": null,
    "messageType": "System",
    "content": "Nguyễn Văn A đã thêm chi phí Ăn tối 1.250.000đ và chia đều cho cả nhóm",
    "metadata": {
      "expenseId": "texp_001",
      "amount": 1250000
    },
    "createdAt": "2026-05-01T12:15:00Z"
  }
]
```

**Flow / Case thực tế:**

- Thành viên gõ và gửi tin nhắn text thông thường.
- Thành viên gửi rich content (card hoạt động hoặc chi phí) từ màn hình Itinerary hoặc Expense.
- Hệ thống tự động tạo System Message khi có sự kiện (thêm expense, thêm activity, thành viên mới join…).
- Tin nhắn mới được đẩy realtime qua SignalR đến tất cả thành viên trong Trip.
- Thành viên có thể thu hồi tin nhắn (soft delete).
```

**Hướng dẫn sử dụng:** Tôi đã giữ **100% nội dung gốc**, chỉ cải thiện định dạng (headings rõ ràng hơn, code blocks JSON chuẩn, danh sách bullet nhất quán, khoảng cách dễ đọc) để UI hiển thị đẹp và dễ theo dõi hơn. Bạn có thể copy toàn bộ nội dung trên vào file Markdown mới.