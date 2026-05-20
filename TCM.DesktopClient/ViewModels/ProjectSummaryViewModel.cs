using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
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

        public ProjectSummaryViewModel(Project project)
        {
            CurrentProject = project;
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
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
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load dashboard: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}