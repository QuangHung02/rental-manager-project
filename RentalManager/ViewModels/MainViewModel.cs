using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private DashboardSummary _yearlyDashboard = new();
    private Property? _selectedProperty;
    private Room? _selectedRoom;
    private Tenant? _selectedTenant;
    private FeeType? _selectedFeeType;
    private RoomFeeConfig? _selectedRoomFeeConfig;
    private Invoice? _selectedInvoice;
    private string _roomSearch = string.Empty;
    private string _tenantSearch = string.Empty;
    private string _tenantStatusFilter = "Tất cả";
    private string _assignmentHistoryFilter = "Đã kết thúc";
    private string _invoiceSearch = string.Empty;
    private string _paymentSearch = string.Empty;
    private string _roomFeeSearch = string.Empty;
    private int _selectedBillingMonth = DateTime.Today.Month;
    private int _selectedBillingYear = DateTime.Today.Year;
    private int _roomFilterPropertyId;
    private string _roomFilterStatus = "Tất cả";
    private string _roomRepresentativeSearch = string.Empty;
    private int _invoiceFilterMonth = DateTime.Today.Month;
    private int _invoiceFilterYear = DateTime.Today.Year;
    private int _invoiceFilterPropertyId;
    private int _invoiceFilterRoomId;
    private int _invoiceRoomId;
    private int _invoiceNewPropertyId;
    private string _invoiceFilterStatus = "Tất cả";
    private decimal _newPaymentAmount;
    private int _paymentFilterMonth = DateTime.Today.Month;
    private int _paymentFilterYear = DateTime.Today.Year;
    private int _paymentFilterPropertyId;
    private int _paymentFilterRoomId;
    private string _paymentFilterMethod = "Tất cả";
    private int _meterFilterMonth = DateTime.Today.Month;
    private int _meterFilterYear = DateTime.Today.Year;
    private int _meterFilterPropertyId;
    private int _meterFilterRoomId;
    private int _meterFilterFeeTypeId;
    private int _newMeterReadingPropertyId;
    private string _meterReadingHelpMessage = string.Empty;
    private string _roomFeeStatusFilter = "Tất cả";
    private int _roomFeePropertyFilterId;
    private int _roomFeeRoomFilterId;
    private int _roomFeeFeeTypeFilterId;
    private int _newRoomFeePropertyId;
    private int _assignmentFilterPropertyId;
    private int _assignmentFilterRoomId;
    private string _assignmentRoomSearch = string.Empty;
    private bool _assignmentVacantOnly;
    private int _assignmentNewPropertyId;
    private int _assignmentHistoryPropertyId;
    private int _assignmentHistoryRoomId;
    private string _assignmentHistoryTenantSearch = string.Empty;
    private string _assignmentTenantSearchText = string.Empty;
    private Tenant? _selectedAssignmentTenant;
    private bool _isAssignmentTenantDropdownOpen;
    private bool _isUpdatingAssignmentTenantText;
    private DateTime _assignmentEndDate = DateTime.Today;
    // Bulk room creation
    private int _bulkRoomPropertyId;
    private string _bulkRoomPrefix = "Phòng ";
    private int _bulkRoomStart = 101;
    private int _bulkRoomEnd = 110;
    private decimal _bulkRoomBaseRent;
    // Drawer state
    private bool _isPropertyDrawerOpen;
    private bool _isRoomDrawerOpen;
    private bool _isBulkRoomDrawerOpen;
    private bool _isRoomFeeDrawerOpen;
    private bool _isTenantDrawerOpen;
    private bool _isAssignmentDrawerOpen;
    private bool _isAssignmentHistoryDrawerOpen;
    private bool _isAssignmentRoomDetailDrawerOpen;
    private bool _isTransferRoomDrawerOpen;
    private bool _isInvoiceGenerationDrawerOpen;
    private bool _isPaymentDrawerOpen;
    private Room? _selectedAssignmentRoom;
    private bool _assignmentShowFormerTenants;
    private RoomTenant? _transferAssignment;
    private int _transferPropertyId;
    private int _transferRoomId;
    private DateTime _transferMoveDate = DateTime.Today;
    private bool _transferIsRepresentative;
    private string _invoiceGenerationSummaryText = string.Empty;

    public MainViewModel()
    {
        DbContextFactory.EnsureDatabase();
        AddPropertyCommand = new RelayCommand(() => Run(AddProperty), () => NewProperty.Id == 0);
        SavePropertyCommand = new RelayCommand(() => Run(SaveProperty), () => NewProperty.Id > 0);
        EditPropertyCommand = new RelayCommand(() => Run(EditProperty), () => SelectedProperty is not null);
        DeactivatePropertyCommand = new RelayCommand(() => Run(DeactivateProperty), () => SelectedProperty is not null);
        AddRoomCommand = new RelayCommand(() => Run(AddRoom), () => NewRoom.Id == 0);
        SaveRoomCommand = new RelayCommand(() => Run(SaveRoom), () => NewRoom.Id > 0);
        EditRoomCommand = new RelayCommand(() => Run(EditRoom), () => SelectedRoom is not null);
        DeactivateRoomCommand = new RelayCommand(() => Run(CheckoutRoom), () => SelectedRoom is not null);
        AddTenantCommand = new RelayCommand(() => Run(AddTenant), () => NewTenant.Id == 0);
        SaveTenantCommand = new RelayCommand(() => Run(SaveTenant), () => NewTenant.Id > 0);
        EditTenantCommand = new RelayCommand(() => Run(EditTenant), () => SelectedTenant is not null);
        AssignTenantCommand = new RelayCommand(() => Run(AssignTenant));
        EndAssignmentRowCommand = new RelayCommand<RoomTenant>(assignment => Run(() => EndAssignment(assignment)));
        ChangeRoomRowCommand = new RelayCommand<RoomTenant>(assignment => Run(() => ChangeRoom(assignment)));
        SetRepresentativeRowCommand = new RelayCommand<RoomTenant>(assignment => Run(() => SetRepresentative(assignment)));
        AddFeeTypeCommand = new RelayCommand(() => Run(AddFeeType), () => NewFeeType.Id == 0);
        SaveFeeTypeCommand = new RelayCommand(() => Run(SaveFeeType), () => NewFeeType.Id > 0);
        EditFeeTypeCommand = new RelayCommand(() => Run(EditFeeType), () => SelectedFeeType is not null);
        DeactivateFeeTypeCommand = new RelayCommand(() => Run(DeactivateFeeType), () => SelectedFeeType is not null);
        AddRoomFeeConfigCommand = new RelayCommand(() => Run(AddRoomFeeConfig), () => NewRoomFeeConfig.Id == 0);
        SaveRoomFeeConfigCommand = new RelayCommand(() => Run(SaveRoomFeeConfig), () => NewRoomFeeConfig.Id > 0);
        EditRoomFeeConfigCommand = new RelayCommand(() => Run(EditRoomFeeConfig), () => SelectedRoomFeeConfig is not null);
        DisableRoomFeeConfigCommand = new RelayCommand(() => Run(DisableRoomFeeConfig), () => SelectedRoomFeeConfig is not null);
        AddMeterReadingCommand = new RelayCommand(() => Run(AddMeterReading));
        GenerateInvoiceCommand = new RelayCommand(() => Run(GenerateInvoice));
        GenerateAllInvoicesCommand = new RelayCommand(() => Run(GenerateAllInvoices));
        GenerateReadyInvoicesCommand = new RelayCommand(() => Run(GenerateReadyInvoices));
        OpenInvoiceGenerationDrawerCommand = new RelayCommand(OpenInvoiceGenerationDrawer);
        OpenPaymentDrawerCommand = new RelayCommand(() => Run(() => OpenPaymentDrawer(SelectedInvoice)), () => CanPayInvoice(SelectedInvoice));
        IssueInvoiceCommand = new RelayCommand(() => Run(IssueInvoice), () => SelectedInvoice is not null);
        RecordPaymentCommand = new RelayCommand(() => Run(RecordPayment), () => CanPayInvoice(SelectedInvoice));
        FillRemainingPaymentCommand = new RelayCommand(FillRemainingPayment, () => CanPayInvoice(SelectedInvoice));
        CopyInvoiceCommand = new RelayCommand(() => Run(CopyInvoice), () => SelectedInvoice is not null);
        CancelInvoiceCommand = new RelayCommand(() => Run(CancelInvoice), () => SelectedInvoice is not null);
        BackupCommand = new RelayCommand(() => Run(Backup));
        RestoreCommand = new RelayCommand(() => Run(Restore));
        SeedDemoDataCommand = new RelayCommand(() => Run(SeedDemoData));
        OpenDocsCommand = new RelayCommand(OpenDocs);
        ApplyFiltersCommand = new RelayCommand(RefreshAllFilters);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        ClearAssignmentFiltersCommand = new RelayCommand(ClearAssignmentFilters);
        OpenAssignmentHistoryDrawerCommand = new RelayCommand(OpenAssignmentHistoryDrawer);
        OpenAssignmentHistoryForRowCommand = new RelayCommand<RoomTenant>(OpenAssignmentHistoryForRow);
        OpenAssignmentHistoryForRoomCommand = new RelayCommand<Room>(OpenAssignmentHistoryForRoom);
        OpenAssignmentDrawerForRoomCommand = new RelayCommand<Room>(room => Run(() => OpenAssignmentDrawer(room)));
        OpenAssignmentRoomDetailCommand = new RelayCommand<Room>(room => Run(() => OpenAssignmentRoomDetail(room)));
        SelectAssignmentPropertyCommand = new RelayCommand<Property>(SelectAssignmentProperty);
        ShowAllAssignmentPropertiesCommand = new RelayCommand(ShowAllAssignmentProperties);
        EndSelectedAssignmentRoomCommand = new RelayCommand(() => Run(EndSelectedAssignmentRoom));
        ConfirmTransferRoomCommand = new RelayCommand(() => Run(ConfirmTransferRoom));
        CloseTransferRoomDrawerCommand = new RelayCommand(CloseTransferRoomDrawer);
        ClearAssignmentHistoryFiltersCommand = new RelayCommand(ClearAssignmentHistoryFilters);
        SelectAssignmentTenantCommand = new RelayCommand<Tenant>(SelectAssignmentTenant);
        AddRoomsRangeCommand = new RelayCommand(() => Run(AddRoomsRange));
        OpenAddPropertyDrawerCommand = new RelayCommand(() => { CancelPropertyEdit(); IsPropertyDrawerOpen = true; });
        OpenAddRoomDrawerCommand    = new RelayCommand(() => { CancelRoomEdit();     IsRoomDrawerOpen     = true; });
        OpenBulkRoomDrawerCommand   = new RelayCommand(() => { IsBulkRoomDrawerOpen  = true; });
        CloseDrawerCommand          = new RelayCommand(CloseAllDrawers);
        OpenAddTenantDrawerCommand  = new RelayCommand(() => { CancelTenantEdit(); IsTenantDrawerOpen = true; });
        OpenEditTenantDrawerCommand = new RelayCommand<Tenant>(t => Run(() => { SelectedTenant = t; EditTenant(); IsTenantDrawerOpen = true; }));
        OpenAssignmentDrawerCommand = new RelayCommand(() => Run(OpenAssignmentDrawer));
        OpenEditPropertyDrawerCommand = new RelayCommand<Property>(p => Run(() => { SelectedProperty = p; EditProperty(); IsPropertyDrawerOpen = true; }));
        OpenEditRoomDrawerCommand     = new RelayCommand<Room>(r => Run(() => { SelectedRoom = r; EditRoom(); IsRoomDrawerOpen = true; }));
        OpenRoomFeeDrawerCommand      = new RelayCommand<Room>(r => { SelectedRoom = r; NewRoomFeePropertyId = r?.PropertyId ?? 0; NewRoomFeeConfig.RoomId = r?.Id ?? 0; OnPropertyChanged(nameof(NewRoomFeeConfig)); IsRoomFeeDrawerOpen = true; });
        CancelPropertyEditCommand = new RelayCommand(CancelPropertyEdit, () => NewProperty.Id > 0);
        CancelRoomEditCommand = new RelayCommand(CancelRoomEdit, () => NewRoom.Id > 0);
        CancelTenantEditCommand = new RelayCommand(CancelTenantEdit, () => NewTenant.Id > 0);
        CancelFeeTypeEditCommand = new RelayCommand(CancelFeeTypeEdit, () => NewFeeType.Id > 0);
        CancelRoomFeeConfigEditCommand = new RelayCommand(CancelRoomFeeConfigEdit, () => NewRoomFeeConfig.Id > 0);
        EditPropertyRowCommand = new RelayCommand<Property>(property => Run(() => { SelectedProperty = property; EditProperty(); }));
        DeactivatePropertyRowCommand = new RelayCommand<Property>(property => Run(() => { SelectedProperty = property; DeactivateProperty(); }));
        EditRoomRowCommand = new RelayCommand<Room>(room => Run(() => { SelectedRoom = room; EditRoom(); }));
        DeactivateRoomRowCommand = new RelayCommand<Room>(room => Run(() => { SelectedRoom = room; CheckoutRoom(); }));
        EditTenantRowCommand = new RelayCommand<Tenant>(tenant => Run(() => { SelectedTenant = tenant; EditTenant(); }));
        EditFeeTypeRowCommand = new RelayCommand<FeeType>(feeType => Run(() => { SelectedFeeType = feeType; EditFeeType(); }));
        DeactivateFeeTypeRowCommand = new RelayCommand<FeeType>(feeType => Run(() => { SelectedFeeType = feeType; DeactivateFeeType(); }));
        EditRoomFeeRowCommand = new RelayCommand<RoomFeeConfig>(config => Run(() => { SelectedRoomFeeConfig = config; EditRoomFeeConfig(); }));
        DisableRoomFeeRowCommand = new RelayCommand<RoomFeeConfig>(config => Run(() => { SelectedRoomFeeConfig = config; DisableRoomFeeConfig(); }));
        SelectInvoiceRowCommand = new RelayCommand<Invoice>(invoice => { SelectedInvoice = invoice; });
        PayInvoiceRowCommand = new RelayCommand<Invoice>(invoice => Run(() => OpenPaymentDrawer(invoice)));
        IssueInvoiceRowCommand = new RelayCommand<Invoice>(invoice => Run(() => { SelectedInvoice = invoice; IssueInvoice(); }));
        CopyInvoiceRowCommand = new RelayCommand<Invoice>(invoice => Run(() => { SelectedInvoice = invoice; CopyInvoice(); }));
        CancelInvoiceRowCommand = new RelayCommand<Invoice>(invoice => Run(() => { SelectedInvoice = invoice; CancelInvoice(); }));
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    public ObservableCollection<Property> Properties { get; } = new();
    public ObservableCollection<PropertyFilterOption> PropertyFilterOptions { get; } = new();
    public ObservableCollection<Room> Rooms { get; } = new();
    public ObservableCollection<Room> FilteredRooms { get; } = new();
    public ObservableCollection<Tenant> Tenants { get; } = new();
    public ObservableCollection<Tenant> FilteredTenants { get; } = new();
    public ObservableCollection<Tenant> RentingTenants { get; } = new();
    public ObservableCollection<Tenant> UnassignedTenants { get; } = new();
    public ObservableCollection<Tenant> FormerTenants { get; } = new();
    public ObservableCollection<Tenant> AssignableTenants { get; } = new();
    public ObservableCollection<RoomTenant> RoomTenants { get; } = new();
    public ObservableCollection<RoomTenant> ActiveRoomTenants { get; } = new();
    public ObservableCollection<RoomTenant> FilteredAssignmentHistory { get; } = new();
    public ObservableCollection<Room> AssignmentRoomOptions { get; } = new();
    public ObservableCollection<Room> AssignmentFilterRoomOptions { get; } = new();
    public ObservableCollection<Room> FilteredAssignmentRooms { get; } = new();
    public ObservableCollection<Room> AssignmentHistoryRoomOptions { get; } = new();
    public ObservableCollection<RoomTenant> SelectedAssignmentRoomTenants { get; } = new();
    public ObservableCollection<Room> TransferRoomOptions { get; } = new();
    public ObservableCollection<Tenant> AssignmentTenantOptions { get; } = new();
    public ObservableCollection<FeeType> FeeTypes { get; } = new();
    public ObservableCollection<FeeType> MeterReadingFeeTypeOptions { get; } = new();
    public ObservableCollection<Room> MeterReadingRoomOptions { get; } = new();
    public ObservableCollection<RoomFeeConfig> RoomFeeConfigs { get; } = new();
    public ObservableCollection<RoomFeeConfig> FilteredRoomFeeConfigs { get; } = new();
    public ObservableCollection<Room> RoomFeeFilterRoomOptions { get; } = new();
    public ObservableCollection<Room> RoomFeeFormRoomOptions { get; } = new();
    public ObservableCollection<Room> MeterFilterRoomOptions { get; } = new();
    public ObservableCollection<Room> WorkspaceRooms { get; } = new();
    public ObservableCollection<RoomFeeConfig> WorkspaceRoomFees { get; } = new();
    public ObservableCollection<MeterReading> MeterReadings { get; } = new();
    public ObservableCollection<MeterReading> FilteredMeterReadings { get; } = new();
    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<Invoice> FilteredInvoices { get; } = new();
    public ObservableCollection<Room> InvoiceFilterRoomOptions { get; } = new();
    public ObservableCollection<Room> InvoiceFormRoomOptions { get; } = new();
    public ObservableCollection<Invoice> DashboardInvoices { get; } = new();
    public ObservableCollection<Invoice> DashboardUnpaidInvoices { get; } = new();
    public ObservableCollection<MissingReadingRow> DashboardMissingReadings { get; } = new();
    public ObservableCollection<Payment> DashboardRecentPayments { get; } = new();
    public ObservableCollection<DashboardMonthlySummary> DashboardMonthlySummaries { get; } = new();
    public ObservableCollection<Payment> Payments { get; } = new();
    public ObservableCollection<Payment> FilteredPayments { get; } = new();
    public ObservableCollection<InvoiceReadinessRow> InvoiceReadinessRows { get; } = new();
    public ObservableCollection<InvoiceGenerationSkipRow> InvoiceGenerationSkippedRooms { get; } = new();
    public ObservableCollection<InvoiceItem> SelectedInvoiceItems { get; } = new();
    public ObservableCollection<Payment> SelectedInvoicePayments { get; } = new();

    public Property NewProperty { get; set; } = new();
    public Room NewRoom { get; set; } = new();
    public Tenant NewTenant { get; set; } = new();
    public RoomTenant NewRoomTenant { get; set; } = new();
    public FeeType NewFeeType { get; set; } = new();
    public RoomFeeConfig NewRoomFeeConfig { get; set; } = new();
    public MeterReading NewMeterReading { get; set; } = new();
    public int InvoiceRoomId
    {
        get => _invoiceRoomId;
        set => SetProperty(ref _invoiceRoomId, value);
    }
    public decimal NewPaymentAmount
    {
        get => _newPaymentAmount;
        set => SetProperty(ref _newPaymentAmount, value);
    }
    public PaymentMethod NewPaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? NewPaymentNote { get; set; }
    public string InvoiceGenerationSummaryText
    {
        get => _invoiceGenerationSummaryText;
        set => SetProperty(ref _invoiceGenerationSummaryText, value);
    }

    public IReadOnlyList<string> DashboardRangeOptions { get; } = new[] { "Tháng hiện tại", "3 tháng gần nhất", "6 tháng gần nhất", "Năm hiện tại", "Tùy chọn tháng" };
    public IReadOnlyList<int> MonthOptions { get; } = Enumerable.Range(1, 12).ToList();
    public ObservableCollection<int> YearOptions { get; } = new();
    public IReadOnlyList<string> RoomStatusFilterOptions { get; } = new[] { "Tất cả", "Đang cho thuê", "Đang trống" };
    public IReadOnlyList<string> TenantStatusFilterOptions { get; } = new[] { "Tất cả", "Đang thuê", "Chưa phân phòng", "Đã rời" };
    public IReadOnlyList<string> AssignmentHistoryFilterOptions { get; } = new[] { "Đã kết thúc", "Đang thuê", "Tất cả" };
    public IReadOnlyList<string> InvoiceStatusFilterOptions { get; } = new[] { "Tất cả", "Nháp", "Đã chốt", "Thanh toán một phần", "Đã trả", "Đã hủy" };
    public IReadOnlyList<string> PaymentMethodFilterOptions { get; } = new[] { "Tất cả", "Tiền mặt", "Chuyển khoản", "Momo", "Khác" };
    public IReadOnlyList<string> RoomFeeStatusFilterOptions { get; } = new[] { "Tất cả", "Đang áp dụng", "Ngừng áp dụng" };
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

    public bool HasAssignableTenants => AssignableTenants.Count > 0;
    public bool HasNoAssignableTenants => !HasAssignableTenants;
    public string UnassignedTenantCountText => $"Người chưa phân phòng: {UnassignedTenants.Count}";

    public Property? SelectedProperty
    {
        get => _selectedProperty;
        set
        {
            if (SetProperty(ref _selectedProperty, value))
            {
                RefreshWorkspaceRooms();
                OnPropertyChanged(nameof(WorkspacePropertyHeader));
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
                RefreshWorkspaceRoomFees();
                OnPropertyChanged(nameof(WorkspaceRoomHeader));
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
                OnPropertyChanged(nameof(SelectedFeeTypeToggleActionText));
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
                OnPropertyChanged(nameof(SelectedRoomFeeToggleActionText));
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
                Replace(SelectedInvoiceItems, value?.Items.Select(ToDisplayInvoiceItem) ?? Enumerable.Empty<InvoiceItem>());
                Replace(SelectedInvoicePayments, value?.Payments ?? Enumerable.Empty<Payment>());
                if (value is null || value.RemainingAmount <= 0)
                {
                    NewPaymentAmount = 0;
                }
                else
                {
                    NewPaymentAmount = value.RemainingAmount;
                    NewPaymentMethod = PaymentMethod.Cash;
                    OnPropertyChanged(nameof(NewPaymentMethod));
                }

                OnPropertyChanged(nameof(SelectedInvoiceSummary));
                OnPropertyChanged(nameof(SelectedInvoiceRoomText));
                OnPropertyChanged(nameof(SelectedInvoiceRepresentativeText));
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

    public string TenantStatusFilter
    {
        get => _tenantStatusFilter;
        set
        {
            if (SetProperty(ref _tenantStatusFilter, value))
            {
                RefreshTenantFilters();
            }
        }
    }

    public int AssignmentFilterPropertyId
    {
        get => _assignmentFilterPropertyId;
        set
        {
            if (SetProperty(ref _assignmentFilterPropertyId, value))
            {
                RefreshAssignmentFilterRoomOptions();
                RefreshAssignmentFilters();
            }
        }
    }

    public int AssignmentFilterRoomId
    {
        get => _assignmentFilterRoomId;
        set
        {
            if (SetProperty(ref _assignmentFilterRoomId, value))
            {
                RefreshAssignmentFilters();
                NewRoomTenant.RoomId = value;
                OnPropertyChanged(nameof(NewRoomTenant));
                OnPropertyChanged(nameof(SelectedAssignmentRoomText));
            }
        }
    }

    public string AssignmentRoomSearch
    {
        get => _assignmentRoomSearch;
        set
        {
            if (SetProperty(ref _assignmentRoomSearch, value))
            {
                RefreshAssignmentFilterRoomOptions();
            }
        }
    }

    public bool AssignmentVacantOnly
    {
        get => _assignmentVacantOnly;
        set
        {
            if (SetProperty(ref _assignmentVacantOnly, value))
            {
                RefreshAssignmentFilterRoomOptions();
            }
        }
    }

    public DateTime? AssignmentStartDate
    {
        get => NewRoomTenant.StartDate;
        set
        {
            NewRoomTenant.StartDate = value ?? DateTime.Today;
            OnPropertyChanged(nameof(AssignmentStartDate));
            OnPropertyChanged(nameof(NewRoomTenant));
        }
    }

    public int AssignmentRoomId
    {
        get => NewRoomTenant.RoomId;
        set
        {
            if (NewRoomTenant.RoomId == value)
            {
                return;
            }

            NewRoomTenant.RoomId = value;
            NewRoomTenant.IsRepresentative = value <= 0 || !RoomTenants.Any(x => x.RoomId == value && x.Status == RoomTenantStatus.Active && x.IsRepresentative);
            OnPropertyChanged(nameof(AssignmentRoomId));
            OnPropertyChanged(nameof(NewRoomTenant));
            OnPropertyChanged(nameof(SelectedAssignmentRoomText));
        }
    }

    public int AssignmentNewPropertyId
    {
        get => _assignmentNewPropertyId;
        set
        {
            if (SetProperty(ref _assignmentNewPropertyId, value))
            {
                RefreshAssignmentRoomOptions();
            }
        }
    }

    public string AssignmentTenantSearchText
    {
        get => _assignmentTenantSearchText;
        set
        {
            if (SetProperty(ref _assignmentTenantSearchText, value))
            {
                if (_isUpdatingAssignmentTenantText)
                {
                    return;
                }

                ClearAssignmentTenantSelectionIfTextNoLongerMatches();
                RefreshAssignmentTenantOptions();
                IsAssignmentTenantDropdownOpen = IsAssignmentDrawerOpen;
            }
        }
    }

    public Tenant? SelectedAssignmentTenant
    {
        get => _selectedAssignmentTenant;
        set
        {
            if (!SetProperty(ref _selectedAssignmentTenant, value))
            {
                return;
            }

            NewRoomTenant.TenantId = value?.Id ?? 0;
            OnPropertyChanged(nameof(NewRoomTenant));

        }
    }

    public string AssignmentHistoryFilter
    {
        get => _assignmentHistoryFilter;
        set
        {
            if (SetProperty(ref _assignmentHistoryFilter, value))
            {
                RefreshAssignmentFilters();
            }
        }
    }

    public int AssignmentHistoryPropertyId
    {
        get => _assignmentHistoryPropertyId;
        set
        {
            if (SetProperty(ref _assignmentHistoryPropertyId, value))
            {
                RefreshAssignmentHistoryRoomOptions();
                RefreshAssignmentHistoryFilters();
            }
        }
    }

    public int AssignmentHistoryRoomId
    {
        get => _assignmentHistoryRoomId;
        set
        {
            if (SetProperty(ref _assignmentHistoryRoomId, value))
            {
                RefreshAssignmentHistoryFilters();
            }
        }
    }

    public string AssignmentHistoryTenantSearch
    {
        get => _assignmentHistoryTenantSearch;
        set
        {
            if (SetProperty(ref _assignmentHistoryTenantSearch, value))
            {
                RefreshAssignmentHistoryFilters();
            }
        }
    }

    public string SelectedAssignmentRoomText
    {
        get
        {
            var room = Rooms.FirstOrDefault(x => x.Id == NewRoomTenant.RoomId);
            return room is null
                ? "Chưa chọn phòng"
                : $"{room.PropertyName} - {room.RoomName}";
        }
    }

    public string AssignmentRoomDetailTitle => SelectedAssignmentRoom is null
        ? "Chi tiết phòng"
        : $"Chi tiết phòng {SelectedAssignmentRoom.RoomName} — {SelectedAssignmentRoom.PropertyName}";

    public bool HasSelectedAssignmentRoomTenants => SelectedAssignmentRoomTenants.Count > 0;
    public bool HasNoSelectedAssignmentRoomTenants => !HasSelectedAssignmentRoomTenants;

    public string TransferTenantName => _transferAssignment?.TenantName ?? string.Empty;
    public string TransferCurrentRoomText => _transferAssignment is null
        ? string.Empty
        : $"{_transferAssignment.PropertyName} - {_transferAssignment.RoomName}";

    public int TransferPropertyId
    {
        get => _transferPropertyId;
        set
        {
            if (SetProperty(ref _transferPropertyId, value))
            {
                RefreshTransferRoomOptions();
            }
        }
    }

    public int TransferRoomId
    {
        get => _transferRoomId;
        set => SetProperty(ref _transferRoomId, value);
    }

    public DateTime? TransferMoveDate
    {
        get => _transferMoveDate;
        set => SetProperty(ref _transferMoveDate, value ?? DateTime.Today);
    }

    public bool TransferIsRepresentative
    {
        get => _transferIsRepresentative;
        set => SetProperty(ref _transferIsRepresentative, value);
    }

    public Room? SelectedAssignmentRoom
    {
        get => _selectedAssignmentRoom;
        set
        {
            if (SetProperty(ref _selectedAssignmentRoom, value))
            {
                OnPropertyChanged(nameof(AssignmentRoomDetailTitle));
            }
        }
    }

    public DateTime AssignmentEndDate
    {
        get => _assignmentEndDate;
        set => SetProperty(ref _assignmentEndDate, value);
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
        set
        {
            if (SetProperty(ref _roomFeePropertyFilterId, value))
            {
                RefreshRoomFeeFilterRoomOptions();
                RefreshRoomFeeFilters();
            }
        }
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

    public int NewRoomFeeFeeTypeId
    {
        get => NewRoomFeeConfig.FeeTypeId;
        set
        {
            if (NewRoomFeeConfig.FeeTypeId == value)
            {
                return;
            }

            NewRoomFeeConfig.FeeTypeId = value;
            var feeType = FeeTypes.FirstOrDefault(x => x.Id == value);
            if (feeType is not null)
            {
                NewRoomFeeConfig.CalculationType = feeType.DefaultCalculationType;
                ClearIrrelevantRoomFeePriceFields();
                ClearRoomFeeCustomPrice();
                OnPropertyChanged(nameof(NewRoomFeeCalculationType));
                RaiseRoomFeeFieldVisibility();
            }

            OnPropertyChanged(nameof(NewRoomFeeFeeTypeId));
            OnPropertyChanged(nameof(NewRoomFeeConfig));
            OnPropertyChanged(nameof(RoomFeeFormMode));
            RaiseRoomFeePricingState();
        }
    }

    public string BillingMonth
    {
        get => _billingMonth;
        set
        {
            if (SetProperty(ref _billingMonth, value))
            {
                SyncPickerFromBillingMonth();
                LoadDashboard();
            }
        }
    }

    public int SelectedBillingMonth
    {
        get => _selectedBillingMonth;
        set
        {
            if (SetProperty(ref _selectedBillingMonth, value))
            {
                SyncBillingMonthFromPicker();
            }
        }
    }

    public int SelectedBillingYear
    {
        get => _selectedBillingYear;
        set
        {
            if (SetProperty(ref _selectedBillingYear, value))
            {
                SyncBillingMonthFromPicker();
            }
        }
    }

    public int RoomFilterPropertyId { get => _roomFilterPropertyId; set { if (SetProperty(ref _roomFilterPropertyId, value)) RefreshRoomFilters(); } }
    public string RoomFilterStatus { get => _roomFilterStatus; set { if (SetProperty(ref _roomFilterStatus, value)) RefreshRoomFilters(); } }
    public string RoomRepresentativeSearch { get => _roomRepresentativeSearch; set { if (SetProperty(ref _roomRepresentativeSearch, value)) RefreshRoomFilters(); } }

    public int InvoiceFilterMonth { get => _invoiceFilterMonth; set { if (SetProperty(ref _invoiceFilterMonth, value)) RefreshInvoiceFilters(); } }
    public int InvoiceFilterYear { get => _invoiceFilterYear; set { if (SetProperty(ref _invoiceFilterYear, value)) RefreshInvoiceFilters(); } }
    public int InvoiceFilterPropertyId
    {
        get => _invoiceFilterPropertyId;
        set
        {
            if (SetProperty(ref _invoiceFilterPropertyId, value))
            {
                RefreshInvoiceFilterRoomOptions();
                RefreshInvoiceFilters();
            }
        }
    }
    public int InvoiceFilterRoomId { get => _invoiceFilterRoomId; set { if (SetProperty(ref _invoiceFilterRoomId, value)) RefreshInvoiceFilters(); } }
    public int InvoiceNewPropertyId
    {
        get => _invoiceNewPropertyId;
        set
        {
            if (SetProperty(ref _invoiceNewPropertyId, value))
            {
                RefreshInvoiceFormRoomOptions();
            }
        }
    }
    public string InvoiceFilterStatus { get => _invoiceFilterStatus; set { if (SetProperty(ref _invoiceFilterStatus, value)) RefreshInvoiceFilters(); } }

    public int PaymentFilterMonth { get => _paymentFilterMonth; set { if (SetProperty(ref _paymentFilterMonth, value)) RefreshPaymentFilters(); } }
    public int PaymentFilterYear { get => _paymentFilterYear; set { if (SetProperty(ref _paymentFilterYear, value)) RefreshPaymentFilters(); } }
    public int PaymentFilterPropertyId { get => _paymentFilterPropertyId; set { if (SetProperty(ref _paymentFilterPropertyId, value)) RefreshPaymentFilters(); } }
    public int PaymentFilterRoomId { get => _paymentFilterRoomId; set { if (SetProperty(ref _paymentFilterRoomId, value)) RefreshPaymentFilters(); } }
    public string PaymentFilterMethod { get => _paymentFilterMethod; set { if (SetProperty(ref _paymentFilterMethod, value)) RefreshPaymentFilters(); } }

    public int MeterFilterMonth { get => _meterFilterMonth; set { if (SetProperty(ref _meterFilterMonth, value)) { RefreshMeterReadingFilters(); LoadMeterReadingFormForSelection(); } } }
    public int MeterFilterYear { get => _meterFilterYear; set { if (SetProperty(ref _meterFilterYear, value)) { RefreshMeterReadingFilters(); LoadMeterReadingFormForSelection(); } } }
    public int MeterFilterPropertyId { get => _meterFilterPropertyId; set { if (SetProperty(ref _meterFilterPropertyId, value)) { RefreshMeterFilterRoomOptions(); RefreshMeterReadingFilters(); } } }
    public int MeterFilterRoomId { get => _meterFilterRoomId; set { if (SetProperty(ref _meterFilterRoomId, value)) RefreshMeterReadingFilters(); } }
    public int MeterFilterFeeTypeId { get => _meterFilterFeeTypeId; set { if (SetProperty(ref _meterFilterFeeTypeId, value)) RefreshMeterReadingFilters(); } }

    public int NewMeterReadingPropertyId
    {
        get => _newMeterReadingPropertyId;
        set
        {
            if (SetProperty(ref _newMeterReadingPropertyId, value))
            {
                RefreshMeterReadingRoomOptions();
            }
        }
    }

    public int NewMeterReadingRoomId
    {
        get => NewMeterReading.RoomId;
        set
        {
            if (NewMeterReading.RoomId == value)
            {
                return;
            }

            NewMeterReading.RoomId = value;
            OnPropertyChanged(nameof(NewMeterReadingRoomId));
            RefreshMeterReadingFeeTypeOptions();
            LoadMeterReadingFormForSelection();
        }
    }

    public int NewMeterReadingFeeTypeId
    {
        get => NewMeterReading.FeeTypeId;
        set
        {
            if (NewMeterReading.FeeTypeId == value)
            {
                return;
            }

            NewMeterReading.FeeTypeId = value;
            OnPropertyChanged(nameof(NewMeterReadingFeeTypeId));
            LoadMeterReadingFormForSelection();
        }
    }

    public string MeterReadingHelpMessage
    {
        get => _meterReadingHelpMessage;
        private set => SetProperty(ref _meterReadingHelpMessage, value);
    }

    public CalculationType NewRoomFeeCalculationType
    {
        get => NewRoomFeeConfig.CalculationType;
        set
        {
            if (NewRoomFeeConfig.CalculationType == value)
            {
                return;
            }

            var wasUsingDefault = NewRoomFeeUseDefaultPrice;
            NewRoomFeeConfig.CalculationType = value;
            ClearIrrelevantRoomFeePriceFields();
            if (wasUsingDefault || !RoomFeeCalculationMatchesFeeTypeDefault())
            {
                ClearRoomFeeCustomPrice();
            }
            else
            {
                FillRoomFeeCustomPriceFromDefault();
            }

            OnPropertyChanged(nameof(NewRoomFeeCalculationType));
            OnPropertyChanged(nameof(RoomFeeFormMode));
            OnPropertyChanged(nameof(NewRoomFeeConfig));
            RaiseRoomFeeFieldVisibility();
            RaiseRoomFeePricingState();
        }
    }

    public Visibility RoomFeeUnitPriceVisibility =>
        NewRoomFeeConfig.CalculationType is CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility RoomFeeFixedAmountVisibility =>
        NewRoomFeeConfig.CalculationType is CalculationType.Fixed or CalculationType.Manual
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility RoomFeeQuantityVisibility =>
        NewRoomFeeConfig.CalculationType == CalculationType.PerUnit
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility RoomFeeDefaultPriceVisibility =>
        NewRoomFeeConfig.CalculationType == CalculationType.Manual
            ? Visibility.Collapsed
            : Visibility.Visible;

    public bool NewRoomFeeUseDefaultPrice
    {
        get => NewRoomFeeConfig.CalculationType != CalculationType.Manual &&
               RoomFeeCalculationMatchesFeeTypeDefault() &&
               GetRoomFeeCustomPrice() is null;
        set
        {
            if (NewRoomFeeConfig.CalculationType == CalculationType.Manual)
            {
                return;
            }

            if (value)
            {
                if (!RoomFeeCalculationMatchesFeeTypeDefault())
                {
                    ClearRoomFeeCustomPrice();
                    OnPropertyChanged(nameof(NewRoomFeeConfig));
                    RaiseRoomFeePricingState();
                    return;
                }

                ClearRoomFeeCustomPrice();
            }
            else
            {
                FillRoomFeeCustomPriceFromDefault();
            }

            OnPropertyChanged(nameof(NewRoomFeeConfig));
            RaiseRoomFeePricingState();
        }
    }

    public bool RoomFeeCustomPriceInputEnabled => NewRoomFeeConfig.CalculationType == CalculationType.Manual || !NewRoomFeeUseDefaultPrice;

    public bool RoomFeeDefaultPriceInputEnabled => RoomFeeCalculationMatchesFeeTypeDefault();

    public int NewRoomFeePropertyId
    {
        get => _newRoomFeePropertyId;
        set
        {
            if (SetProperty(ref _newRoomFeePropertyId, value))
            {
                RefreshRoomFeeFormRoomOptions();
            }
        }
    }

    public string RoomFeeStatusFilter { get => _roomFeeStatusFilter; set { if (SetProperty(ref _roomFeeStatusFilter, value)) RefreshRoomFeeFilters(); } }

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

    public DashboardSummary YearlyDashboard
    {
        get => _yearlyDashboard;
        set => SetProperty(ref _yearlyDashboard, value);
    }

    public string DatabasePath => _backupService.DatabasePath;

    public string CliPath
    {
        get
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(basePath, "..\\..\\..\\..\\..\\RentalManager.Cli\\bin\\Debug\\net8.0-windows\\RentalManager.Cli.exe"));
        }
    }
    
    public string ResolvedCliPath
    {
        get
        {
            var path1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RentalManager.Cli.exe");
            if (File.Exists(path1)) return path1;
            
            var path2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\RentalManager.Cli\\bin\\Debug\\net8.0-windows\\RentalManager.Cli.exe"));
            return path2;
        }
    }

    public bool IsCliAvailable => File.Exists(ResolvedCliPath);
    public string CliStatusText => IsCliAvailable ? "Đã cài đặt (Sẵn sàng)" : "Không tìm thấy file thực thi";
    public string CliStatusColor => IsCliAvailable ? "Green" : "Red";
    public string PropertyFormMode => NewProperty.Id > 0 ? $"Đang sửa: {NewProperty.Name}" : "Đang thêm mới";
    public string RoomFormMode => NewRoom.Id > 0 ? $"Đang sửa: {NewRoom.RoomName}" : "Đang thêm mới";
    public string TenantFormMode => NewTenant.Id > 0 ? $"Đang sửa: {NewTenant.FullName}" : "Đang thêm mới";
    public string FeeTypeFormMode => NewFeeType.Id > 0 ? $"Đang sửa: {NewFeeType.DisplayName}" : "Đang thêm mới";
    public string SelectedFeeTypeToggleActionText => SelectedFeeType?.ToggleActionText ?? "Ngừng";
    public string RoomFeeFormMode => NewRoomFeeConfig.Id > 0 ? $"Đang sửa: {RoomFeeEditTitle}" : "Thêm khoản phí cho phòng";
    public string SelectedRoomFeeToggleActionText => SelectedRoomFeeConfig?.ToggleActionText ?? "Ngừng";
    public string WorkspacePropertyHeader => SelectedProperty is null
        ? "Chọn nhà / khu trọ bên trái"
        : $"{SelectedProperty.Name} — {WorkspaceRooms.Count} phòng";
    public string WorkspaceRoomHeader => SelectedRoom is null
        ? "Chọn phòng để xem khoản phí"
        : $"Khoản phí — {SelectedRoom.RoomName}";
    private string RoomFeeEditTitle
    {
        get
        {
            var room = Rooms.FirstOrDefault(x => x.Id == NewRoomFeeConfig.RoomId);
            var feeType = FeeTypes.FirstOrDefault(x => x.Id == NewRoomFeeConfig.FeeTypeId);
            var roomText = room?.DisplayNameWithProperty ?? "Phòng";
            var feeText = feeType?.DisplayName ?? "Loại phí";
            return $"{roomText} - {feeText}";
        }
    }
    public string SelectedInvoiceSummary => SelectedInvoice is null
        ? "Chưa chọn hóa đơn"
        : $"{SelectedInvoice.RoomName} - {SelectedInvoice.RepresentativeTenantName} | Tổng: {SelectedInvoice.TotalAmount:N0} | Đã thu: {SelectedInvoice.PaidAmount:N0} | Còn lại: {SelectedInvoice.RemainingAmount:N0}";

    public string SelectedInvoiceRoomText => SelectedInvoice is null
        ? string.Empty
        : $"{SelectedInvoice.PropertyName} - {SelectedInvoice.RoomName}";

    public string SelectedInvoiceRepresentativeText => SelectedInvoice?.RepresentativeTenantName ?? string.Empty;

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
    public RelayCommand<RoomTenant> EndAssignmentRowCommand { get; }
    public RelayCommand<RoomTenant> ChangeRoomRowCommand { get; }
    public RelayCommand<RoomTenant> SetRepresentativeRowCommand { get; }
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
    public RelayCommand OpenInvoiceGenerationDrawerCommand { get; }
    public RelayCommand IssueInvoiceCommand { get; }
    public RelayCommand RecordPaymentCommand { get; }
    public RelayCommand FillRemainingPaymentCommand { get; }
    public RelayCommand OpenPaymentDrawerCommand { get; }
    public RelayCommand CopyInvoiceCommand { get; }
    public RelayCommand CancelInvoiceCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand SeedDemoDataCommand { get; }
    public RelayCommand ApplyFiltersCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }
    public RelayCommand ClearAssignmentFiltersCommand { get; }
    public RelayCommand OpenAssignmentHistoryDrawerCommand { get; }
    public RelayCommand<RoomTenant> OpenAssignmentHistoryForRowCommand { get; }
    public RelayCommand<Room> OpenAssignmentHistoryForRoomCommand { get; }
    public RelayCommand<Room> OpenAssignmentDrawerForRoomCommand { get; }
    public RelayCommand<Room> OpenAssignmentRoomDetailCommand { get; }
    public RelayCommand<Property> SelectAssignmentPropertyCommand { get; }
    public RelayCommand ShowAllAssignmentPropertiesCommand { get; }
    public RelayCommand EndSelectedAssignmentRoomCommand { get; }
    public RelayCommand ConfirmTransferRoomCommand { get; }
    public RelayCommand CloseTransferRoomDrawerCommand { get; }
    public RelayCommand ClearAssignmentHistoryFiltersCommand { get; }
    public RelayCommand<Tenant> SelectAssignmentTenantCommand { get; }
    public RelayCommand AddRoomsRangeCommand { get; }
    // Drawer commands
    public RelayCommand OpenAddPropertyDrawerCommand  { get; }
    public RelayCommand OpenAddRoomDrawerCommand      { get; }
    public RelayCommand OpenBulkRoomDrawerCommand     { get; }
    public RelayCommand CloseDrawerCommand            { get; }
    public RelayCommand OpenAssignmentDrawerCommand   { get; }
    public RelayCommand<Property> OpenEditPropertyDrawerCommand { get; }
    public RelayCommand<Room>     OpenEditRoomDrawerCommand     { get; }
    public RelayCommand<Room>     OpenRoomFeeDrawerCommand      { get; }
    // Drawer state properties
    public bool IsPropertyDrawerOpen  { get => _isPropertyDrawerOpen;  set { if (SetProperty(ref _isPropertyDrawerOpen,  value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsRoomDrawerOpen      { get => _isRoomDrawerOpen;      set { if (SetProperty(ref _isRoomDrawerOpen,      value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsBulkRoomDrawerOpen  { get => _isBulkRoomDrawerOpen;  set { if (SetProperty(ref _isBulkRoomDrawerOpen,  value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsRoomFeeDrawerOpen   { get => _isRoomFeeDrawerOpen;   set { if (SetProperty(ref _isRoomFeeDrawerOpen,   value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsTenantDrawerOpen    { get => _isTenantDrawerOpen;    set { if (SetProperty(ref _isTenantDrawerOpen,    value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsAssignmentDrawerOpen { get => _isAssignmentDrawerOpen; set { if (SetProperty(ref _isAssignmentDrawerOpen, value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsAssignmentHistoryDrawerOpen { get => _isAssignmentHistoryDrawerOpen; set { if (SetProperty(ref _isAssignmentHistoryDrawerOpen, value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsAssignmentRoomDetailDrawerOpen { get => _isAssignmentRoomDetailDrawerOpen; set { if (SetProperty(ref _isAssignmentRoomDetailDrawerOpen, value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsTransferRoomDrawerOpen { get => _isTransferRoomDrawerOpen; set { if (SetProperty(ref _isTransferRoomDrawerOpen, value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsInvoiceGenerationDrawerOpen { get => _isInvoiceGenerationDrawerOpen; set { if (SetProperty(ref _isInvoiceGenerationDrawerOpen, value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsPaymentDrawerOpen { get => _isPaymentDrawerOpen; set { if (SetProperty(ref _isPaymentDrawerOpen, value)) OnPropertyChanged(nameof(IsDrawerOpen)); } }
    public bool IsDrawerOpen => IsPropertyDrawerOpen || IsRoomDrawerOpen || IsBulkRoomDrawerOpen || IsRoomFeeDrawerOpen || IsTenantDrawerOpen || IsAssignmentDrawerOpen || IsAssignmentHistoryDrawerOpen || IsAssignmentRoomDetailDrawerOpen || IsTransferRoomDrawerOpen || IsInvoiceGenerationDrawerOpen || IsPaymentDrawerOpen;

    public bool IsAssignmentTenantDropdownOpen
    {
        get => _isAssignmentTenantDropdownOpen;
        set => SetProperty(ref _isAssignmentTenantDropdownOpen, value);
    }

    public bool AssignmentShowFormerTenants
    {
        get => _assignmentShowFormerTenants;
        set
        {
            if (SetProperty(ref _assignmentShowFormerTenants, value))
            {
                RefreshTenantStatusCollections();
                if (SelectedAssignmentTenant is not null && AssignableTenants.All(x => x.Id != SelectedAssignmentTenant.Id))
                {
                    SelectedAssignmentTenant = null;
                    SetAssignmentTenantSearchText(string.Empty);
                }

                RefreshAssignmentTenantOptions();
            }
        }
    }

    public RelayCommand CancelPropertyEditCommand { get; }
    public RelayCommand CancelRoomEditCommand { get; }
    public RelayCommand CancelTenantEditCommand { get; }
    public RelayCommand CancelFeeTypeEditCommand { get; }
    public RelayCommand CancelRoomFeeConfigEditCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand OpenDocsCommand { get; }
    public RelayCommand<Property> EditPropertyRowCommand { get; }
    public RelayCommand<Property> DeactivatePropertyRowCommand { get; }
    public RelayCommand<Room> EditRoomRowCommand { get; }
    public RelayCommand<Room> DeactivateRoomRowCommand { get; }
    public RelayCommand<Tenant> EditTenantRowCommand { get; }
    public RelayCommand<FeeType> EditFeeTypeRowCommand { get; }
    public RelayCommand<FeeType> DeactivateFeeTypeRowCommand { get; }
    public RelayCommand<RoomFeeConfig> EditRoomFeeRowCommand { get; }
    public RelayCommand<RoomFeeConfig> DisableRoomFeeRowCommand { get; }
    public RelayCommand<Invoice> SelectInvoiceRowCommand { get; }
    public RelayCommand<Invoice> PayInvoiceRowCommand { get; }
    public RelayCommand<Invoice> IssueInvoiceRowCommand { get; }
    public RelayCommand<Invoice> CopyInvoiceRowCommand { get; }
    public RelayCommand<Invoice> CancelInvoiceRowCommand { get; }
    public RelayCommand OpenAddTenantDrawerCommand { get; }
    public RelayCommand<Tenant> OpenEditTenantDrawerCommand { get; }

    public void Load()
    {
        ResetModule2SelectionsBeforeReload();
        Replace(Properties, _propertyService.GetAll());
        Replace(PropertyFilterOptions, new[] { new PropertyFilterOption { Id = 0, Name = "Tất cả nhà / khu trọ" } }.Concat(Properties.Select(x => new PropertyFilterOption { Id = x.Id, Name = x.Name })));
        Replace(Rooms, _roomService.GetAll());
        RefreshRoomFeeFilterRoomOptions();
        RefreshRoomFeeFormRoomOptions();
        RefreshMeterReadingRoomOptions();
        RefreshMeterFilterRoomOptions();
        RefreshWorkspaceRooms();
        RefreshInvoiceFilterRoomOptions();
        RefreshInvoiceFormRoomOptions();
        RefreshAssignmentFilterRoomOptions();
        RefreshAssignmentHistoryRoomOptions();
        RefreshAssignmentRoomOptions();
        _tenantService.SyncStatusesFromAssignments();
        Replace(Tenants, _tenantService.GetAll());
        Replace(RoomTenants, _roomTenantService.GetAll());
        RefreshAssignmentFilterRoomOptions();
        RefreshTenantStatusCollections();
        RefreshAssignmentTenantOptions();
        Replace(FeeTypes, _feeTypeService.GetAll());
        Replace(RoomFeeConfigs, _roomFeeConfigService.GetAll());
        Replace(MeterReadings, _meterReadingService.GetAll());
        RefreshMeterReadingFeeTypeOptions();
        Replace(Invoices, _invoiceService.GetAll());
        Replace(Payments, _paymentService.GetAll());
        UpdateYearOptions();
        Replace(InvoiceReadinessRows, _invoiceService.GetReadiness(BillingMonth));
        RefreshAllFilters();
        LoadDashboard();
        RaiseCommandStates();
    }

    private void ResetModule2SelectionsBeforeReload()
    {
        _assignmentFilterPropertyId = 0;
        _assignmentFilterRoomId = 0;
        _assignmentRoomSearch = string.Empty;
        _assignmentVacantOnly = false;
        _assignmentNewPropertyId = 0;
        _assignmentHistoryPropertyId = 0;
        _assignmentHistoryRoomId = 0;
        _assignmentHistoryTenantSearch = string.Empty;
        _assignmentTenantSearchText = string.Empty;
        _selectedAssignmentTenant = null;
        _isAssignmentTenantDropdownOpen = false;
        _assignmentShowFormerTenants = false;
        _assignmentHistoryFilter = "Đã kết thúc";
        NewRoomTenant = new RoomTenant();
        IsAssignmentDrawerOpen = false;
        IsAssignmentHistoryDrawerOpen = false;
        IsTransferRoomDrawerOpen = false;
        IsInvoiceGenerationDrawerOpen = false;
        IsPaymentDrawerOpen = false;
        _transferAssignment = null;
        _transferPropertyId = 0;
        _transferRoomId = 0;
        _transferMoveDate = DateTime.Today;
        _transferIsRepresentative = false;

        OnPropertyChanged(nameof(AssignmentFilterPropertyId));
        OnPropertyChanged(nameof(AssignmentFilterRoomId));
        OnPropertyChanged(nameof(AssignmentRoomSearch));
        OnPropertyChanged(nameof(AssignmentVacantOnly));
        OnPropertyChanged(nameof(AssignmentNewPropertyId));
        OnPropertyChanged(nameof(AssignmentHistoryPropertyId));
        OnPropertyChanged(nameof(AssignmentHistoryRoomId));
        OnPropertyChanged(nameof(AssignmentHistoryTenantSearch));
        OnPropertyChanged(nameof(AssignmentTenantSearchText));
        OnPropertyChanged(nameof(SelectedAssignmentTenant));
        OnPropertyChanged(nameof(IsAssignmentTenantDropdownOpen));
        OnPropertyChanged(nameof(AssignmentShowFormerTenants));
        OnPropertyChanged(nameof(AssignmentHistoryFilter));
        OnPropertyChanged(nameof(NewRoomTenant));
        OnPropertyChanged(nameof(AssignmentStartDate));
        OnPropertyChanged(nameof(AssignmentRoomId));
        OnPropertyChanged(nameof(SelectedAssignmentRoomText));
        OnPropertyChanged(nameof(TransferTenantName));
        OnPropertyChanged(nameof(TransferCurrentRoomText));
        OnPropertyChanged(nameof(TransferPropertyId));
        OnPropertyChanged(nameof(TransferRoomId));
        OnPropertyChanged(nameof(TransferMoveDate));
        OnPropertyChanged(nameof(TransferIsRepresentative));
    }

    private void LoadDashboard()
    {
        var (startMonth, endMonth) = GetDashboardRange();
        ActiveDashboardPeriod = startMonth == endMonth ? $"Đang xem: {startMonth}" : $"Đang xem: {startMonth} đến {endMonth}";
        int? propertyId = DashboardPropertyFilterId == 0 ? null : DashboardPropertyFilterId;
        Dashboard = _dashboardService.GetSummary(startMonth, endMonth, propertyId);
        YearlyDashboard = _dashboardService.GetSummary($"{DashboardYear:0000}-01", $"{DashboardYear:0000}-12", propertyId);
        Replace(DashboardInvoices, _dashboardService.GetInvoices(startMonth, endMonth, propertyId));
        Replace(DashboardUnpaidInvoices, _dashboardService.GetUnpaidInvoices(startMonth, endMonth, propertyId));
        Replace(DashboardMissingReadings, _dashboardService.GetMissingReadings(BillingMonth, propertyId));
        Replace(DashboardRecentPayments, _dashboardService.GetRecentPayments(startMonth, endMonth, propertyId));
        Replace(DashboardMonthlySummaries, _dashboardService.GetMonthlySummaries(DashboardYear, propertyId));
    }

    private void AddProperty()
    {
        NewProperty.Id = 0;
        _propertyService.Save(NewProperty);
        NewProperty = new Property();
        NotifyFormModes();
        CloseAllDrawers();
        Load();
    }

    private void SaveProperty()
    {
        RequireExisting(NewProperty.Id);
        _propertyService.Save(NewProperty);
        NewProperty = new Property();
        NotifyFormModes();
        CloseAllDrawers();
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

    private void OpenDocs()
    {
        var docsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\docs\\CLI_USAGE.md"));
        if (File.Exists(docsPath))
        {
            Process.Start(new ProcessStartInfo { FileName = docsPath, UseShellExecute = true });
        }
        else
        {
            MessageBox.Show("Không tìm thấy file tài liệu CLI_USAGE.md", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CancelPropertyEdit()
    {
        NewProperty = new Property();
        NotifyFormModes();
    }

    private void AddRoom()
    {
        if (NewRoom.Id == 0 && !string.IsNullOrWhiteSpace(NewRoom.RoomName))
        {
            var names = NewRoom.RoomName.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(n => n.Trim())
                                        .Where(n => !string.IsNullOrEmpty(n))
                                        .Distinct()
                                        .Take(100) // Max 100 rooms per batch
                                        .ToList();

            if (names.Count > 0)
            {
                int created = 0;
                int skipped = 0;
                var existingRooms = _roomService.GetAll()
                    .Where(r => r.PropertyId == NewRoom.PropertyId)
                    .Select(r => r.RoomName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var name in names)
                {
                    if (existingRooms.Contains(name))
                    {
                        skipped++;
                        continue;
                    }
                    var room = new Room
                    {
                        PropertyId = NewRoom.PropertyId,
                        RoomName = name,
                        Floor = NewRoom.Floor,
                        BaseRent = NewRoom.BaseRent,
                        Status = NewRoom.Status,
                        Note = NewRoom.Note
                    };
                    _roomService.Save(room);
                    created++;
                }

                if (names.Count > 1 || skipped > 0)
                {
                    System.Windows.MessageBox.Show($"Đã tạo {created} phòng. Bỏ qua {skipped} phòng (trùng tên).", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }
        else
        {
            // Edit mode or empty name fallback
            _roomService.Save(NewRoom);
        }

        NewRoom = new Room();
        NotifyFormModes();
        CloseAllDrawers();
        Load();
    }

    private void SaveRoom()
    {
        RequireExisting(NewRoom.Id);
        _roomService.Save(NewRoom);
        NewRoom = new Room();
        NotifyFormModes();
        CloseAllDrawers();
        Load();
    }

    private void EditRoom()
    {
        if (SelectedRoom is null) throw new ValidationException("Chọn phòng trước.");
        NewRoom = new Room { Id = SelectedRoom.Id, PropertyId = SelectedRoom.PropertyId, RoomName = SelectedRoom.RoomName, Floor = SelectedRoom.Floor, BaseRent = SelectedRoom.BaseRent, Status = SelectedRoom.Status is RoomStatus.Occupied ? RoomStatus.Occupied : RoomStatus.Vacant, Note = SelectedRoom.Note, CreatedAt = SelectedRoom.CreatedAt, UpdatedAt = SelectedRoom.UpdatedAt };
        NotifyFormModes();
    }

    private void CheckoutRoom()
    {
        if (SelectedRoom is null) throw new ValidationException("Chọn phòng trước.");
        var confirm = MessageBox.Show(
            "Bạn có chắc muốn trả phòng này không? Tất cả người thuê đang ở phòng này sẽ được chuyển sang trạng thái đã ngừng thuê.",
            "Trả phòng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var endedCount = _roomService.Checkout(SelectedRoom.Id);
        Load();
        StatusMessage = endedCount > 0
            ? "Đã trả phòng. Phòng hiện đang trống."
            : "Phòng đã được chuyển sang trạng thái đang trống.";
    }

    private void CancelRoomEdit()
    {
        NewRoom = new Room();
        NotifyFormModes();
    }

    // ── Bulk room creation ──────────────────────────────────────────────────
    public int BulkRoomPropertyId
    {
        get => _bulkRoomPropertyId;
        set => SetProperty(ref _bulkRoomPropertyId, value);
    }
    public string BulkRoomPrefix
    {
        get => _bulkRoomPrefix;
        set => SetProperty(ref _bulkRoomPrefix, value);
    }
    public int BulkRoomStart
    {
        get => _bulkRoomStart;
        set => SetProperty(ref _bulkRoomStart, value);
    }
    public int BulkRoomEnd
    {
        get => _bulkRoomEnd;
        set => SetProperty(ref _bulkRoomEnd, value);
    }
    public decimal BulkRoomBaseRent
    {
        get => _bulkRoomBaseRent;
        set => SetProperty(ref _bulkRoomBaseRent, value);
    }

    private void AddRoomsRange()
    {
        if (BulkRoomPropertyId <= 0)
            throw new ValidationException("Vui lòng chọn nhà / khu trọ.");
        if (BulkRoomStart > BulkRoomEnd)
            throw new ValidationException("Số bắt đầu phải nhỏ hơn hoặc bằng số kết thúc.");
        int total = BulkRoomEnd - BulkRoomStart + 1;
        if (total > 100)
            throw new ValidationException("Mỗi lần chỉ có thể tạo tối đa 100 phòng.");

        int created = 0, skipped = 0;
        var errors = new List<string>();
        for (int n = BulkRoomStart; n <= BulkRoomEnd; n++)
        {
            var name = $"{BulkRoomPrefix}{n}".Trim();
            var room = new Room
            {
                PropertyId = BulkRoomPropertyId,
                RoomName   = name,
                BaseRent   = BulkRoomBaseRent,
                Status     = Enums.RoomStatus.Vacant
            };
            try
            {
                _roomService.Save(room);
                created++;
            }
            catch (ValidationException ex) when (ex.Message.Contains("đã tồn tại"))
            {
                skipped++;
            }
            catch (ValidationException ex)
            {
                errors.Add($"{name}: {ex.Message}");
            }
        }

        Load();

        var parts = new List<string>();
        if (created > 0)  parts.Add($"Đã tạo {created} phòng.");
        if (skipped > 0)  parts.Add($"Bỏ qua {skipped} phòng đã tồn tại.");
        if (errors.Count > 0) parts.Add($"Lỗi: {string.Join("; ", errors)}");
        StatusMessage = string.Join(" ", parts);
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
        NewTenant = new Tenant { Id = SelectedTenant.Id, FullName = SelectedTenant.FullName, Phone = SelectedTenant.Phone, Email = SelectedTenant.Email, IdentityNumber = SelectedTenant.IdentityNumber, Status = SelectedTenant.Status, Note = SelectedTenant.Note, CreatedAt = SelectedTenant.CreatedAt, UpdatedAt = SelectedTenant.UpdatedAt };
        NotifyFormModes();
    }

    private void CancelTenantEdit()
    {
        NewTenant = new Tenant();
        NotifyFormModes();
    }

    private void AssignTenant()
    {
        if (AssignmentNewPropertyId <= 0)
        {
            throw new ValidationException("Vui lòng chọn nhà / khu trọ.");
        }

        if (NewRoomTenant.RoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        var selectedRoom = Rooms.FirstOrDefault(x => x.Id == NewRoomTenant.RoomId);
        if (selectedRoom is null || selectedRoom.PropertyId != AssignmentNewPropertyId)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        if (SelectedAssignmentTenant is null ||
            NewRoomTenant.TenantId <= 0 ||
            SelectedAssignmentTenant.Id != NewRoomTenant.TenantId)
        {
            throw new ValidationException("Vui lòng chọn người thuê hợp lệ.");
        }

        _roomTenantService.Save(NewRoomTenant);
        NewRoomTenant = new RoomTenant();
        SelectedAssignmentTenant = null;
        SetAssignmentTenantSearchText(string.Empty);
        RefreshAssignmentRoomOptions();
        OnPropertyChanged(nameof(NewRoomTenant));
        OnPropertyChanged(nameof(AssignmentStartDate));
        CloseAllDrawers();
        Load();
    }

    private void EndAssignment(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chọn lượt thuê trước.");
        var roomId = assignment.RoomId;
        var detailRoomId = IsAssignmentRoomDetailDrawerOpen ? SelectedAssignmentRoom?.Id : null;
        _roomTenantService.EndAssignment(assignment.Id, AssignmentEndDate);
        ReloadAfterAssignmentChange(detailRoomId);
        StatusMessage = RoomNeedsRepresentative(roomId)
            ? "Đã kết thúc thuê. Phòng này chưa có người đại diện."
            : "Đã kết thúc thuê.";
    }

    private void ChangeRoom(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chọn lượt thuê trước.");
        _transferAssignment = assignment;
        _transferMoveDate = DateTime.Today;
        _transferIsRepresentative = false;
        _transferPropertyId = assignment.Room?.PropertyId
            ?? Rooms.FirstOrDefault(x => x.Id == assignment.RoomId)?.PropertyId
            ?? 0;
        _transferRoomId = 0;

        OnPropertyChanged(nameof(TransferTenantName));
        OnPropertyChanged(nameof(TransferCurrentRoomText));
        OnPropertyChanged(nameof(TransferMoveDate));
        OnPropertyChanged(nameof(TransferIsRepresentative));
        OnPropertyChanged(nameof(TransferPropertyId));
        OnPropertyChanged(nameof(TransferRoomId));

        RefreshTransferRoomOptions();
        IsTransferRoomDrawerOpen = true;
    }

    private void SetRepresentative(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chọn lượt thuê trước.");
        var detailRoomId = IsAssignmentRoomDetailDrawerOpen ? SelectedAssignmentRoom?.Id : null;
        _roomTenantService.SetRepresentative(assignment.Id);
        StatusMessage = "Đã đặt người đại diện.";
        ReloadAfterAssignmentChange(detailRoomId);
    }

    private void EndSelectedAssignmentRoom()
    {
        if (SelectedAssignmentRoom is null)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        var roomId = SelectedAssignmentRoom.Id;
        var activeAssignments = RoomTenants
            .Where(x => x.RoomId == roomId && x.Status == RoomTenantStatus.Active)
            .ToList();

        if (activeAssignments.Count == 0)
        {
            throw new ValidationException("Phòng này chưa có người thuê.");
        }

        var confirm = MessageBox.Show(
            "Bạn có chắc muốn kết thúc thuê cho tất cả người thuê trong phòng này không?",
            "Kết thúc thuê cả phòng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var assignment in activeAssignments)
        {
            _roomTenantService.EndAssignment(assignment.Id, DateTime.Today);
        }

        ReloadAfterAssignmentChange(roomId);
        StatusMessage = "Đã kết thúc thuê cho tất cả người thuê trong phòng.";
    }

    private void ConfirmTransferRoom()
    {
        if (_transferAssignment is null)
        {
            throw new ValidationException("Chọn lượt thuê trước.");
        }

        if (TransferPropertyId <= 0)
        {
            throw new ValidationException("Vui lòng chọn nhà / khu trọ.");
        }

        if (TransferRoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        if (TransferRoomId == _transferAssignment.RoomId)
        {
            throw new ValidationException("Phòng mới phải khác phòng hiện tại.");
        }

        var detailRoomId = IsAssignmentRoomDetailDrawerOpen ? SelectedAssignmentRoom?.Id : null;
        _roomTenantService.ChangeRoom(_transferAssignment.Id, TransferRoomId, TransferMoveDate, TransferIsRepresentative);
        IsTransferRoomDrawerOpen = false;
        _transferAssignment = null;
        ReloadAfterAssignmentChange(detailRoomId);
        StatusMessage = "Đã chuyển phòng thành công.";
    }

    private void CloseTransferRoomDrawer()
    {
        IsTransferRoomDrawerOpen = false;
        _transferAssignment = null;
    }

    private void ReloadAfterAssignmentChange(int? detailRoomId)
    {
        var propertyId = AssignmentFilterPropertyId;
        var roomSearch = AssignmentRoomSearch;
        var vacantOnly = AssignmentVacantOnly;
        var shouldKeepDetailOpen = detailRoomId.HasValue && IsAssignmentRoomDetailDrawerOpen;

        Load();

        AssignmentFilterPropertyId = propertyId;
        AssignmentRoomSearch = roomSearch;
        AssignmentVacantOnly = vacantOnly;

        if (shouldKeepDetailOpen)
        {
            RefreshSelectedAssignmentRoomDetail(detailRoomId.GetValueOrDefault());
        }
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
        _feeTypeService.ToggleActive(SelectedFeeType.Id);
        Load();
    }

    private void CancelFeeTypeEdit()
    {
        NewFeeType = new FeeType();
        NotifyFormModes();
    }

    private void AddRoomFeeConfig()
    {
        ValidateRoomFeePropertySelection();
        NewRoomFeeConfig.Id = 0;
        _roomFeeConfigService.Save(NewRoomFeeConfig);
        NewRoomFeeConfig = new RoomFeeConfig();
        NewRoomFeePropertyId = 0;
        NotifyFormModes();
        Load();
    }

    private void SaveRoomFeeConfig()
    {
        RequireExisting(NewRoomFeeConfig.Id);
        ValidateRoomFeePropertySelection();
        _roomFeeConfigService.Save(NewRoomFeeConfig);
        NewRoomFeeConfig = new RoomFeeConfig();
        NewRoomFeePropertyId = 0;
        NotifyFormModes();
        Load();
    }

    private void EditRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null) throw new ValidationException("Chọn cấu hình phí trước.");
        NewRoomFeeConfig = new RoomFeeConfig { Id = SelectedRoomFeeConfig.Id, RoomId = SelectedRoomFeeConfig.RoomId, FeeTypeId = SelectedRoomFeeConfig.FeeTypeId, CalculationType = SelectedRoomFeeConfig.CalculationType, UnitPrice = SelectedRoomFeeConfig.UnitPrice, FixedAmount = SelectedRoomFeeConfig.FixedAmount, Quantity = SelectedRoomFeeConfig.Quantity, Enabled = SelectedRoomFeeConfig.Enabled, Note = SelectedRoomFeeConfig.Note };
        NewRoomFeePropertyId = SelectedRoomFeeConfig.Room?.PropertyId ?? Rooms.FirstOrDefault(x => x.Id == SelectedRoomFeeConfig.RoomId)?.PropertyId ?? 0;
        NotifyFormModes();
    }

    private void DisableRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null) throw new ValidationException("Chọn cấu hình phí trước.");
        _roomFeeConfigService.ToggleActive(SelectedRoomFeeConfig.Id);
        Load();
    }

    private void ValidateRoomFeePropertySelection()
    {
        if (NewRoomFeePropertyId <= 0)
        {
            throw new ValidationException("Vui lòng chọn nhà / khu trọ.");
        }

        if (NewRoomFeeConfig.RoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        var room = Rooms.FirstOrDefault(x => x.Id == NewRoomFeeConfig.RoomId);
        if (room is null || room.PropertyId != NewRoomFeePropertyId)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }
    }

    private void CancelRoomFeeConfigEdit()
    {
        NewRoomFeeConfig = new RoomFeeConfig();
        NewRoomFeePropertyId = 0;
        NotifyFormModes();
    }

    private void AddMeterReading()
    {
        if (NewMeterReading.RoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        if (NewMeterReadingPropertyId <= 0)
        {
            throw new ValidationException("Vui lòng chọn nhà / khu trọ.");
        }

        var room = Rooms.FirstOrDefault(x => x.Id == NewMeterReading.RoomId);
        if (room is null || room.PropertyId != NewMeterReadingPropertyId)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        if (NewMeterReading.FeeTypeId <= 0 || MeterReadingFeeTypeOptions.All(x => x.Id != NewMeterReading.FeeTypeId))
        {
            throw new ValidationException("Vui lòng chọn loại phí theo chỉ số đang áp dụng cho phòng.");
        }

        NewMeterReading.BillingMonth = MeterReadingBillingMonth;
        _meterReadingService.Save(NewMeterReading);
        NewMeterReading = new MeterReading();
        NewMeterReadingPropertyId = 0;
        OnPropertyChanged(nameof(NewMeterReading));
        OnPropertyChanged(nameof(NewMeterReadingPropertyId));
        OnPropertyChanged(nameof(NewMeterReadingRoomId));
        OnPropertyChanged(nameof(NewMeterReadingFeeTypeId));
        RefreshMeterReadingRoomOptions();
        RefreshMeterReadingFeeTypeOptions();
        Load();
    }

    private void GenerateInvoice()
    {
        if (InvoiceNewPropertyId <= 0) throw new ValidationException("Vui lòng chọn nhà / khu trọ.");
        if (InvoiceRoomId <= 0) throw new ValidationException("Chọn phòng để tạo hóa đơn.");
        var room = Rooms.FirstOrDefault(x => x.Id == InvoiceRoomId);
        if (room is null || room.PropertyId != InvoiceNewPropertyId)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        _invoiceService.Generate(InvoiceRoomId, BillingMonth);
        Load();
        IsInvoiceGenerationDrawerOpen = true;
        RefreshInvoiceGenerationReadiness();
        InvoiceGenerationSummaryText = "Đã tạo hóa đơn cho phòng đã chọn.";
    }

    private void GenerateAllInvoices()
    {
        var result = _invoiceService.GenerateAllEligible(BillingMonth);
        Load();
        IsInvoiceGenerationDrawerOpen = true;
        RefreshInvoiceGenerationReadiness();
        InvoiceGenerationSummaryText = result.SummaryText;
        Replace(InvoiceGenerationSkippedRooms, result.SkippedRooms);
    }

    private void GenerateReadyInvoices()
    {
        GenerateAllInvoices();
    }

    private void OpenInvoiceGenerationDrawer()
    {
        RefreshInvoiceGenerationReadiness();
        InvoiceGenerationSummaryText = string.Empty;
        Replace(InvoiceGenerationSkippedRooms, Array.Empty<InvoiceGenerationSkipRow>());
        IsInvoiceGenerationDrawerOpen = true;
    }

    private void RefreshInvoiceGenerationReadiness()
    {
        Replace(InvoiceReadinessRows, _invoiceService.GetReadiness(BillingMonth));
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
        if (!CanPayInvoice(SelectedInvoice))
        {
            throw new ValidationException("Hóa đơn này không thể ghi nhận thanh toán.");
        }

        if (NewPaymentAmount <= 0)
        {
            throw new ValidationException("Vui lòng nhập số tiền thanh toán.");
        }

        var invoiceId = SelectedInvoice!.Id;
        _paymentService.Record(invoiceId, NewPaymentAmount, NewPaymentMethod, DateTime.Today, NewPaymentNote);
        NewPaymentNote = null;
        OnPropertyChanged(nameof(NewPaymentNote));
        Load();
        SelectedInvoice = null;
        SelectedInvoice = Invoices.FirstOrDefault(x => x.Id == invoiceId);
        IsPaymentDrawerOpen = false;
        StatusMessage = "Đã ghi nhận thanh toán.";
    }

    private void FillRemainingPayment()
    {
        if (SelectedInvoice is null) return;
        if (!CanPayInvoice(SelectedInvoice))
        {
            StatusMessage = "Hóa đơn này không thể ghi nhận thanh toán.";
            return;
        }

        NewPaymentAmount = SelectedInvoice.RemainingAmount;
        NewPaymentMethod = PaymentMethod.Cash;
        OnPropertyChanged(nameof(NewPaymentMethod));
    }

    private void SelectInvoiceForPayment(Invoice? invoice)
    {
        OpenPaymentDrawer(invoice);
    }

    private void OpenPaymentDrawer(Invoice? invoice)
    {
        if (invoice is null)
        {
            return;
        }

        SelectedInvoice = invoice;
        if (!CanPayInvoice(invoice))
        {
            StatusMessage = "Hóa đơn này không thể ghi nhận thanh toán.";
            return;
        }

        NewPaymentAmount = invoice.RemainingAmount;
        NewPaymentMethod = PaymentMethod.Cash;
        OnPropertyChanged(nameof(NewPaymentMethod));
        IsPaymentDrawerOpen = true;
    }

    private static bool CanPayInvoice(Invoice? invoice)
    {
        return invoice is not null &&
            invoice.RemainingAmount > 0 &&
            invoice.Status != InvoiceStatus.Paid &&
            invoice.Status != InvoiceStatus.Cancelled;
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
            Debug.WriteLine(ex);
            var message = ErrorMessageMapper.ToUserMessage(ex);
            StatusMessage = message;
            MessageBox.Show(message, "Quản lý nhà trọ", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private void SyncBillingMonthFromPicker()
    {
        _billingMonth = $"{SelectedBillingYear:0000}-{SelectedBillingMonth:00}";
        _dashboardYear = SelectedBillingYear;
        _invoiceFilterMonth = SelectedBillingMonth;
        _invoiceFilterYear = SelectedBillingYear;
        _paymentFilterMonth = SelectedBillingMonth;
        _paymentFilterYear = SelectedBillingYear;
        _meterFilterMonth = SelectedBillingMonth;
        _meterFilterYear = SelectedBillingYear;
        OnPropertyChanged(nameof(BillingMonth));
        OnPropertyChanged(nameof(DashboardYear));
        OnPropertyChanged(nameof(InvoiceFilterMonth));
        OnPropertyChanged(nameof(InvoiceFilterYear));
        OnPropertyChanged(nameof(PaymentFilterMonth));
        OnPropertyChanged(nameof(PaymentFilterYear));
        OnPropertyChanged(nameof(MeterFilterMonth));
        OnPropertyChanged(nameof(MeterFilterYear));
        RefreshAllFilters();
        LoadDashboard();
    }

    private void SyncPickerFromBillingMonth()
    {
        if (DateTime.TryParse($"{BillingMonth}-01", out var month))
        {
            _selectedBillingMonth = month.Month;
            _selectedBillingYear = month.Year;
            OnPropertyChanged(nameof(SelectedBillingMonth));
            OnPropertyChanged(nameof(SelectedBillingYear));
        }
    }

    private void UpdateYearOptions()
    {
        var currentYear = DateTime.Today.Year;
        var minYear = currentYear - 1;
        var maxYear = currentYear + 2;

        var dataYears = Invoices
            .Select(x => ParseBillingYear(x.BillingMonth))
            .Concat(MeterReadings.Select(x => ParseBillingYear(x.BillingMonth)))
            .Concat(Payments.Select(x => x.PaymentDate.Year))
            .Where(x => x > 0)
            .ToList();

        if (dataYears.Count > 0)
        {
            minYear = Math.Min(minYear, dataYears.Min());
        }

        var years = Enumerable.Range(minYear, maxYear - minYear + 1).ToList();
        if (YearOptions.SequenceEqual(years))
        {
            return;
        }

        Replace(YearOptions, years);
    }

    private static int ParseBillingYear(string? billingMonth)
    {
        return !string.IsNullOrWhiteSpace(billingMonth) &&
               billingMonth.Length >= 4 &&
               int.TryParse(billingMonth[..4], out var year)
            ? year
            : 0;
    }

    private void ClearFilters()
    {
        RoomSearch = string.Empty;
        RoomRepresentativeSearch = string.Empty;
        RoomFilterPropertyId = 0;
        RoomFilterStatus = "Tất cả";
        TenantSearch = string.Empty;
        TenantStatusFilter = "Tất cả";
        AssignmentFilterPropertyId = 0;
        AssignmentFilterRoomId = 0;
        AssignmentHistoryFilter = "Đã kết thúc";
        InvoiceSearch = string.Empty;
        InvoiceFilterMonth = SelectedBillingMonth;
        InvoiceFilterYear = SelectedBillingYear;
        InvoiceFilterPropertyId = 0;
        InvoiceFilterRoomId = 0;
        InvoiceNewPropertyId = 0;
        InvoiceRoomId = 0;
        InvoiceFilterStatus = "Tất cả";
        PaymentSearch = string.Empty;
        PaymentFilterMonth = SelectedBillingMonth;
        PaymentFilterYear = SelectedBillingYear;
        PaymentFilterPropertyId = 0;
        PaymentFilterRoomId = 0;
        PaymentFilterMethod = "Tất cả";
        MeterFilterMonth = SelectedBillingMonth;
        MeterFilterYear = SelectedBillingYear;
        MeterFilterPropertyId = 0;
        MeterFilterRoomId = 0;
        MeterFilterFeeTypeId = 0;
        RoomFeeSearch = string.Empty;
        RoomFeePropertyFilterId = 0;
        RoomFeeRoomFilterId = 0;
        RoomFeeFeeTypeFilterId = 0;
        RoomFeeStatusFilter = "Tất cả";
        RefreshAllFilters();
    }

    private void RefreshAssignmentRoomOptions()
    {
        var rooms = AssignmentNewPropertyId <= 0
            ? new List<Room>()
            : Rooms
                .Where(x => x.Status is RoomStatus.Occupied or RoomStatus.Vacant)
                .Where(x => x.PropertyId == AssignmentNewPropertyId)
                .OrderBy(x => x.PropertyName)
                .ThenBy(x => x.RoomName)
                .ToList();

        Replace(AssignmentRoomOptions, rooms);

        if (NewRoomTenant.RoomId > 0 && rooms.All(x => x.Id != NewRoomTenant.RoomId))
        {
            NewRoomTenant.RoomId = 0;
            OnPropertyChanged(nameof(NewRoomTenant));
        }
    }

    private void RefreshTransferRoomOptions()
    {
        var rooms = TransferPropertyId <= 0
            ? new List<Room>()
            : Rooms
                .Where(x => x.Status is RoomStatus.Occupied or RoomStatus.Vacant)
                .Where(x => x.PropertyId == TransferPropertyId)
                .OrderBy(x => x.RoomName)
                .ToList();

        Replace(TransferRoomOptions, rooms);

        if (TransferRoomId > 0 && rooms.All(x => x.Id != TransferRoomId))
        {
            TransferRoomId = 0;
        }

        if (TransferRoomId <= 0)
        {
            var defaultRoom = rooms.FirstOrDefault(x => _transferAssignment is null || x.Id != _transferAssignment.RoomId)
                ?? rooms.FirstOrDefault();
            TransferRoomId = defaultRoom?.Id ?? 0;
        }
    }

    private void RefreshAssignmentTenantOptions()
    {
        var text = AssignmentTenantSearchText.Trim();
        var tenants = AssignableTenants.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            tenants = tenants.Where(x => TenantMatchesSearch(x, text));
        }

        Replace(AssignmentTenantOptions, tenants.OrderBy(x => x.FullName).ThenBy(x => x.Phone));
    }

    private void RefreshTenantStatusCollections()
    {
        foreach (var t in Tenants)
        {
            var activeRT = RoomTenants.FirstOrDefault(x => x.TenantId == t.Id && x.Status == RoomTenantStatus.Active);
            t.CurrentRoomName = activeRT != null ? $"{activeRT.RoomName} ({activeRT.PropertyName})" : string.Empty;
        }

        Replace(RentingTenants, Tenants.Where(x => x.Status == TenantStatus.Renting).OrderBy(x => x.FullName).ThenBy(x => x.Phone));
        Replace(UnassignedTenants, Tenants.Where(x => x.Status == TenantStatus.Unassigned).OrderBy(x => x.FullName).ThenBy(x => x.Phone));
        Replace(FormerTenants, Tenants.Where(x => x.Status == TenantStatus.Former).OrderBy(x => x.FullName).ThenBy(x => x.Phone));
        OnPropertyChanged(nameof(UnassignedTenantCountText));
        
        var assignable = AssignmentShowFormerTenants 
            ? UnassignedTenants.Concat(FormerTenants).OrderBy(x => x.FullName).ThenBy(x => x.Phone)
            : UnassignedTenants.AsEnumerable();
        
        Replace(AssignableTenants, assignable);
        OnPropertyChanged(nameof(HasAssignableTenants));
        OnPropertyChanged(nameof(HasNoAssignableTenants));
    }

    private void ClearAssignmentFilters()
    {
        AssignmentFilterPropertyId = 0;
        AssignmentFilterRoomId = 0;
        AssignmentRoomSearch = string.Empty;
        RefreshAssignmentFilters();
    }

    private void ClearAssignmentHistoryFilters()
    {
        AssignmentHistoryPropertyId = 0;
        AssignmentHistoryRoomId = 0;
        AssignmentHistoryTenantSearch = string.Empty;
        RefreshAssignmentHistoryFilters();
    }

    private void SelectAssignmentTenant(Tenant? tenant)
    {
        if (tenant is null)
        {
            return;
        }

        if (SelectedAssignmentTenant?.Id == tenant.Id)
        {
            SelectedAssignmentTenant = null;
        }

        SelectedAssignmentTenant = tenant;
        SetAssignmentTenantSearchText(tenant.AssignmentDisplayText);
        RefreshAssignmentTenantOptions();
        IsAssignmentTenantDropdownOpen = false;
    }

    private static InvoiceItem ToDisplayInvoiceItem(InvoiceItem item)
    {
        return new InvoiceItem
        {
            Id = item.Id,
            InvoiceId = item.InvoiceId,
            FeeTypeId = item.FeeTypeId,
            FeeType = item.FeeType,
            ItemName = item.FeeType?.DisplayName ?? DisplayText.FeeName(item.ItemName),
            CalculationType = item.CalculationType,
            Quantity = item.Quantity,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            Amount = item.Amount,
            Note = item.Note
        };
    }

    private void SetAssignmentTenantSearchText(string text)
    {
        try
        {
            _isUpdatingAssignmentTenantText = true;
            AssignmentTenantSearchText = text;
        }
        finally
        {
            _isUpdatingAssignmentTenantText = false;
        }
    }

    private void ClearAssignmentTenantSelectionIfTextNoLongerMatches()
    {
        if (SelectedAssignmentTenant is null)
        {
            return;
        }

        if (string.Equals(AssignmentTenantSearchText, SelectedAssignmentTenant.AssignmentDisplayText, StringComparison.CurrentCulture))
        {
            return;
        }

        SelectedAssignmentTenant = null;
    }

    private void RefreshAllFilters()
    {
        RefreshRoomFilters();
        RefreshTenantFilters();
        RefreshAssignmentFilters();
        RefreshRoomFeeFilters();
        RefreshInvoiceFilters();
        RefreshPaymentFilters();
        RefreshMeterReadingFilters();
    }

    private void RefreshRoomFilters()
    {
        var text = RoomSearch.Trim();
        IEnumerable<Room> rooms = Rooms.Where(x => x.Status is RoomStatus.Occupied or RoomStatus.Vacant);
        if (RoomFilterPropertyId > 0)
        {
            rooms = rooms.Where(x => x.PropertyId == RoomFilterPropertyId);
        }

        if (RoomFilterStatus != "Tất cả")
        {
            rooms = rooms.Where(x => x.StatusText == RoomFilterStatus);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            rooms = rooms.Where(x => Contains(x.RoomName, text));
        }

        if (!string.IsNullOrWhiteSpace(RoomRepresentativeSearch))
        {
            rooms = rooms.Where(x => Contains(x.RepresentativeTenantName, RoomRepresentativeSearch));
        }

        Replace(FilteredRooms, rooms);
    }

    private void RefreshTenantFilters()
    {
        var text = TenantSearch.Trim();
        IEnumerable<Tenant> tenants = Tenants;
        if (TenantStatusFilter == "Đang thuê")
        {
            tenants = tenants.Where(x => x.Status == TenantStatus.Renting);
        }
        else if (TenantStatusFilter == "Chưa phân phòng")
        {
            tenants = tenants.Where(x => x.Status == TenantStatus.Unassigned);
        }
        else if (TenantStatusFilter == "Đã rời")
        {
            tenants = tenants.Where(x => x.Status == TenantStatus.Former);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            tenants = tenants.Where(x => Contains(x.FullName, text) || Contains(x.Phone, text) || Contains(x.Email, text));
        }

        Replace(FilteredTenants, tenants);
    }

    private void RefreshAssignmentFilters()
    {
        IEnumerable<RoomTenant> assignments = RoomTenants;
        if (AssignmentFilterPropertyId > 0)
        {
            assignments = assignments.Where(x => x.Room?.PropertyId == AssignmentFilterPropertyId);
        }

        if (AssignmentFilterRoomId > 0)
        {
            assignments = assignments.Where(x => x.RoomId == AssignmentFilterRoomId);
        }

        Replace(ActiveRoomTenants, assignments.Where(x => x.Status == RoomTenantStatus.Active));
        RefreshAssignmentHistoryFilters();
    }

    private void RefreshAssignmentHistoryFilters()
    {
        IEnumerable<RoomTenant> history = RoomTenants.Where(x => x.Status == RoomTenantStatus.Ended);

        if (AssignmentHistoryPropertyId > 0)
        {
            history = history.Where(x => x.Room?.PropertyId == AssignmentHistoryPropertyId);
        }

        if (AssignmentHistoryRoomId > 0)
        {
            history = history.Where(x => x.RoomId == AssignmentHistoryRoomId);
        }

        var tenantSearch = AssignmentHistoryTenantSearch.Trim();
        if (!string.IsNullOrWhiteSpace(tenantSearch))
        {
            history = history.Where(x => Contains(x.TenantName, tenantSearch));
        }

        Replace(FilteredAssignmentHistory, history.OrderByDescending(x => x.EndDate ?? x.StartDate));
    }

    private void RefreshInvoiceFilters()
    {
        var text = InvoiceSearch.Trim();
        var month = $"{InvoiceFilterYear:0000}-{InvoiceFilterMonth:00}";
        IEnumerable<Invoice> invoices = Invoices.Where(x => x.BillingMonth == month);
        if (InvoiceFilterPropertyId > 0)
        {
            invoices = invoices.Where(x => x.Room?.PropertyId == InvoiceFilterPropertyId);
        }

        if (InvoiceFilterRoomId > 0)
        {
            invoices = invoices.Where(x => x.RoomId == InvoiceFilterRoomId);
        }

        if (InvoiceFilterStatus != "Tất cả")
        {
            invoices = invoices.Where(x => x.StatusText == InvoiceFilterStatus);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            invoices = invoices.Where(x => Contains(x.RepresentativeTenantName, text));
        }

        Replace(FilteredInvoices, invoices);
    }

    private void RefreshPaymentFilters()
    {
        var text = PaymentSearch.Trim();
        var month = $"{PaymentFilterYear:0000}-{PaymentFilterMonth:00}";
        IEnumerable<Payment> payments = Payments.Where(x => x.BillingMonth == month);
        if (PaymentFilterPropertyId > 0)
        {
            payments = payments.Where(x => x.Invoice?.Room?.PropertyId == PaymentFilterPropertyId);
        }

        if (PaymentFilterRoomId > 0)
        {
            payments = payments.Where(x => x.Invoice?.RoomId == PaymentFilterRoomId);
        }

        if (PaymentFilterMethod != "Tất cả")
        {
            payments = payments.Where(x => x.MethodText == PaymentFilterMethod);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            payments = payments.Where(x => Contains(x.Invoice?.RepresentativeTenantName, text));
        }

        Replace(FilteredPayments, payments);
    }

    private void RefreshRoomFeeFilters()
    {
        IEnumerable<RoomFeeConfig> configs = RoomFeeConfigs;
        if (RoomFeePropertyFilterId > 0) configs = configs.Where(x => x.Room?.PropertyId == RoomFeePropertyFilterId);
        if (RoomFeeRoomFilterId > 0) configs = configs.Where(x => x.RoomId == RoomFeeRoomFilterId);
        if (RoomFeeFeeTypeFilterId > 0) configs = configs.Where(x => x.FeeTypeId == RoomFeeFeeTypeFilterId);
        if (RoomFeeStatusFilter == "Đang áp dụng") configs = configs.Where(x => x.IsEffectivelyActive);
        if (RoomFeeStatusFilter == "Ngừng áp dụng") configs = configs.Where(x => !x.IsEffectivelyActive);
        if (!string.IsNullOrWhiteSpace(RoomFeeSearch))
        {
            configs = configs.Where(x => Contains(x.PropertyName, RoomFeeSearch) || Contains(x.RoomName, RoomFeeSearch) || Contains(x.FeeTypeName, RoomFeeSearch));
        }
        var filteredConfigs = configs.ToList();
        Debug.WriteLine($"RoomFee filter: propertyId={RoomFeePropertyFilterId}, roomId={RoomFeeRoomFilterId}, feeTypeId={RoomFeeFeeTypeFilterId}, status={RoomFeeStatusFilter}, count={filteredConfigs.Count}");
        foreach (var config in filteredConfigs.Where(x => x.Room is null || x.FeeType is null))
        {
            Debug.WriteLine($"RoomFee orphan/missing navigation: id={config.Id}, roomId={config.RoomId}, roomName={config.RoomName}, feeTypeId={config.FeeTypeId}, feeName={config.FeeTypeName}, enabled={config.Enabled}");
        }

        Replace(FilteredRoomFeeConfigs, filteredConfigs);
    }

    private void RefreshMeterReadingFilters()
    {
        var month = $"{MeterFilterYear:0000}-{MeterFilterMonth:00}";
        IEnumerable<MeterReading> readings = MeterReadings.Where(x => x.BillingMonth == month);
        if (MeterFilterPropertyId > 0)
        {
            readings = readings.Where(x => x.Room?.PropertyId == MeterFilterPropertyId);
        }

        if (MeterFilterRoomId > 0)
        {
            readings = readings.Where(x => x.RoomId == MeterFilterRoomId);
        }

        if (MeterFilterFeeTypeId > 0)
        {
            readings = readings.Where(x => x.FeeTypeId == MeterFilterFeeTypeId);
        }

        Replace(FilteredMeterReadings, readings);
    }

    private void RefreshMeterReadingFeeTypeOptions()
    {
        // Diagnostics: if this list is empty, the selected room has no enabled RoomFeeConfig with CalculationType.Meter.
        var feeTypeIds = RoomFeeConfigs
            .Where(x => x.RoomId == NewMeterReading.RoomId && x.Enabled && x.CalculationType == CalculationType.Meter)
            .Select(x => x.FeeTypeId)
            .Distinct()
            .ToHashSet();

        var feeTypes = FeeTypes
            .Where(x => x.IsActive && feeTypeIds.Contains(x.Id))
            .OrderBy(x => x.DisplayName)
            .ToList();

        Replace(MeterReadingFeeTypeOptions, feeTypes);

        MeterReadingHelpMessage = NewMeterReading.RoomId > 0 && feeTypes.Count == 0
            ? "Phòng này chưa có loại phí theo chỉ số đang áp dụng."
            : string.Empty;

        if (NewMeterReading.FeeTypeId > 0 && feeTypes.All(x => x.Id != NewMeterReading.FeeTypeId))
        {
            NewMeterReading.FeeTypeId = 0;
            OnPropertyChanged(nameof(NewMeterReadingFeeTypeId));
        }

        LoadMeterReadingFormForSelection();
    }

    private void RefreshMeterReadingRoomOptions()
    {
        var rooms = NewMeterReadingPropertyId <= 0
            ? new List<Room>()
            : Rooms
                .Where(x => x.PropertyId == NewMeterReadingPropertyId)
                .OrderBy(x => x.RoomName)
                .ToList();

        Replace(MeterReadingRoomOptions, rooms);

        if (NewMeterReading.RoomId > 0 && rooms.All(x => x.Id != NewMeterReading.RoomId))
        {
            NewMeterReading.RoomId = 0;
            NewMeterReading.FeeTypeId = 0;
            NewMeterReading.Id = 0;
            NewMeterReading.PreviousReading = 0;
            NewMeterReading.CurrentReading = 0;
            NewMeterReading.Note = null;
            OnPropertyChanged(nameof(NewMeterReading));
            OnPropertyChanged(nameof(NewMeterReadingRoomId));
            OnPropertyChanged(nameof(NewMeterReadingFeeTypeId));
        }

        RefreshMeterReadingFeeTypeOptions();
    }

    private string MeterReadingBillingMonth => $"{MeterFilterYear:0000}-{MeterFilterMonth:00}";

    private void LoadMeterReadingFormForSelection()
    {
        if (NewMeterReading.RoomId <= 0 || NewMeterReading.FeeTypeId <= 0)
        {
            NewMeterReading.Id = 0;
            NewMeterReading.PreviousReading = 0;
            NewMeterReading.CurrentReading = 0;
            NewMeterReading.Note = null;
            OnPropertyChanged(nameof(NewMeterReading));
            return;
        }

        var existingReading = MeterReadings.FirstOrDefault(x =>
            x.RoomId == NewMeterReading.RoomId &&
            x.FeeTypeId == NewMeterReading.FeeTypeId &&
            x.BillingMonth == MeterReadingBillingMonth);

        if (existingReading is not null)
        {
            NewMeterReading.Id = existingReading.Id;
            NewMeterReading.BillingMonth = existingReading.BillingMonth;
            NewMeterReading.PreviousReading = existingReading.PreviousReading;
            NewMeterReading.CurrentReading = existingReading.CurrentReading;
            NewMeterReading.Note = existingReading.Note;
            OnPropertyChanged(nameof(NewMeterReading));
            return;
        }

        NewMeterReading.Id = 0;
        NewMeterReading.BillingMonth = MeterReadingBillingMonth;
        NewMeterReading.PreviousReading = _meterReadingService.GetPreviousReading(NewMeterReading.RoomId, NewMeterReading.FeeTypeId, MeterReadingBillingMonth);
        NewMeterReading.CurrentReading = 0;
        NewMeterReading.Note = null;
        OnPropertyChanged(nameof(NewMeterReading));
    }

    private void CloseAllDrawers()
    {
        IsPropertyDrawerOpen = false;
        IsRoomDrawerOpen     = false;
        IsBulkRoomDrawerOpen = false;
        IsRoomFeeDrawerOpen  = false;
        IsTenantDrawerOpen   = false;
        IsAssignmentDrawerOpen = false;
        IsAssignmentHistoryDrawerOpen = false;
        IsAssignmentRoomDetailDrawerOpen = false;
        IsTransferRoomDrawerOpen = false;
        IsInvoiceGenerationDrawerOpen = false;
        IsPaymentDrawerOpen = false;
        IsAssignmentTenantDropdownOpen = false;
    }

    private void OpenAssignmentDrawer()
    {
        OpenAssignmentDrawer(Rooms.FirstOrDefault(x => x.Id == AssignmentFilterRoomId));
    }

    private void SelectAssignmentProperty(Property? property)
    {
        AssignmentFilterPropertyId = property?.Id ?? 0;
    }

    private void ShowAllAssignmentProperties()
    {
        AssignmentFilterPropertyId = 0;
    }

    private void OpenAssignmentDrawer(Room? room)
    {
        AssignmentNewPropertyId = room?.PropertyId ?? 0;
        NewRoomTenant = new RoomTenant
        {
            RoomId = room?.Id ?? 0,
            StartDate = DateTime.Today,
            IsRepresentative = room is null || !RoomTenants.Any(x => x.RoomId == room.Id && x.Status == RoomTenantStatus.Active && x.IsRepresentative)
        };
        AssignmentShowFormerTenants = false;
        SelectedAssignmentTenant = null;
        SetAssignmentTenantSearchText(string.Empty);
        IsAssignmentTenantDropdownOpen = false;
        RefreshAssignmentRoomOptions();
        RefreshAssignmentTenantOptions();
        OnPropertyChanged(nameof(NewRoomTenant));
        OnPropertyChanged(nameof(AssignmentStartDate));
        OnPropertyChanged(nameof(AssignmentRoomId));
        OnPropertyChanged(nameof(SelectedAssignmentRoomText));
        IsAssignmentDrawerOpen = true;
    }

    private void OpenAssignmentRoomDetail(Room? room)
    {
        if (room is null)
        {
            return;
        }

        RefreshSelectedAssignmentRoomDetail(room.Id);
        IsAssignmentRoomDetailDrawerOpen = true;
    }

    private void RefreshSelectedAssignmentRoomDetail(int roomId)
    {
        var room = Rooms.FirstOrDefault(x => x.Id == roomId);
        if (room is null)
        {
            SelectedAssignmentRoom = null;
            Replace(SelectedAssignmentRoomTenants, Array.Empty<RoomTenant>());
            OnPropertyChanged(nameof(HasSelectedAssignmentRoomTenants));
            OnPropertyChanged(nameof(HasNoSelectedAssignmentRoomTenants));
            return;
        }

        SelectedAssignmentRoom = room;
        var activeTenants = RoomTenants
            .Where(x => x.RoomId == room.Id && x.Status == RoomTenantStatus.Active)
            .OrderByDescending(x => x.IsRepresentative)
            .ThenBy(x => x.TenantName)
            .ToList();
        Replace(SelectedAssignmentRoomTenants, activeTenants);
        OnPropertyChanged(nameof(HasSelectedAssignmentRoomTenants));
        OnPropertyChanged(nameof(HasNoSelectedAssignmentRoomTenants));
    }

    private void OpenAssignmentHistoryDrawer()
    {
        IsAssignmentHistoryDrawerOpen = true;
        RefreshAssignmentHistoryFilters();
    }

    private void OpenAssignmentHistoryForRow(RoomTenant? assignment)
    {
        if (assignment is null)
        {
            OpenAssignmentHistoryDrawer();
            return;
        }

        AssignmentHistoryPropertyId = assignment.Room?.PropertyId ?? Rooms.FirstOrDefault(x => x.Id == assignment.RoomId)?.PropertyId ?? 0;
        AssignmentHistoryRoomId = assignment.RoomId;
        AssignmentHistoryTenantSearch = assignment.TenantName;
        IsAssignmentHistoryDrawerOpen = true;
        RefreshAssignmentHistoryFilters();
    }

    private void OpenAssignmentHistoryForRoom(Room? room)
    {
        if (room is null)
        {
            OpenAssignmentHistoryDrawer();
            return;
        }

        AssignmentHistoryPropertyId = room.PropertyId;
        AssignmentHistoryRoomId = room.Id;
        AssignmentHistoryTenantSearch = string.Empty;
        IsAssignmentHistoryDrawerOpen = true;
        RefreshAssignmentHistoryFilters();
    }

    private void RefreshAssignmentFilterRoomOptions()
    {
        var rooms = Rooms
            .Where(x => x.Status is RoomStatus.Occupied or RoomStatus.Vacant)
            .Where(x => AssignmentFilterPropertyId <= 0 || x.PropertyId == AssignmentFilterPropertyId);

        var search = AssignmentRoomSearch.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            rooms = rooms.Where(x => Contains(x.RoomName, search) || Contains(x.PropertyName, search));
        }

        if (AssignmentVacantOnly)
        {
            rooms = rooms.Where(x => !RoomTenants.Any(rt => rt.RoomId == x.Id && rt.Status == RoomTenantStatus.Active));
        }

        var filteredRooms = rooms
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();

        Replace(AssignmentFilterRoomOptions, filteredRooms);
        Replace(FilteredAssignmentRooms, filteredRooms);

        if (AssignmentFilterRoomId > 0 && filteredRooms.All(x => x.Id != AssignmentFilterRoomId))
        {
            AssignmentFilterRoomId = 0;
        }
    }

    private void RefreshAssignmentHistoryRoomOptions()
    {
        var rooms = Rooms
            .Where(x => AssignmentHistoryPropertyId <= 0 || x.PropertyId == AssignmentHistoryPropertyId)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();

        Replace(AssignmentHistoryRoomOptions, rooms);

        if (AssignmentHistoryRoomId > 0 && rooms.All(x => x.Id != AssignmentHistoryRoomId))
        {
            AssignmentHistoryRoomId = 0;
        }
    }

    private void RefreshWorkspaceRooms()
    {
        var rooms = SelectedProperty is null
            ? new List<Room>()
            : Rooms.Where(x => x.PropertyId == SelectedProperty.Id)
                   .OrderBy(x => x.RoomName).ToList();
        Replace(WorkspaceRooms, rooms);
        OnPropertyChanged(nameof(WorkspacePropertyHeader));
        if (SelectedRoom is not null && (SelectedProperty is null || SelectedRoom.PropertyId != SelectedProperty.Id))
            SelectedRoom = null;
    }

    private void RefreshWorkspaceRoomFees()
    {
        var fees = SelectedRoom is null
            ? new List<RoomFeeConfig>()
            : RoomFeeConfigs.Where(x => x.RoomId == SelectedRoom.Id)
                            .OrderBy(x => x.FeeTypeName).ToList();
        Replace(WorkspaceRoomFees, fees);
        OnPropertyChanged(nameof(WorkspaceRoomHeader));
    }

    private void RefreshMeterFilterRoomOptions()
    {
        var rooms = Rooms
            .Where(x => MeterFilterPropertyId <= 0 || x.PropertyId == MeterFilterPropertyId)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();

        Replace(MeterFilterRoomOptions, rooms);

        if (MeterFilterRoomId > 0 && rooms.All(x => x.Id != MeterFilterRoomId))
        {
            MeterFilterRoomId = 0;
        }
    }

    private void RefreshRoomFeeFilterRoomOptions()
    {
        var rooms = Rooms
            .Where(x => RoomFeePropertyFilterId <= 0 || x.PropertyId == RoomFeePropertyFilterId)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();

        Replace(RoomFeeFilterRoomOptions, rooms);

        if (RoomFeeRoomFilterId > 0 && rooms.All(x => x.Id != RoomFeeRoomFilterId))
        {
            RoomFeeRoomFilterId = 0;
        }
    }

    private void RefreshRoomFeeFormRoomOptions()
    {
        var rooms = NewRoomFeePropertyId <= 0
            ? new List<Room>()
            : Rooms
                .Where(x => x.PropertyId == NewRoomFeePropertyId)
                .OrderBy(x => x.PropertyName)
                .ThenBy(x => x.RoomName)
                .ToList();

        Replace(RoomFeeFormRoomOptions, rooms);

        if (NewRoomFeeConfig.RoomId > 0 && rooms.All(x => x.Id != NewRoomFeeConfig.RoomId))
        {
            NewRoomFeeConfig.RoomId = 0;
            OnPropertyChanged(nameof(NewRoomFeeConfig));
        }
    }

    private void RefreshInvoiceFilterRoomOptions()
    {
        var rooms = Rooms
            .Where(x => InvoiceFilterPropertyId <= 0 || x.PropertyId == InvoiceFilterPropertyId)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();

        Replace(InvoiceFilterRoomOptions, rooms);

        if (InvoiceFilterRoomId > 0 && rooms.All(x => x.Id != InvoiceFilterRoomId))
        {
            InvoiceFilterRoomId = 0;
        }
    }

    private void RefreshInvoiceFormRoomOptions()
    {
        var rooms = InvoiceNewPropertyId <= 0
            ? new List<Room>()
            : Rooms
                .Where(x => x.PropertyId == InvoiceNewPropertyId)
                .OrderBy(x => x.RoomName)
                .ToList();

        Replace(InvoiceFormRoomOptions, rooms);

        if (InvoiceRoomId > 0 && rooms.All(x => x.Id != InvoiceRoomId))
        {
            InvoiceRoomId = 0;
        }
    }

    private void RaiseRoomFeeFieldVisibility()
    {
        OnPropertyChanged(nameof(RoomFeeUnitPriceVisibility));
        OnPropertyChanged(nameof(RoomFeeFixedAmountVisibility));
        OnPropertyChanged(nameof(RoomFeeQuantityVisibility));
        OnPropertyChanged(nameof(RoomFeeDefaultPriceVisibility));
    }

    private void RaiseRoomFeePricingState()
    {
        OnPropertyChanged(nameof(NewRoomFeeUseDefaultPrice));
        OnPropertyChanged(nameof(RoomFeeCustomPriceInputEnabled));
        OnPropertyChanged(nameof(RoomFeeDefaultPriceInputEnabled));
    }

    private decimal? GetRoomFeeCustomPrice()
    {
        return NewRoomFeeConfig.CalculationType switch
        {
            CalculationType.Fixed => NewRoomFeeConfig.FixedAmount,
            CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit => NewRoomFeeConfig.UnitPrice,
            CalculationType.Manual => NewRoomFeeConfig.FixedAmount,
            _ => null
        };
    }

    private bool RoomFeeCalculationMatchesFeeTypeDefault()
    {
        var feeType = FeeTypes.FirstOrDefault(x => x.Id == NewRoomFeeConfig.FeeTypeId);
        return feeType is not null && NewRoomFeeConfig.CalculationType == feeType.DefaultCalculationType;
    }

    private void ClearRoomFeeCustomPrice()
    {
        if (NewRoomFeeConfig.CalculationType == CalculationType.Fixed)
        {
            NewRoomFeeConfig.FixedAmount = null;
        }
        else if (NewRoomFeeConfig.CalculationType is CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit)
        {
            NewRoomFeeConfig.UnitPrice = null;
        }
    }

    private void FillRoomFeeCustomPriceFromDefault()
    {
        var defaultPrice = FeeTypes.FirstOrDefault(x => x.Id == NewRoomFeeConfig.FeeTypeId)?.DefaultUnitPrice ?? 0;
        if (NewRoomFeeConfig.CalculationType == CalculationType.Fixed && NewRoomFeeConfig.FixedAmount is null)
        {
            NewRoomFeeConfig.FixedAmount = defaultPrice;
        }
        else if (NewRoomFeeConfig.CalculationType is CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit &&
                 NewRoomFeeConfig.UnitPrice is null)
        {
            NewRoomFeeConfig.UnitPrice = defaultPrice;
        }
    }

    private void ClearIrrelevantRoomFeePriceFields()
    {
        switch (NewRoomFeeConfig.CalculationType)
        {
            case CalculationType.Fixed:
                NewRoomFeeConfig.UnitPrice = null;
                NewRoomFeeConfig.Quantity = null;
                break;
            case CalculationType.Meter:
            case CalculationType.PerPerson:
                NewRoomFeeConfig.FixedAmount = null;
                NewRoomFeeConfig.Quantity = null;
                break;
            case CalculationType.PerUnit:
                NewRoomFeeConfig.FixedAmount = null;
                break;
            case CalculationType.Manual:
                NewRoomFeeConfig.UnitPrice = null;
                NewRoomFeeConfig.Quantity = null;
                break;
        }
    }

    private void NotifyFormModes()
    {
        OnPropertyChanged(nameof(NewProperty));
        OnPropertyChanged(nameof(NewRoom));
        OnPropertyChanged(nameof(NewTenant));
        OnPropertyChanged(nameof(NewFeeType));
        OnPropertyChanged(nameof(NewRoomFeeConfig));
        OnPropertyChanged(nameof(NewRoomFeeFeeTypeId));
        OnPropertyChanged(nameof(NewRoomFeeCalculationType));
        OnPropertyChanged(nameof(PropertyFormMode));
        OnPropertyChanged(nameof(RoomFormMode));
        OnPropertyChanged(nameof(TenantFormMode));
        OnPropertyChanged(nameof(FeeTypeFormMode));
        OnPropertyChanged(nameof(RoomFeeFormMode));
        OnPropertyChanged(nameof(NewMeterReadingRoomId));
        OnPropertyChanged(nameof(NewMeterReadingFeeTypeId));
        OnPropertyChanged(nameof(NewMeterReadingPropertyId));
        OnPropertyChanged(nameof(NewRoomFeePropertyId));
        RaiseRoomFeeFieldVisibility();
        RaiseRoomFeePricingState();
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        AddPropertyCommand.RaiseCanExecuteChanged();
        SavePropertyCommand.RaiseCanExecuteChanged();
        EditPropertyCommand.RaiseCanExecuteChanged();
        DeactivatePropertyCommand.RaiseCanExecuteChanged();
        CancelPropertyEditCommand.RaiseCanExecuteChanged();
        AddRoomCommand.RaiseCanExecuteChanged();
        SaveRoomCommand.RaiseCanExecuteChanged();
        EditRoomCommand.RaiseCanExecuteChanged();
        DeactivateRoomCommand.RaiseCanExecuteChanged();
        CancelRoomEditCommand.RaiseCanExecuteChanged();
        AddTenantCommand.RaiseCanExecuteChanged();
        SaveTenantCommand.RaiseCanExecuteChanged();
        EditTenantCommand.RaiseCanExecuteChanged();
        CancelTenantEditCommand.RaiseCanExecuteChanged();
        AddFeeTypeCommand.RaiseCanExecuteChanged();
        SaveFeeTypeCommand.RaiseCanExecuteChanged();
        EditFeeTypeCommand.RaiseCanExecuteChanged();
        DeactivateFeeTypeCommand.RaiseCanExecuteChanged();
        CancelFeeTypeEditCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedFeeTypeToggleActionText));
        AddRoomFeeConfigCommand.RaiseCanExecuteChanged();
        SaveRoomFeeConfigCommand.RaiseCanExecuteChanged();
        EditRoomFeeConfigCommand.RaiseCanExecuteChanged();
        DisableRoomFeeConfigCommand.RaiseCanExecuteChanged();
        CancelRoomFeeConfigEditCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedRoomFeeToggleActionText));
        IssueInvoiceCommand.RaiseCanExecuteChanged();
        OpenPaymentDrawerCommand.RaiseCanExecuteChanged();
        RecordPaymentCommand.RaiseCanExecuteChanged();
        FillRemainingPaymentCommand.RaiseCanExecuteChanged();
        CopyInvoiceCommand.RaiseCanExecuteChanged();
        CancelInvoiceCommand.RaiseCanExecuteChanged();
        AddRoomsRangeCommand.RaiseCanExecuteChanged();
    }

    private static void RequireExisting(int id)
    {
        if (id <= 0) throw new ValidationException("Chọn dòng cần sửa trước khi lưu thay đổi.");
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    private static bool TenantMatchesSearch(Tenant tenant, string search)
    {
        return Contains(tenant.FullName, search) ||
               Contains(tenant.Phone, search) ||
               Contains(tenant.IdentityNumber, search) ||
               Contains(tenant.AssignmentDisplayText, search);
    }

    private bool HasActiveAssignment(Tenant tenant)
    {
        return RoomTenants.Any(x => x.TenantId == tenant.Id && x.Status == RoomTenantStatus.Active);
    }

    private bool HasEndedAssignment(Tenant tenant)
    {
        return RoomTenants.Any(x => x.TenantId == tenant.Id && x.Status == RoomTenantStatus.Ended);
    }

    private bool HasAnyAssignment(Tenant tenant)
    {
        return RoomTenants.Any(x => x.TenantId == tenant.Id);
    }

    private bool RoomNeedsRepresentative(int roomId)
    {
        var activeAssignments = RoomTenants.Where(x => x.RoomId == roomId && x.Status == RoomTenantStatus.Active).ToList();
        return activeAssignments.Count > 0 && activeAssignments.All(x => !x.IsRepresentative);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var item in values) target.Add(item);
    }
}

