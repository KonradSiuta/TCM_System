using System.ComponentModel;
using TCM.Shared.Models;

namespace TCM.DesktopClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;

        // Ta właściwość trzyma aktywny ekran. Zmiana jej wartości od razu podmieni widok!
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(nameof(CurrentView)); }
        }

        public MainViewModel()
        {
            ShowProjectList(); // Aplikacja zawsze startuje od listy projektów
        }

        private void ShowProjectList()
        {
            var listVm = new ProjectListViewModel();
            // Subskrybujemy zdarzenie: jeśli klikną projekt, odpal szczegóły
            listVm.OnProjectSelected = ShowProjectDetails;
            CurrentView = listVm;
        }

        private void ShowProjectDetails(Project project)
        {
            var detailsVm = new ProjectDetailsViewModel(project);
            // Subskrybujemy zdarzenie: jeśli klikną 'Back', wróć do listy
            detailsVm.OnBackRequested = ShowProjectList;
            CurrentView = detailsVm;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}