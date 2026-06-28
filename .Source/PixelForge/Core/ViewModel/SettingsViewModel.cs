using Ookii.Dialogs.Wpf;
using PixelForge.Core.Base;
using PixelForge.Core.Model;
using PixelForge.Core.Services;

namespace PixelForge.Core.ViewModel
{
    internal class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsModel _model = SettingsModel.Instance;

        public RelayCommand SelectFolderCommand { get; }

        public bool IsNextToOriginal
        {
            get => SettingsModel.SelectedSaveMode == SaveMode.NextToOriginal;
            set
            {
                if (!value)
                {
                    return;
                }

                SetSaveMode(SaveMode.NextToOriginal);
            }
        }

        public bool IsCustomFolder
        {
            get => SettingsModel.SelectedSaveMode == SaveMode.CustomFolder;
            set
            {
                if (!value)
                {
                    return;
                }

                SetSaveMode(SaveMode.CustomFolder);
            }
        }

        public bool IsOverwriteOriginal
        {
            get => SettingsModel.SelectedSaveMode == SaveMode.OverwriteOriginal;
            set
            {
                if (!value)
                {
                    return;
                }

                SetSaveMode(SaveMode.OverwriteOriginal);
            }
        }

        public string CustomFolderPath
        {
            get => _model.CustomFolderPath;
            set
            {
                if (_model.CustomFolderPath == value)
                {
                    return;
                }

                _model.CustomFolderPath = value;
                SettingsService.Save();
                OnPropertyChanged();
            }
        }

        private void SelectFolder()
        {
            VistaFolderBrowserDialog dialog = new()
            {
                UseDescriptionForTitle = true,
                SelectedPath = CustomFolderPath,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == true)
            {
                CustomFolderPath = dialog.SelectedPath;
                SetSaveMode(SaveMode.CustomFolder);
            }
        }

        public bool IsOptimizeImmediately
        {
            get => _model.AutoOptimizeOnLoad;
            set
            {
                if (!value)
                {
                    return;
                }

                SetAutoOptimize(true);
            }
        }

        public bool IsKeepOriginal
        {
            get => !_model.AutoOptimizeOnLoad;
            set
            {
                if (!value)
                {
                    return;
                }

                SetAutoOptimize(false);
            }
        }
        public SettingsViewModel()
        {
            SelectFolderCommand = new RelayCommand(_ => SelectFolder());
        }

        private void SetSaveMode(SaveMode mode)
        {
            if (SettingsModel.SelectedSaveMode != mode)
            {
                SettingsModel.SelectedSaveMode = mode;
                SettingsService.Save();

                OnPropertyChanged(nameof(IsNextToOriginal));
                OnPropertyChanged(nameof(IsCustomFolder));
                OnPropertyChanged(nameof(IsOverwriteOriginal));
            }
        }

        private void SetAutoOptimize(bool autoOptimize)
        {
            if (_model.AutoOptimizeOnLoad != autoOptimize)
            {
                _model.AutoOptimizeOnLoad = autoOptimize;
                SettingsService.Save();

                OnPropertyChanged(nameof(IsOptimizeImmediately));
                OnPropertyChanged(nameof(IsKeepOriginal));
            }
        }
    }
}