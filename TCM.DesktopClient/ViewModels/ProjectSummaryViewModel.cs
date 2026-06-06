using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class ChartItem
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public double BarWidth { get; set; }
        public string ColorHex { get; set; }
    }

    public class ProjectSummaryViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        public Project CurrentProject { get; }

        private ProjectSummaryDto _summary;
        public ProjectSummaryDto Summary
        {
            get => _summary;
            set { _summary = value; OnPropertyChanged(nameof(Summary)); }
        }

        public ObservableCollection<ChartItem> ReqStatusChart { get; set; } = new();
        public ObservableCollection<ChartItem> TcPriorityChart { get; set; } = new();

        private DoubleCollection _coverageDashArray;
        public DoubleCollection CoverageDashArray
        {
            get => _coverageDashArray;
            set { _coverageDashArray = value; OnPropertyChanged(nameof(CoverageDashArray)); }
        }

        public ProjectSummaryViewModel(Project project)
        {
            CurrentProject = project;
            _httpClient = new HttpClient { BaseAddress = new Uri(AppSettings.ApiBaseUrl) };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            _ = LoadSummaryAsync();
        }

        private async Task LoadSummaryAsync()
        {
            try
            {
                var summary = await _httpClient.GetFromJsonAsync<ProjectSummaryDto>($"api/Projects/{CurrentProject.Id}/summary");
                if (summary != null)
                {
                    Summary = summary;
                    GenerateCharts();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load dashboard: {ex.Message}");
            }
        }

        private void GenerateCharts()
        {
            ReqStatusChart.Clear();
            int maxReq = Summary.RequirementsByStatus.Values.DefaultIfEmpty(1).Max();
            string[] reqColors = { "#007ACC", "#28A745", "#FFC107", "#DC3545", "#6F42C1" };
            int colorIndex = 0;

            foreach (var kvp in Summary.RequirementsByStatus)
            {
                ReqStatusChart.Add(new ChartItem
                {
                    Label = kvp.Key,
                    Value = kvp.Value,
                    BarWidth = ((double)kvp.Value / maxReq) * 180,
                    ColorHex = reqColors[colorIndex++ % reqColors.Length]
                });
            }

            TcPriorityChart.Clear();
            int maxTc = Summary.TestCasesByPriority.Values.DefaultIfEmpty(1).Max();
            string[] tcColors = { "#DC3545", "#FD7E14", "#FFC107", "#20C997", "#0D6EFD" };
            colorIndex = 0;

            foreach (var kvp in Summary.TestCasesByPriority.OrderByDescending(k => k.Value))
            {
                TcPriorityChart.Add(new ChartItem
                {
                    Label = kvp.Key,
                    Value = kvp.Value,
                    BarWidth = ((double)kvp.Value / maxTc) * 180,
                    ColorHex = tcColors[colorIndex++ % tcColors.Length]
                });
            }

            double totalDashLength = (2 * Math.PI * 40) / 20.0;
            double coveredDashLength = (Summary.CoveragePercentage / 100.0) * totalDashLength;

            CoverageDashArray = new DoubleCollection { coveredDashLength, totalDashLength };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}