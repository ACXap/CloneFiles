using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;
using System.Diagnostics;
using MsdnMag;

namespace CloneFile
{
    public class MainWindowViewModel : ViewModelBase
    {
        CancellationTokenSource _cancelTokenSource;

        private ObservableCollection<MyFiles> _collectionFiles = new ObservableCollection<MyFiles>();
        public ObservableCollection<MyFiles> CollectionFiles
        {
            get => _collectionFiles;
            set => Set("CollectionFiles", ref _collectionFiles, value);
        }

        private ICollectionView _collectionViewMyFiles;
        public ICollectionView CollectionViewMyFiles
        {
            get { return _collectionViewMyFiles; }
            set { Set("CollectionViewMyFiles", ref _collectionViewMyFiles, value); }
        }

        private ICollectionView _collectionViewMyFilesClone;
        public ICollectionView CollectionViewMyFilesClone
        {
            get { return _collectionViewMyFilesClone; }
            set { Set("CollectionViewMyFilesClone", ref _collectionViewMyFilesClone, value); }
        }

        private ICollectionView _collectionViewMyFilesModification;
        public ICollectionView CollectionViewMyFilesModification
        {
            get { return _collectionViewMyFilesModification; }
            set { Set("CollectionViewMyFilesModification", ref _collectionViewMyFilesModification, value); }
        }

        private MyFiles _currentFile;
        public MyFiles CurrentFile
        {
            get => _currentFile;
            set
            {
                Set("CurrentFile", ref _currentFile, value);

                if (CurrentFile != null)
                {
                    var file = CurrentFile.Path + "\\" + CurrentFile.Name;
                    if (File.Exists(file))
                    {
                        var isImage = System.Web.MimeMapping.GetMimeMapping(file).StartsWith("image/");
                        if (isImage)
                        {
                            CurrentImage = file;
                        }
                        else
                        {
                            CurrentImage = null;
                        }
                    }
                }
                else
                {
                    CurrentImage = null;
                }
            }
        }

        private string _currentImage;
        public string CurrentImage
        {
            get => _currentImage;
            set => Set("CurrentImage", ref _currentImage, value);
        }

        private bool _isGroupPath = false;
        public bool IsGroupPath
        {
            get => _isGroupPath;
            set
            {
                Set("IsGroupPath", ref _isGroupPath, value);
                CheckGroup();
            }
        }

        private bool _isOpenPopUp = false;
        public bool IsOpenPopUp
        {
            get => _isOpenPopUp;
            set => Set("IsOpenPopUp", ref _isOpenPopUp, value);
        }

        private RelayCommand<string> _commandOpenCloseExpander;
        public RelayCommand<string> CommandOpenCloseExpander
        {
            get
            {
                return _commandOpenCloseExpander
                    ?? (_commandOpenCloseExpander = new RelayCommand<string>(
                    p =>
                    {
                        Messenger.Default.Send(new NotificationMessage(null, p, "expander"));
                    }));
            }
        }

        private string _state = "Не проводился";
        public string State
        {
            get => _state;
            set => Set("State", ref _state, value);
        }

        private RelayCommand<DragEventArgs> _commandDrop;
        public RelayCommand<DragEventArgs> CommandDrop
        {
            get
            {
                return _commandDrop
                    ?? (_commandDrop = new RelayCommand<DragEventArgs>(p =>
                    {
                        if (p.Data.GetDataPresent(DataFormats.FileDrop, true) == true)
                        {
                            var files = (string[])p.Data.GetData(DataFormats.FileDrop, true);
                            if (files != null && files.Length > 0)
                            {
                                AddCollectionFiles(files);
                            }
                        }
                    }));
            }
        }

        private RelayCommand _commandClearCollectionFiles;
        public RelayCommand CommandClearCollectionFiles
        {
            get
            {
                return _commandClearCollectionFiles
                    ?? (_commandClearCollectionFiles = new RelayCommand(
                    () =>
                    {
                        CollectionFiles?.Clear();
                        var col = (ObservableCollection<MyFiles>)CollectionViewMyFilesClone?.SourceCollection;
                        col?.Clear();
                        CountFolder = 0;
                        CountClone = 0;
                        CurrentFile = null;
                    }, () =>
                    {
                        if (CollectionFiles == null || !CollectionFiles.Any())
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }));
            }
        }

        private RelayCommand _commandAnalyzeCheckSum;
        public RelayCommand CommandAnalyzeCheckSum
        {
            get
            {
                return _commandAnalyzeCheckSum
                    ?? (_commandAnalyzeCheckSum = new RelayCommand(
                    () =>
                    {
                        _cancelTokenSource = new CancellationTokenSource();
                        IsOpenPopUp = true;
                        State = "Запуск процедуры";
                        var task = Task.Run(() => Analize(_cancelTokenSource.Token), _cancelTokenSource.Token);
                    },
                    () =>
                    {
                        if (CollectionFiles == null || !CollectionFiles.Any())
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }));
            }
        }

        private RelayCommand _commandStopAnalize;
        public RelayCommand CommandStopAnalize
        {
            get
            {
                return _commandStopAnalize
                    ?? (_commandStopAnalize = new RelayCommand(
                    () =>
                    {
                        _cancelTokenSource.Cancel();
                    }));
            }
        }

        private RelayCommand _commandAnalyzeModification;
        public RelayCommand CommandAnalyzeModification
        {
            get
            {
                return _commandAnalyzeModification
                    ?? (_commandAnalyzeModification = new RelayCommand(
                    () =>
                    {
                        _cancelTokenSource = new CancellationTokenSource();
                        IsOpenPopUp = true;
                        State = "Запуск процедуры";
                        var task = Task.Run(() => AnalizeModification(_cancelTokenSource.Token), _cancelTokenSource.Token);
                    },
                    () =>
                    {
                        if (CollectionFiles == null || !CollectionFiles.Any())
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }));
            }
        }

        private RelayCommand _commandOpenFolder;
        public RelayCommand CommandOpenFolder
        {
            get
            {
                return _commandOpenFolder
                    ?? (_commandOpenFolder = new RelayCommand(
                    () =>
                    {
                        var file = CurrentFile.Path + "\\" + CurrentFile.Name;
                        if (File.Exists(file))
                        {
                            Process.Start("explorer.exe", string.Format("/select,\"{0}\"", file));
                        }
                    }));
            }
        }

        private RelayCommand _commandOpenFile;
        public RelayCommand CommandOpenFile
        {
            get
            {
                return _commandOpenFile
                    ?? (_commandOpenFile = new RelayCommand(
                    () =>
                    {
                        var file = CurrentFile.Path + "\\" + CurrentFile.Name;
                        if (File.Exists(file))
                        {
                            Process.Start(file);
                        }
                    }));
            }
        }

        private RelayCommand _commandDelFile;
        public RelayCommand CommandDelFile
        {
            get
            {
                return _commandDelFile
                    ?? (_commandDelFile = new RelayCommand(
                    () =>
                    {
                        var currentFile = CurrentFile;
                        var file = currentFile.Path + "\\" + currentFile.Name;
                        if (File.Exists(file))
                        {
                            try
                            {
                                if (CurrentImage != null)
                                {
                                    CurrentImage = null;
                                }
                                using (FileOperation fo = new FileOperation(new FileOperationProgressSink()))
                                {
                                    try
                                    {
                                        fo.DeleteItem(file);
                                        fo.PerformOperations();

                                    }
                                    catch (Exception ex)
                                    {
                                        return;
                                    }
                                }
                                var col = (ObservableCollection<MyFiles>)CollectionViewMyFilesClone.SourceCollection;
                                col.Remove(currentFile);

                                // подсчет нужен дублирующих файлов
                                //CountClone--;
                                RemoveOfCollectionFiles(currentFile);
                            }
                            catch { }
                        }
                    }));
            }
        }

        private RelayCommand _commandDelFileOfCollection;
        public RelayCommand CommandDelFileOfCollection
        {
            get
            {
                return _commandDelFileOfCollection
                    ?? (_commandDelFileOfCollection = new RelayCommand(
                    () =>
                    {
                        var col = (ObservableCollection<MyFiles>)CollectionViewMyFilesClone.SourceCollection;
                        col.Remove(CurrentFile);
                        CurrentFile = null;
                    }));
            }
        }

        //подсчет папок правильный нужен
        private int _countFolder = 0;
        public int CountFolder
        {
            get => _countFolder;
            set => Set("CountFolder", ref _countFolder, value);
        }

        private int _countClone = 0;
        public int CountClone
        {
            get => _countClone;
            set => Set("CountClone", ref _countClone, value);
        }

        private int _countModification = 0;
        public int CountModification
        {
            get => _countModification;
            set => Set("CountModification", ref _countModification, value);
        }

        private int _counfFile = 0;
        public int CountFile
        {
            get => _counfFile;
            set => Set("CountFile", ref _counfFile, value);
        }

        private void AddCollectionFiles(string[] files)
        {
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    FileInfo fi = new FileInfo(file);
                    var f = new MyFiles
                    {
                        Size = fi.Length,
                        Name = fi.Name,
                        Path = fi.DirectoryName

                    };
                    CollectionFiles.Add(f);

                    continue;
                }
                if (Directory.Exists(file))
                {
                    CountFolder++;
                    try
                    {
                        AddCollectionFiles(Directory.GetFiles(file));
                        AddCollectionFiles(Directory.GetDirectories(file));
                    }
                    catch { }
                }
            }
        }

        private string CheckSum(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void CheckGroup()
        {
            if (IsGroupPath)
            {
                CollectionViewMyFiles?.GroupDescriptions.Add(new PropertyGroupDescription("Path"));
            }
            else
            {
                CollectionViewMyFiles?.GroupDescriptions.Clear();
            }
        }

        private void Analize(CancellationToken cancellationToken)
        {
            var collectionClone = new ObservableCollection<MyFiles>();
            CollectionViewMyFilesClone = null;
            CountClone = 0;
            CountFile = 0;
            State = "Подсчет контрольных сумм";

            try
            {
                foreach (var file in CollectionFiles)
                {
                    var myfile = file.Path + "\\" + file.Name;
                    if (File.Exists(myfile))
                    {
                        file.CheckSum = CheckSum(myfile);
                        CountFile++;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        State = "Отменено пользователем";
                        IsOpenPopUp = false;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                State = ex.Message;
                return;
            }

            State = "Сортировка файлов";
            var col = CollectionFiles.OrderBy(s => s.CheckSum).ToList();
            bool isAddFirst = false;
            State = "Анализ дубликатов";

            if (cancellationToken.IsCancellationRequested)
            {
                State = "Отменено пользователем";
                IsOpenPopUp = false;
                return;
            }

            try
            {
                for (int i = 1; i < col.Count; i++)
                {
                    var file = col[i];
                    if (col[i - 1].CheckSum == file.CheckSum)
                    {
                        if (!isAddFirst)
                        {
                            collectionClone.Add(file);
                            collectionClone.Add(col[i - 1]);
                            isAddFirst = true;
                            CountClone++;
                        }
                        else
                        {
                            collectionClone.Add(file);
                        }
                    }
                    else
                    {
                        isAddFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                State = ex.Message;
                return;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                State = "Отменено пользователем";
                IsOpenPopUp = false;
                return;
            }
            State = "Завершено";
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                CollectionViewMyFilesClone = new CollectionViewSource { Source = collectionClone }.View;
                CollectionViewMyFilesClone.GroupDescriptions.Add(new PropertyGroupDescription("CheckSum"));
                IsOpenPopUp = false;
                CurrentFile = null;
            }));
        }

        private void AnalizeModification(CancellationToken token)
        {
            var collectionModification = new ObservableCollection<MyFiles>();
            CollectionViewMyFilesModification = null;
            CountModification = 0;
            State = "Поиск возможных модификаций";
            var col = CollectionFiles.OrderBy(s => s.Name).ToList();
            bool isAddFirst = false;

            try
            {
                for (int i = 1; i < col.Count; i++)
                {
                    var file = col[i];
                    if (col[i - 1].Name == file.Name)
                    {
                        if (!isAddFirst)
                        {
                            AddTime(file);
                            AddTime(col[i - 1]);
                            collectionModification.Add(file);
                            collectionModification.Add(col[i - 1]);
                            isAddFirst = true;
                            CountModification++;
                        }
                        else
                        {
                            AddTime(file);
                            collectionModification.Add(file);
                        }
                    }
                    else
                    {
                        isAddFirst = false;
                    }
                }
            }
            catch (Exception ex)
            {
                State = ex.Message;
                return;
            }

            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                CollectionViewMyFilesModification = new CollectionViewSource { Source = collectionModification }.View;
                CollectionViewMyFilesModification.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                IsOpenPopUp = false;
                CurrentFile = null;
                State = "Готово";
            }));

        }

        private void AddTime(MyFiles file)
        {
            FileInfo fi = new FileInfo(file.Path + "\\" + file.Name);
            file.CreationTime = fi.CreationTime;
            file.LastAccessTime = fi.LastAccessTime;
            file.LastWriteTime = fi.LastWriteTime;
        }

        private void RemoveOfCollectionFiles(MyFiles currentFile)
        {
            var file = CollectionFiles.FirstOrDefault(s => s.Name == currentFile.Name && s.Path == currentFile.Path);
            if (file != null)
            {
                CollectionFiles.Remove(file);
                CurrentFile = null;
            }
        }

        public MainWindowViewModel()
        {
            CollectionViewMyFiles = new CollectionViewSource { Source = CollectionFiles }.View;
        }
    }

    public class MyFiles : ViewModelBase
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }

        private string _checkSum;
        public string CheckSum
        {
            get => _checkSum;
            set => Set("CheckSum", ref _checkSum, value);
        }

        private DateTime _creationTime;
        public DateTime CreationTime
        {
            get => _creationTime;
            set => Set("CreationTime", ref _creationTime, value);
        }
        private DateTime _lastWriteTime;
        public DateTime LastWriteTime
        {
            get => _lastWriteTime;
            set => Set("LastWriteTime", ref _lastWriteTime, value);
        }
        private DateTime _lastAccessTime;
        public DateTime LastAccessTime
        {
            get => _lastAccessTime;
            set => Set("LastAccessTime", ref _lastAccessTime, value);
        }
    }
}
