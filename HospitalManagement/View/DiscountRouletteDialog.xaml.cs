using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HospitalManagement.View
{
    public sealed partial class DiscountRouletteDialog : ContentDialog, INotifyPropertyChanged
    {
        private decimal _basePrice;
        private int _discountPercentage;
        private decimal _finalPrice;
        private bool _showResult;
        private bool _isSpinning;
        private bool _isPrimaryButtonEnabled;
        
        private readonly int[] _discountOptions = { 0, 10, 25, 50, 100 };

        public decimal BasePrice
        {
            get => _basePrice;
            set { _basePrice = value; OnPropertyChanged(); }
        }

        public int DiscountPercentage
        {
            get => _discountPercentage;
            set { _discountPercentage = value; OnPropertyChanged(); }
        }

        public decimal FinalPrice
        {
            get => _finalPrice;
            set { _finalPrice = value; OnPropertyChanged(); }
        }

        public Visibility ShowResult
        {
            get => _showResult ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool IsPrimaryButtonEnabled
        {
            get => _isPrimaryButtonEnabled;
            set { _isPrimaryButtonEnabled = value; OnPropertyChanged(); }
        }

        public int SelectedDiscountPercentage { get; private set; }
        public Action<int, decimal> OnSpinComplete { get; set; }

        public DiscountRouletteDialog()
        {
            this.InitializeComponent();
            this.DataContext = this;
            _isSpinning = false;
            _isPrimaryButtonEnabled = true;
        }

        public void Initialize(decimal basePrice)
        {
            BasePrice = basePrice;
            _showResult = false;
            SelectedDiscountPercentage = 0;
            IsPrimaryButtonEnabled = true;
        }

        public async void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSpinning)
                return;

            await SpinWheel();
        }

        private async Task SpinWheel()
        {
            _isSpinning = true;
            IsPrimaryButtonEnabled = false;

            // Simulate spinning animation (3 seconds)
            await Task.Delay(3000);

            // Select random discount
            Random rand = new Random();
            int randomIndex = rand.Next(_discountOptions.Length);
            SelectedDiscountPercentage = _discountOptions[randomIndex];

            // Calculate final price
            FinalPrice = BasePrice * (1 - (SelectedDiscountPercentage / 100m));

            // Show result
            _showResult = true;
            OnPropertyChanged(nameof(ShowResult));
            OnPropertyChanged(nameof(DiscountPercentage));
            OnPropertyChanged(nameof(FinalPrice));

            // Wait for user to see result (2 seconds)
            await Task.Delay(2000);

            // Close dialog and invoke callback
            OnSpinComplete?.Invoke(SelectedDiscountPercentage, FinalPrice);
            this.Hide();

            _isSpinning = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
