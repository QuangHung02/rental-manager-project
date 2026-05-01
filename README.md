# RentalManager 🏠

**RentalManager** là một ứng dụng Desktop được thiết kế để hỗ trợ quản lý nhà trọ, căn hộ và các tài sản cho thuê. Được xây dựng dựa trên **WPF** (Windows Presentation Foundation) và **Entity Framework Core (SQLite)**, phần mềm cung cấp giải pháp quản lý phòng, lưu trữ thông tin người thuê, tính toán chi phí điện nước và xuất hóa đơn.

RentalManager còn được tích hợp sẵn Automation CLI, cho phép các script tự động, bot hoặc AI Agent tương tác với nghiệp vụ của ứng dụng thông qua dòng lệnh và JSON output.

---

## 🌟 Các tính năng nổi bật

### 🖥️ 1. Giao diện Đồ họa Trực quan (GUI)
* **Bảng thống kê (Dashboard):** Hiển thị tổng quan trạng thái tài sản, tỷ lệ phòng trống, doanh thu dự kiến và các khoản nợ chưa thu.
* **Quản lý Khách & Phòng:** Dễ dàng thêm khách thuê, xếp phòng, theo dõi trạng thái hợp đồng và lưu trữ thông tin nhận diện.
* **Cấu hình phí linh hoạt:** Cho phép thiết lập giá mặc định hoặc giá tùy chỉnh cho từng loại phí (Điện, Nước, Rác, Gửi xe) tại từng phòng riêng biệt.
* **Chốt chỉ số điện/nước (Meter Readings):** Giao diện nhập liệu hàng loạt tiện lợi, tự động tính toán khối lượng sử dụng và số tiền trong tháng.
* **Tự động hóa Hóa đơn & Thanh toán:** Tự động tạo hóa đơn với cơ chế chống trùng lặp. Cho phép ghi nhận trả góp (trả một phần) hoặc trả toàn bộ.

### 🤖 2. Automation CLI (Sẵn sàng cho AI / Bot)
* Cung cấp một ứng dụng **CLI độc lập** (`RentalManager.Cli`) đi kèm với phần mềm chính.
* Tái sử dụng cùng Service Layer và kết nối Database với ứng dụng WPF, đảm bảo dữ liệu luôn đồng nhất.
* Nhận các tham số đầu vào tiêu chuẩn và trả về **định dạng JSON chuẩn**.
* Hỗ trợ tích hợp với Telegram Bot hoặc AI Agent để tự động kiểm tra nợ, chốt điện nước, hoặc ghi nhận thanh toán.

---

## 📸 Hình ảnh giao diện

### 📊 Tổng quan (Dashboard)
Theo dõi nhanh tình trạng kinh doanh của các khu trọ.
![Dashboard](docs/images/Dashboard.png)

### ⚡ Chốt số điện nước
Giao diện nhập liệu nhanh và xác thực dữ liệu chặt chẽ.
![Meter Readings](docs/images/meter-readings.png)

### 💰 Quản lý Hóa đơn & Thanh toán
Theo dõi công nợ, xem chi tiết hóa đơn và thanh toán nhanh chóng.
![Invoices](docs/images/invoices.png)

### ⚙️ Trạng thái Automation CLI
Tab tích hợp sẵn để kiểm tra trạng thái CLI, cung cấp tài liệu và câu lệnh mẫu cho lập trình viên/AI.
![Automation CLI](docs/images/automation-cli.png)

---

## 🚀 Cài đặt & Sử dụng

### Yêu cầu hệ thống
* Hệ điều hành Windows 10/11 64-bit.
* Nếu sử dụng bản tải về từ GitHub Release (self-contained), **không yêu cầu** cài đặt .NET Runtime.
* Nếu build từ mã nguồn, yêu cầu cài đặt [.NET 8.0 Desktop Runtime / SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### Tải về (Download)
1. Truy cập mục **Releases** trên GitHub.
2. Tải về file `.zip` của phiên bản mới nhất.
3. Giải nén thư mục và chạy trực tiếp file `RentalManager.exe` để sử dụng.

### Build từ Mã nguồn
1. Clone repository về máy.
2. Mở terminal tại thư mục gốc.
3. Build project bằng .NET CLI:
   ```bash
   dotnet build RentalManager.sln -c Release
   ```
4. Chạy ứng dụng WPF:
   ```bash
   dotnet run --project RentalManager\RentalManager.csproj -c Release
   ```

### Vị trí Database
RentalManager sử dụng SQLite làm cơ sở dữ liệu cục bộ. File Database sẽ được tự động tạo ra trong lần chạy đầu tiên, lưu trữ an toàn tại thư mục AppData:
`C:\Users\<Tên_User>\AppData\Local\RentalManager\rental-manager.sqlite`

---

## 💻 Sử dụng Automation CLI

`RentalManager.Cli.exe` đóng vai trò là một cổng giao tiếp tự động hóa. Mọi kết quả trả về đều là JSON thuần để dễ dàng cho các hệ thống khác đọc hiểu.

**Xem tài liệu hướng dẫn chi tiết tại:**  
👉 [Tài liệu sử dụng CLI (docs/CLI_USAGE.md)](docs/CLI_USAGE.md)

**Ví dụ: Tự động tạo hóa đơn**
```bash
RentalManager.Cli.exe invoice create --property "Nhà A" --room "Phòng 101" --month "2026-04"
```
**Kết quả JSON trả về:**
```json
{
  "success": true,
  "message": "Đã tạo hóa đơn.",
  "data": {
    "invoiceId": 12,
    "amount": 3500000.0
  }
}
```

---

## 🏗️ Kiến trúc & Khả năng bảo trì

### Công nghệ sử dụng
* **Giao diện (UI):** WPF (Windows Presentation Foundation) với mẫu thiết kế MVVM.
* **Giao diện đồ họa:** MaterialDesignThemes for WPF.
* **Ngôn ngữ & Nền tảng:** C# 12, .NET 8.0.
* **Truy xuất dữ liệu:** Entity Framework Core (Code-First) qua SQLite.
* **Tự động hóa:** C# Console Application dùng chung Service Layer.

### Khả năng bảo trì (Maintainability)
Phần mềm tuân thủ nghiêm ngặt nguyên tắc **Separation of Concerns (SoC) - Tách biệt các mối quan tâm**:
* **Models:** Định nghĩa các thực thể và cấu trúc bảng SQLite.
* **Services Layer:** Chứa toàn bộ business logic (nghiệp vụ), xác thực và lưu trữ dữ liệu. Cả WPF GUI và CLI đều gọi trực tiếp qua lớp này.
* **ViewModels Layer (WPF):** Kết nối UI với Services thông qua `RelayCommand` và `INotifyPropertyChanged`.
* **CLI Program:** Xử lý tham số đầu vào và định dạng các exception của Service Layer thành JSON lỗi chuẩn hóa.

Với kiến trúc này, logic nghiệp vụ được tập trung tại tầng Services và có thể tái sử dụng dễ dàng bởi WPF hay CLI. Mặc dù vậy, khi phát triển tính năng mới, bạn vẫn cần thực hiện cập nhật đồng bộ ở các tầng giao diện (UI), Models, kiểm tra dữ liệu (Validation), CLI và viết thêm các bài test tương ứng.

---

## ⚠️ Hạn chế & Khả năng mở rộng

### Những hạn chế hiện tại
1. **Hoạt động đơn lẻ (Single-User):** Vì sử dụng SQLite (file-based database), phần mềm phù hợp với mô hình sử dụng cá nhân hoặc một máy quản lý chính. Tuy nhiên, nếu triển khai ở môi trường hàng trăm user cùng ghi dữ liệu một lúc, SQLite sẽ gặp tình trạng Database Lock.
2. **Lưu trữ cục bộ:** Dữ liệu được lưu trong máy cá nhân (AppData). Do đó, cần có giải pháp backup thủ công hoặc đồng bộ cloud để tránh mất dữ liệu khi hỏng máy.

### Khả năng mở rộng (Scalability)
* **Nâng cấp Database:** Entity Framework Core giúp việc chuyển đổi sang cơ sở dữ liệu lớn (như SQL Server hay PostgreSQL) trở nên khả thi hơn. Dù vậy, quá trình chuyển đổi thực tế sẽ luôn đòi hỏi thiết lập hạ tầng (Infrastructure), viết các file Migrations, xử lý tranh chấp đồng thời (Concurrency), sao lưu (Backup), triển khai (Deployment) và kiểm thử (Testing) kỹ lưỡng.
* **Web API:** Lớp `Services` tạo nền tảng thuận lợi để tách thành Web API trong tương lai, nhưng vẫn cần thực hiện xác thực (Authentication), phân quyền (Authorization), xử lý tranh chấp (Concurrency handling), quy trình triển khai (Deployment) và kiểm thử bảo mật (Security testing).
* **Tích hợp AI Agent:** Vì CLI có thể xuất ra JSON dự đoán được, các lập trình viên có thể kết hợp với các framework AI như OpenAI/LangChain để quản lý dữ liệu khu trọ bằng "Ngôn ngữ tự nhiên" mà không cần phải đụng chạm đến logic giao diện.

---

## 🔒 Dữ liệu & Bảo mật riêng tư (Data & Privacy)
* **Lưu trữ Local-first:** Mọi dữ liệu của ứng dụng được lưu trữ hoàn toàn trên file SQLite nội bộ máy tính của bạn.
* **Không lưu trữ Đám mây (No Cloud):** Ứng dụng mặc định không đẩy bất kỳ dữ liệu nào lên server hay cloud của bên thứ ba.
* **Bảo mật mã nguồn:** Tuyệt đối không commit (đẩy) file database thực tế chứa thông tin nhạy cảm của khách thuê lên GitHub hay bất kỳ repository công khai nào.
* **Sao lưu (Backup):** Người dùng nên có thói quen tự sao lưu (copy) file database định kỳ để phòng ngừa rủi ro mất mát dữ liệu do hỏng hóc thiết bị.

