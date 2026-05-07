using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public ICommand ImportCsvCommand { get; }

        public RequirementsViewModel(Project project)
        {
            CurrentProject = project;
            Requirements = new ObservableCollection<Requirement>();

            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            ImportCsvCommand = new RelayCommand(async () => await ImportCsvAsync());

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

        private async Task ImportCsvAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select CSV file with Requirements"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var content = new MultipartFormDataContent();
                    using var fileStream = File.OpenRead(openFileDialog.FileName);
                    using var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                    content.Add(streamContent, "file", Path.GetFileName(openFileDialog.FileName));

                    var response = await _httpClient.PostAsync($"api/Requirements/Project/{CurrentProject.Id}/import", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var summaryMessage = await response.Content.ReadAsStringAsync();

                        System.Windows.MessageBox.Show(summaryMessage, "Import Summary", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        await LoadRequirementsAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show($"Import failed:\n{error}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred:\n{ex.Message}", "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}