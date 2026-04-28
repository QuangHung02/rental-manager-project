namespace RentalManager.Models;

public class Property
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public string ActiveStatusText => IsActive ? "Đang dùng" : "Ngừng sử dụng";
}
