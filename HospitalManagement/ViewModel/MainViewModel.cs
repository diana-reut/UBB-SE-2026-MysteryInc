using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        //private int _ghostSightingCount = 0;
        //private DateTime _lastResetTime = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;
         
        //I SAW A GHOST LOGIC
        private readonly GhostService _ghostService;

        private bool _isExorcismAlertVisible;
        public bool IsExorcismAlertVisible
        {
            get => _isExorcismAlertVisible;
            set { _isExorcismAlertVisible = value; OnPropertyChanged(); }
        }

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
            //GhostSightingCommand = new RelayCommand(RecordGhostSighting);

            _ghostService = GhostService.Instance;
            _ghostService.ExorcismTriggered += (s, e) => IsExorcismAlertVisible = true;
            GhostSightingCommand = new RelayCommand(() => _ghostService.SawAGhost());
            IsExorcismAlertVisible = _ghostService.IsExorcismTriggered();
        }

        private void RedirectToAdminRole() => CurrentView = "AdminDashboard";
        private void RedirectToStaffRole() => CurrentView = "StaffDashboard";
        private void RedirectToPharmacistRole() => CurrentView = "PharmacistDashboard";

        //// SV23
        //private void RecordGhostSighting()
        //{
        //    if ((DateTime.Now - _lastResetTime).TotalHours > 24)
        //    {
        //        _ghostSightingCount = 0;
        //        _lastResetTime = DateTime.Now;
        //    }

        //    _ghostSightingCount++;

        //    if (_ghostSightingCount > 3)
        //    {
        //        TriggerExorcismAlert();
        //    }
        //}

        //private void TriggerExorcismAlert()
        //{
        //   //this is an example
        //    string message = "CRITICAL PARANORMAL ACTIVITY DETECTED: Multiple sightings confirmed. Please CALL THE PRIEST immediately.";
        //    Console.WriteLine(message); 
        //}

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = null;
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}