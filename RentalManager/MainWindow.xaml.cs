using System.Windows;
using RentalManager.ViewModels;

namespace RentalManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
