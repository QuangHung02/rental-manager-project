using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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
