using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using PixelForge.Core.Base;
using PixelForge.Core.Model;
using PixelForge.Core.Services;
using PixelForge.Helpers;
using PixelForge.Helpers.ImageOptimization;
using PixelForge.Helpers.Managers;
using PixelForge.Windows;

namespace PixelForge.Core.ViewModel
{
    internal class OptimizerViewModel : ViewModelBase
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".svg" };

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
            ProcessAllCommand = new RelayCommand(async _ => await ProcessAllAsync(), _ => Cards.Any(c => !c.IsDone && !c.IsProcessing));
            SaveAllCommand = new RelayCommand(_ => SaveAll(), _ => HasFiles && Cards.Any(c => c.IsDone && !c.IsSaved));
            ClearAllCommand = new RelayCommand(_ =>
            {
                Cards.Clear();
                RaiseStatsChanged();
                OnPropertyChanged(nameof(HasFiles));
                CommandManager.InvalidateRequerySuggested();
            }, _ => HasFiles);
            ProcessCardCommand = new RelayCommand(async p =>
            {
                if (p is FileCardModel card)
                {
                    await ProcessOneAsync(card);
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
            VistaOpenFileDialog dialog = new()
            {
                Multiselect = true,
                Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.svg"
            };

            if (dialog.ShowDialog() == true)
            {
                AddPaths(dialog.FileNames);
            }
        }

        public void AddPaths(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    AddPaths(Directory.GetFiles(path));
                    continue;
                }
                if (!File.Exists(path))
                {
                    continue;
                }

                if (!SupportedExtensions.Contains(Path.GetExtension(path)))
                {
                    continue;
                }

                if (Cards.Any(c => c.FilePath == path))
                {
                    continue;
                }

                FileInfo info = new(path);
                OptimizerModel model = new()
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path),
                    SizeBytes = info.Length
                };
                Cards.Add(new FileCardModel(model, ImageThumbnailService.GetThumbnail(path)));
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

        private async Task ProcessOneAsync(FileCardModel card)
        {
            card.HasError = false;
            card.IsDone = false;
            card.IsProcessing = true;

            try
            {
                OptimizationResult result = await Task.Run(() => ImageOptimizer.Optimize(card.FilePath));
                card.ApplyResult(result);
            }
            catch (Exception ex)
            {
                card.HasError = true;
                MessageWindowManager.ShowOptimizeError(ex);
            }
            finally
            {
                card.IsProcessing = false;
                RaiseStatsChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task ProcessAllAsync()
        {
            int maxParallelism = Math.Max(2, Environment.ProcessorCount / 2);
            List<FileCardModel> toProcess = [.. Cards.Where(c => !c.IsDone)];

            using SemaphoreSlim sem = new(maxParallelism);
            await Task.WhenAll(toProcess.Select(async card =>
            {
                await sem.WaitAsync();
                try
                {
                    if (Cards.Contains(card))
                    {
                        await ProcessOneAsync(card);
                    }
                }
                finally { sem.Release(); }
            }));
        }

        private void SaveAll()
        {
            List<(string FileName, Exception Ex)> errors = [];

            foreach (FileCardModel card in Cards.Where(c => c.IsDone && !c.IsSaved && c.Model.OptimizationResult is not null).ToList())
            {
                try
                {
                    SaveService.Instance.SaveResult(card.FilePath, card.Model.OptimizationResult!);
                    card.IsSaved = true;
                }
                catch (Exception ex) { errors.Add((card.FileName, ex)); }
            }

            if (errors.Count > 0)
            {
                MessageWindowManager.ShowErrors(errors.Select(e => new Exception(e.FileName, e.Ex)), MessageWindowType.ErrorSave);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private string? SaveOne(FileCardModel card)
        {
            if (card.Model.OptimizationResult is null)
            {
                return null;
            }

            try
            {
                card.IsSaved = true;
                return SaveService.Instance.SaveResult(card.FilePath, card.Model.OptimizationResult);
            }
            catch (Exception ex)
            {
                MessageWindowManager.ShowSaveError(ex);
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