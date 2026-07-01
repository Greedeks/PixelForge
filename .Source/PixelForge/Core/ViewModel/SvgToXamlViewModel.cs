using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PixelForge.Core.Base;
using PixelForge.Core.Model;
using PixelForge.Core.Providers.Images;
using PixelForge.Core.Services;
using PixelForge.Helpers.ImageOptimization;
using PixelForge.Helpers.SvgHandling;

namespace PixelForge.Core.ViewModel
{
    internal class SvgToXamlViewModel : ViewModelBase
    {
        private enum CloseDirection
        {
            Left,
            Right
        }

        private SvgToXamlModel? _activeFile;
        private readonly SemaphoreSlim _semaphore = new(Math.Max(2, Environment.ProcessorCount / 2));

        public ObservableCollection<SvgToXamlModel> Files { get; } = [];

        public ICommand AddFilesCommand { get; }
        public ICommand CloseFileCommand { get; }
        public ICommand SelectFileCommand { get; }
        public ICommand CloseAllCommand { get; }
        public ICommand CloseOthersCommand { get; }
        public ICommand CloseToRightCommand { get; }
        public ICommand CloseToLeftCommand { get; }

        public bool HasFiles => Files.Count > 0;

        public SvgToXamlModel? ActiveFile
        {
            get => _activeFile;
            set
            {
                _activeFile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasFiles));
            }
        }

        public SvgToXamlViewModel()
        {
            AddFilesCommand = new RelayCommand(_ => AddFiles());

            CloseFileCommand = new RelayCommand(p =>
            {
                if (p is SvgToXamlModel f)
                {
                    CloseFile(f);
                }
            });

            SelectFileCommand = new RelayCommand(p =>
            {
                if (p is SvgToXamlModel f)
                {
                    ActiveFile = f;
                }
            });

            CloseAllCommand = new RelayCommand(_ => CloseAll());
            CloseOthersCommand = new RelayCommand(_ => CloseOthers());
            CloseToRightCommand = new RelayCommand(_ => CloseSide(CloseDirection.Right));
            CloseToLeftCommand = new RelayCommand(_ => CloseSide(CloseDirection.Left));
        }

        private void CloseAll()
        {
            Files.Clear();
            ActiveFile = null;
            OnPropertyChanged(nameof(HasFiles));
        }

        private void CloseOthers()
        {
            if (ActiveFile is not SvgToXamlModel current)
            {
                return;
            }

            for (int i = Files.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(Files[i], current))
                {
                    Files.RemoveAt(i);
                }
            }

            OnPropertyChanged(nameof(HasFiles));
        }

        private void CloseSide(CloseDirection direction)
        {
            if (ActiveFile is null)
            {
                return;
            }

            int activeIndex = Files.IndexOf(ActiveFile);

            for (int i = Files.Count - 1; i >= 0; i--)
            {
                bool remove = direction switch
                {
                    CloseDirection.Left => i < activeIndex,
                    CloseDirection.Right => i > activeIndex,
                    _ => false
                };

                if (remove)
                {
                    Files.RemoveAt(i);
                }
            }

            OnPropertyChanged(nameof(HasFiles));
        }


        private void AddFiles()
        {
            string[] paths = FileImportService.ImportFiles("SVG files|*.svg");
            if (paths.Length > 0)
            {
                AddPaths(paths);
            }
        }

        public void AddPaths(IEnumerable<string> paths)
        {
            SvgToXamlModel? firstAdded = null;

            foreach (string path in FileImportService.ExpandToSupportedFiles(paths, false, ".svg"))
            {
                if (Files.Any(f => f.Source is ResourceImageSource r && r.Path == path))
                {
                    continue;
                }

                SvgToXamlModel file = new(FileImportService.CreateSource(path));
                Files.Add(file);
                firstAdded ??= file;
                _ = ConvertFile(file);
            }

            if (firstAdded is not null)
            {
                ActiveFile = firstAdded;
                OnPropertyChanged(nameof(HasFiles));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CloseFile(SvgToXamlModel file)
        {
            int idx = Files.IndexOf(file);
            Files.Remove(file);

            ActiveFile = Files.Count == 0 ? null : Files[Math.Min(idx, Files.Count - 1)];

            OnPropertyChanged(nameof(HasFiles));
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task ConvertFile(SvgToXamlModel file)
        {
            if (file.IsConverting)
            {
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (Files.Contains(file))
                {
                    file.IsConverting = true;
                    file.HasError = false;

                    using Stream stream = file.Source.OpenRead();
                    string svgText = await new StreamReader(stream).ReadToEndAsync();

                    SvgConversionResult result = await Task.Run(() =>
                    {
                        if (SettingsService.Instance.AutoOptimizeOnLoad)
                        {
                            svgText = SvgOptimizer.Optimize(svgText);
                        }

                        return SvgConverter.ConvertSvgText(svgText, file.Source.Name);
                    });

                    if (result.IsError)
                    {
                        await Task.Delay(200);
                        result = await Task.Run(() => SvgConverter.ConvertSvgText(svgText, file.Source.Name));
                    }

                    file.Result = result;
                    file.HasError = result.IsError;
                }
            }
            catch { file.HasError = true; }
            finally
            {
                file.IsConverting = false;
                _semaphore.Release();
            }
        }
    }
}