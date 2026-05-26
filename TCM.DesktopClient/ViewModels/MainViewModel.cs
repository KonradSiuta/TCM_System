using System.ComponentModel;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(nameof(CurrentView)); }
        }

        public MainViewModel()
        {
            ShowProjectList();
        }

        private void ShowProjectList()
        {
            var listVm = new ProjectListViewModel();
            listVm.OnProjectSelected = ShowProjectDetails;
            CurrentView = listVm;
        }

        private void ShowProjectDetails(Project project)
        {
            var detailsVm = new ProjectDetailsViewModel(project);
            detailsVm.OnBackRequested = ShowProjectList;
            CurrentView = detailsVm;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}