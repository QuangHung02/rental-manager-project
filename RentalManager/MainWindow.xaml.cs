using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using RentalManager.Models;
using RentalManager.ViewModels;

namespace RentalManager;

public partial class MainWindow : Window
{
    private bool _isCommittingAssignmentTenantSelection;
    private TextBox? _assignmentTenantTextBox;

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
            SettingsTab,
            AutomationTab
        };

        MainTabs.Items.Clear();
        foreach (var tab in orderedTabs)
        {
            MainTabs.Items.Add(tab);
        }
    }

    private void AssignmentTenantComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
        }
    }

    private void AssignmentTenantComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        if (!comboBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            comboBox.Focus();
        }

        comboBox.IsDropDownOpen = true;
    }

    private void AssignmentTenantComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        comboBox.ApplyTemplate();
        if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
        {
            return;
        }

        if (_assignmentTenantTextBox is not null)
        {
            _assignmentTenantTextBox.TextChanged -= AssignmentTenantTextBox_TextChanged;
        }

        _assignmentTenantTextBox = textBox;
        _assignmentTenantTextBox.TextChanged += AssignmentTenantTextBox_TextChanged;
    }

    private void AssignmentTenantTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isCommittingAssignmentTenantSelection || DataContext is not MainViewModel viewModel || sender is not TextBox textBox)
        {
            return;
        }

        viewModel.AssignmentTenantSearchText = textBox.Text;
        AssignmentTenantComboBox.IsDropDownOpen = true;
    }

    private void AssignmentTenantComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (comboBox.SelectedItem is not Tenant tenant)
        {
            if (viewModel.SelectedAssignmentTenant is null && string.IsNullOrWhiteSpace(viewModel.AssignmentTenantSearchText))
            {
                comboBox.Text = string.Empty;
                if (_assignmentTenantTextBox is not null)
                {
                    _assignmentTenantTextBox.Text = string.Empty;
                }
            }

            return;
        }

        _isCommittingAssignmentTenantSelection = true;
        viewModel.SelectedAssignmentTenant = tenant;

        Dispatcher.BeginInvoke(() =>
        {
            comboBox.Text = tenant.AssignmentDisplayText;
            if (_assignmentTenantTextBox is not null)
            {
                _assignmentTenantTextBox.Text = tenant.AssignmentDisplayText;
                _assignmentTenantTextBox.CaretIndex = tenant.AssignmentDisplayText.Length;
            }

            _isCommittingAssignmentTenantSelection = false;
        }, DispatcherPriority.Background);
    }
}
