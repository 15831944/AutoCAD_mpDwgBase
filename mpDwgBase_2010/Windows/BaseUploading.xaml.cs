using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using Ionic.Zip;
using mpDwgBase.Annotations;
using ModPlus;
using ModPlusAPI.Web.FTP;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;

namespace mpDwgBase.Windows
{
    public partial class BaseUploading
    {
        private readonly string _mpDwgBaseFile;
        private readonly string _userDwgBaseFile;
        // Путь к папке с базами dwg плагина
        private readonly string _dwgBaseFolder;
        // Список значений 
        private readonly List<DwgBaseItem> _dwgBaseItems;
        private List<FileToBind> _filesToBind;
        //Create a Delegate that matches the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);
        private delegate void UpdateProgressTextDelegate(DependencyProperty dp, object value);
        // current archive to upload
        private string _currentFileToUpload;

        public BaseUploading(string mpDwgBaseFile, string userDwgBaseFile, string dwgBaseFolder, List<DwgBaseItem> dwgBaseItems)
        {
            InitializeComponent();
            this.OnWindowStartUp();
            _mpDwgBaseFile = mpDwgBaseFile;
            _userDwgBaseFile = userDwgBaseFile;
            _dwgBaseFolder = dwgBaseFolder;
            _dwgBaseItems = dwgBaseItems;
        }

        private void BaseUploading_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BaseUploading_OnLoaded(object sender, RoutedEventArgs e)
        {
            // button visibility
            BtSeeArchive.Visibility = Visibility.Collapsed;
            BtUploadArchive.Visibility = Visibility.Collapsed;
            BtDeleteArchive.Visibility = Visibility.Collapsed;

            _filesToBind = new List<FileToBind>();
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                var file = Path.Combine(_dwgBaseFolder, dwgBaseItem.SourceFile);
                if (File.Exists(file))
                {
                    var fi = new FileInfo(file);
                    var filetobind = new FileToBind
                    {
                        FileName = fi.Name,
                        FullFileName = fi.FullName,
                        SourceFile = dwgBaseItem.SourceFile,
                        Selected = false,
                        FullDirectory = fi.DirectoryName,
                        SubDirectory = fi.DirectoryName?.Replace(_dwgBaseFolder + @"\", "")
                    };
                    if (!HasFileToBindInList(filetobind))
                        _filesToBind.Add(filetobind);
                }
            }
            LvDwgFiles.ItemsSource = _filesToBind;
        }

        private bool HasFileToBindInList(FileToBind fileToBind)
        {
            var has = false;
            foreach (var toBind in _filesToBind)
            {
                if (toBind.FileName.Equals(fileToBind.FileName) &
                    toBind.FullFileName.Equals(fileToBind.FullFileName) &
                    toBind.SourceFile.Equals(fileToBind.SourceFile))
                {
                    has = true; break;
                }
            }
            return has;
        }

        private void LvDwgFiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = sender as ListView;
            var selectedFile = lv?.SelectedItem as FileToBind;
            if (selectedFile == null) return;
            LvItemsInFile.ItemsSource = null;
            var lstToBind = new List<ItemToBind>();
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                if (dwgBaseItem.SourceFile.Equals(selectedFile.SourceFile))
                {
                    var itemToBnd = new ItemToBind
                    {
                        Name = dwgBaseItem.Name,
                        Description = dwgBaseItem.Description,
                        Author = dwgBaseItem.Author,
                        Source = dwgBaseItem.Source
                    };
                    if (dwgBaseItem.IsBlock)
                    {
                        itemToBnd.IsBlock = Visibility.Visible;
                        itemToBnd.IsDrawing = Visibility.Collapsed;
                    }
                    else
                    {
                        itemToBnd.IsBlock = Visibility.Collapsed;
                        itemToBnd.IsDrawing = Visibility.Visible;
                    }

                    if (!lstToBind.Contains(itemToBnd))
                        lstToBind.Add(itemToBnd);
                }
            }
            LvItemsInFile.ItemsSource = lstToBind;
        }

        private void BtMakeArchive_OnClick(object sender, RoutedEventArgs e)
        {
            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
            if (!_filesToBind.Any(x => x.Selected))
            {
               ModPlusAPI.Windows.MessageBox.Show("Вы не выбрали ни одного файла для архивации!", MessageBoxIcon.Alert);
                return;
            }
            CreateArchive();
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
            Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
        }

        private void CreateArchive()
        {
            //Create a new instance of our ProgressBar Delegate that points
            //  to the ProgressBar's SetValue method.
            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
            // progress text
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Создание временной папки");
            // create temp folder
            var tmpFolder = Path.Combine(_dwgBaseFolder, "Temp");
            if (!Directory.Exists(tmpFolder))
                Directory.CreateDirectory(tmpFolder);
            // create base file with selected files items
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = _dwgBaseItems.Count;
            ProgressBar.Value = 0;
            // Соберем список файлов-источников чтобы проще их сверять
            var sourceFiles = new List<string>();
            foreach (FileToBind fileToBind in _filesToBind)
            {
                if (!sourceFiles.Contains(fileToBind.SourceFile))
                    sourceFiles.Add(fileToBind.SourceFile);
            }
            var baseFileToArchive = new List<DwgBaseItem>();
            for (var i = 0; i < _dwgBaseItems.Count; i++)
            {
                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Сбор требуемых элементов из базы: " + i + "/" + _dwgBaseItems.Count);
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)i);
                DwgBaseItem dwgBaseItem = _dwgBaseItems[i];
                if (sourceFiles.Contains(dwgBaseItem.SourceFile))
                    if (!baseFileToArchive.Contains(dwgBaseItem))
                        baseFileToArchive.Add(dwgBaseItem);
            }
            // save xml file
            var xmlToArchive = Path.Combine(tmpFolder, "UserDwgBase.xml");
            DwgBaseHelpers.SerializerToXml(baseFileToArchive, xmlToArchive);
            if (!File.Exists(xmlToArchive))
            {
                ModPlusAPI.Windows.MessageBox.Show("Не удалось создать файл-указатель", MessageBoxIcon.Close);
                return;
            }
            // comment file
            var commentFile = CreateCommentFile(tmpFolder);
            // create zip
            using (var zip = new ZipFile(Encoding.GetEncoding("cp866")))
            {
                // create directories
                foreach (FileToBind fileToBind in _filesToBind.Where(x => x.Selected))
                {
                    zip.AddFile(fileToBind.FullFileName, fileToBind.SubDirectory);
                }
                // add xml file and delete him
                zip.AddFile(xmlToArchive, "");
                // add comment file
                if (!string.IsNullOrEmpty(commentFile) && File.Exists(commentFile))
                    zip.AddFile(commentFile, "");
                // save to zip
                _currentFileToUpload = Path.ChangeExtension(Path.Combine(tmpFolder, Path.GetRandomFileName()), ".zip");
                zip.Save(_currentFileToUpload);
            }
            File.Delete(xmlToArchive);
            // show buttons
            BtMakeArchive.Visibility = Visibility.Collapsed;
            BtSeeArchive.Visibility = Visibility.Visible;
            BtDeleteArchive.Visibility = Visibility.Visible;
            BtUploadArchive.Visibility = Visibility.Visible;
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Файл готов к отправке");
        }
        // window closed
        private void BaseUploading_OnClosed(object sender, EventArgs e)
        {
            var tmpFolder = Path.Combine(_dwgBaseFolder, "Temp");
            try
            {
                // check if temp directory exist
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
            }
            catch
            {
                ModPlusAPI.Windows.MessageBox.Show("Не удалось удалить временную папку " + tmpFolder + Environment.NewLine +
                              "Возможно у вас открыт архив");
            }
        }
        // open
        private void BtSeeArchive_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_currentFileToUpload))
                Process.Start(_currentFileToUpload);
            else ModPlusAPI.Windows.MessageBox.Show("Не удалось найти файл для отправки. Возможно Вы его удалили?");
        }
        // upload
        private void BtUploadArchive_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ModPlusAPI.Web.Connection.CheckForInternetConnection())
            {
                ModPlusAPI.Windows.MessageBox.Show("Отсутсвует доступ к сети internet или сайту modplus.org");
                return;
            }
            if (File.Exists(_currentFileToUpload))
            {
                try
                {
                    //Create a new instance of our ProgressBar Delegate that points
                    //  to the ProgressBar's SetValue method.
                    var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
                    var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBox.TextProperty, "Получение данных авторизации");
                    // get connect data
                    if (!DwgBaseHelpers.GetConfigFileFromSite(out XmlDocument docFromSite)) return;
                    var ftpClient = new FtpClient
                    {
                        UserName = docFromSite.DocumentElement["FTP"].GetAttribute("login"),
                        Host = docFromSite.DocumentElement["FTP"].GetAttribute("host"),
                        Password = docFromSite.DocumentElement["FTP"].GetAttribute("password")
                    };
                    var directoryToUpload = "/dwgBases/";
                    string shortName = _currentFileToUpload.Remove(0, _currentFileToUpload.LastIndexOf('\\') + 1);
                    FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + ftpClient.Host + directoryToUpload + shortName);
                    ftpRequest.Credentials = new NetworkCredential(ftpClient.UserName, ftpClient.Password);
                    ftpRequest.EnableSsl = false;
                    ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Загрузка файла на сервер");
                    using (var inputStream = File.OpenRead(_currentFileToUpload))
                    {
                        using (var outputStream = ftpRequest.GetRequestStream())
                        {
                            ProgressBar.Minimum = 0;
                            ProgressBar.Maximum = 100;
                            ProgressBar.Value = 0;
                            var buffer = new byte[1024 * 10];
                            int totalReadBytesCount = 0;
                            int readBytesCount;
                            while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outputStream.Write(buffer, 0, readBytesCount);
                                var progress = (int)((totalReadBytesCount += readBytesCount) / (float)inputStream.Length * 100);
                                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Загрузка файла на сервер: " + progress + "%");
                                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)progress);
                            }
                        }
                    }
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Загрузка файла на сервер завершена");
                    Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
                    ModPlusAPI.Windows.MessageBox.Show("Файл " + _currentFileToUpload + " отправлен на сервер modplus.org" + Environment.NewLine + "Как только файл будет проверен, мы добавим его содержимое в общедоступную базу");
                    //
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            }
            else ModPlusAPI.Windows.MessageBox.Show("Не удалось найти файл для отправки. Возможно Вы его удалили?");
        }
        // create comment file
        private string CreateCommentFile(string tmpFolder)
        {
            var file = string.Empty;
            if (!string.IsNullOrEmpty(TbComment.Text))
            {
                file = Path.Combine(tmpFolder, "comment.txt");
                File.WriteAllText(file, TbComment.Text);
            }
            return file;
        }
        //
        private void BtDeleteArchive_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_currentFileToUpload))
            {
                if (ModPlusAPI.Windows.MessageBox.ShowYesNo("Удалить созданный архив?", MessageBoxIcon.Question))
                {
                    var wasDel = false;
                    try
                    {
                        File.Delete(_currentFileToUpload);
                        wasDel = true;
                    }
                    catch
                    {
                        ModPlusAPI.Windows.MessageBox.Show("Не удалось удалить архив! Возможно он открыт в данный момент");
                    }
                    if (wasDel)
                    {
                        BtDeleteArchive.Visibility = Visibility.Collapsed;
                        BtUploadArchive.Visibility = Visibility.Collapsed;
                        BtSeeArchive.Visibility = Visibility.Collapsed;
                        BtMakeArchive.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void BaseUploading_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }

    internal class FileToBind : INotifyPropertyChanged
    {
        public string FileName { get; set; }
        public string FullFileName { get; set; }
        public string SourceFile { get; set; }
        private bool _selected;
        public bool Selected { get { return _selected; } set { _selected = value; OnPropertyChanged(nameof(Selected)); } }
        // Полный путь к директории
        public string FullDirectory { get; set; }
        // Усеченный путь
        public string SubDirectory { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class ItemToBind
    {
        public string Name { get; set; }
        public Visibility IsBlock { get; set; }
        public Visibility IsDrawing { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Source { get; set; }
    }
}
