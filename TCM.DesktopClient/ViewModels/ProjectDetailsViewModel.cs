using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class ProjectDetailsViewModel : INotifyPropertyChanged
    {
        public Project CurrentProject { get; }
        public Action OnBackRequested { get; set; }

        private object _currentSubView;
        public object CurrentSubView
        {
            get => _currentSubView;
            set { _currentSubView = value; OnPropertyChanged(nameof(CurrentSubView)); }
        }

        public ICommand BackCommand { get; }
        public ICommand LogoutCommand { get; }

        public ICommand ShowSummaryCommand { get; }
        public ICommand ShowRequirementsCommand { get; }
        public ICommand ShowTestCasesCommand { get; }
        public ICommand ShowTestRunsCommand { get; }
        public ICommand ShowTeamCommand { get; }

        public ProjectDetailsViewModel(Project project)
        {
            CurrentProject = project;
            BackCommand = new RelayCommand(() => OnBackRequested?.Invoke());

            LogoutCommand = new RelayCommand(() =>
            {
                AppSession.JwtToken = null;

                var loginWindow = new LoginWindow();
                loginWindow.Show();

                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });

            ShowSummaryCommand = new RelayCommand(() => CurrentSubView = new ProjectSummaryViewModel(CurrentProject));
            ShowRequirementsCommand = new RelayCommand(() => CurrentSubView = new RequirementsViewModel(CurrentProject));
            ShowTestCasesCommand = new RelayCommand(() => CurrentSubView = new TestCasesViewModel(CurrentProject));
            ShowTestRunsCommand = new RelayCommand(() => CurrentSubView = new TestRunsViewModel(CurrentProject));
            ShowTeamCommand = new RelayCommand(() => CurrentSubView = new TeamViewModel(CurrentProject));

            CurrentSubView = new ProjectSummaryViewModel(CurrentProject);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}