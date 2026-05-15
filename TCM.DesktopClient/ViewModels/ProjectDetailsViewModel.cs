using System;
using System.ComponentModel;
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

        public ICommand ShowRequirementsCommand { get; }
        public ICommand ShowTestCasesCommand { get; }
        public ICommand ShowTestRunsCommand { get; }

        public ProjectDetailsViewModel(Project project)
        {
            CurrentProject = project;
            BackCommand = new RelayCommand(() => OnBackRequested?.Invoke());

            ShowRequirementsCommand = new RelayCommand(() => CurrentSubView = new RequirementsViewModel(CurrentProject));

            ShowTestCasesCommand = new RelayCommand(() => CurrentSubView = new TestCasesViewModel(CurrentProject));

            ShowTestRunsCommand = new RelayCommand(() => CurrentSubView = new TestRunsViewModel(CurrentProject));

            CurrentSubView = new RequirementsViewModel(CurrentProject);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}