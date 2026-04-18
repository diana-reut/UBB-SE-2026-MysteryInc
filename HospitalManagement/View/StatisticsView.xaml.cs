using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Linq;

namespace HospitalManagement.View;

internal sealed partial class StatisticsWindow : Window, IDisposable
{
    private readonly IDbContext _dbContext;
    private readonly IStatisticsService _statisticsService;
    private string _currentStatistic;
    private readonly bool _ownsDbContext;

    public StatisticsWindow(IDbContext dbContext)
    {
        InitializeComponent();

        _currentStatistic = "";

        // Use provided context or create new one
        if (dbContext is null)
        {
            _dbContext = new HospitalDbContext();
            _ownsDbContext = true;
        }
        else
        {
            _dbContext = dbContext;
            _ownsDbContext = false;
        }

        // Initialize service
        var pRepo = new PatientRepository(_dbContext);
        var prRepo = new PrescriptionRepository(_dbContext);
        var rRepo = new MedicalRecordRepository(_dbContext);
        _statisticsService = new StatisticsService(pRepo, rRepo, prRepo);

        ConfigureWindow();
        LoadInitialStatistics();
    }

    private void ConfigureWindow()
    {
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Restore();
        }

        Closed += StatisticsWindow_Closed;
    }

    private void StatisticsWindow_Closed(object sender, WindowEventArgs args)
    {
        Dispose();
    }

    private void LoadInitialStatistics()
    {
        _currentStatistic = "Patient Distribution";
        StatisticTitle.Text = _currentStatistic;
        LoadPatientDistribution();
    }

    private void StatisticsMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatisticsMenu.SelectedItem is string selected)
        {
            _currentStatistic = selected;
            StatisticTitle.Text = selected;
            LoadStatisticByType(selected);
        }
    }

    private void LoadStatisticByType(string statisticType)
    {
        try
        {
            HideAllCharts();
            ErrorInfoBar.IsOpen = false;

            switch (statisticType)
            {
                case "Patient Distribution":
                {
                    LoadPatientDistribution();
                    break;
                }
                case "Consultation Source":
                {
                    LoadConsultationSources();
                    break;
                }
                case "Top Diagnoses":
                {
                    LoadTopDiagnoses();
                    break;
                }
                case "Top Medications":
                {
                    LoadTopMedications();
                    break;
                }
                case "Demographics":
                {
                    LoadDemographics();
                    break;
                }
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error loading statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// VM1: Load Patient Distribution - Active vs Archived ratio pie chart.
    /// </summary>
    private void LoadPatientDistribution()
    {
        try
        {
            Dictionary<string, int> data = _statisticsService.GetActiveVsArchivedRatio();
            var series = new List<ISeries>();

            foreach (KeyValuePair<string, int> kvp in data.Where(kvp => kvp.Value >= 0))
            {
                series.Add(new PieSeries<int>
                {
                    Name = kvp.Key,
                    Values = new int[] { kvp.Value, },
                });
            }

            PieChartControl.Series = series;
            PieChartControl.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load patient distribution: {ex.Message}");
        }
    }

    /// <summary>
    /// VM2: Load Consultation Sources - ER vs Appointments pie chart.
    /// </summary>
    private void LoadConsultationSources()
    {
        try
        {
            Dictionary<string, int> data = _statisticsService.GetConsultationDistribution();
            var series = new List<ISeries>();

            foreach (KeyValuePair<string, int> kvp in data.Where(kvp => kvp.Value >= 0))
            {
                string label = kvp.Key switch
                {
                    "ER" => "Emergency Department",
                    "App" => "Scheduled Appointments",
                    "Admin" => "Administrative",
                    _ => kvp.Key,
                };

                series.Add(new PieSeries<int>
                {
                    Name = label,
                    Values = new int[] { kvp.Value, },
                });
            }

            PieChartControl.Series = series;
            PieChartControl.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load consultation sources: {ex.Message}");
        }
    }

    /// <summary>
    /// VM3: Load Top Diagnoses - Bar chart of most common diagnoses.
    /// </summary>
    private void LoadTopDiagnoses()
    {
        try
        {
            Dictionary<string, int> data = _statisticsService.GetTopDiagnoses();

            if (data is null || data.Count == 0)
            {
                ShowError("No diagnosis data available");
                return;
            }

            var validatedData = data
                .Where(kvp => kvp.Value >= 0 && !string.IsNullOrWhiteSpace(kvp.Key))
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Axis[] xAxes = [
                new Axis
            {
                Labels = validatedData.Keys.ToArray(),
                LabelsRotation = 15,
            },
        ];

            var series = new ISeries[]
            {
                new ColumnSeries<int>
                    {
                        Name = "Diagnoses",
                        Values = validatedData.Values.ToArray(),
                    },
            };

            CartesianChartControl.XAxes = xAxes;
            CartesianChartControl.Series = series;
            CartesianChartControl.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load top diagnoses: {ex.Message}");
        }
    }

    /// <summary>
    /// VM4: Load Top Medications - Bar chart of most prescribed medications.
    /// </summary>
    private void LoadTopMedications()
    {
        try
        {
            Dictionary<string, int> data = _statisticsService.GetMostPrescribedMeds();

            if (data is null || data.Count == 0)
            {
                ShowError("No medication data available");
                return;
            }

            var validatedData = data
                .Where(kvp => kvp.Value >= 0 && !string.IsNullOrWhiteSpace(kvp.Key))
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Axis[] xAxes =
            [
                new Axis
            {
                Labels = validatedData.Keys.ToArray(),
            },
        ];

            var series = new ISeries[]
            {
                new ColumnSeries<int>
                    {
                        Name = "Prescriptions",
                        Values = validatedData.Values.ToArray(),
                    },
            };

            CartesianChartControl.XAxes = xAxes;
            CartesianChartControl.Series = series;
            CartesianChartControl.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load top medications: {ex.Message}");
        }
    }

    /// <summary>
    /// VM5: Load Demographics - Gender pie chart and age bar chart.
    /// </summary>
    private void LoadDemographics()
    {
        try
        {
            // Load gender distribution
            Dictionary<string, int> genderData = _statisticsService.GetPatientGenderDistribution();
            var genderSeries = new List<ISeries>();

            foreach (KeyValuePair<string, int> entry in genderData.Where(kvp => kvp.Value >= 0))
            {
                genderSeries.Add(new PieSeries<int>
                {
                    Name = entry.Key,
                    Values = new int[] { entry.Value, },
                });
            }

            GenderChart.Series = genderSeries;

            // Load age distribution
            Dictionary<string, int> ageData = _statisticsService.GetAgeDistribution();
            if (ageData?.Count > 0)
            {
                var validatedAgeData = ageData
                    .Where(kvp => kvp.Value >= 0 && !string.IsNullOrWhiteSpace(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                Axis[] xAxes = [
                    new Axis
                {
                    Labels = validatedAgeData.Keys.ToArray(),
                },
            ];

                var series = new ISeries[]
                {
                    new ColumnSeries<int>
                        {
                            Name = "Patients",
                            Values = validatedAgeData.Values.ToArray(),
                        },
                };

                AgeChart.XAxes = xAxes;
                AgeChart.Series = series;
            }

            DemographicsGrid.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load demographics: {ex.Message}");
        }
    }

    private void HideAllCharts()
    {
        PieChartControl.Visibility = Visibility.Collapsed;
        CartesianChartControl.Visibility = Visibility.Collapsed;
        DemographicsGrid.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message)
    {
        ErrorInfoBar.Message = message;
        ErrorInfoBar.IsOpen = true;
    }

    public void Dispose()
    {
        if (_ownsDbContext)
        {
            _dbContext.Dispose();
        }
    }
}
