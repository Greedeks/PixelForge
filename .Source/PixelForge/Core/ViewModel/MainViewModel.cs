using System.Windows.Input;
using PixelForge.Core.Base;
using PixelForge.Core.Model;
using PixelForge.View;

namespace PixelForge.Core.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        private readonly MainModel _model = new();

        public object CurrentView
        {
            get => _model.CurrentView;
            set { _model.CurrentView = value; OnPropertyChanged(); }
        }

        public ICommand NavigateToOptimizerCommand { get; set; }
        public ICommand NavigateToSvgToXamlCommand { get; set; }
        public ICommand NavigateToSettingsCommand { get; set; }

        private void NavigateToOptimizer(object obj) => CurrentView = new OptimizerView();
        private void NavigateToSvgToXaml(object obj) => CurrentView = new SvgToXamlView();
        private void NavigateToSettings(object obj) => CurrentView = new SettingsView();

        public MainViewModel()
        {
            CurrentView = new OptimizerView();

            NavigateToOptimizerCommand = new RelayCommand(NavigateToOptimizer);
            NavigateToSvgToXamlCommand = new RelayCommand(NavigateToSvgToXaml);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
        }
    }
}
