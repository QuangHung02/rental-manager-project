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
    private int _dashboardYear = DateTime.Today.Year;
    private string _dashboardRange = "Tùy chọn tháng";
    private int _dashboardPropertyFilterId;
    private string _activeDashboardPeriod = string.Empty;
    private string _statusMessage = "Sẵn sàng";
    private DashboardSummary _dashboard = new();
    private Property? _selectedProperty;
    private Room? _selectedRoom;
    private Tenant? _selectedTenant;
    private FeeType? _selectedFeeType;
    private RoomFeeConfig? _selectedRoomFeeConfig;
    private Invoice? _selectedInvoice;
    private string _roomSearch = string.Empty;
    private string _tenantSearch = string.Empty;
    private string _invoiceSearch = string.Empty;
    private string _paymentSearch = string.Empty;
    private string _roomFeeSearch = string.Empty;
    private int _roomFeePropertyFilterId;
    private int _roomFeeRoomFilterId;
    private int _roomFeeFeeTypeFilterId;
    private bool _roomFeeEnabledOnly = true;

    public MainViewModel()
    {
        DbContextFactory.EnsureDatabase();
        AddPropertyCommand = new RelayCommand(() => Run(AddProperty));
        SavePropertyCommand = new RelayCommand(() => Run(SaveProperty), () => NewProperty.Id > 0);
        EditPropertyCommand = new RelayCommand(() => Run(EditProperty), () => SelectedProperty is not null);
        DeactivatePropertyCommand = new RelayCommand(() => Run(DeactivateProperty), () => SelectedProperty is not null);
        AddRoomCommand = new RelayCommand(() => Run(AddRoom));
        SaveRoomCommand = new RelayCommand(() => Run(SaveRoom), () => NewRoom.Id > 0);
        EditRoomCommand = new RelayCommand(() => Run(EditRoom), () => SelectedRoom is not null);
        DeactivateRoomCommand = new RelayCommand(() => Run(DeactivateRoom), () => SelectedRoom is not null);
        AddTenantCommand = new RelayCommand(() => Run(AddTenant));
        SaveTenantCommand = new RelayCommand(() => Run(SaveTenant), () => NewTenant.Id > 0);
        EditTenantCommand = new RelayCommand(() => Run(EditTenant), () => SelectedTenant is not null);
        AssignTenantCommand = new RelayCommand(() => Run(AssignTenant));
        AddFeeTypeCommand = new RelayCommand(() => Run(AddFeeType));
        SaveFeeTypeCommand = new RelayCommand(() => Run(SaveFeeType), () => NewFeeType.Id > 0);
        EditFeeTypeCommand = new RelayCommand(() => Run(EditFeeType), () => SelectedFeeType is not null);
        DeactivateFeeTypeCommand = new RelayCommand(() => Run(DeactivateFeeType), () => SelectedFeeType is not null);
        AddRoomFeeConfigCommand = new RelayCommand(() => Run(AddRoomFeeConfig));
        SaveRoomFeeConfigCommand = new RelayCommand(() => Run(SaveRoomFeeConfig), () => NewRoomFeeConfig.Id > 0);
        EditRoomFeeConfigCommand = new RelayCommand(() => Run(EditRoomFeeConfig), () => SelectedRoomFeeConfig is not null);
        DisableRoomFeeConfigCommand = new RelayCommand(() => Run(DisableRoomFeeConfig), () => SelectedRoomFeeConfig is not null);
        AddMeterReadingCommand = new RelayCommand(() => Run(AddMeterReading));
        GenerateInvoiceCommand = new RelayCommand(() => Run(GenerateInvoice));
        GenerateAllInvoicesCommand = new RelayCommand(() => Run(GenerateAllInvoices));
        GenerateReadyInvoicesCommand = new RelayCommand(() => Run(GenerateReadyInvoices));
        IssueInvoiceCommand = new RelayCommand(() => Run(IssueInvoice), () => SelectedInvoice is not null);
        RecordPaymentCommand = new RelayCommand(() => Run(RecordPayment), () => SelectedInvoice is not null);
        FillRemainingPaymentCommand = new RelayCommand(FillRemainingPayment, () => SelectedInvoice is not null);
        CopyInvoiceCommand = new RelayCommand(() => Run(CopyInvoice), () => SelectedInvoice is not null);
        CancelInvoiceCommand = new RelayCommand(() => Run(CancelInvoice), () => SelectedInvoice is not null);
        BackupCommand = new RelayCommand(() => Run(Backup));
        RestoreCommand = new RelayCommand(() => Run(Restore));
        SeedDemoDataCommand = new RelayCommand(() => Run(SeedDemoData));
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    public ObservableCollection<Property> Properties { get; } = new();
    public ObservableCollection<PropertyFilterOption> PropertyFilterOptions { get; } = new();
    public ObservableCollection<Room> Rooms { get; } = new();
    public ObservableCollection<Room> FilteredRooms { get; } = new();
    public ObservableCollection<Tenant> Tenants { get; } = new();
    public ObservableCollection<Tenant> FilteredTenants { get; } = new();
    public ObservableCollection<RoomTenant> RoomTenants { get; } = new();
    public ObservableCollection<FeeType> FeeTypes { get; } = new();
    public ObservableCollection<RoomFeeConfig> RoomFeeConfigs { get; } = new();
    public ObservableCollection<RoomFeeConfig> FilteredRoomFeeConfigs { get; } = new();
    public ObservableCollection<MeterReading> MeterReadings { get; } = new();
    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<Invoice> FilteredInvoices { get; } = new();
    public ObservableCollection<Invoice> DashboardInvoices { get; } = new();
    public ObservableCollection<Invoice> DashboardUnpaidInvoices { get; } = new();
    public ObservableCollection<MissingReadingRow> DashboardMissingReadings { get; } = new();
    public ObservableCollection<Payment> DashboardRecentPayments { get; } = new();
    public ObservableCollection<Payment> Payments { get; } = new();
    public ObservableCollection<Payment> FilteredPayments { get; } = new();
    public ObservableCollection<InvoiceReadinessRow> InvoiceReadinessRows { get; } = new();
    public ObservableCollection<InvoiceItem> SelectedInvoiceItems { get; } = new();
    public ObservableCollection<Payment> SelectedInvoicePayments { get; } = new();

    public Property NewProperty { get; set; } = new();
    public Room NewRoom { get; set; } = new();
    public Tenant NewTenant { get; set; } = new();
    public RoomTenant NewRoomTenant { get; set; } = new();
    public FeeType NewFeeType { get; set; } = new();
    public RoomFeeConfig NewRoomFeeConfig { get; set; } = new();
    public MeterReading NewMeterReading { get; set; } = new();
    public int InvoiceRoomId { get; set; }
    public decimal NewPaymentAmount { get; set; }
    public PaymentMethod NewPaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? NewPaymentNote { get; set; }

    public IReadOnlyList<string> DashboardRangeOptions { get; } = new[] { "Tháng hiện tại", "3 tháng gần nhất", "6 tháng gần nhất", "Năm hiện tại", "Tùy chọn tháng" };
    public IReadOnlyList<EnumOption<RoomStatus>> RoomFormStatusOptions { get; } = new[] { new EnumOption<RoomStatus>(RoomStatus.Occupied, "Đang cho thuê"), new EnumOption<RoomStatus>(RoomStatus.Vacant, "Đang trống") };
    public IReadOnlyList<EnumOption<CalculationType>> CalculationTypeOptions { get; } = new[]
    {
        new EnumOption<CalculationType>(CalculationType.Fixed, "Cố định"),
        new EnumOption<CalculationType>(CalculationType.Meter, "Theo chỉ số"),
        new EnumOption<CalculationType>(CalculationType.PerPerson, "Theo người"),
        new EnumOption<CalculationType>(CalculationType.PerUnit, "Theo số lượng"),
        new EnumOption<CalculationType>(CalculationType.Manual, "Nhập tay")
    };
    public IReadOnlyList<EnumOption<PaymentMethod>> PaymentMethodOptions { get; } = new[]
    {
        new EnumOption<PaymentMethod>(PaymentMethod.Cash, "Tiền mặt"),
        new EnumOption<PaymentMethod>(PaymentMethod.BankTransfer, "Chuyển khoản"),
        new EnumOption<PaymentMethod>(PaymentMethod.Momo, "Momo"),
        new EnumOption<PaymentMethod>(PaymentMethod.Other, "Khác")
    };

    public Property? SelectedProperty
    {
        get => _selectedProperty;
        set
        {
            if (SetProperty(ref _selectedProperty, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Room? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (SetProperty(ref _selectedRoom, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Tenant? SelectedTenant
    {
        get => _selectedTenant;
        set
        {
            if (SetProperty(ref _selectedTenant, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public FeeType? SelectedFeeType
    {
        get => _selectedFeeType;
        set
        {
            if (SetProperty(ref _selectedFeeType, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public RoomFeeConfig? SelectedRoomFeeConfig
    {
        get => _selectedRoomFeeConfig;
        set
        {
            if (SetProperty(ref _selectedRoomFeeConfig, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                Replace(SelectedInvoiceItems, value?.Items ?? Enumerable.Empty<InvoiceItem>());
                Replace(SelectedInvoicePayments, value?.Payments ?? Enumerable.Empty<Payment>());
                OnPropertyChanged(nameof(SelectedInvoiceSummary));
                RaiseCommandStates();
            }
        }
    }

    public string RoomSearch
    {
        get => _roomSearch;
        set
        {
            if (SetProperty(ref _roomSearch, value))
            {
                RefreshRoomFilters();
            }
        }
    }

    public string TenantSearch
    {
        get => _tenantSearch;
        set
        {
            if (SetProperty(ref _tenantSearch, value))
            {
                RefreshTenantFilters();
            }
        }
    }

    public string InvoiceSearch
    {
        get => _invoiceSearch;
        set
        {
            if (SetProperty(ref _invoiceSearch, value))
            {
                RefreshInvoiceFilters();
            }
        }
    }

    public string PaymentSearch
    {
        get => _paymentSearch;
        set
        {
            if (SetProperty(ref _paymentSearch, value))
            {
                RefreshPaymentFilters();
            }
        }
    }

    public string RoomFeeSearch
    {
        get => _roomFeeSearch;
        set
        {
            if (SetProperty(ref _roomFeeSearch, value))
            {
                RefreshRoomFeeFilters();
            }
        }
    }

    public int RoomFeePropertyFilterId
    {
        get => _roomFeePropertyFilterId;
        set { if (SetProperty(ref _roomFeePropertyFilterId, value)) RefreshRoomFeeFilters(); }
    }

    public int RoomFeeRoomFilterId
    {
        get => _roomFeeRoomFilterId;
        set { if (SetProperty(ref _roomFeeRoomFilterId, value)) RefreshRoomFeeFilters(); }
    }

    public int RoomFeeFeeTypeFilterId
    {
        get => _roomFeeFeeTypeFilterId;
        set { if (SetProperty(ref _roomFeeFeeTypeFilterId, value)) RefreshRoomFeeFilters(); }
    }

    public bool RoomFeeEnabledOnly
    {
        get => _roomFeeEnabledOnly;
        set { if (SetProperty(ref _roomFeeEnabledOnly, value)) RefreshRoomFeeFilters(); }
    }

    public string BillingMonth
    {
        get => _billingMonth;
        set { if (SetProperty(ref _billingMonth, value)) LoadDashboard(); }
    }

    public int DashboardYear
    {
        get => _dashboardYear;
        set { if (SetProperty(ref _dashboardYear, value)) LoadDashboard(); }
    }

    public string DashboardRange
    {
        get => _dashboardRange;
        set
        {
            if (SetProperty(ref _dashboardRange, value))
            {
                ApplyDashboardRangeDefaults();
                LoadDashboard();
            }
        }
    }

    public int DashboardPropertyFilterId
    {
        get => _dashboardPropertyFilterId;
        set { if (SetProperty(ref _dashboardPropertyFilterId, value)) LoadDashboard(); }
    }

    public string ActiveDashboardPeriod
    {
        get => _activeDashboardPeriod;
        set => SetProperty(ref _activeDashboardPeriod, value);
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
    public string PropertyFormMode => NewProperty.Id > 0 ? $"Đang sửa: {NewProperty.Name}" : "Đang thêm mới";
    public string RoomFormMode => NewRoom.Id > 0 ? $"Đang sửa: {NewRoom.RoomName}" : "Đang thêm mới";
    public string TenantFormMode => NewTenant.Id > 0 ? $"Đang sửa: {NewTenant.FullName}" : "Đang thêm mới";
    public string FeeTypeFormMode => NewFeeType.Id > 0 ? $"Đang sửa: {NewFeeType.DisplayName}" : "Đang thêm mới";
    public string RoomFeeFormMode => NewRoomFeeConfig.Id > 0 ? "Đang sửa cấu hình phí" : "Đang thêm mới";
    public string SelectedInvoiceSummary => SelectedInvoice is null
        ? "Chưa chọn hóa đơn"
        : $"{SelectedInvoice.RoomName} - {SelectedInvoice.RepresentativeTenantName} | Tổng: {SelectedInvoice.TotalAmount:N0} | Đã thu: {SelectedInvoice.PaidAmount:N0} | Còn lại: {SelectedInvoice.RemainingAmount:N0}";

    public RelayCommand AddPropertyCommand { get; }
    public RelayCommand SavePropertyCommand { get; }
    public RelayCommand EditPropertyCommand { get; }
    public RelayCommand DeactivatePropertyCommand { get; }
    public RelayCommand AddRoomCommand { get; }
    public RelayCommand SaveRoomCommand { get; }
    public RelayCommand EditRoomCommand { get; }
    public RelayCommand DeactivateRoomCommand { get; }
    public RelayCommand AddTenantCommand { get; }
    public RelayCommand SaveTenantCommand { get; }
    public RelayCommand EditTenantCommand { get; }
    public RelayCommand AssignTenantCommand { get; }
    public RelayCommand AddFeeTypeCommand { get; }
    public RelayCommand SaveFeeTypeCommand { get; }
    public RelayCommand EditFeeTypeCommand { get; }
    public RelayCommand DeactivateFeeTypeCommand { get; }
    public RelayCommand AddRoomFeeConfigCommand { get; }
    public RelayCommand SaveRoomFeeConfigCommand { get; }
    public RelayCommand EditRoomFeeConfigCommand { get; }
    public RelayCommand DisableRoomFeeConfigCommand { get; }
    public RelayCommand AddMeterReadingCommand { get; }
    public RelayCommand GenerateInvoiceCommand { get; }
    public RelayCommand GenerateAllInvoicesCommand { get; }
    public RelayCommand GenerateReadyInvoicesCommand { get; }
    public RelayCommand IssueInvoiceCommand { get; }
    public RelayCommand RecordPaymentCommand { get; }
    public RelayCommand FillRemainingPaymentCommand { get; }
    public RelayCommand CopyInvoiceCommand { get; }
    public RelayCommand CancelInvoiceCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand SeedDemoDataCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public void Load()
    {
        Replace(Properties, _propertyService.GetAll());
        Replace(PropertyFilterOptions, new[] { new PropertyFilterOption { Id = 0, Name = "Tất cả nhà / khu trọ" } }.Concat(Properties.Select(x => new PropertyFilterOption { Id = x.Id, Name = x.Name })));
        Replace(Rooms, _roomService.GetAll());
        Replace(Tenants, _tenantService.GetAll());
        Replace(RoomTenants, _roomTenantService.GetAll());
        Replace(FeeTypes, _feeTypeService.GetAll());
        Replace(RoomFeeConfigs, _roomFeeConfigService.GetAll());
        Replace(MeterReadings, _meterReadingService.GetAll());
        Replace(Invoices, _invoiceService.GetAll());
        Replace(Payments, _paymentService.GetAll());
        Replace(InvoiceReadinessRows, _invoiceService.GetReadiness(BillingMonth));
        RefreshAllFilters();
        LoadDashboard();
        RaiseCommandStates();
    }

    private void LoadDashboard()
    {
        var (startMonth, endMonth) = GetDashboardRange();
        ActiveDashboardPeriod = startMonth == endMonth ? $"Đang xem: {startMonth}" : $"Đang xem: {startMonth} đến {endMonth}";
        int? propertyId = DashboardPropertyFilterId == 0 ? null : DashboardPropertyFilterId;
        Dashboard = _dashboardService.GetSummary(startMonth, endMonth, propertyId);
        Replace(DashboardInvoices, _dashboardService.GetInvoices(startMonth, endMonth, propertyId));
        Replace(DashboardUnpaidInvoices, _dashboardService.GetUnpaidInvoices(startMonth, endMonth, propertyId));
        Replace(DashboardMissingReadings, _dashboardService.GetMissingReadings(BillingMonth, propertyId));
        Replace(DashboardRecentPayments, _dashboardService.GetRecentPayments(startMonth, endMonth, propertyId));
    }

    private void AddProperty()
    {
        NewProperty.Id = 0;
        _propertyService.Save(NewProperty);
        NewProperty = new Property();
        NotifyFormModes();
        Load();
    }

    private void SaveProperty()
    {
        RequireExisting(NewProperty.Id);
        _propertyService.Save(NewProperty);
        NewProperty = new Property();
        NotifyFormModes();
        Load();
    }

    private void EditProperty()
    {
        if (SelectedProperty is null) throw new ValidationException("Chọn nhà/khu trọ trước.");
        NewProperty = new Property { Id = SelectedProperty.Id, Name = SelectedProperty.Name, Address = SelectedProperty.Address, Note = SelectedProperty.Note, IsActive = SelectedProperty.IsActive, CreatedAt = SelectedProperty.CreatedAt, UpdatedAt = SelectedProperty.UpdatedAt };
        NotifyFormModes();
    }

    private void DeactivateProperty()
    {
        if (SelectedProperty is null) throw new ValidationException("Chọn nhà/khu trọ trước.");
        _propertyService.Deactivate(SelectedProperty.Id);
        Load();
    }

    private void AddRoom()
    {
        NewRoom.Id = 0;
        _roomService.Save(NewRoom);
        NewRoom = new Room();
        NotifyFormModes();
        Load();
    }

    private void SaveRoom()
    {
        RequireExisting(NewRoom.Id);
        _roomService.Save(NewRoom);
        NewRoom = new Room();
        NotifyFormModes();
        Load();
    }

    private void EditRoom()
    {
        if (SelectedRoom is null) throw new ValidationException("Chọn phòng trước.");
        NewRoom = new Room { Id = SelectedRoom.Id, PropertyId = SelectedRoom.PropertyId, RoomName = SelectedRoom.RoomName, Floor = SelectedRoom.Floor, BaseRent = SelectedRoom.BaseRent, Status = SelectedRoom.Status is RoomStatus.Occupied ? RoomStatus.Occupied : RoomStatus.Vacant, Note = SelectedRoom.Note, CreatedAt = SelectedRoom.CreatedAt, UpdatedAt = SelectedRoom.UpdatedAt };
        NotifyFormModes();
    }

    private void DeactivateRoom()
    {
        if (SelectedRoom is null) throw new ValidationException("Chọn phòng trước.");
        _roomService.Deactivate(SelectedRoom.Id);
        Load();
    }

    private void AddTenant()
    {
        NewTenant.Id = 0;
        _tenantService.Save(NewTenant);
        NewTenant = new Tenant();
        NotifyFormModes();
        Load();
    }

    private void SaveTenant()
    {
        RequireExisting(NewTenant.Id);
        _tenantService.Save(NewTenant);
        NewTenant = new Tenant();
        NotifyFormModes();
        Load();
    }

    private void EditTenant()
    {
        if (SelectedTenant is null) throw new ValidationException("Chọn người thuê trước.");
        NewTenant = new Tenant { Id = SelectedTenant.Id, FullName = SelectedTenant.FullName, Phone = SelectedTenant.Phone, Email = SelectedTenant.Email, IdentityNumber = SelectedTenant.IdentityNumber, Note = SelectedTenant.Note, CreatedAt = SelectedTenant.CreatedAt, UpdatedAt = SelectedTenant.UpdatedAt };
        NotifyFormModes();
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
        NewFeeType.Id = 0;
        _feeTypeService.Save(NewFeeType);
        NewFeeType = new FeeType();
        NotifyFormModes();
        Load();
    }

    private void SaveFeeType()
    {
        RequireExisting(NewFeeType.Id);
        _feeTypeService.Save(NewFeeType);
        NewFeeType = new FeeType();
        NotifyFormModes();
        Load();
    }

    private void EditFeeType()
    {
        if (SelectedFeeType is null) throw new ValidationException("Chọn loại phí trước.");
        NewFeeType = new FeeType { Id = SelectedFeeType.Id, Name = SelectedFeeType.Name, DefaultCalculationType = SelectedFeeType.DefaultCalculationType, DefaultUnit = SelectedFeeType.DefaultUnit, DefaultUnitPrice = SelectedFeeType.DefaultUnitPrice, IsSystem = SelectedFeeType.IsSystem, IsActive = SelectedFeeType.IsActive };
        NotifyFormModes();
    }

    private void DeactivateFeeType()
    {
        if (SelectedFeeType is null) throw new ValidationException("Chọn loại phí trước.");
        _feeTypeService.Deactivate(SelectedFeeType.Id);
        Load();
    }

    private void AddRoomFeeConfig()
    {
        NewRoomFeeConfig.Id = 0;
        _roomFeeConfigService.Save(NewRoomFeeConfig);
        NewRoomFeeConfig = new RoomFeeConfig();
        NotifyFormModes();
        Load();
    }

    private void SaveRoomFeeConfig()
    {
        RequireExisting(NewRoomFeeConfig.Id);
        _roomFeeConfigService.Save(NewRoomFeeConfig);
        NewRoomFeeConfig = new RoomFeeConfig();
        NotifyFormModes();
        Load();
    }

    private void EditRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null) throw new ValidationException("Chọn cấu hình phí trước.");
        NewRoomFeeConfig = new RoomFeeConfig { Id = SelectedRoomFeeConfig.Id, RoomId = SelectedRoomFeeConfig.RoomId, FeeTypeId = SelectedRoomFeeConfig.FeeTypeId, CalculationType = SelectedRoomFeeConfig.CalculationType, UnitPrice = SelectedRoomFeeConfig.UnitPrice, FixedAmount = SelectedRoomFeeConfig.FixedAmount, Quantity = SelectedRoomFeeConfig.Quantity, Enabled = SelectedRoomFeeConfig.Enabled, Note = SelectedRoomFeeConfig.Note };
        NotifyFormModes();
    }

    private void DisableRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null) throw new ValidationException("Chọn cấu hình phí trước.");
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
        if (InvoiceRoomId <= 0) throw new ValidationException("Chọn phòng để tạo hóa đơn.");
        _invoiceService.Generate(InvoiceRoomId, BillingMonth);
        Load();
    }

    private void GenerateAllInvoices()
    {
        _invoiceService.GenerateAll(BillingMonth);
        Load();
    }

    private void GenerateReadyInvoices()
    {
        var count = _invoiceService.GenerateReady(BillingMonth);
        StatusMessage = $"Đã tạo {count} hóa đơn đủ dữ liệu.";
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

    private void FillRemainingPayment()
    {
        if (SelectedInvoice is null) return;
        NewPaymentAmount = SelectedInvoice.RemainingAmount;
        OnPropertyChanged(nameof(NewPaymentAmount));
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
        var dialog = new OpenFileDialog { Filter = "SQLite backup (*.sqlite)|*.sqlite|All files (*.*)|*.*", Title = "Chọn bản sao lưu" };
        if (dialog.ShowDialog() == true)
        {
            var confirm = MessageBox.Show("Khôi phục bản sao lưu này? Dữ liệu hiện tại sẽ bị thay thế.", "Khôi phục dữ liệu", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _backupService.RestoreFrom(dialog.FileName);
            Load();
            StatusMessage = "Đã khôi phục dữ liệu.";
        }
    }

    private void SeedDemoData()
    {
        _demoDataService.Seed();
        BillingMonth = "2026-04";
        DashboardYear = 2026;
        DashboardRange = "Tùy chọn tháng";
        Load();
    }

    private void EnsureInvoiceSelected()
    {
        if (SelectedInvoice is null) throw new ValidationException("Chọn hóa đơn trước.");
    }

    private void Run(Action action)
    {
        try
        {
            action();
            if (string.IsNullOrWhiteSpace(StatusMessage)) StatusMessage = "Đã lưu.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, "Quản lý nhà trọ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private (string StartMonth, string EndMonth) GetDashboardRange()
    {
        var selectedMonth = DateTime.TryParse($"{BillingMonth}-01", out var parsedMonth) ? parsedMonth : DateTime.Today;
        return DashboardRange switch
        {
            "Tháng hiện tại" => (DateTime.Today.ToString("yyyy-MM"), DateTime.Today.ToString("yyyy-MM")),
            "3 tháng gần nhất" => (selectedMonth.AddMonths(-2).ToString("yyyy-MM"), selectedMonth.ToString("yyyy-MM")),
            "6 tháng gần nhất" => (selectedMonth.AddMonths(-5).ToString("yyyy-MM"), selectedMonth.ToString("yyyy-MM")),
            "Năm hiện tại" => ($"{DashboardYear:0000}-01", $"{DashboardYear:0000}-12"),
            _ => (BillingMonth, BillingMonth)
        };
    }

    private void ApplyDashboardRangeDefaults()
    {
        if (DashboardRange == "Tháng hiện tại")
        {
            _billingMonth = DateTime.Today.ToString("yyyy-MM");
            _dashboardYear = DateTime.Today.Year;
            OnPropertyChanged(nameof(BillingMonth));
            OnPropertyChanged(nameof(DashboardYear));
        }
        else if (DashboardRange == "Năm hiện tại")
        {
            _dashboardYear = DateTime.Today.Year;
            OnPropertyChanged(nameof(DashboardYear));
        }
    }

    private void RefreshAllFilters()
    {
        RefreshRoomFilters();
        RefreshTenantFilters();
        RefreshRoomFeeFilters();
        RefreshInvoiceFilters();
        RefreshPaymentFilters();
    }

    private void RefreshRoomFilters()
    {
        var text = RoomSearch.Trim();
        var rooms = string.IsNullOrWhiteSpace(text)
            ? Rooms
            : Rooms.Where(x => Contains(x.RoomName, text) || Contains(x.PropertyName, text) || Contains(x.RepresentativeTenantName, text));
        Replace(FilteredRooms, rooms);
    }

    private void RefreshTenantFilters()
    {
        var text = TenantSearch.Trim();
        var tenants = string.IsNullOrWhiteSpace(text)
            ? Tenants
            : Tenants.Where(x => Contains(x.FullName, text) || Contains(x.Phone, text) || Contains(x.Email, text));
        Replace(FilteredTenants, tenants);
    }

    private void RefreshInvoiceFilters()
    {
        var text = InvoiceSearch.Trim();
        var invoices = string.IsNullOrWhiteSpace(text)
            ? Invoices
            : Invoices.Where(x => Contains(x.BillingMonth, text) || Contains(x.RoomName, text) || Contains(x.PropertyName, text) || Contains(x.RepresentativeTenantName, text));
        Replace(FilteredInvoices, invoices);
    }

    private void RefreshPaymentFilters()
    {
        var text = PaymentSearch.Trim();
        var payments = string.IsNullOrWhiteSpace(text)
            ? Payments
            : Payments.Where(x => Contains(x.BillingMonth, text) || Contains(x.RoomName, text) || Contains(x.PropertyName, text) || Contains(x.MethodText, text));
        Replace(FilteredPayments, payments);
    }

    private void RefreshRoomFeeFilters()
    {
        IEnumerable<RoomFeeConfig> configs = RoomFeeConfigs;
        if (RoomFeePropertyFilterId > 0) configs = configs.Where(x => x.Room?.PropertyId == RoomFeePropertyFilterId);
        if (RoomFeeRoomFilterId > 0) configs = configs.Where(x => x.RoomId == RoomFeeRoomFilterId);
        if (RoomFeeFeeTypeFilterId > 0) configs = configs.Where(x => x.FeeTypeId == RoomFeeFeeTypeFilterId);
        if (RoomFeeEnabledOnly) configs = configs.Where(x => x.Enabled);
        if (!string.IsNullOrWhiteSpace(RoomFeeSearch))
        {
            configs = configs.Where(x => Contains(x.PropertyName, RoomFeeSearch) || Contains(x.RoomName, RoomFeeSearch) || Contains(x.FeeTypeName, RoomFeeSearch));
        }
        Replace(FilteredRoomFeeConfigs, configs);
    }

    private void NotifyFormModes()
    {
        OnPropertyChanged(nameof(NewProperty));
        OnPropertyChanged(nameof(NewRoom));
        OnPropertyChanged(nameof(NewTenant));
        OnPropertyChanged(nameof(NewFeeType));
        OnPropertyChanged(nameof(NewRoomFeeConfig));
        OnPropertyChanged(nameof(PropertyFormMode));
        OnPropertyChanged(nameof(RoomFormMode));
        OnPropertyChanged(nameof(TenantFormMode));
        OnPropertyChanged(nameof(FeeTypeFormMode));
        OnPropertyChanged(nameof(RoomFeeFormMode));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        SavePropertyCommand.RaiseCanExecuteChanged();
        EditPropertyCommand.RaiseCanExecuteChanged();
        DeactivatePropertyCommand.RaiseCanExecuteChanged();
        SaveRoomCommand.RaiseCanExecuteChanged();
        EditRoomCommand.RaiseCanExecuteChanged();
        DeactivateRoomCommand.RaiseCanExecuteChanged();
        SaveTenantCommand.RaiseCanExecuteChanged();
        EditTenantCommand.RaiseCanExecuteChanged();
        SaveFeeTypeCommand.RaiseCanExecuteChanged();
        EditFeeTypeCommand.RaiseCanExecuteChanged();
        DeactivateFeeTypeCommand.RaiseCanExecuteChanged();
        SaveRoomFeeConfigCommand.RaiseCanExecuteChanged();
        EditRoomFeeConfigCommand.RaiseCanExecuteChanged();
        DisableRoomFeeConfigCommand.RaiseCanExecuteChanged();
        IssueInvoiceCommand.RaiseCanExecuteChanged();
        RecordPaymentCommand.RaiseCanExecuteChanged();
        FillRemainingPaymentCommand.RaiseCanExecuteChanged();
        CopyInvoiceCommand.RaiseCanExecuteChanged();
        CancelInvoiceCommand.RaiseCanExecuteChanged();
    }

    private static void RequireExisting(int id)
    {
        if (id <= 0) throw new ValidationException("Chọn dòng cần sửa trước khi lưu thay đổi.");
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var item in values) target.Add(item);
    }
}
