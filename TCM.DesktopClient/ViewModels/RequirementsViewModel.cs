using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class RequirementsViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        public Project CurrentProject { get; }

        private ObservableCollection<Requirement> _requirements;
        public ObservableCollection<Requirement> Requirements
        {
            get => _requirements;
            set { _requirements = value; OnPropertyChanged(nameof(Requirements)); }
        }

        public RequirementsViewModel(Project project)
        {
            CurrentProject = project;
            Requirements = new ObservableCollection<Requirement>();

            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            _ = LoadRequirementsAsync();
        }

        private async Task LoadRequirementsAsync()
        {
            try
            {
                var reqs = await _httpClient.GetFromJsonAsync<Requirement[]>($"api/Requirements/Project/{CurrentProject.Id}");
                if (reqs != null)
                {
                    Requirements = new ObservableCollection<Requirement>(reqs);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error fetching requirements:\n{ex.Message}", "API Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}