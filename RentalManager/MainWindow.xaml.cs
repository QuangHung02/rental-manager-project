using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using RentalManager.DTOs;
using RentalManager.ViewModels;

namespace RentalManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void AssignmentTenantSearchBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        OpenAssignmentTenantDropdown();
    }

    private void AssignmentTenantSearchBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpenAssignmentTenantDropdown();
    }

    private void AssignmentTenantSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (AssignmentTenantSearchBox.IsKeyboardFocusWithin &&
            DataContext is MainViewModel { IsAssignmentDrawerOpen: true })
        {
            OpenAssignmentTenantDropdown();
        }
    }

    private void AssignmentDrawerBody_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsDescendantOf(e.OriginalSource as DependencyObject, AssignmentTenantSearchBox) ||
            IsDescendantOf(e.OriginalSource as DependencyObject, AssignmentTenantDropdown))
        {
            return;
        }

        CloseAssignmentTenantDropdown();
    }

    private void OpenAssignmentTenantDropdown()
    {
        if (DataContext is MainViewModel viewModel)
        {
            PositionAssignmentTenantDropdown();
            viewModel.IsAssignmentTenantDropdownOpen = true;
        }
    }

    private void CloseAssignmentTenantDropdown()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.IsAssignmentTenantDropdownOpen = false;
        }
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender) ||
            DataContext is not MainViewModel { IsDrawerOpen: true } viewModel)
        {
            return;
        }

        viewModel.CloseDrawerCommand.Execute(null);
    }

    private void DashboardPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedItem = BillingTab;
        BillingTabs.SelectedItem = InvoicesTab;
    }

    private void MonthlyOverviewContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var shouldStack = e.NewSize.Width < 980;

        MonthlyOverviewTablesFirstColumn.Width = new GridLength(1, GridUnitType.Star);
        MonthlyOverviewTablesSecondColumn.Width = shouldStack
            ? new GridLength(0)
            : new GridLength(1, GridUnitType.Star);
        MonthlyOverviewTablesSecondRow.Height = shouldStack
            ? GridLength.Auto
            : new GridLength(0);

        Grid.SetRow(DashboardPaymentsGroup, shouldStack ? 1 : 0);
        Grid.SetColumn(DashboardPaymentsGroup, shouldStack ? 0 : 1);
        Grid.SetColumnSpan(DashboardPaymentsGroup, shouldStack ? 2 : 1);
        Grid.SetColumnSpan(DashboardInvoicesGroup, shouldStack ? 2 : 1);
    }

    private void InvoicesPageContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var shouldStack = e.NewSize.Width < 980;

        InvoicesWorkspaceFirstColumn.Width = new GridLength(shouldStack ? 1 : 2, GridUnitType.Star);
        InvoicesWorkspaceSecondColumn.Width = shouldStack
            ? new GridLength(0)
            : new GridLength(1.2, GridUnitType.Star);
        InvoicesWorkspaceSecondRow.Height = shouldStack
            ? GridLength.Auto
            : new GridLength(0);

        Grid.SetRow(InvoiceDetailGroup, shouldStack ? 1 : 0);
        Grid.SetColumn(InvoiceDetailGroup, shouldStack ? 0 : 1);
        Grid.SetColumnSpan(InvoiceDetailGroup, shouldStack ? 2 : 1);
        Grid.SetColumnSpan(InvoicesListGroup, shouldStack ? 2 : 1);
    }

    private void InvoiceReadinessRows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel &&
            sender is DataGrid { SelectedItem: InvoiceReadinessRow row } &&
            viewModel.SelectInvoiceReadinessRoomCommand.CanExecute(row))
        {
            viewModel.SelectInvoiceReadinessRoomCommand.Execute(row);
        }
    }

    private static bool IsDescendantOf(DependencyObject? source, DependencyObject target)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject source)
    {
        if (source is Visual or Visual3D)
        {
            var visualParent = VisualTreeHelper.GetParent(source);
            if (visualParent is not null)
            {
                return visualParent;
            }
        }

        return LogicalTreeHelper.GetParent(source);
    }

    private void PositionAssignmentTenantDropdown()
    {
        if (!AssignmentTenantSearchBox.IsLoaded || !AssignmentDrawerBody.IsLoaded)
        {
            return;
        }

        var inputBottomLeft = AssignmentTenantSearchBox
            .TransformToAncestor(AssignmentDrawerBody)
            .Transform(new Point(0, AssignmentTenantSearchBox.ActualHeight));

        Canvas.SetLeft(AssignmentTenantDropdown, inputBottomLeft.X);
        Canvas.SetTop(AssignmentTenantDropdown, inputBottomLeft.Y);
    }
}
