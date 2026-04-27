using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using RentalManager.Data;
using RentalManager.DTOs;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;
using RentalManager.Services;

namespace RentalManager.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly PropertyService _propertyService = new();
    private readonly RoomService _roomService = new();
    private readonly TenantService _tenantService = new();
    private readonly RoomTenantService _roomTenantService = new();
    private readonly FeeTypeService _feeTypeService = new();
    private readonly RoomFeeConfigService _roomFeeConfigService = new();
    private readonly MeterReadingService _meterReadingService = new();
    private readonly InvoiceService _invoiceService = new();
    private readonly PaymentService _paymentService = new();
    private readonly DashboardService _dashboardService = new();
    private readonly BackupService _backupService = new();
    private readonly DemoDataService _demoDataService = new();

    private string _billingMonth = DateTime.Today.ToString("yyyy-MM");
    private string _statusMessage = "Sẵn sàng";
    private DashboardSummary _dashboard = new();
    private Property? _selectedProperty;
    private Room? _selectedRoom;
    private Tenant? _selectedTenant;
    private FeeType? _selectedFeeType;
    private RoomFeeConfig? _selectedRoomFeeConfig;
    private int _roomFeePropertyFilterId;
    private int _roomFeeRoomFilterId;
    private int _roomFeeFeeTypeFilterId;
    private bool _roomFeeEnabledOnly = true;

    public MainViewModel()
    {
        DbContextFactory.EnsureDatabase();
        AddPropertyCommand = new RelayCommand(() => Run(AddProperty));
        EditPropertyCommand = new RelayCommand(() => Run(EditProperty));
        DeactivatePropertyCommand = new RelayCommand(() => Run(DeactivateProperty));
        AddRoomCommand = new RelayCommand(() => Run(AddRoom));
        EditRoomCommand = new RelayCommand(() => Run(EditRoom));
        DeactivateRoomCommand = new RelayCommand(() => Run(DeactivateRoom));
        AddTenantCommand = new RelayCommand(() => Run(AddTenant));
        EditTenantCommand = new RelayCommand(() => Run(EditTenant));
        AssignTenantCommand = new RelayCommand(() => Run(AssignTenant));
        AddFeeTypeCommand = new RelayCommand(() => Run(AddFeeType));
        EditFeeTypeCommand = new RelayCommand(() => Run(EditFeeType));
        DeactivateFeeTypeCommand = new RelayCommand(() => Run(DeactivateFeeType));
        AddRoomFeeConfigCommand = new RelayCommand(() => Run(AddRoomFeeConfig));
        EditRoomFeeConfigCommand = new RelayCommand(() => Run(EditRoomFeeConfig));
        DisableRoomFeeConfigCommand = new RelayCommand(() => Run(DisableRoomFeeConfig));
        AddMeterReadingCommand = new RelayCommand(() => Run(AddMeterReading));
        GenerateInvoiceCommand = new RelayCommand(() => Run(GenerateInvoice));
        GenerateAllInvoicesCommand = new RelayCommand(() => Run(GenerateAllInvoices));
        IssueInvoiceCommand = new RelayCommand(() => Run(IssueInvoice));
        RecordPaymentCommand = new RelayCommand(() => Run(RecordPayment));
        CopyInvoiceCommand = new RelayCommand(() => Run(CopyInvoice));
        CancelInvoiceCommand = new RelayCommand(() => Run(CancelInvoice));
        BackupCommand = new RelayCommand(() => Run(Backup));
        RestoreCommand = new RelayCommand(() => Run(Restore));
        SeedDemoDataCommand = new RelayCommand(() => Run(SeedDemoData));
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    public ObservableCollection<Property> Properties { get; } = new();
    public ObservableCollection<Room> Rooms { get; } = new();
    public ObservableCollection<Tenant> Tenants { get; } = new();
    public ObservableCollection<RoomTenant> RoomTenants { get; } = new();
    public ObservableCollection<FeeType> FeeTypes { get; } = new();
    public ObservableCollection<RoomFeeConfig> RoomFeeConfigs { get; } = new();
    public ObservableCollection<RoomFeeConfig> FilteredRoomFeeConfigs { get; } = new();
    public ObservableCollection<MeterReading> MeterReadings { get; } = new();
    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<Payment> Payments { get; } = new();

    public Property NewProperty { get; set; } = new();
    public Room NewRoom { get; set; } = new();
    public Tenant NewTenant { get; set; } = new();
    public RoomTenant NewRoomTenant { get; set; } = new();
    public FeeType NewFeeType { get; set; } = new();
    public RoomFeeConfig NewRoomFeeConfig { get; set; } = new();
    public MeterReading NewMeterReading { get; set; } = new();
    public Invoice? SelectedInvoice { get; set; }
    public int InvoiceRoomId { get; set; }
    public decimal NewPaymentAmount { get; set; }
    public PaymentMethod NewPaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? NewPaymentNote { get; set; }

    public Property? SelectedProperty
    {
        get => _selectedProperty;
        set => SetProperty(ref _selectedProperty, value);
    }

    public Room? SelectedRoom
    {
        get => _selectedRoom;
        set => SetProperty(ref _selectedRoom, value);
    }

    public Tenant? SelectedTenant
    {
        get => _selectedTenant;
        set => SetProperty(ref _selectedTenant, value);
    }

    public FeeType? SelectedFeeType
    {
        get => _selectedFeeType;
        set => SetProperty(ref _selectedFeeType, value);
    }

    public RoomFeeConfig? SelectedRoomFeeConfig
    {
        get => _selectedRoomFeeConfig;
        set => SetProperty(ref _selectedRoomFeeConfig, value);
    }

    public int RoomFeePropertyFilterId
    {
        get => _roomFeePropertyFilterId;
        set
        {
            if (SetProperty(ref _roomFeePropertyFilterId, value))
            {
                RefreshRoomFeeFilters();
            }
        }
    }

    public int RoomFeeRoomFilterId
    {
        get => _roomFeeRoomFilterId;
        set
        {
            if (SetProperty(ref _roomFeeRoomFilterId, value))
            {
                RefreshRoomFeeFilters();
            }
        }
    }

    public int RoomFeeFeeTypeFilterId
    {
        get => _roomFeeFeeTypeFilterId;
        set
        {
            if (SetProperty(ref _roomFeeFeeTypeFilterId, value))
            {
                RefreshRoomFeeFilters();
            }
        }
    }

    public bool RoomFeeEnabledOnly
    {
        get => _roomFeeEnabledOnly;
        set
        {
            if (SetProperty(ref _roomFeeEnabledOnly, value))
            {
                RefreshRoomFeeFilters();
            }
        }
    }

    public Array RoomStatuses => Enum.GetValues(typeof(RoomStatus));
    public Array AssignmentStatuses => Enum.GetValues(typeof(RoomTenantStatus));
    public Array CalculationTypes => Enum.GetValues(typeof(CalculationType));
    public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

    public string BillingMonth
    {
        get => _billingMonth;
        set
        {
            if (SetProperty(ref _billingMonth, value))
            {
                LoadDashboard();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public DashboardSummary Dashboard
    {
        get => _dashboard;
        set => SetProperty(ref _dashboard, value);
    }

    public string DatabasePath => _backupService.DatabasePath;

    public RelayCommand AddPropertyCommand { get; }
    public RelayCommand EditPropertyCommand { get; }
    public RelayCommand DeactivatePropertyCommand { get; }
    public RelayCommand AddRoomCommand { get; }
    public RelayCommand EditRoomCommand { get; }
    public RelayCommand DeactivateRoomCommand { get; }
    public RelayCommand AddTenantCommand { get; }
    public RelayCommand EditTenantCommand { get; }
    public RelayCommand AssignTenantCommand { get; }
    public RelayCommand AddFeeTypeCommand { get; }
    public RelayCommand EditFeeTypeCommand { get; }
    public RelayCommand DeactivateFeeTypeCommand { get; }
    public RelayCommand AddRoomFeeConfigCommand { get; }
    public RelayCommand EditRoomFeeConfigCommand { get; }
    public RelayCommand DisableRoomFeeConfigCommand { get; }
    public RelayCommand AddMeterReadingCommand { get; }
    public RelayCommand GenerateInvoiceCommand { get; }
    public RelayCommand GenerateAllInvoicesCommand { get; }
    public RelayCommand IssueInvoiceCommand { get; }
    public RelayCommand RecordPaymentCommand { get; }
    public RelayCommand CopyInvoiceCommand { get; }
    public RelayCommand CancelInvoiceCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand SeedDemoDataCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public void Load()
    {
        Replace(Properties, _propertyService.GetAll());
        Replace(Rooms, _roomService.GetAll());
        Replace(Tenants, _tenantService.GetAll());
        Replace(RoomTenants, _roomTenantService.GetAll());
        Replace(FeeTypes, _feeTypeService.GetAll());
        Replace(RoomFeeConfigs, _roomFeeConfigService.GetAll());
        RefreshRoomFeeFilters();
        Replace(MeterReadings, _meterReadingService.GetAll());
        Replace(Invoices, _invoiceService.GetAll());
        Replace(Payments, _paymentService.GetAll());
        LoadDashboard();
    }

    private void LoadDashboard()
    {
        Dashboard = _dashboardService.GetSummary(BillingMonth);
    }

    private void AddProperty()
    {
        _propertyService.Save(NewProperty);
        NewProperty = new Property();
        OnPropertyChanged(nameof(NewProperty));
        Load();
    }

    private void EditProperty()
    {
        if (SelectedProperty is null)
        {
            throw new ValidationException("Chọn nhà/khu trọ trước.");
        }

        NewProperty = new Property
        {
            Id = SelectedProperty.Id,
            Name = SelectedProperty.Name,
            Address = SelectedProperty.Address,
            Note = SelectedProperty.Note,
            IsActive = SelectedProperty.IsActive,
            CreatedAt = SelectedProperty.CreatedAt,
            UpdatedAt = SelectedProperty.UpdatedAt
        };
        OnPropertyChanged(nameof(NewProperty));
    }

    private void DeactivateProperty()
    {
        if (SelectedProperty is null)
        {
            throw new ValidationException("Chọn nhà/khu trọ trước.");
        }

        _propertyService.Deactivate(SelectedProperty.Id);
        Load();
    }

    private void AddRoom()
    {
        _roomService.Save(NewRoom);
        NewRoom = new Room();
        OnPropertyChanged(nameof(NewRoom));
        Load();
    }

    private void EditRoom()
    {
        if (SelectedRoom is null)
        {
            throw new ValidationException("Chọn phòng trước.");
        }

        NewRoom = new Room
        {
            Id = SelectedRoom.Id,
            PropertyId = SelectedRoom.PropertyId,
            RoomName = SelectedRoom.RoomName,
            Floor = SelectedRoom.Floor,
            BaseRent = SelectedRoom.BaseRent,
            Status = SelectedRoom.Status,
            Note = SelectedRoom.Note,
            CreatedAt = SelectedRoom.CreatedAt,
            UpdatedAt = SelectedRoom.UpdatedAt
        };
        OnPropertyChanged(nameof(NewRoom));
    }

    private void DeactivateRoom()
    {
        if (SelectedRoom is null)
        {
            throw new ValidationException("Chọn phòng trước.");
        }

        _roomService.Deactivate(SelectedRoom.Id);
        Load();
    }

    private void AddTenant()
    {
        _tenantService.Save(NewTenant);
        NewTenant = new Tenant();
        OnPropertyChanged(nameof(NewTenant));
        Load();
    }

    private void EditTenant()
    {
        if (SelectedTenant is null)
        {
            throw new ValidationException("Chọn người thuê trước.");
        }

        NewTenant = new Tenant
        {
            Id = SelectedTenant.Id,
            FullName = SelectedTenant.FullName,
            Phone = SelectedTenant.Phone,
            Email = SelectedTenant.Email,
            IdentityNumber = SelectedTenant.IdentityNumber,
            Note = SelectedTenant.Note,
            CreatedAt = SelectedTenant.CreatedAt,
            UpdatedAt = SelectedTenant.UpdatedAt
        };
        OnPropertyChanged(nameof(NewTenant));
    }

    private void AssignTenant()
    {
        _roomTenantService.Save(NewRoomTenant);
        NewRoomTenant = new RoomTenant();
        OnPropertyChanged(nameof(NewRoomTenant));
        Load();
    }

    private void AddFeeType()
    {
        _feeTypeService.Save(NewFeeType);
        NewFeeType = new FeeType();
        OnPropertyChanged(nameof(NewFeeType));
        Load();
    }

    private void EditFeeType()
    {
        if (SelectedFeeType is null)
        {
            throw new ValidationException("Chọn loại phí trước.");
        }

        NewFeeType = new FeeType
        {
            Id = SelectedFeeType.Id,
            Name = SelectedFeeType.Name,
            DefaultCalculationType = SelectedFeeType.DefaultCalculationType,
            DefaultUnit = SelectedFeeType.DefaultUnit,
            DefaultUnitPrice = SelectedFeeType.DefaultUnitPrice,
            IsSystem = SelectedFeeType.IsSystem,
            IsActive = SelectedFeeType.IsActive
        };
        OnPropertyChanged(nameof(NewFeeType));
    }

    private void DeactivateFeeType()
    {
        if (SelectedFeeType is null)
        {
            throw new ValidationException("Chọn loại phí trước.");
        }

        _feeTypeService.Deactivate(SelectedFeeType.Id);
        Load();
    }

    private void AddRoomFeeConfig()
    {
        _roomFeeConfigService.Save(NewRoomFeeConfig);
        NewRoomFeeConfig = new RoomFeeConfig();
        OnPropertyChanged(nameof(NewRoomFeeConfig));
        Load();
    }

    private void EditRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null)
        {
            throw new ValidationException("Chọn cấu hình phí trước.");
        }

        NewRoomFeeConfig = new RoomFeeConfig
        {
            Id = SelectedRoomFeeConfig.Id,
            RoomId = SelectedRoomFeeConfig.RoomId,
            FeeTypeId = SelectedRoomFeeConfig.FeeTypeId,
            CalculationType = SelectedRoomFeeConfig.CalculationType,
            UnitPrice = SelectedRoomFeeConfig.UnitPrice,
            FixedAmount = SelectedRoomFeeConfig.FixedAmount,
            Quantity = SelectedRoomFeeConfig.Quantity,
            Enabled = SelectedRoomFeeConfig.Enabled,
            Note = SelectedRoomFeeConfig.Note
        };
        OnPropertyChanged(nameof(NewRoomFeeConfig));
    }

    private void DisableRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null)
        {
            throw new ValidationException("Chọn cấu hình phí trước.");
        }

        _roomFeeConfigService.Disable(SelectedRoomFeeConfig.Id);
        Load();
    }

    private void AddMeterReading()
    {
        if (NewMeterReading.PreviousReading == 0)
        {
            NewMeterReading.PreviousReading = _meterReadingService.GetPreviousReading(NewMeterReading.RoomId, NewMeterReading.FeeTypeId, BillingMonth);
        }

        NewMeterReading.BillingMonth = BillingMonth;
        _meterReadingService.Save(NewMeterReading);
        NewMeterReading = new MeterReading();
        OnPropertyChanged(nameof(NewMeterReading));
        Load();
    }

    private void GenerateInvoice()
    {
        if (InvoiceRoomId <= 0)
        {
            throw new ValidationException("Chọn phòng để tạo hóa đơn.");
        }

        _invoiceService.Generate(InvoiceRoomId, BillingMonth);
        Load();
    }

    private void GenerateAllInvoices()
    {
        _invoiceService.GenerateAll(BillingMonth);
        Load();
    }

    private void IssueInvoice()
    {
        EnsureInvoiceSelected();
        _invoiceService.Issue(SelectedInvoice!.Id);
        Load();
    }

    private void RecordPayment()
    {
        EnsureInvoiceSelected();
        _paymentService.Record(SelectedInvoice!.Id, NewPaymentAmount, NewPaymentMethod, DateTime.Today, NewPaymentNote);
        NewPaymentAmount = 0;
        NewPaymentNote = null;
        OnPropertyChanged(nameof(NewPaymentAmount));
        OnPropertyChanged(nameof(NewPaymentNote));
        Load();
    }

    private void CopyInvoice()
    {
        EnsureInvoiceSelected();
        Clipboard.SetText(_invoiceService.CopyText(SelectedInvoice!.Id));
        StatusMessage = "Đã sao chép hóa đơn.";
    }

    private void CancelInvoice()
    {
        EnsureInvoiceSelected();
        _invoiceService.Cancel(SelectedInvoice!.Id);
        Load();
    }

    private void Backup()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "RentalManagerBackups");
        var path = _backupService.BackupTo(folder);
        StatusMessage = $"Đã sao lưu: {path}";
    }

    private void Restore()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "SQLite backup (*.sqlite)|*.sqlite|All files (*.*)|*.*",
            Title = "Chọn bản sao lưu"
        };

        if (dialog.ShowDialog() == true)
        {
            var confirm = MessageBox.Show("Khôi phục bản sao lưu này? Dữ liệu hiện tại sẽ bị thay thế.", "Khôi phục dữ liệu", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _backupService.RestoreFrom(dialog.FileName);
            Load();
            StatusMessage = "Đã khôi phục dữ liệu.";
        }
    }

    private void SeedDemoData()
    {
        _demoDataService.Seed();
        BillingMonth = "2026-04";
        Load();
    }

    private void EnsureInvoiceSelected()
    {
        if (SelectedInvoice is null)
        {
            throw new ValidationException("Chọn hóa đơn trước.");
        }
    }

    private void Run(Action action)
    {
        try
        {
            action();
            StatusMessage = "Đã lưu.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, "Quản lý nhà trọ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshRoomFeeFilters()
    {
        IEnumerable<RoomFeeConfig> configs = RoomFeeConfigs;
        if (RoomFeePropertyFilterId > 0)
        {
            configs = configs.Where(x => x.Room?.PropertyId == RoomFeePropertyFilterId);
        }

        if (RoomFeeRoomFilterId > 0)
        {
            configs = configs.Where(x => x.RoomId == RoomFeeRoomFilterId);
        }

        if (RoomFeeFeeTypeFilterId > 0)
        {
            configs = configs.Where(x => x.FeeTypeId == RoomFeeFeeTypeFilterId);
        }

        if (RoomFeeEnabledOnly)
        {
            configs = configs.Where(x => x.Enabled);
        }

        Replace(FilteredRoomFeeConfigs, configs);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var item in values)
        {
            target.Add(item);
        }
    }
}
