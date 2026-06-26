using System.Windows;
using System.Windows.Controls;
using RentalManager.DTOs;

namespace RentalManager;

public sealed class UpdateDialog : Window
{
    public UpdateDialog(AppUpdateInfo update)
    {
        Title = "RentalManager update available";
        Width = 520;
        Height = 420;
        MinWidth = 420;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;

        var root = new DockPanel { Margin = new Thickness(18) };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var updateButton = new Button
        {
            Content = "Update now",
            MinWidth = 100,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        updateButton.Click += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        var laterButton = new Button
        {
            Content = "Later",
            MinWidth = 88,
            Height = 34,
            IsCancel = true
        };
        laterButton.Click += (_, _) =>
        {
            DialogResult = false;
            Close();
        };

        actions.Children.Add(updateButton);
        actions.Children.Add(laterButton);
        DockPanel.SetDock(actions, Dock.Bottom);
        root.Children.Add(actions);

        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = $"A new version is available: {update.Version}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        body.Children.Add(new TextBlock
        {
            Text = "Release notes",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var releaseNotes = string.IsNullOrWhiteSpace(update.ReleaseNotes)
            ? "No release notes were provided for this version."
            : update.ReleaseNotes.Trim();

        body.Children.Add(new TextBox
        {
            Text = releaseNotes,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MinHeight = 180
        });

        root.Children.Add(body);
        Content = root;
    }
}
