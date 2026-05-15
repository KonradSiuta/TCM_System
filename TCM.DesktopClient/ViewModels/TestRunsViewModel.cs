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

        private bool _isCreatingNew;
        public bool IsCreatingNew
        {
            get => _isCreatingNew;
            set { _isCreatingNew = value; OnPropertyChanged(nameof(IsCreatingNew)); }
        }

        public string NewRunName { get; set; } = "Sprint 1 Run";
        public string NewRunEnvironment { get; set; } = "QA Environment";

        public TestPriority SelectedPriorityFilter { get; set; } = TestPriority.Smoke;
        public Array Priorities => Enum.GetValues(typeof(TestPriority));

        // Komendy
        public ICommand OpenCreatorCommand { get; }
        public ICommand CancelCreatorCommand { get; }
        public ICommand SelectByPriorityCommand { get; }
        public ICommand CreateRunCommand { get; }
        public ICommand SetPassedCommand { get; }
        public ICommand SetFailedCommand { get; }

        public TestRunsViewModel(Project project)
        {
            CurrentProject = project;
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            OpenCreatorCommand = new RelayCommand(() =>
            {
                SelectedRun = null;
                IsCreatingNew = true;
            });
            CancelCreatorCommand = new RelayCommand(() => IsCreatingNew = false);

            SelectByPriorityCommand = new RelayCommand(SelectTestCasesByPriority);
            CreateRunCommand = new RelayCommand(async () => await CreateRunAsync());

            SetPassedCommand = new RelayCommand<TestExecution>(async (ex) => await UpdateStatusAsync(ex, ExecutionStatus.Passed));
            SetFailedCommand = new RelayCommand<TestExecution>(async (ex) => await UpdateStatusAsync(ex, ExecutionStatus.Failed));

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}