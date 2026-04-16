using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;

namespace HospitalManagement.View;

internal sealed partial class AddictView : UserControl
{
    public ViewModel.AddictViewModel ViewModel { get; set; } = null!;

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool MessageBeep(uint uType);

    public AddictView()
    {
        InitializeComponent();
    }

    private async void OnNotifyPoliceClickedAsync(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null && sender is Button btn && btn.Tag is int patientId)
        {
            string reportText = ViewModel.GetPoliceReportMessage(patientId);

            var dialog = new ContentDialog
            {
                Title = "LAW ENFORCEMENT ALERT",
                XamlRoot = Content.XamlRoot,

                Content = new ScrollViewer
                {
                    MaxHeight = 450,
                    Content = new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 250, 250, 250)),
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 10, 0, 10),

                        Child = new TextBlock
                        {
                            Text = reportText,
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                            FontSize = 14,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                            LineHeight = 22,
                        },
                    },
                },

                CloseButtonText = "Cancel",

                PrimaryButtonText = "Send Alert",

                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = ElementTheme.Light,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            };

            dialog.Resources["ContentDialogPrimaryButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 38, 38));
            dialog.Resources["ContentDialogPrimaryButtonForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
            dialog.Resources["ContentDialogPrimaryButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 185, 28, 28));

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    Console.Beep(1200, 200);
                    Console.Beep(800, 200);
                    Console.Beep(1200, 200);
                    Console.Beep(800, 200);
                    Console.Beep(1500, 500);
                });

                ViewModel.RemoveFlaggedPatient(patientId);
            }
        }
    }
}
