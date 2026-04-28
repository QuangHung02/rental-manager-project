using System.Windows;
using RentalManager.ViewModels;

namespace RentalManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ApplyWorkflowTabOrder();
        DataContext = new MainViewModel();
    }

    private void ApplyWorkflowTabOrder()
    {
        var orderedTabs = new[]
        {
            DashboardTab,
            PropertiesTab,
            RoomsTab,
            TenantsTab,
            AssignmentsTab,
            FeeTypesTab,
            RoomFeesTab,
            MeterReadingsTab,
            InvoicesTab,
            PaymentsTab,
            SettingsTab
        };

        MainTabs.Items.Clear();
        foreach (var tab in orderedTabs)
        {
            MainTabs.Items.Add(tab);
        }
    }
}
