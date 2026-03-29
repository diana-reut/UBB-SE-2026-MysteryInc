using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HospitalManagement.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        private int _ghostSightingCount = 0;
        private DateTime _lastResetTime = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;


        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        
        public ICommand RedirectToAdminRoleCommand { get; }
        public ICommand RedirectToStaffRoleCommand { get; }
        public ICommand RedirectToPharmacistRoleCommand { get; }
        public ICommand GhostSightingCommand { get; }

        public MainWindowViewModel()
        {
            RedirectToAdminRoleCommand = new RelayCommand(RedirectToAdminRole);
            RedirectToStaffRoleCommand = new RelayCommand(RedirectToStaffRole);
            RedirectToPharmacistRoleCommand = new RelayCommand(RedirectToPharmacistRole);
            GhostSightingCommand = new RelayCommand(RecordGhostSighting);
        }

        private void RedirectToAdminRole() => CurrentView = "AdminDashboard";
        private void RedirectToStaffRole() => CurrentView = "StaffDashboard";
        private void RedirectToPharmacistRole() => CurrentView = "PharmacistDashboard";

        // SV23
        private void RecordGhostSighting()
        {
            if ((DateTime.Now - _lastResetTime).TotalHours > 24)
            {
                _ghostSightingCount = 0;
                _lastResetTime = DateTime.Now;
            }

            _ghostSightingCount++;

            if (_ghostSightingCount > 3)
            {
                TriggerExorcismAlert();
            }
        }

        private void TriggerExorcismAlert()
        {
           //this is an example
            string message = "CRITICAL PARANORMAL ACTIVITY DETECTED: Multiple sightings confirmed. Please CALL THE PRIEST immediately.";
            Console.WriteLine(message); 
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}