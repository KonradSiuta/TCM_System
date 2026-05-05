using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private ObservableCollection<Project> _projects;

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set
            {
                _projects = value;
                OnPropertyChanged(nameof(Projects));
            }
        }

        public MainViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
            Projects = new ObservableCollection<Project>();

            _ = LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                var projectsFromApi = await _httpClient.GetFromJsonAsync<Project[]>("api/Projects");
                if (projectsFromApi != null)
                {
                    Projects = new ObservableCollection<Project>(projectsFromApi);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas pobierania projektów: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}