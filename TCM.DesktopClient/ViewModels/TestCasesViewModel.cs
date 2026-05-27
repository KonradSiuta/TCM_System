using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class TestCasesViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        public Project CurrentProject { get; }

        private List<TestCase> _allTestCasesDb = new();

        private ObservableCollection<TestCase> _testCases;
        public ObservableCollection<TestCase> TestCases
        {
            get => _testCases;
            set { _testCases = value; OnPropertyChanged(nameof(TestCases)); }
        }

        private bool _canEdit;
        public bool CanEdit
        {
            get => _canEdit;
            set { _canEdit = value; OnPropertyChanged(nameof(CanEdit)); }
        }

        private bool _showAllTestCases = false;
        public bool ShowAllTestCases
        {
            get => _showAllTestCases;
            set
            {
                _showAllTestCases = value;
                OnPropertyChanged(nameof(ShowAllTestCases));
                OnPropertyChanged(nameof(ToggleViewButtonText));
                ApplyFilter();
            }
        }

        public string ToggleViewButtonText => ShowAllTestCases ? "View active tests only" : "View all tests";

        public ICommand ToggleViewCommand { get; }
        public ICommand ImportSourceCommand { get; }
        public ICommand DeactivateCommand { get; }

        public TestCasesViewModel(Project project)
        {
            CurrentProject = project;
            TestCases = new ObservableCollection<TestCase>();

            _httpClient = new HttpClient { BaseAddress = new Uri(AppSettings.ApiBaseUrl) };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            ToggleViewCommand = new RelayCommand(() => ShowAllTestCases = !ShowAllTestCases);
            ImportSourceCommand = new RelayCommand(async () => await ImportSourceAsync());
            DeactivateCommand = new RelayCommand<TestCase>(async (tc) => await DeactivateTestCaseAsync(tc));

            _ = LoadTestCasesAsync();
            _ = LoadMyRoleAsync();
        }

        private async Task LoadTestCasesAsync()
        {
            try
            {
                var tcs = await _httpClient.GetFromJsonAsync<TestCase[]>($"api/TestCases/Project/{CurrentProject.Id}");
                if (tcs != null)
                {
                    _allTestCasesDb = tcs.ToList();
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error fetching test cases:\n{ex.Message}", "API Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_allTestCasesDb == null) return;

            var filtered = ShowAllTestCases
                ? _allTestCasesDb
                : _allTestCasesDb.Where(tc => tc.IsActive).ToList();

            TestCases = new ObservableCollection<TestCase>(filtered);
        }

        private async Task ImportSourceAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Source files (*.cpp;*.h;*.py;*.cs)|*.cpp;*.h;*.py;*.cs|All files (*.*)|*.*",
                Title = "Select Source Code File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var content = new MultipartFormDataContent();
                    using var fileStream = File.OpenRead(openFileDialog.FileName);
                    using var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                    content.Add(streamContent, "file", Path.GetFileName(openFileDialog.FileName));

                    var response = await _httpClient.PostAsync($"api/TestCases/Project/{CurrentProject.Id}/import-source", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var summaryMessage = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show(summaryMessage, "Import Summary", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        await LoadTestCasesAsync();
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

        private async Task DeactivateTestCaseAsync(TestCase tc)
        {
            if (tc == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to deactivate test case: {tc.ExternalId}?",
                "Confirm Deactivation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"api/TestCases/{tc.Id}/deactivate");
                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        await LoadTestCasesAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show($"Failed to deactivate:\n{error}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error: {ex.Message}", "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadMyRoleAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/ProjectMembers/Project/{CurrentProject.Id}/MyRole");
                if (response.IsSuccessStatusCode)
                {
                    var rawContent = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(rawContent, out int roleValue))
                    {
                        CanEdit = (roleValue == (int)ProjectRole.Administrator || roleValue == (int)ProjectRole.Tester);
                    }
                }
            }
            catch
            {
                CanEdit = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}