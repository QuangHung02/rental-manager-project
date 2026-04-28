using System.Windows;
using System.Windows.Controls;
using RentalManager.Models;
using PropertyModel = RentalManager.Models.Property;

namespace RentalManager.Views;

public partial class RoomTransferDialog : Window
{
    private readonly RoomTenant _assignment;
    private readonly List<PropertyModel> _properties;
    private readonly List<Room> _rooms;

    public RoomTransferDialog(RoomTenant assignment, IEnumerable<PropertyModel> properties, IEnumerable<Room> rooms)
    {
        InitializeComponent();
        _assignment = assignment;
        _properties = properties.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        _rooms = rooms
            .Where(x => x.Status is Enums.RoomStatus.Vacant or Enums.RoomStatus.Occupied)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();

        TenantText.Text = assignment.TenantName;
        CurrentRoomText.Text = $"{assignment.PropertyName} - {assignment.RoomName}";
        MoveDatePicker.SelectedDate = DateTime.Today;

        PropertyCombo.ItemsSource = _properties;
        PropertyCombo.DisplayMemberPath = nameof(PropertyModel.Name);
        PropertyCombo.SelectedValuePath = nameof(PropertyModel.Id);

        RoomCombo.DisplayMemberPath = nameof(Room.RoomName);
        RoomCombo.SelectedValuePath = nameof(Room.Id);

        var currentPropertyId = assignment.Room?.PropertyId;
        if (currentPropertyId is > 0 && _properties.Any(x => x.Id == currentPropertyId))
        {
            PropertyCombo.SelectedValue = currentPropertyId.Value;
        }
        else
        {
            PropertyCombo.SelectedIndex = _properties.Count > 0 ? 0 : -1;
        }
    }

    public int TargetRoomId { get; private set; }
    public DateTime MoveDate => MoveDatePicker.SelectedDate ?? DateTime.Today;
    public bool IsRepresentative => RepresentativeCheckBox.IsChecked == true;

    private void PropertyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedPropertyId = PropertyCombo.SelectedValue as int?;
        var rooms = selectedPropertyId is > 0
            ? _rooms.Where(x => x.PropertyId == selectedPropertyId.Value).ToList()
            : _rooms;

        RoomCombo.ItemsSource = rooms;
        RoomCombo.SelectedIndex = rooms.Count > 0 ? 0 : -1;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (RoomCombo.SelectedValue is not int roomId || roomId <= 0)
        {
            MessageBox.Show("Vui lòng chọn phòng mới.", "Chuyển phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (roomId == _assignment.RoomId)
        {
            MessageBox.Show("Phòng mới phải khác phòng hiện tại.", "Chuyển phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TargetRoomId = roomId;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
