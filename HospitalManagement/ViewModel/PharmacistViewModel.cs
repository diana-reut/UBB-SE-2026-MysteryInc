using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HospitalManagement.ViewModel
{
    public class PharmacistViewModel : INotifyPropertyChanged
    {
        private string _currentView;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowPrescriptionsCommand { get; }
        public ICommand ShowAddictsCommand { get; }

        public PharmacistViewModel()
        {
            // Inițializăm comenzile
            ShowPrescriptionsCommand = new RelayCommand(ShowPrescriptions);
            ShowAddictsCommand = new RelayCommand(ShowAddicts);

            // Setăm View-ul default la deschidere
            CurrentView = "Prescriptions";
        }

        private void ShowPrescriptions() => CurrentView = "Prescriptions";
        private void ShowAddicts() => CurrentView = "Addicts";

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
