using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using PixelForge.Core.Base;
using PixelForge.Core.Model;
using PixelForge.Core.Providers.Images;
using PixelForge.Core.Services;
using PixelForge.Helpers.Formatters;
using PixelForge.Helpers.ImageOptimization;
using PixelForge.Helpers.Managers;
using PixelForge.Windows;

namespace PixelForge.Core.ViewModel
{
    internal class OptimizerViewModel : ViewModelBase
    {
        private readonly SemaphoreSlim semaphoreSlim = new(Math.Max(2, Environment.ProcessorCount / 2));
        public ObservableCollection<FileCardModel> Cards { get; } = [];

        public bool HasFiles => Cards.Count > 0;
        public bool HasQueued => Cards.Any(c => !c.IsDone);
        public bool HasDone => Cards.Any(c => c.IsDone && !c.IsSaved);
        public bool HasSaved => Cards.Any(c => c.IsSaved);

        public ICommand AddFilesCommand { get; }
        public ICommand ProcessAllCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ProcessCardCommand { get; }
        public ICommand SaveCardCommand { get; }
        public ICommand RemoveCardCommand { get; }

        public string ProcessedText
        {
            get
            {
                int done = Cards.Count(c => c.IsDone);
                int total = Cards.Count;
                return total > 0 ? $"{done}/{total}" : "0/0";
            }
        }

        public string TotalSaved
        {
            get
            {
                long saved = Cards.Where(c => c.IsDone && c.Model.OptimizationResult is not null).Sum(c => c.Model.SizeBytes - c.Model.OptimizationResult!.SizeAfterBytes);
                return saved > 0 ? FileSizeFormatter.Format(saved) : "0 B";
            }
        }

        public string AvgSavedPercent
        {
            get
            {
                List<FileCardModel> done = [.. Cards.Where(c => c.IsDone && c.Model.OptimizationResult is not null)];
                return done.Count != 0 ? $"{done.Average(c => c.Model.OptimizationResult!.SavedPercent):0}%" : "0%";
            }
        }

        public OptimizerViewModel()
        {
            AddFilesCommand = new RelayCommand(_ => AddFiles());
            ProcessAllCommand = new RelayCommand(async _ => await ProcessAll(), _ => Cards.Any(c => !c.IsDone && !c.IsProcessing));
            SaveAllCommand = new RelayCommand(_ => SaveAll(), _ => HasFiles && Cards.Any(c => c.IsDone && !c.IsSaved));
            ClearAllCommand = new RelayCommand(_ =>
            {
                foreach (FileCardModel card in Cards)
                {
                    card.PropertyChanged -= OnCardPropertyChanged;
                }

                Cards.Clear();
                RaiseStatsChanged();
                OnPropertyChanged(nameof(HasFiles));
                CommandManager.InvalidateRequerySuggested();
            }, _ => HasFiles);
            ProcessCardCommand = new RelayCommand(async p =>
            {
                if (p is FileCardModel card)
                {
                    await ProcessOne(card);
                }
            }, p => p is FileCardModel c && !c.IsDone && !c.IsProcessing);
            SaveCardCommand = new RelayCommand(p =>
            {
                if (p is FileCardModel card)
                {
                    SaveOne(card);
                }
            }, p => p is FileCardModel c && c.IsDone && !c.IsSaved);
            RemoveCardCommand = new RelayCommand(p =>
            {
                if (p is FileCardModel card)
                {
                    RemoveCard(card);
                }
            });

            Cards.CollectionChanged += OnCardsCollectionChanged;
        }

        private void OnCardsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (FileCardModel card in e.NewItems)
                {
                    card.PropertyChanged += OnCardPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (FileCardModel card in e.OldItems)
                {
                    card.PropertyChanged -= OnCardPropertyChanged;
                }
            }
        }

        private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(FileCardModel.IsDone) or nameof(FileCardModel.IsSaved) or nameof(FileCardModel.IsProcessing))
            {
                RaiseStatsChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void AddFiles()
        {
            string[] paths = FileImportService.ImportFiles("Images|*.png;*.jpg;*.jpeg;*.webp;*.svg");
            if (paths.Length > 0)
            {
                AddPaths(paths);
            }
        }

        public void AddPaths(IEnumerable<string> paths)
        {
            foreach (string path in FileImportService.ExpandToSupportedFiles(paths, true, ".png", ".jpg", ".jpeg", ".webp", ".svg"))
            {
                if (Cards.Any(c => c.Model.Source is ResourceImageSource f && f.Path == path))
                {
                    continue;
                }

                IImageSource source;
                try
                {
                    source = FileImportService.CreateSource(path);
                }
                catch
                {
                    continue;
                }

                OptimizerModel model = new()
                {
                    Source = source,
                    FileName = source.Name,
                    SizeBytes = source.SizeBytes ?? 0
                };

                Cards.Add(new FileCardModel(model, source, ImageThumbnailService.GetThumbnail(source)));
            }

            OnPropertyChanged(nameof(HasFiles));
            RaiseStatsChanged();
            CommandManager.InvalidateRequerySuggested();
        }


        private void RemoveCard(FileCardModel card)
        {
            Cards.Remove(card);
            OnPropertyChanged(nameof(HasFiles));
            RaiseStatsChanged();
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task ProcessOne(FileCardModel card)
        {
            await semaphoreSlim.WaitAsync();

            try
            {
                card.HasError = false;
                card.IsDone = false;
                card.IsProcessing = true;

                OptimizationResult result = await Task.Run(() => ImageOptimizer.Optimize(card.Model.Source));
                card.ApplyResult(result);
            }
            catch (Exception ex)
            {
                card.HasError = true;
                MessageManager.ShowOptimizeError(ex);
            }
            finally
            {
                card.IsProcessing = false;
                semaphoreSlim.Release();

                RaiseStatsChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }


        private async Task ProcessAll()
        {
            List<FileCardModel> toProcess = [.. Cards.Where(c => !c.IsDone)];
            await Task.WhenAll(toProcess.Where(Cards.Contains).Select(ProcessOne));
        }

        private void SaveAll()
        {
            List<(string FileName, Exception Ex)> errors = [];

            foreach (FileCardModel? card in Cards.Where(c => c.IsDone && !c.IsSaved && c.Model.OptimizationResult is not null).ToList())
            {
                try
                {
                    SaveService.Instance.SaveResult(card.Model.Source, card.Model.OptimizationResult!);
                    card.IsSaved = true;
                }
                catch (Exception ex) { errors.Add((card.FileName, ex)); }
            }

            if (errors.Count > 0)
            {
                MessageManager.ShowErrors(errors.Select(e => new Exception(e.FileName, e.Ex)), MessageWindowType.ErrorSave);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private static string? SaveOne(FileCardModel card)
        {
            if (card.Model.OptimizationResult is null)
            {
                return null;
            }

            try
            {
                string path = SaveService.Instance.SaveResult(card.Model.Source, card.Model.OptimizationResult);
                card.IsSaved = true;
                return path;
            }
            catch (Exception ex)
            {
                MessageManager.ShowSaveError(ex);
                return null;
            }
        }

        private void RaiseStatsChanged()
        {
            OnPropertyChanged(nameof(ProcessedText));
            OnPropertyChanged(nameof(TotalSaved));
            OnPropertyChanged(nameof(AvgSavedPercent));
            OnPropertyChanged(nameof(HasQueued));
            OnPropertyChanged(nameof(HasDone));
            OnPropertyChanged(nameof(HasSaved));
            OnPropertyChanged(nameof(HasFiles));
        }
    }
}