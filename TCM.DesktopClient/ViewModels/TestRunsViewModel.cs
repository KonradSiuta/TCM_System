using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class TestRunsViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private bool _isFetchingDetails = false;
        public Project CurrentProject { get; }

        public ObservableCollection<TestRun> TestRuns { get; set; } = new();
        public ObservableCollection<TestCase> AvailableTestCases { get; set; } = new();

        private TestRun _selectedRun;
        public TestRun SelectedRun
        {
            get => _selectedRun;
            set
            {
                if (_selectedRun == value) return;

                _selectedRun = value;
                OnPropertyChanged(nameof(SelectedRun));

                if (_selectedRun != null && !_isFetchingDetails)
                {
                    IsCreatingNew = false;
                    _ = LoadRunDetailsAsync();
                }
            }
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(nameof(IsAdmin)); }
        }

        private bool _canExecute;
        public bool CanExecute
        {
            get => _canExecute;
            set { _canExecute = value; OnPropertyChanged(nameof(CanExecute)); }
        }

        private bool _isEditingRun;
        public bool IsEditingRun
        {
            get => _isEditingRun;
            set { _isEditingRun = value; OnPropertyChanged(nameof(IsEditingRun)); }
        }

        public string EditRunName { get; set; }
        public string EditRunEnvironment { get; set; }

        private bool _isCreatingNew;
        public bool IsCreatingNew
        {
            get => _isCreatingNew;
            set { _isCreatingNew = value; OnPropertyChanged(nameof(IsCreatingNew)); }
        }

        public string NewRunName { get; set; } = "Sprint 1 Run";
        public string NewRunEnvironment { get; set; } = "QA Environment";

        private DateTime? _newRunPlannedEndDate;
        public DateTime? NewRunPlannedEndDate
        {
            get => _newRunPlannedEndDate;
            set { _newRunPlannedEndDate = value; OnPropertyChanged(nameof(NewRunPlannedEndDate)); }
        }

        private DateTime? _editRunPlannedEndDate;
        public DateTime? EditRunPlannedEndDate
        {
            get => _editRunPlannedEndDate;
            set { _editRunPlannedEndDate = value; OnPropertyChanged(nameof(EditRunPlannedEndDate)); }
        }

        public TestPriority SelectedPriorityFilter { get; set; } = TestPriority.Smoke;
        public Array Priorities => Enum.GetValues(typeof(TestPriority));

        public ICommand OpenCreatorCommand { get; }
        public ICommand CancelCreatorCommand { get; }
        public ICommand SelectByPriorityCommand { get; }
        public ICommand CreateRunCommand { get; }
        public ICommand SetPassedCommand { get; }
        public ICommand SetFailedCommand { get; }
        public ICommand ImportResultsCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand StartEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand DeleteRunCommand { get; }

        public TestRunsViewModel(Project project)
        {
            CurrentProject = project;
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            OpenCreatorCommand = new RelayCommand(() =>
            {
                SelectedRun = null;
                NewRunPlannedEndDate = null;
                IsCreatingNew = true;
            });
            CancelCreatorCommand = new RelayCommand(() => IsCreatingNew = false);

            SelectByPriorityCommand = new RelayCommand(SelectTestCasesByPriority);
            CreateRunCommand = new RelayCommand(async () => await CreateRunAsync());

            SetPassedCommand = new RelayCommand<TestExecution>(async (ex) => await UpdateStatusAsync(ex, ExecutionStatus.Passed));
            SetFailedCommand = new RelayCommand<TestExecution>(async (ex) => await UpdateStatusAsync(ex, ExecutionStatus.Failed));
            
            ImportResultsCommand = new RelayCommand(async () => await ImportResultsAsync());

            ExportPdfCommand = new RelayCommand(async () => await ExportPdfReportAsync());

            StartEditCommand = new RelayCommand(() => 
            { 
                EditRunName = SelectedRun.Name; 
                EditRunEnvironment = SelectedRun.Environment;
                EditRunPlannedEndDate = SelectedRun.PlannedEndDate;
                OnPropertyChanged(nameof(EditRunName));
                OnPropertyChanged(nameof(EditRunEnvironment));
                OnPropertyChanged(nameof(EditRunPlannedEndDate));
                IsEditingRun = true; 
            });
            CancelEditCommand = new RelayCommand(() => IsEditingRun = false);
            SaveEditCommand = new RelayCommand(async () => await SaveEditAsync());
            DeleteRunCommand = new RelayCommand(async () => await DeleteRunAsync());

            _ = LoadMyRoleAsync();

            _ = LoadRunsAsync();
            _ = LoadAvailableTestCasesAsync();
        }

        private async Task LoadRunsAsync()
        {
            var runs = await _httpClient.GetFromJsonAsync<TestRun[]>($"api/TestRuns/Project/{CurrentProject.Id}");
            if (runs != null)
            {
                TestRuns.Clear();
                foreach (var r in runs) TestRuns.Add(r);
            }
        }

        private async Task LoadAvailableTestCasesAsync()
        {
            var tcs = await _httpClient.GetFromJsonAsync<TestCase[]>($"api/TestCases/Project/{CurrentProject.Id}");
            if (tcs != null)
            {
                AvailableTestCases.Clear();
                foreach (var tc in tcs) AvailableTestCases.Add(tc);
            }
        }

        private async Task LoadRunDetailsAsync()
        {
            if (SelectedRun == null) return;

            int targetId = SelectedRun.Id;
            _isFetchingDetails = true;

            try
            {
                var details = await _httpClient.GetFromJsonAsync<TestRun>($"api/TestRuns/{targetId}");

                if (details != null && SelectedRun != null && SelectedRun.Id == targetId)
                {
                    SelectedRun = details;
                }
            }
            finally
            {
                _isFetchingDetails = false;
            }
        }

        private void SelectTestCasesByPriority()
        {
            var temp = AvailableTestCases.ToList();

            foreach (var tc in temp) tc.IsSelected = false;

            foreach (var tc in temp.Where(t => t.Priority == SelectedPriorityFilter))
            {
                tc.IsSelected = true;
            }

            AvailableTestCases.Clear();
            foreach (var tc in temp)
            {
                AvailableTestCases.Add(tc);
            }
        }

        private async Task CreateRunAsync()
        {
            var selectedIds = AvailableTestCases.Where(tc => tc.IsSelected).Select(tc => tc.Id).ToList();

            if (!selectedIds.Any())
            {
                System.Windows.MessageBox.Show("Select at least one test case!");
                return;
            }

            var dto = new CreateTestRunDto
            {
                ProjectId = CurrentProject.Id,
                Name = NewRunName,
                Environment = NewRunEnvironment,
                PlannedEndDate = NewRunPlannedEndDate,
                TestCaseIds = selectedIds
            };

            var response = await _httpClient.PostAsJsonAsync("api/TestRuns", dto);
            if (response.IsSuccessStatusCode)
            {
                await LoadRunsAsync();
                IsCreatingNew = false;

                foreach (var tc in AvailableTestCases) tc.IsSelected = false;
            }
        }

        private async Task UpdateStatusAsync(TestExecution execution, ExecutionStatus status)
        {
            if (execution == null) return;
            var response = await _httpClient.PatchAsJsonAsync($"api/TestRuns/Execution/{execution.Id}/status", status);
            if (response.IsSuccessStatusCode)
            {
                await LoadRunDetailsAsync();
            }
        }

        private async Task ImportResultsAsync()
        {
            if (SelectedRun == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "XML Reports (*.xml)|*.xml|All files (*.*)|*.*",
                Title = "Select Test Report File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var content = new MultipartFormDataContent();
                    using var fileStream = System.IO.File.OpenRead(openFileDialog.FileName);
                    using var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
                    content.Add(streamContent, "file", System.IO.Path.GetFileName(openFileDialog.FileName));

                    var response = await _httpClient.PostAsync($"api/TestRuns/{SelectedRun.Id}/import-results", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var summaryMessage = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show(summaryMessage, "Import Summary", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                        await LoadRunDetailsAsync();
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

        private async Task ExportPdfReportAsync()
        {
            if (SelectedRun == null) return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Documents (*.pdf)|*.pdf",
                FileName = $"Report_{SelectedRun.Name.Replace(" ", "_")}.pdf",
                Title = "Save Test Run PDF Report"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"api/TestRuns/{SelectedRun.Id}/pdf-report");

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] pdfBytes = await response.Content.ReadAsByteArrayAsync();
                        await System.IO.File.WriteAllBytesAsync(saveFileDialog.FileName, pdfBytes);

                        System.Windows.MessageBox.Show("PDF report generated and saved successfully!", "Success",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        System.Windows.MessageBox.Show($"Failed to generate report:\n{error}", "Server Error",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred:\n{ex.Message}", "Critical Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                        IsAdmin = (roleValue == (int)ProjectRole.Administrator);

                        CanExecute = (roleValue == (int)ProjectRole.Administrator || roleValue == (int)ProjectRole.Tester);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Windows.MessageBox.Show($"Serwer odrzucił żądanie pobrania roli.\nStatus: {response.StatusCode}\nTreść: {error}", "Debug Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Wyjątek podczas łączenia z API w LoadMyRoleAsync:\n{ex.Message}", "Debug Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                IsAdmin = false;
                CanExecute = false;
            }
        }

        private async Task SaveEditAsync()
        {
            if (SelectedRun == null) return;

            var dto = new UpdateTestRunDto {
                Name = EditRunName,
                Environment = EditRunEnvironment,
                PlannedEndDate = EditRunPlannedEndDate 
            };
            var response = await _httpClient.PutAsJsonAsync($"api/TestRuns/{SelectedRun.Id}", dto);

            if (response.IsSuccessStatusCode)
            {
                IsEditingRun = false;
                await LoadRunsAsync();
                await LoadRunDetailsAsync();
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to update test run. Access Denied?", "Error");
            }
        }

        private async Task DeleteRunAsync()
        {
            if (SelectedRun == null) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to permanently delete test run: {SelectedRun.Name}?", "Warning", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var response = await _httpClient.DeleteAsync($"api/TestRuns/{SelectedRun.Id}");
                if (response.IsSuccessStatusCode)
                {
                    SelectedRun = null;
                    await LoadRunsAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to delete test run. Access Denied?", "Error");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}