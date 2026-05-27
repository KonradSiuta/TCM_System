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
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        public Project CurrentProject { get; }

        public ObservableCollection<ProjectMemberDto> Members { get; set; } = new();

        public ObservableCollection<UserDto> AvailableSystemUsers { get; set; } = new();

        private UserDto _selectedSystemUser;
        public UserDto SelectedSystemUser
        {
            get => _selectedSystemUser;
            set { _selectedSystemUser = value; OnPropertyChanged(nameof(SelectedSystemUser)); }
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(nameof(IsAdmin)); }
        }

        public ProjectRole SelectedRole { get; set; } = ProjectRole.Tester;
        public Array AvailableRoles => Enum.GetValues(typeof(ProjectRole));

        public ICommand SaveMemberCommand { get; }
        public ICommand RemoveMemberCommand { get; }

        public TeamViewModel(Project project)
        {
            CurrentProject = project;
            _httpClient = new HttpClient { BaseAddress = new Uri(AppSettings.ApiBaseUrl) };
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSession.JwtToken);

            SaveMemberCommand = new RelayCommand(async () => await SaveMemberAsync());
            RemoveMemberCommand = new RelayCommand<ProjectMemberDto>(async (m) => await RemoveMemberAsync(m));

            _ = LoadMembersAsync();
            _ = LoadSystemUsersAsync();
            _ = LoadMyRoleAsync();
        }

        private async Task LoadMembersAsync()
        {
            try
            {
                var members = await _httpClient.GetFromJsonAsync<ProjectMemberDto[]>($"api/ProjectMembers/Project/{CurrentProject.Id}");
                if (members != null)
                {
                    Members.Clear();
                    foreach (var m in members) Members.Add(m);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load team: {ex.Message}");
            }
        }

        private async Task SaveMemberAsync()
        {
            if (SelectedSystemUser == null)
            {
                System.Windows.MessageBox.Show("Please select a user from the list.");
                return;
            }

            var dto = new UpsertMemberDto
            {
                ProjectId = CurrentProject.Id,
                Username = SelectedSystemUser.Username,
                Role = SelectedRole
            };

            var response = await _httpClient.PostAsJsonAsync("api/ProjectMembers", dto);
            if (response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(msg, "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                SelectedSystemUser = null;
                await LoadMembersAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Windows.MessageBox.Show(error, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task RemoveMemberAsync(ProjectMemberDto member)
        {
            if (member == null) return;

            var result = System.Windows.MessageBox.Show($"Remove {member.Username} from project?", "Confirm", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var response = await _httpClient.DeleteAsync($"api/ProjectMembers/Project/{CurrentProject.Id}/User/{member.UserId}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadMembersAsync();
                }
            }
        }

        private async Task LoadSystemUsersAsync()
        {
            try
            {
                var users = await _httpClient.GetFromJsonAsync<UserDto[]>("api/Users");
                if (users != null)
                {
                    AvailableSystemUsers.Clear();
                    foreach (var u in users) AvailableSystemUsers.Add(u);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load system users: {ex.Message}");
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
                    }
                }
            }
            catch
            {
                IsAdmin = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}