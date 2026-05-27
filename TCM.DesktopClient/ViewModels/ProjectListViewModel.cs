using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class ProjectListViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;

        private ObservableCollection<Project> _projects;
        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set { _projects = value; OnPropertyChanged(nameof(Projects)); }
        }

        private bool _isCreatingProject;
        public bool IsCreatingProject
        {
            get => _isCreatingProject;
            set { _isCreatingProject = value; OnPropertyChanged(nameof(IsCreatingProject)); }
        }

        private string _newProjectName;
        public string NewProjectName
        {
            get => _newProjectName;
            set { _newProjectName = value; OnPropertyChanged(nameof(NewProjectName)); }
        }

        private string _newProjectDescription;
        public string NewProjectDescription
        {
            get => _newProjectDescription;
            set { _newProjectDescription = value; OnPropertyChanged(nameof(NewProjectDescription)); }
        }

        public ObservableCollection<string> AvailableParsers { get; set; } = new();
        public ObservableCollection<string> AvailableTestResultParsers { get; set; } = new();

        private string _selectedParserName;
        public string SelectedParserName
        {
            get => _selectedParserName;
            set { _selectedParserName = value; OnPropertyChanged(nameof(SelectedParserName)); }
        }

        private string _selectedTestResultParser;
        public string SelectedTestResultParser
        {
            get => _selectedTestResultParser;
            set { _selectedTestResultParser = value; OnPropertyChanged(nameof(SelectedTestResultParser)); }
        }

        public Action<Project> OnProjectSelected { get; set; }

        private Project _selectedProject;
        public Project SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged(nameof(SelectedProject));
            }
        }

        public ICommand CreateProjectCommand { get; }
        public ICommand ShowCreateFormCommand { get; }
        public ICommand CancelCreateCommand { get; }
        public ICommand UploadTestCaseParserCommand { get; }
        public ICommand UploadTestResultParserCommand { get; }
        public ICommand OpenProjectCommand { get; }

        public ProjectListViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(AppSettings.ApiBaseUrl) };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            Projects = new ObservableCollection<Project>();

            CreateProjectCommand = new RelayCommand(async () => await CreateProjectAsync());
            ShowCreateFormCommand = new RelayCommand(() => IsCreatingProject = true);
            CancelCreateCommand = new RelayCommand(() =>
            {
                IsCreatingProject = false;
                NewProjectName = "";
                NewProjectDescription = "";
            });

            UploadTestCaseParserCommand = new RelayCommand(async () => await UploadParserAsync("TestCases"));
            UploadTestResultParserCommand = new RelayCommand(async () => await UploadParserAsync("TestResults"));
            OpenProjectCommand = new RelayCommand(() =>
            {
                if (SelectedProject != null)
                {
                    OnProjectSelected?.Invoke(SelectedProject);
                }
            });

            _ = LoadProjectsAsync();
            _ = LoadParsersAsync();
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

        private async Task LoadParsersAsync()
        {
            try
            {
                var tcParsers = await _httpClient.GetFromJsonAsync<List<string>>("api/Parsers/TestCases");
                if (tcParsers != null)
                {
                    AvailableParsers.Clear();
                    foreach (var p in tcParsers) AvailableParsers.Add(p);
                    SelectedParserName = AvailableParsers.FirstOrDefault();
                }

                var trParsers = await _httpClient.GetFromJsonAsync<List<string>>("api/Parsers/TestResults");
                if (trParsers != null)
                {
                    AvailableTestResultParsers.Clear();
                    foreach (var p in trParsers) AvailableTestResultParsers.Add(p);
                    SelectedTestResultParser = AvailableTestResultParsers.FirstOrDefault();
                }
            }
            catch { }
        }

        private async Task UploadParserAsync(string type)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = $"Select {type} Parser Configuration"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var content = new MultipartFormDataContent();
                    using var fileStream = System.IO.File.OpenRead(openFileDialog.FileName);
                    using var streamContent = new StreamContent(fileStream);

                    content.Add(streamContent, "file", System.IO.Path.GetFileName(openFileDialog.FileName));

                    var response = await _httpClient.PostAsync($"api/Parsers/{type}/upload", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var uploadedName = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show($"Parser '{uploadedName}' uploaded successfully!", "Success");

                        await LoadParsersAsync();

                        if (type == "TestCases") SelectedParserName = uploadedName;
                        else SelectedTestResultParser = uploadedName;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show($"Failed to upload parser:\n{error}", "Error");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error uploading file: {ex.Message}", "Critical Error");
                }
            }
        }

        private async Task CreateProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProjectName)) return;

            var newProject = new Project
            {
                Name = NewProjectName,
                Description = NewProjectDescription,
                ParserName = SelectedParserName,
                TestResultParser = SelectedTestResultParser,
                Status = ProjectStatus.Active
            };

            var response = await _httpClient.PostAsJsonAsync("api/Projects", newProject);
            if (response.IsSuccessStatusCode)
            {
                NewProjectName = string.Empty;
                NewProjectDescription = string.Empty;
                IsCreatingProject = false;

                await LoadProjectsAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show($"Failed to create project.\n{error}", "Error");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}