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
    private string _invoiceFilterStatus = "Tất cả";
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
    private int _assignmentNewPropertyId;
    private string _assignmentTenantSearchText = string.Empty;
    private Tenant? _selectedAssignmentTenant;
    private bool _isUpdatingAssignmentTenantText;
    private DateTime _assignmentEndDate = DateTime.Today;

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
    public ObservableCollection<Room> AssignmentRoomOptions { get; } = new();
    public ObservableCollection<Tenant> AssignmentTenantOptions { get; } = new();
    public ObservableCollection<FeeType> FeeTypes { get; } = new();
    public ObservableCollection<FeeType> MeterReadingFeeTypeOptions { get; } = new();
    public ObservableCollection<Room> MeterReadingRoomOptions { get; } = new();
    public ObservableCollection<RoomFeeConfig> RoomFeeConfigs { get; } = new();
    public ObservableCollection<RoomFeeConfig> FilteredRoomFeeConfigs { get; } = new();
    public ObservableCollection<Room> RoomFeeFilterRoomOptions { get; } = new();
    public ObservableCollection<Room> RoomFeeFormRoomOptions { get; } = new();
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

    public IReadOnlyList<string> DashboardRangeOptions { get; } = new[] { "Tháng hiện tại", "3 tháng gần nhất", "6 tháng gần nhất", "Năm hiện tại", "Tùy chọn tháng" };
    public IReadOnlyList<int> MonthOptions { get; } = Enumerable.Range(1, 12).ToList();
    public ObservableCollection<int> YearOptions { get; } = new();
    public IReadOnlyList<string> RoomStatusFilterOptions { get; } = new[] { "Tất cả", "Đang cho thuê", "Đang trống" };
    public IReadOnlyList<string> TenantStatusFilterOptions { get; } = new[] { "Tất cả", "Đang thuê", "Chưa phân phòng", "Đã từng thuê" };
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
                Replace(SelectedInvoiceItems, value?.Items ?? Enumerable.Empty<InvoiceItem>());
                Replace(SelectedInvoicePayments, value?.Payments ?? Enumerable.Empty<Payment>());
                if (value is not null && value.RemainingAmount > 0 && NewPaymentAmount == 0)
                {
                    NewPaymentAmount = value.RemainingAmount;
                    NewPaymentMethod = PaymentMethod.Cash;
                    OnPropertyChanged(nameof(NewPaymentAmount));
                    OnPropertyChanged(nameof(NewPaymentMethod));
                }
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

                RefreshAssignmentTenantOptions();
                ClearAssignmentTenantSelectionIfTextNoLongerMatches();
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

            if (value is null)
            {
                return;
            }

            SetAssignmentTenantSearchText(value.AssignmentDisplayText);
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
            OnPropertyChanged(nameof(NewRoomFeeFeeTypeId));
            if (!NewRoomFeeUseDefaultPrice)
            {
                FillRoomFeeCustomPriceFromDefault();
            }

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
    public int InvoiceFilterPropertyId { get => _invoiceFilterPropertyId; set { if (SetProperty(ref _invoiceFilterPropertyId, value)) RefreshInvoiceFilters(); } }
    public int InvoiceFilterRoomId { get => _invoiceFilterRoomId; set { if (SetProperty(ref _invoiceFilterRoomId, value)) RefreshInvoiceFilters(); } }
    public string InvoiceFilterStatus { get => _invoiceFilterStatus; set { if (SetProperty(ref _invoiceFilterStatus, value)) RefreshInvoiceFilters(); } }

    public int PaymentFilterMonth { get => _paymentFilterMonth; set { if (SetProperty(ref _paymentFilterMonth, value)) RefreshPaymentFilters(); } }
    public int PaymentFilterYear { get => _paymentFilterYear; set { if (SetProperty(ref _paymentFilterYear, value)) RefreshPaymentFilters(); } }
    public int PaymentFilterPropertyId { get => _paymentFilterPropertyId; set { if (SetProperty(ref _paymentFilterPropertyId, value)) RefreshPaymentFilters(); } }
    public int PaymentFilterRoomId { get => _paymentFilterRoomId; set { if (SetProperty(ref _paymentFilterRoomId, value)) RefreshPaymentFilters(); } }
    public string PaymentFilterMethod { get => _paymentFilterMethod; set { if (SetProperty(ref _paymentFilterMethod, value)) RefreshPaymentFilters(); } }

    public int MeterFilterMonth { get => _meterFilterMonth; set { if (SetProperty(ref _meterFilterMonth, value)) { RefreshMeterReadingFilters(); LoadMeterReadingFormForSelection(); } } }
    public int MeterFilterYear { get => _meterFilterYear; set { if (SetProperty(ref _meterFilterYear, value)) { RefreshMeterReadingFilters(); LoadMeterReadingFormForSelection(); } } }
    public int MeterFilterPropertyId { get => _meterFilterPropertyId; set { if (SetProperty(ref _meterFilterPropertyId, value)) RefreshMeterReadingFilters(); } }
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
            if (wasUsingDefault)
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
        get => NewRoomFeeConfig.CalculationType != CalculationType.Manual && GetRoomFeeCustomPrice() is null;
        set
        {
            if (NewRoomFeeConfig.CalculationType == CalculationType.Manual)
            {
                return;
            }

            if (value)
            {
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

    public string DatabasePath => _backupService.DatabasePath;
    public string PropertyFormMode => NewProperty.Id > 0 ? $"Đang sửa: {NewProperty.Name}" : "Đang thêm mới";
    public string RoomFormMode => NewRoom.Id > 0 ? $"Đang sửa: {NewRoom.RoomName}" : "Đang thêm mới";
    public string TenantFormMode => NewTenant.Id > 0 ? $"Đang sửa: {NewTenant.FullName}" : "Đang thêm mới";
    public string FeeTypeFormMode => NewFeeType.Id > 0 ? $"Đang sửa: {NewFeeType.DisplayName}" : "Đang thêm mới";
    public string SelectedFeeTypeToggleActionText => SelectedFeeType?.ToggleActionText ?? "Ngừng";
    public string RoomFeeFormMode => NewRoomFeeConfig.Id > 0 ? $"Đang sửa: {RoomFeeEditTitle}" : "Đang thêm mới";
    public string SelectedRoomFeeToggleActionText => SelectedRoomFeeConfig?.ToggleActionText ?? "Ngừng";
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
    public RelayCommand CancelPropertyEditCommand { get; }
    public RelayCommand CancelRoomEditCommand { get; }
    public RelayCommand CancelTenantEditCommand { get; }
    public RelayCommand CancelFeeTypeEditCommand { get; }
    public RelayCommand CancelRoomFeeConfigEditCommand { get; }
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
        Replace(PropertyFilterOptions, new[] { new PropertyFilterOption { Id = 0, Name = "Tất cả nhà / khu trọ" } }.Concat(Properties.Select(x => new PropertyFilterOption { Id = x.Id, Name = x.Name })));
        Replace(Rooms, _roomService.GetAll());
        RefreshRoomFeeFilterRoomOptions();
        RefreshRoomFeeFormRoomOptions();
        RefreshMeterReadingRoomOptions();
        RefreshAssignmentRoomOptions();
        Replace(Tenants, _tenantService.GetAll());
        RefreshAssignmentTenantOptions();
        Replace(RoomTenants, _roomTenantService.GetAll());
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

    private void CancelPropertyEdit()
    {
        NewProperty = new Property();
        NotifyFormModes();
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
            SelectedAssignmentTenant.Id != NewRoomTenant.TenantId ||
            !string.Equals(AssignmentTenantSearchText, SelectedAssignmentTenant.AssignmentDisplayText, StringComparison.CurrentCulture))
        {
            throw new ValidationException("Vui lòng chọn người thuê hợp lệ.");
        }

        _roomTenantService.Save(NewRoomTenant);
        NewRoomTenant = new RoomTenant();
        AssignmentNewPropertyId = 0;
        SelectedAssignmentTenant = null;
        SetAssignmentTenantSearchText(string.Empty);
        RefreshAssignmentRoomOptions();
        OnPropertyChanged(nameof(NewRoomTenant));
        Load();
    }

    private void EndAssignment(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chọn lượt thuê trước.");
        var roomId = assignment.RoomId;
        _roomTenantService.EndAssignment(assignment.Id, AssignmentEndDate);
        Load();
        StatusMessage = RoomNeedsRepresentative(roomId)
            ? "Đã kết thúc thuê. Phòng này chưa có người đại diện."
            : "Đã kết thúc thuê.";
    }

    private void ChangeRoom(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chọn lượt thuê trước.");
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
        StatusMessage = "Đã chuyển phòng thành công.";
    }

    private void SetRepresentative(RoomTenant? assignment)
    {
        if (assignment is null) throw new ValidationException("Chọn lượt thuê trước.");
        _roomTenantService.SetRepresentative(assignment.Id);
        StatusMessage = "Đã đặt người đại diện.";
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
        if (SelectedInvoice!.RemainingAmount <= 0)
        {
            throw new ValidationException("Hóa đơn này đã được thanh toán đủ.");
        }

        _paymentService.Record(SelectedInvoice!.Id, NewPaymentAmount, NewPaymentMethod, DateTime.Today, NewPaymentNote);
        NewPaymentAmount = 0;
        NewPaymentNote = null;
        OnPropertyChanged(nameof(NewPaymentAmount));
        OnPropertyChanged(nameof(NewPaymentNote));
        Load();
        StatusMessage = "Đã ghi nhận thanh toán.";
    }

    private void FillRemainingPayment()
    {
        if (SelectedInvoice is null) return;
        if (SelectedInvoice.RemainingAmount <= 0)
        {
            StatusMessage = "Hóa đơn này đã được thanh toán đủ.";
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
            StatusMessage = "Hóa đơn này đã được thanh toán đủ.";
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

    private void RefreshAssignmentTenantOptions()
    {
        var text = AssignmentTenantSearchText.Trim();
        var tenants = Tenants.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            tenants = tenants.Where(x => TenantMatchesSearch(x, text));
        }

        Replace(AssignmentTenantOptions, tenants.OrderBy(x => x.FullName).ThenBy(x => x.Phone));
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
            tenants = tenants.Where(HasActiveAssignment);
        }
        else if (TenantStatusFilter == "Chưa phân phòng")
        {
            tenants = tenants.Where(x => !HasAnyAssignment(x));
        }
        else if (TenantStatusFilter == "Đã từng thuê")
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
        if (AssignmentHistoryFilter == "Đã kết thúc")
        {
            history = history.Where(x => x.Status == RoomTenantStatus.Ended);
        }
        else if (AssignmentHistoryFilter == "Đang thuê")
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

