using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    public class PharmacistViewModel : INotifyPropertyChanged
    {
        private readonly GhostService _ghostService;


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

        private bool _isExorcismAlertVisible;
        public bool IsExorcismAlertVisible
        {
            get => _isExorcismAlertVisible;
            set { _isExorcismAlertVisible = value; OnPropertyChanged(); }
        }


        public ICommand ShowPrescriptionsCommand { get; }
        public ICommand ShowAddictsCommand { get; }
        public ICommand ReportGhostCommand { get; }
        public PharmacistViewModel()
        {
            _ghostService = GhostService.Instance;
            _ghostService.ExorcismTriggered += (s, e) => IsExorcismAlertVisible = true;


         
            ShowPrescriptionsCommand = new RelayCommand(ShowPrescriptions);
            ShowAddictsCommand = new RelayCommand(ShowAddicts);

            ReportGhostCommand = new RelayCommand(ReportGhost);
            CurrentView = "Prescriptions";

            IsExorcismAlertVisible = _ghostService.IsExorcismTriggered();
        }

        private void ShowPrescriptions() => CurrentView = "Prescriptions";
        private void ShowAddicts() => CurrentView = "Addicts";

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ReportGhost()
        {
            _ghostService.SawAGhost();
            
            //System.Diagnostics.Debug.WriteLine($">> GHOST REPORTED FROM PHARMACIST AT {DateTime.Now} <<");
        }
    }
}
