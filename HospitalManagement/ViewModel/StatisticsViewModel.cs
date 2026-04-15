using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Service;
using Windows.Services.Maps;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Runtime.CompilerServices;

namespace HospitalManagement.ViewModel
{
    internal class StatisticsViewModel : INotifyPropertyChanged
    {
        private readonly StatisticsService _statisticsService;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ISeries> GenderSeries { get; set; } = new();
        public ObservableCollection<ISeries> AgeSeries { get; set; } = new();
        public Axis[] AgeXAxes { get; set; } = Array.Empty<Axis>();

        public ObservableCollection<string> MenuOptions { get; } = new() { 
            "Patient Distribution",
            "Consultation Source",
            "Top Diagnoses",
            "Top Medications",
            "Demographics"
        };

        private string _selectedStatistic;

        public string SelectedStatistic
        {
            get => _selectedStatistic;
            set
            {
                _selectedStatistic = value;
                OnPropertyChanged();
                LoadDataForSelection(value);
            }
        }

        private ISeries[] _currentSeries = Array.Empty<ISeries>();
        public ISeries[] CurrentSeries
        {
            get => _currentSeries;
            set { _currentSeries = value; OnPropertyChanged(); }
        }

        private Axis[] _xAxes = Array.Empty<Axis>();
        public Axis[] XAxes
        {
            get => _xAxes;
            set { _xAxes = value; OnPropertyChanged(); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ISeries> PatientDistributionSeries { get; set; } =new ObservableCollection<ISeries>();
        public ObservableCollection<ISeries> ConsultationSourceSeries { get; set; } = new ObservableCollection<ISeries>();
       
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public StatisticsViewModel(StatisticsService statisticsService)
        {
            this._statisticsService = statisticsService;
            SelectedStatistic = MenuOptions[0];
        }

        private void LoadDataForSelection(string selection)
        {
            ErrorMessage = string.Empty;
            
            switch (selection)
            {
                case "Patient Distribution":
                    LoadPatientDistribution();
                    break;
                case "Consultation Source": 
                    LoadConsultationSources(); 
                    break;
                case "Top Diagnoses": 
                    LoadTopDiagnoses(); 
                    break;
                case "Top Medications":
                    LoadTopMedications();
                    break;
                case "Demographics":
                    LoadDemographics();
                    break;
                default:
                    ErrorMessage = "Unknown statistic selected";
                    CurrentSeries = Array.Empty<ISeries>();
                    break;
            }
        }

        /// <summary>
        /// VM1: LoadPatientDistribution
        /// Calls StatisticsService.GetActiveVsArchivedRatio() and populates a pie chart 
        /// showing the ratio of active patients to those who are no longer under current care.
        /// </summary>
        public void LoadPatientDistribution()
        {
            try
            {
                var data = _statisticsService.GetActiveVsArchivedRatio();
                
                if (data == null || data.Count == 0)
                {
                    ErrorMessage = "No patient distribution data available";
                    CurrentSeries = Array.Empty<ISeries>();
                    return;
                }

                var series = new List<ISeries>();
                foreach (var kvp in data)
                {
                    if (kvp.Value >= 0) // Ensure non-negative values
                    {
                        series.Add(new PieSeries<int>
                        {
                            Name = kvp.Key ?? "Unknown",
                            Values = new[] { kvp.Value }
                        });
                    }
                }
                
                CurrentSeries = series.ToArray();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load patient distribution: {ex.Message}";
                CurrentSeries = Array.Empty<ISeries>();
            }
        }
        /// <summary>
        /// VM2: LoadConsultationSources
        /// Implements LoadConsultationSources() to format the distribution of ER visits vs. appointments 
        /// for graphical display with a pie chart. Maps raw Map keys (e.g., "ER", "Staff") into UI-friendly 
        /// labels (e.g., "Emergency Department", "Scheduled Appointments").
        /// </summary>
        private void LoadConsultationSources()
        {
            try
            {
                var data = _statisticsService.GetConsultationDistribution();

                if (data == null || data.Count == 0)
                {
                    ErrorMessage = "No consultation source data available";
                    CurrentSeries = Array.Empty<ISeries>();
                    return;
                }

                // Data Transformation: Map raw Map keys into UI-friendly labels with validation
                var series = new List<ISeries>();
                foreach (var kvp in data)
                {
                    if (kvp.Value >= 0) // Ensure non-negative values
                    {
                        series.Add(new PieSeries<int>
                        {
                            Name = MapConsultationSourceLabel(kvp.Key ?? "Unknown"),
                            Values = new[] { kvp.Value }
                        });
                    }
                }

                CurrentSeries = series.ToArray();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                // Error Handling: Populate ErrorMessage property to alert user
                ErrorMessage = $"Failed to load consultation sources: {ex.Message}";
                CurrentSeries = Array.Empty<ISeries>();
            }
        }

        /// <summary>
        /// Helper method to map consultation source keys to UI-friendly labels
        /// </summary>
        private string MapConsultationSourceLabel(string sourceType)
        {
            return sourceType switch
            {
                "ER" => "Emergency Department",
                "Emergency" => "Emergency Department",
                "Scheduled" => "Scheduled Appointments",
                "Appointment" => "Scheduled Appointments",
                _ => sourceType // Return original if no mapping found
            };
        }

        /// <summary>
        /// VM3: LoadTopDiagnoses
        /// Implements LoadTopDiagnoses() to fetch from StatisticsService.GetTopDiagnoses() 
        /// the most common clinical diagnoses for bar chart rendering.
        /// </summary>
        private void LoadTopDiagnoses()
        {
            try
            {
                var data = _statisticsService.GetTopDiagnoses();

                if (data == null || data.Count == 0)
                {
                    ErrorMessage = "No diagnosis data available";
                    CurrentSeries = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                    return;
                }

                // Validate and filter data with non-negative values
                var validatedData = data
                    .Where(kvp => kvp.Value >= 0 && !string.IsNullOrWhiteSpace(kvp.Key))
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (validatedData.Count == 0)
                {
                    ErrorMessage = "No valid diagnosis data available";
                    CurrentSeries = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                    return;
                }

                // Set X-axis labels from diagnosis names
                XAxes = new[] 
                { 
                    new Axis 
                    { 
                        Labels = validatedData.Keys.ToArray(), 
                        LabelsRotation = 15 
                    } 
                };

                // Create column series for bar chart with validated values
                CurrentSeries = new ISeries[] 
                { 
                    new ColumnSeries<int> 
                    { 
                        Values = validatedData.Values.ToArray(), 
                        Name = "Diagnoses" 
                    } 
                };

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                // Error Handling: Catch any service-level exceptions
                ErrorMessage = $"Failed to load top diagnoses: {ex.Message}";
                CurrentSeries = Array.Empty<ISeries>();
                XAxes = Array.Empty<Axis>();
            }
        }

        /// <summary>
        /// VM4: LoadTopMedications
        /// Implements LoadTopMedications() to retrieve frequency data for prescribed medications 
        /// for bar chart rendering. Calls statisticsService.GetMostPrescribedMeds()
        /// </summary>
        private void LoadTopMedications()
        {
            try
            {
                var data = _statisticsService.GetMostPrescribedMeds();

                if (data == null || data.Count == 0)
                {
                    ErrorMessage = "No medication data available";
                    CurrentSeries = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                    return;
                }

                // Validate and filter data with non-negative values
                var validatedData = data
                    .Where(kvp => kvp.Value >= 0 && !string.IsNullOrWhiteSpace(kvp.Key))
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (validatedData.Count == 0)
                {
                    ErrorMessage = "No valid medication data available";
                    CurrentSeries = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                    return;
                }

                // Set X-axis labels from medication names
                XAxes = new[] 
                { 
                    new Axis 
                    { 
                        Labels = validatedData.Keys.ToArray() 
                    } 
                };

                // Create column series for bar chart with validated values
                CurrentSeries = new ISeries[] 
                { 
                    new ColumnSeries<int> 
                    { 
                        Values = validatedData.Values.ToArray(), 
                        Name = "Prescriptions" 
                    } 
                };

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                // Error Handling: Catch any service-level exceptions
                ErrorMessage = $"Failed to load top medications: {ex.Message}";
                CurrentSeries = Array.Empty<ISeries>();
                XAxes = Array.Empty<Axis>();
            }
        }
        /// <summary>
        /// VM5: LoadDemographics
        /// Implements LoadDemographics() to fetch age "buckets" (0-17, 18-64, 65+) and sex distribution data 
        /// by calling GetAgeDistribution() and GetPatientGenderDistribution() from StatisticsService. 
        /// Maps the SexEnum counts into a two-segment pie chart and the age buckets to a bar chart.
        /// </summary>
        private void LoadDemographics()
        {
            try
            {
                // 1. Fetch Gender Data (Pie Chart) - Map SexEnum counts into pie chart
                var genderData = _statisticsService.GetPatientGenderDistribution();
                
                if (genderData == null || genderData.Count == 0)
                {
                    ErrorMessage = "No demographic data available";
                    GenderSeries.Clear();
                    AgeSeries.Clear();
                    AgeXAxes = Array.Empty<Axis>();
                    return;
                }

                GenderSeries.Clear();
                foreach (var entry in genderData)
                {
                    if (entry.Value >= 0 && !string.IsNullOrWhiteSpace(entry.Key))
                    {
                        GenderSeries.Add(new PieSeries<int>
                        {
                            Name = MapSexEnumLabel(entry.Key),
                            Values = new[] { entry.Value }
                        });
                    }
                }

                // 2. Fetch Age Data (Bar Chart) - Map age buckets to bar chart
                var ageData = _statisticsService.GetAgeDistribution();

                if (ageData == null || ageData.Count == 0)
                {
                    ErrorMessage = "No age distribution data available";
                    AgeSeries.Clear();
                    AgeXAxes = Array.Empty<Axis>();
                    return;
                }

                // Validate age data
                var validatedAgeData = ageData
                    .Where(kvp => kvp.Value >= 0 && !string.IsNullOrWhiteSpace(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                AgeSeries.Clear();
                if (validatedAgeData.Count > 0)
                {
                    AgeSeries.Add(new ColumnSeries<int>
                    {
                        Name = "Patients",
                        Values = validatedAgeData.Values.ToArray()
                    });

                    AgeXAxes = new[] 
                    {
                        new Axis 
                        { 
                            Labels = validatedAgeData.Keys.ToArray()
                        }
                    };
                }

                // Trigger UI update for the Axes
                OnPropertyChanged(nameof(AgeXAxes));
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                // Error Handling: Catch and display service-level exceptions
                ErrorMessage = $"Failed to load demographics: {ex.Message}";
                GenderSeries.Clear();
                AgeSeries.Clear();
                AgeXAxes = Array.Empty<Axis>();
            }
        }

        /// <summary>
        /// Helper method to map SexEnum display values to UI-friendly labels
        /// </summary>
        private string MapSexEnumLabel(string sexEnumValue)
        {
            return sexEnumValue switch
            {
                "Male" => "Male",
                "Female" => "Female",
                _ => sexEnumValue
            };
        }


    }
}
