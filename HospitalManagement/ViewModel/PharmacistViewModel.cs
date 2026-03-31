using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HospitalManagement.ViewModel
{
    public class PharmacistViewModel : INotifyPropertyChanged
    {
        private string _currentView;
        //cand voi avea ghost: private readonly GhostService _ghostService;
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
        public ICommand ReportGhostCommand { get; }
        public PharmacistViewModel()
        {
            // _ghostService = new GhostService(); 
            ShowPrescriptionsCommand = new RelayCommand(ShowPrescriptions);
            ShowAddictsCommand = new RelayCommand(ShowAddicts);

            ReportGhostCommand = new RelayCommand(ReportGhost);
            CurrentView = "Prescriptions";
        }

        private void ShowPrescriptions() => CurrentView = "Prescriptions";
        private void ShowAddicts() => CurrentView = "Addicts";

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ReportGhost()
        {
            //_ghostService.RegisterGhostSighting(DateTime.Now);
            System.Diagnostics.Debug.WriteLine($">> GHOST REPORTED FROM PHARMACIST AT {DateTime.Now} <<");
        }
    }
}
