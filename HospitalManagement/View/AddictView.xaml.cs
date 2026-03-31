using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace HospitalManagement.View
{
    public sealed partial class AddictView : UserControl
    {
        public ViewModel.AddictViewModel ViewModel { get; set; }

        public AddictView()
        {
            this.InitializeComponent();
        }

        private async void OnNotifyPoliceClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && sender is Button btn && btn.Tag is int patientId)
            {
                string reportText = ViewModel.GetPoliceReportMessage(patientId);

                ContentDialog dialog = new ContentDialog
                {
                    Title = "LAW ENFORCEMENT ALERT",
                    XamlRoot = this.XamlRoot,
                    
                    Content = new ScrollViewer 
                    {
                        MaxHeight = 500, 
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
                                LineHeight = 22
                            }
                        }
                    },
                    
                    CloseButtonText = "Acknowledge & Close",
                    DefaultButton = ContentDialogButton.Close, 
                    
                    RequestedTheme = ElementTheme.Light
                };

                dialog.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);

                dialog.Resources["ContentDialogCloseButtonBackground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 38, 38)); 
                dialog.Resources["ContentDialogCloseButtonForeground"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                
                dialog.Resources["ContentDialogCloseButtonBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 185, 28, 28));

                await dialog.ShowAsync();
            }
        }
    }
}
