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
using RentalManager.Views;

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
    private string _dashboardRange = "TÃ¹y chá»n thÃ¡ng";
    private int _dashboardPropertyFilterId;
    private string _activeDashboardPeriod = string.Empty;
    private string _statusMessage = "Sáºµn sÃ ng";
    private DashboardSummary _dashboard = new();
    private Property? _selectedProperty;
    private Room? _selectedRoom;
    private Tenant? _selectedTenant;
    private FeeType? _selectedFeeType;
    private RoomFeeConfig? _selectedRoomFeeConfig;
    private Invoice? _selectedInvoice;
    private string _roomSearch = string.Empty;
    private string _tenantSearch = string.Empty;
    private string _tenantStatusFilter = "Táº¥t cáº£";
    private string _assignmentHistoryFilter = "ÄÃ£ káº¿t thÃºc";
    private string _invoiceSearch = string.Empty;
    private string _paymentSearch = string.Empty;
    private string _roomFeeSearch = string.Empty;
    private int _selectedBillingMonth = DateTime.Today.Month;
    private int _selectedBillingYear = DateTime.Today.Year;
    private int _roomFilterPropertyId;
    private string _roomFilterStatus = "Táº¥t cáº£";
    private string _roomRepresentativeSearch = string.Empty;
    private int _invoiceFilterMonth = DateTime.Today.Month;
    private int _invoiceFilterYear = DateTime.Today.Year;
    private int _invoiceFilterPropertyId;
    private int _invoiceFilterRoomId;
    private string _invoiceFilterStatus = "Táº¥t cáº£";
    private int _paymentFilterMonth = DateTime.Today.Month;
    private int _paymentFilterYear = DateTime.Today.Year;
    private int _paymentFilterPropertyId;
    private int _paymentFilterRoomId;
    private string _paymentFilterMethod = "Táº¥t cáº£";
    private int _meterFilterMonth = DateTime.Today.Month;
    private int _meterFilterYear = DateTime.Today.Year;
    private int _meterFilterPropertyId;
    private int _meterFilterRoomId;
    private int _meterFilterFeeTypeId;
    private string _roomFeeStatusFilter = "Äang Ã¡p dá»¥ng";
    private int _roomFeePropertyFilterId;
    private int _roomFeeRoomFilterId;
    private int _roomFeeFeeTypeFilterId;
    private bool _roomFeeEnabledOnly = true;
    private int _assignmentFilterPropertyId;
    private int _assignmentFilterRoomId;
    private DateTime _assignmentEndDate = DateTime.Today;

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
        DeactivateRoomCommand = new RelayCommand(() => Run(CheckoutRoom), () => SelectedRoom is not null);
        AddTenantCommand = new RelayCommand(() => Run(AddTenant));
        SaveTenantCommand = new RelayCommand(() => Run(SaveTenant), () => NewTenant.Id > 0);
        EditTenantCommand = new RelayCommand(() => Run(EditTenant), () => SelectedTenant is not null);
        AssignTenantCommand = new RelayCommand(() => Run(AssignTenant));
        EndAssignmentRowCommand = new RelayCommand<RoomTenant>(assignment => Run(() => EndAssignment(assignment)));
        ChangeRoomRowCommand = new RelayCommand<RoomTenant>(assignment => Run(() => ChangeRoom(assignment)));
        SetRepresentativeRowCommand = new RelayCommand<RoomTenant>(assignment => Run(() => SetRepresentative(assignment)));
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
        RecordPaymentCommand = new RelayCommand(() => Run(RecordPayment), () => SelectedInvoice is not null && SelectedInvoice.RemainingAmount > 0);
        FillRemainingPaymentCommand = new RelayCommand(FillRemainingPayment, () => SelectedInvoice is not null && SelectedInvoice.RemainingAmount > 0);
        CopyInvoiceCommand = new RelayCommand(() => Run(CopyInvoice), () => SelectedInvoice is not null);
        CancelInvoiceCommand = new RelayCommand(() => Run(CancelInvoice), () => SelectedInvoice is not null);
        BackupCommand = new RelayCommand(() => Run(Backup));
        RestoreCommand = new RelayCommand(() => Run(Restore));
        SeedDemoDataCommand = new RelayCommand(() => Run(SeedDemoData));
        ApplyFiltersCommand = new RelayCommand(RefreshAllFilters);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
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
        PayInvoiceRowCommand = new RelayCommand<Invoice>(invoice => Run(() => SelectInvoiceForPayment(invoice)));
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
    public ObservableCollection<RoomTenant> RoomTenants { get; } = new();
    public ObservableCollection<RoomTenant> ActiveRoomTenants { get; } = new();
    public ObservableCollection<RoomTenant> FilteredAssignmentHistory { get; } = new();
    public ObservableCollection<FeeType> FeeTypes { get; } = new();
    public ObservableCollection<RoomFeeConfig> RoomFeeConfigs { get; } = new();
    public ObservableCollection<RoomFeeConfig> FilteredRoomFeeConfigs { get; } = new();
    public ObservableCollection<MeterReading> MeterReadings { get; } = new();
    public ObservableCollection<MeterReading> FilteredMeterReadings { get; } = new();
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

    public IReadOnlyList<string> DashboardRangeOptions { get; } = new[] { "ThÃ¡ng hiá»‡n táº¡i", "3 thÃ¡ng gáº§n nháº¥t", "6 thÃ¡ng gáº§n nháº¥t", "NÄƒm hiá»‡n táº¡i", "TÃ¹y chá»n thÃ¡ng" };
    public IReadOnlyList<int> MonthOptions { get; } = Enumerable.Range(1, 12).ToList();
    public IReadOnlyList<int> YearOptions { get; } = Enumerable.Range(DateTime.Today.Year - 5, 11).ToList();
    public IReadOnlyList<string> RoomStatusFilterOptions { get; } = new[] { "Táº¥t cáº£", "Äang cho thuÃª", "Äang trá»‘ng" };
    public IReadOnlyList<string> TenantStatusFilterOptions { get; } = new[] { "Táº¥t cáº£", "Äang thuÃª", "ChÆ°a phÃ¢n phÃ²ng", "ÄÃ£ tá»«ng thuÃª" };
    public IReadOnlyList<string> AssignmentHistoryFilterOptions { get; } = new[] { "ÄÃ£ káº¿t thÃºc", "Äang thuÃª", "Táº¥t cáº£" };
    public IReadOnlyList<string> InvoiceStatusFilterOptions { get; } = new[] { "Táº¥t cáº£", "NhÃ¡p", "ÄÃ£ chá»‘t", "Thanh toÃ¡n má»™t pháº§n", "ÄÃ£ tráº£", "ÄÃ£ há»§y" };
    public IReadOnlyList<string> PaymentMethodFilterOptions { get; } = new[] { "Táº¥t cáº£", "Tiá»n máº·t", "Chuyá»ƒn khoáº£n", "Momo", "KhÃ¡c" };
    public IReadOnlyList<string> RoomFeeStatusFilterOptions { get; } = new[] { "Táº¥t cáº£", "Äang Ã¡p dá»¥ng", "Ngá»«ng Ã¡p dá»¥ng" };
    public IReadOnlyList<EnumOption<RoomStatus>> RoomFormStatusOptions { get; } = new[] { new EnumOption<RoomStatus>(RoomStatus.Occupied, "Äang cho thuÃª"), new EnumOption<RoomStatus>(RoomStatus.Vacant, "Äang trá»‘ng") };
    public IReadOnlyList<EnumOption<CalculationType>> CalculationTypeOptions { get; } = new[]
    {
        new EnumOption<CalculationType>(CalculationType.Fixed, "Cá»‘ Ä‘á»‹nh"),
        new EnumOption<CalculationType>(CalculationType.Meter, "Theo chá»‰ sá»‘"),
        new EnumOption<CalculationType>(CalculationType.PerPerson, "Theo ngÆ°á»i"),
        new EnumOption<CalculationType>(CalculationType.PerUnit, "Theo sá»‘ lÆ°á»£ng"),
        new EnumOption<CalculationType>(CalculationType.Manual, "Nháº­p tay")
    };
    public IReadOnlyList<EnumOption<PaymentMethod>> PaymentMethodOptions { get; } = new[]
    {
        new EnumOption<PaymentMethod>(PaymentMethod.Cash, "Tiá»n máº·t"),
        new EnumOption<PaymentMethod>(PaymentMethod.BankTransfer, "Chuyá»ƒn khoáº£n"),
        new EnumOption<PaymentMethod>(PaymentMethod.Momo, "Momo"),
        new EnumOption<PaymentMethod>(PaymentMethod.Other, "KhÃ¡c")
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
            }
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

    public int RoomFilterPropertyId { get => _roomFilterPropertyId; set => SetProperty(ref _roomFilterPropertyId, value); }
    public string RoomFilterStatus { get => _roomFilterStatus; set => SetProperty(ref _roomFilterStatus, value); }
    public string RoomRepresentativeSearch { get => _roomRepresentativeSearch; set => SetProperty(ref _roomRepresentativeSearch, value); }

    public int InvoiceFilterMonth { get => _invoiceFilterMonth; set => SetProperty(ref _invoiceFilterMonth, value); }
    public int InvoiceFilterYear { get => _invoiceFilterYear; set => SetProperty(ref _invoiceFilterYear, value); }
    public int InvoiceFilterPropertyId { get => _invoiceFilterPropertyId; set => SetProperty(ref _invoiceFilterPropertyId, value); }
    public int InvoiceFilterRoomId { get => _invoiceFilterRoomId; set => SetProperty(ref _invoiceFilterRoomId, value); }
    public string InvoiceFilterStatus { get => _invoiceFilterStatus; set => SetProperty(ref _invoiceFilterStatus, value); }

    public int PaymentFilterMonth { get => _paymentFilterMonth; set => SetProperty(ref _paymentFilterMonth, value); }
    public int PaymentFilterYear { get => _paymentFilterYear; set => SetProperty(ref _paymentFilterYear, value); }
    public int PaymentFilterPropertyId { get => _paymentFilterPropertyId; set => SetProperty(ref _paymentFilterPropertyId, value); }
    public int PaymentFilterRoomId { get => _paymentFilterRoomId; set => SetProperty(ref _paymentFilterRoomId, value); }
    public string PaymentFilterMethod { get => _paymentFilterMethod; set => SetProperty(ref _paymentFilterMethod, value); }

    public int MeterFilterMonth { get => _meterFilterMonth; set => SetProperty(ref _meterFilterMonth, value); }
    public int MeterFilterYear { get => _meterFilterYear; set => SetProperty(ref _meterFilterYear, value); }
    public int MeterFilterPropertyId { get => _meterFilterPropertyId; set => SetProperty(ref _meterFilterPropertyId, value); }
    public int MeterFilterRoomId { get => _meterFilterRoomId; set => SetProperty(ref _meterFilterRoomId, value); }
    public int MeterFilterFeeTypeId { get => _meterFilterFeeTypeId; set => SetProperty(ref _meterFilterFeeTypeId, value); }

    public string RoomFeeStatusFilter { get => _roomFeeStatusFilter; set => SetProperty(ref _roomFeeStatusFilter, value); }

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
    public string PropertyFormMode => NewProperty.Id > 0 ? $"Äang sá»­a: {NewProperty.Name}" : "Äang thÃªm má»›i";
    public string RoomFormMode => NewRoom.Id > 0 ? $"Äang sá»­a: {NewRoom.RoomName}" : "Äang thÃªm má»›i";
    public string TenantFormMode => NewTenant.Id > 0 ? $"Äang sá»­a: {NewTenant.FullName}" : "Äang thÃªm má»›i";
    public string FeeTypeFormMode => NewFeeType.Id > 0 ? $"Äang sá»­a: {NewFeeType.DisplayName}" : "Äang thÃªm má»›i";
    public string RoomFeeFormMode => NewRoomFeeConfig.Id > 0 ? "Äang sá»­a cáº¥u hÃ¬nh phÃ­" : "Äang thÃªm má»›i";
    public string SelectedInvoiceSummary => SelectedInvoice is null
        ? "ChÆ°a chá»n hÃ³a Ä‘Æ¡n"
        : $"{SelectedInvoice.RoomName} - {SelectedInvoice.RepresentativeTenantName} | Tá»•ng: {SelectedInvoice.TotalAmount:N0} | ÄÃ£ thu: {SelectedInvoice.PaidAmount:N0} | CÃ²n láº¡i: {SelectedInvoice.RemainingAmount:N0}";

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
    public RelayCommand IssueInvoiceCommand { get; }
    public RelayCommand RecordPaymentCommand { get; }
    public RelayCommand FillRemainingPaymentCommand { get; }
    public RelayCommand CopyInvoiceCommand { get; }
    public RelayCommand CancelInvoiceCommand { get; }
    public RelayCommand BackupCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand SeedDemoDataCommand { get; }
    public RelayCommand ApplyFiltersCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }
    public RelayCommand RefreshCommand { get; }
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

    public void Load()
    {
        Replace(Properties, _propertyService.GetAll());
        Replace(PropertyFilterOptions, new[] { new PropertyFilterOption { Id = 0, Name = "Táº¥t cáº£ nhÃ  / khu trá»" } }.Concat(Properties.Select(x => new PropertyFilterOption { Id = x.Id, Name = x.Name })));
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
        ActiveDashboardPeriod = startMonth == endMonth ? $"Äang xem: {startMonth}" : $"Äang xem: {startMonth} Ä‘áº¿n {endMonth}";
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
        if (SelectedProperty is null) throw new ValidationException("Chá»n nhÃ /khu trá» trÆ°á»›c.");
        NewProperty = new Property { Id = SelectedProperty.Id, Name = SelectedProperty.Name, Address = SelectedProperty.Address, Note = SelectedProperty.Note, IsActive = SelectedProperty.IsActive, CreatedAt = SelectedProperty.CreatedAt, UpdatedAt = SelectedProperty.UpdatedAt };
        NotifyFormModes();
    }

    private void DeactivateProperty()
    {
        if (SelectedProperty is null) throw new ValidationException("Chá»n nhÃ /khu trá» trÆ°á»›c.");
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
        if (SelectedRoom is null) throw new ValidationException("Chá»n phÃ²ng trÆ°á»›c.");
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
        if (SelectedTenant is null) throw new ValidationException("Chá»n ngÆ°á»i thuÃª trÆ°á»›c.");
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

    private void EndAssignment(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chá»n lÆ°á»£t thuÃª trÆ°á»›c.");
        var roomId = assignment.RoomId;
        _roomTenantService.EndAssignment(assignment.Id, AssignmentEndDate);
        Load();
        StatusMessage = RoomNeedsRepresentative(roomId)
            ? "ÄÃ£ káº¿t thÃºc thuÃª. PhÃ²ng nÃ y chÆ°a cÃ³ ngÆ°á»i Ä‘áº¡i diá»‡n."
            : "ÄÃ£ káº¿t thÃºc thuÃª.";
    }

    private void ChangeRoom(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chá»n lÆ°á»£t thuÃª trÆ°á»›c.");
        var dialog = new RoomTransferDialog(assignment, Properties, Rooms)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _roomTenantService.ChangeRoom(assignment.Id, dialog.TargetRoomId, dialog.MoveDate, dialog.IsRepresentative);
        Load();
        StatusMessage = "ÄÃ£ chuyá»ƒn phÃ²ng thÃ nh cÃ´ng.";
    }

    private void SetRepresentative(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chá»n lÆ°á»£t thuÃª trÆ°á»›c.");
        _roomTenantService.SetRepresentative(assignment.Id);
        StatusMessage = "ÄÃ£ Ä‘áº·t ngÆ°á»i Ä‘áº¡i diá»‡n.";
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
        if (SelectedFeeType is null) throw new ValidationException("Chá»n loáº¡i phÃ­ trÆ°á»›c.");
        NewFeeType = new FeeType { Id = SelectedFeeType.Id, Name = SelectedFeeType.Name, DefaultCalculationType = SelectedFeeType.DefaultCalculationType, DefaultUnit = SelectedFeeType.DefaultUnit, DefaultUnitPrice = SelectedFeeType.DefaultUnitPrice, IsSystem = SelectedFeeType.IsSystem, IsActive = SelectedFeeType.IsActive };
        NotifyFormModes();
    }

    private void DeactivateFeeType()
    {
        if (SelectedFeeType is null) throw new ValidationException("Chá»n loáº¡i phÃ­ trÆ°á»›c.");
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
        if (SelectedRoomFeeConfig is null) throw new ValidationException("Chá»n cáº¥u hÃ¬nh phÃ­ trÆ°á»›c.");
        NewRoomFeeConfig = new RoomFeeConfig { Id = SelectedRoomFeeConfig.Id, RoomId = SelectedRoomFeeConfig.RoomId, FeeTypeId = SelectedRoomFeeConfig.FeeTypeId, CalculationType = SelectedRoomFeeConfig.CalculationType, UnitPrice = SelectedRoomFeeConfig.UnitPrice, FixedAmount = SelectedRoomFeeConfig.FixedAmount, Quantity = SelectedRoomFeeConfig.Quantity, Enabled = SelectedRoomFeeConfig.Enabled, Note = SelectedRoomFeeConfig.Note };
        NotifyFormModes();
    }

    private void DisableRoomFeeConfig()
    {
        if (SelectedRoomFeeConfig is null) throw new ValidationException("Chá»n cáº¥u hÃ¬nh phÃ­ trÆ°á»›c.");
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
        if (InvoiceRoomId <= 0) throw new ValidationException("Chá»n phÃ²ng Ä‘á»ƒ táº¡o hÃ³a Ä‘Æ¡n.");
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
        StatusMessage = $"ÄÃ£ táº¡o {count} hÃ³a Ä‘Æ¡n Ä‘á»§ dá»¯ liá»‡u.";
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
        if (SelectedInvoice!.RemainingAmount <= 0)
        {
            throw new ValidationException("HÃ³a Ä‘Æ¡n nÃ y Ä‘Ã£ Ä‘Æ°á»£c thanh toÃ¡n Ä‘á»§.");
        }

        _paymentService.Record(SelectedInvoice!.Id, NewPaymentAmount, NewPaymentMethod, DateTime.Today, NewPaymentNote);
        NewPaymentAmount = 0;
        NewPaymentNote = null;
        OnPropertyChanged(nameof(NewPaymentAmount));
        OnPropertyChanged(nameof(NewPaymentNote));
        Load();
        StatusMessage = "ÄÃ£ ghi nháº­n thanh toÃ¡n.";
    }

    private void FillRemainingPayment()
    {
        if (SelectedInvoice is null) return;
        if (SelectedInvoice.RemainingAmount <= 0)
        {
            StatusMessage = "HÃ³a Ä‘Æ¡n nÃ y Ä‘Ã£ Ä‘Æ°á»£c thanh toÃ¡n Ä‘á»§.";
            return;
        }

        NewPaymentAmount = SelectedInvoice.RemainingAmount;
        NewPaymentMethod = PaymentMethod.Cash;
        OnPropertyChanged(nameof(NewPaymentAmount));
        OnPropertyChanged(nameof(NewPaymentMethod));
    }

    private void SelectInvoiceForPayment(Invoice? invoice)
    {
        if (invoice is null)
        {
            return;
        }

        SelectedInvoice = invoice;
        if (invoice.RemainingAmount <= 0)
        {
            StatusMessage = "HÃ³a Ä‘Æ¡n nÃ y Ä‘Ã£ Ä‘Æ°á»£c thanh toÃ¡n Ä‘á»§.";
            return;
        }

        NewPaymentAmount = invoice.RemainingAmount;
        NewPaymentMethod = PaymentMethod.Cash;
        OnPropertyChanged(nameof(NewPaymentAmount));
        OnPropertyChanged(nameof(NewPaymentMethod));
    }

    private void CopyInvoice()
    {
        EnsureInvoiceSelected();
        Clipboard.SetText(_invoiceService.CopyText(SelectedInvoice!.Id));
        StatusMessage = "ÄÃ£ sao chÃ©p hÃ³a Ä‘Æ¡n.";
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
        StatusMessage = $"ÄÃ£ sao lÆ°u: {path}";
    }

    private void Restore()
    {
        var dialog = new OpenFileDialog { Filter = "SQLite backup (*.sqlite)|*.sqlite|All files (*.*)|*.*", Title = "Chá»n báº£n sao lÆ°u" };
        if (dialog.ShowDialog() == true)
        {
            var confirm = MessageBox.Show("KhÃ´i phá»¥c báº£n sao lÆ°u nÃ y? Dá»¯ liá»‡u hiá»‡n táº¡i sáº½ bá»‹ thay tháº¿.", "KhÃ´i phá»¥c dá»¯ liá»‡u", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _backupService.RestoreFrom(dialog.FileName);
            Load();
            StatusMessage = "ÄÃ£ khÃ´i phá»¥c dá»¯ liá»‡u.";
        }
    }

    private void SeedDemoData()
    {
        _demoDataService.Seed();
        BillingMonth = "2026-04";
        DashboardYear = 2026;
        DashboardRange = "TÃ¹y chá»n thÃ¡ng";
        Load();
    }

    private void EnsureInvoiceSelected()
    {
        if (SelectedInvoice is null) throw new ValidationException("Chá»n hÃ³a Ä‘Æ¡n trÆ°á»›c.");
    }

    private void Run(Action action)
    {
        try
        {
            action();
            if (string.IsNullOrWhiteSpace(StatusMessage)) StatusMessage = "ÄÃ£ lÆ°u.";
        }
        catch (Exception ex)
        {
            var message = ErrorMessageMapper.ToUserMessage(ex);
            StatusMessage = message;
            MessageBox.Show(message, "Quáº£n lÃ½ nhÃ  trá»", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private (string StartMonth, string EndMonth) GetDashboardRange()
    {
        var selectedMonth = DateTime.TryParse($"{BillingMonth}-01", out var parsedMonth) ? parsedMonth : DateTime.Today;
        return DashboardRange switch
        {
            "ThÃ¡ng hiá»‡n táº¡i" => (DateTime.Today.ToString("yyyy-MM"), DateTime.Today.ToString("yyyy-MM")),
            "3 thÃ¡ng gáº§n nháº¥t" => (selectedMonth.AddMonths(-2).ToString("yyyy-MM"), selectedMonth.ToString("yyyy-MM")),
            "6 thÃ¡ng gáº§n nháº¥t" => (selectedMonth.AddMonths(-5).ToString("yyyy-MM"), selectedMonth.ToString("yyyy-MM")),
            "NÄƒm hiá»‡n táº¡i" => ($"{DashboardYear:0000}-01", $"{DashboardYear:0000}-12"),
            _ => (BillingMonth, BillingMonth)
        };
    }

    private void ApplyDashboardRangeDefaults()
    {
        if (DashboardRange == "ThÃ¡ng hiá»‡n táº¡i")
        {
            _billingMonth = DateTime.Today.ToString("yyyy-MM");
            _dashboardYear = DateTime.Today.Year;
            OnPropertyChanged(nameof(BillingMonth));
            OnPropertyChanged(nameof(DashboardYear));
        }
        else if (DashboardRange == "NÄƒm hiá»‡n táº¡i")
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

    private void ClearFilters()
    {
        RoomSearch = string.Empty;
        RoomRepresentativeSearch = string.Empty;
        RoomFilterPropertyId = 0;
        RoomFilterStatus = "Táº¥t cáº£";
        TenantSearch = string.Empty;
        TenantStatusFilter = "Táº¥t cáº£";
        AssignmentFilterPropertyId = 0;
        AssignmentFilterRoomId = 0;
        AssignmentHistoryFilter = "ÄÃ£ káº¿t thÃºc";
        InvoiceSearch = string.Empty;
        InvoiceFilterMonth = SelectedBillingMonth;
        InvoiceFilterYear = SelectedBillingYear;
        InvoiceFilterPropertyId = 0;
        InvoiceFilterRoomId = 0;
        InvoiceFilterStatus = "Táº¥t cáº£";
        PaymentSearch = string.Empty;
        PaymentFilterMonth = SelectedBillingMonth;
        PaymentFilterYear = SelectedBillingYear;
        PaymentFilterPropertyId = 0;
        PaymentFilterRoomId = 0;
        PaymentFilterMethod = "Táº¥t cáº£";
        MeterFilterMonth = SelectedBillingMonth;
        MeterFilterYear = SelectedBillingYear;
        MeterFilterPropertyId = 0;
        MeterFilterRoomId = 0;
        MeterFilterFeeTypeId = 0;
        RoomFeeSearch = string.Empty;
        RoomFeePropertyFilterId = 0;
        RoomFeeRoomFilterId = 0;
        RoomFeeFeeTypeFilterId = 0;
        RoomFeeStatusFilter = "Äang Ã¡p dá»¥ng";
        RefreshAllFilters();
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

        if (RoomFilterStatus != "Táº¥t cáº£")
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
        if (TenantStatusFilter == "Äang thuÃª")
        {
            tenants = tenants.Where(HasActiveAssignment);
        }
        else if (TenantStatusFilter == "ChÆ°a phÃ¢n phÃ²ng")
        {
            tenants = tenants.Where(x => !HasAnyAssignment(x));
        }
        else if (TenantStatusFilter == "ÄÃ£ tá»«ng thuÃª")
        {
            tenants = tenants.Where(x => !HasActiveAssignment(x) && HasEndedAssignment(x));
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

        IEnumerable<RoomTenant> history = assignments;
        if (AssignmentHistoryFilter == "ÄÃ£ káº¿t thÃºc")
        {
            history = history.Where(x => x.Status == RoomTenantStatus.Ended);
        }
        else if (AssignmentHistoryFilter == "Äang thuÃª")
        {
            history = history.Where(x => x.Status == RoomTenantStatus.Active);
        }

        Replace(FilteredAssignmentHistory, history.OrderByDescending(x => x.StartDate));
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

        if (InvoiceFilterStatus != "Táº¥t cáº£")
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

        if (PaymentFilterMethod != "Táº¥t cáº£")
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
        if (RoomFeeEnabledOnly) configs = configs.Where(x => x.Enabled);
        if (RoomFeeStatusFilter == "Äang Ã¡p dá»¥ng") configs = configs.Where(x => x.Enabled);
        if (RoomFeeStatusFilter == "Ngá»«ng Ã¡p dá»¥ng") configs = configs.Where(x => !x.Enabled);
        if (!string.IsNullOrWhiteSpace(RoomFeeSearch))
        {
            configs = configs.Where(x => Contains(x.PropertyName, RoomFeeSearch) || Contains(x.RoomName, RoomFeeSearch) || Contains(x.FeeTypeName, RoomFeeSearch));
        }
        Replace(FilteredRoomFeeConfigs, configs);
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
        if (id <= 0) throw new ValidationException("Chá»n dÃ²ng cáº§n sá»­a trÆ°á»›c khi lÆ°u thay Ä‘á»•i.");
    }

    private static bool Contains(string? value, string search)
    {
        return value?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true;
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

