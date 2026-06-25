using System.Windows.Input;
using PixelForge.Core.Base;
using PixelForge.Core.Model;
using PixelForge.View;

namespace PixelForge.Core.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        private readonly MainModel _model = new();
        private readonly OptimizerView _optimizerView = new();
        private readonly SvgToXamlView _svgToXamlView = new();
        private readonly SettingsView _settingsView = new();

        public object? CurrentView
        {
            get => _model.CurrentView;
            set { _model.CurrentView = value; OnPropertyChanged(); }
        }

        public ICommand NavigateToOptimizerCommand { get; set; }
        public ICommand NavigateToSvgToXamlCommand { get; set; }
        public ICommand NavigateToSettingsCommand { get; set; }

        public MainViewModel()
        {
            CurrentView = _optimizerView;

            NavigateToOptimizerCommand = new RelayCommand(_ => CurrentView = _optimizerView);
            NavigateToSvgToXamlCommand = new RelayCommand(_ => CurrentView = _svgToXamlView);
            NavigateToSettingsCommand = new RelayCommand(_ => CurrentView = _settingsView);
        }
    }
}
