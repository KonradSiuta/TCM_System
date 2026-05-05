using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private ObservableCollection<Project> _projects;

        private string _newProjectName;
        private string _newProjectDescription;

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set { _projects = value; OnPropertyChanged(nameof(Projects)); }
        }

        public string NewProjectName
        {
            get => _newProjectName;
            set
            {
                _newProjectName = value;
                OnPropertyChanged(nameof(NewProjectName));
            }
        }

        public string NewProjectDescription
        {
            get => _newProjectDescription;
            set
            {
                _newProjectDescription = value;
                OnPropertyChanged(nameof(NewProjectDescription));
            }
        }

        public ICommand AddProjectCommand { get; }

        public MainViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            Projects = new ObservableCollection<Project>();

            AddProjectCommand = new RelayCommand(async () => await AddProjectAsync());

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
                Console.WriteLine($"Error during retrieving projects info: {ex.Message}");
            }
        }

        private async Task AddProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProjectName)) return;

            var newProject = new Project
            {
                Name = NewProjectName,
                Description = NewProjectDescription
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Projects", newProject);

                if (response.IsSuccessStatusCode)
                {
                    await LoadProjectsAsync();

                    NewProjectName = string.Empty;
                    NewProjectDescription = string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during retrieving projects info:\n{ex.Message}", "Błąd API", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}