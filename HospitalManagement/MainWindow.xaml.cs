using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel; 

namespace HospitalManagement
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Preluãm primul element Container din XAML (Grid-ul tãu) ca sã aplicãm DataContext-ul.
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = new MainWindowViewModel();
            }
        }
    }
}
