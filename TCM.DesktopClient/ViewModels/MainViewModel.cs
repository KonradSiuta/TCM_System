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

        // Zmienne dla nowego projektu
        private string _newProjectName;
        private string _newProjectDescription;

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set { _projects = value; OnPropertyChanged(nameof(Projects)); }
        }

        // Właściwość bindowana do pola tekstowego z nazwą
        public string NewProjectName
        {
            get => _newProjectName;
            set
            {
                _newProjectName = value;
                OnPropertyChanged(nameof(NewProjectName));
            }
        }

        // Właściwość bindowana do pola tekstowego z opisem
        public string NewProjectDescription
        {
            get => _newProjectDescription;
            set
            {
                _newProjectDescription = value;
                OnPropertyChanged(nameof(NewProjectDescription));
            }
        }

        // Komenda dla przycisku "Dodaj"
        public ICommand AddProjectCommand { get; }

        public MainViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
            Projects = new ObservableCollection<Project>();

            // Konfiguracja komendy: wykonaj metodę AddProjectAsync
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
                Console.WriteLine($"Błąd podczas pobierania projektów: {ex.Message}");
            }
        }

        private async Task AddProjectAsync()
        {
            // Prosta walidacja - nie dodajemy projektu bez nazwy
            if (string.IsNullOrWhiteSpace(NewProjectName)) return;

            var newProject = new Project
            {
                Name = NewProjectName,
                Description = NewProjectDescription
            };

            try
            {
                // Wysyłamy żądanie POST do API
                var response = await _httpClient.PostAsJsonAsync("api/Projects", newProject);

                if (response.IsSuccessStatusCode)
                {
                    // Odświeżamy listę projektów pobierając ją ponownie z bazy
                    await LoadProjectsAsync();

                    // Czyścimy pola formularza
                    NewProjectName = string.Empty;
                    NewProjectDescription = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas dodawania projektu: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}