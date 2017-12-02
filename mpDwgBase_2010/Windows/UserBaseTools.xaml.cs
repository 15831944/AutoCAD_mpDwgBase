using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using mpDwgBase.Annotations;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;

namespace mpDwgBase.Windows
{
    public partial class UserBaseTools
    {
        // Переменная будет хранить значение были ли внесены изменения в базу
        public bool UserBaseChanged;
        // Путь к файлу, содержащему описание базы
        private string _userDwgBaseFile;
        // Путь к файлу БД плагина
        private string _mpDwgBaseFile;
        // Путь к папке с базами dwg плагина
        private string _dwgBaseFolder;
        // Список значений ПОЛЬЗОВАТЕЛЬСКОЙ базы
        private List<DwgBaseItem> _dwgBaseItems;
        //Create a Delegate that matches the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);
        private delegate void UpdateProgressTextDelegate(DependencyProperty dp, object value);

        public UserBaseTools(string mpDwgBaseFile, string userDwgBaseFile, string dwgBaseFolder, List<DwgBaseItem> dwgBaseItems)
        {
            InitializeComponent();
            this.OnWindowStartUp();
            _mpDwgBaseFile = mpDwgBaseFile;
            _userDwgBaseFile = userDwgBaseFile;
            _dwgBaseFolder = dwgBaseFolder;
            _dwgBaseItems = dwgBaseItems;
            //
            UnusedFiles_DwgBaseFolder.Text = _dwgBaseFolder;
        }

        #region main window
        private void UserBaseTools_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void UserBaseTools_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            // select all
            if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (MultiChangePath_Tab.IsSelected)
                {
                    if (MultiChangePath_LvItems.Items.Count > 0)
                        foreach (DwgBaseItemWithSelector item in MultiChangePath_LvItems.Items)
                        {
                            item.Selected = true;
                        }
                }
                if (MultiChangeSourceAuthor_Tab.IsSelected)
                {
                    if (MultiChangeSourceAuthor_LvItems.Items.Count > 0)
                        foreach (DwgBaseItemWithSelector item in MultiChangeSourceAuthor_LvItems.Items)
                        {
                            item.Selected = true;
                        }
                }
                if (UnusedItems_Tab.IsSelected)
                {
                    if (UnusedItems_LvFiles.Items.Count > 0)
                        foreach (DwgBaseItemWithSelector item in UnusedItems_LvFiles.Items)
                        {
                            item.Selected = true;
                        }
                }
                if (UnusedFiles_Tab.IsSelected)
                {
                    if (UnusedFiles_LvFiles.Items.Count > 0)
                        foreach (UnusedFile item in UnusedFiles_LvFiles.Items)
                        {
                            item.Selected = true;
                        }
                }
            }
        }
        // main tab selection changed
        private void TabControlTools_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Изменить Путь для нескольких блоков
            if (MultiChangePath_Tab.IsSelected)
            {
                if (MultiChangePath_CbMainGroup.SelectedIndex == -1)
                    MultiChangePath_CbMainGroup.SelectedIndex = 0;
            }
            if (MultiChangeSourceAuthor_Tab.IsSelected)
            {
                if(MultiChangeSourceAuthor_LvItems.Items.Count == 0)
                    MultiChangeSourceAuthor_FillData();
            }
        }
        #endregion
        #region Path events
        private void Blocks_TbPath_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && tb.CaretIndex < 6) e.Handled = true;
            if (tb != null && !tb.Text.StartsWith("Блоки/"))
            {
                e.Handled = true;
            }
        }

        private void Blocks_TbPath_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && !tb.Text.StartsWith("Блоки/"))
            {
                Dispatcher.Invoke(new Action(() => tb.Undo()));
            }
        }

        private void Blocks_TbPath_OnTextInput(object sender, TextCompositionEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && !tb.Text.StartsWith("Блоки/"))
            {
                Dispatcher.Invoke(new Action(() => tb.Undo()));
            }
        }
        private void Drawings_TbPath_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && tb.CaretIndex < 8) e.Handled = true;
            if (tb != null && !tb.Text.StartsWith("Чертежи/"))
            {
                e.Handled = true;
            }
        }

        private void Drawings_TbPath_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && !tb.Text.StartsWith("Чертежи/"))
            {
                Dispatcher.Invoke(new Action(() => tb.Undo()));
            }
        }

        private void Drawings_TbPath_OnTextInput(object sender, TextCompositionEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && !tb.Text.StartsWith("Чертежи/"))
            {
                Dispatcher.Invoke(new Action(() => tb.Undo()));
            }
        }
        #endregion
        #region MultiChangePath
        private void MultiChangePath_CbMainGroup_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null) return;
            MultiChangePath_FillData();
        }
        /// <summary>
        /// Заполнение окна данными. Делается методом для возможности повторного вызова после внесения изменений
        /// </summary>
        private void MultiChangePath_FillData()
        {
            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);

            var cb = MultiChangePath_CbMainGroup;
            var tb = MultiChangePath_Path.Template.FindName("PART_EditableTextBox", MultiChangePath_Path) as TextBox;
            // remove events
            MultiChangePath_Path.TextInput -= Blocks_TbPath_OnTextInput;
            MultiChangePath_Path.PreviewTextInput -= Blocks_TbPath_OnPreviewTextInput;
            if (tb != null) tb.TextChanged -= Blocks_TbPath_OnTextChanged;
            MultiChangePath_Path.TextInput -= Drawings_TbPath_OnTextInput;
            MultiChangePath_Path.PreviewTextInput -= Drawings_TbPath_OnPreviewTextInput;
            if (tb != null) tb.TextChanged -= Drawings_TbPath_OnTextChanged;
            // fill cb with path
            var pathes = new List<string>();
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                if (!pathes.Contains(dwgBaseItem.Path))
                    pathes.Add(dwgBaseItem.Path);
            }
            if (cb.SelectedIndex == 0) // blocks
            {
                MultiChangePath_Path.Text = "Блоки/";
                MultiChangePath_Path.TextInput += Blocks_TbPath_OnTextInput;
                MultiChangePath_Path.PreviewTextInput += Blocks_TbPath_OnPreviewTextInput;
                if (tb != null) tb.TextChanged += Blocks_TbPath_OnTextChanged;
                MultiChangePath_Path.ItemsSource = pathes.Where(x => x.StartsWith("Блоки/"));
                // fill items
                MultiChangePath_LvItems.ItemsSource = null;
                var lst = new List<DwgBaseItemWithSelector>();
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = _dwgBaseItems.Count;
                ProgressBar.Value = 0;
                var index = 1;
                foreach (var dwgBaseItem in _dwgBaseItems)
                {
                    if (dwgBaseItem.IsBlock)
                        lst.Add(new DwgBaseItemWithSelector { Selected = false, Item = dwgBaseItem });
                    //if (index % 10 == 0 | index == _dwgBaseItems.Count - 1)
                    //{
                        Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Render, TextBlock.TextProperty,
                            "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                        Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Render,
                            System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double) index);
                    //}
                    index++;
                }
                MultiChangePath_LvItems.ItemsSource = lst;
            }
            if (cb.SelectedIndex == 1) // drawings
            {
                MultiChangePath_Path.Text = "Чертежи/";
                MultiChangePath_Path.TextInput += Drawings_TbPath_OnTextInput;
                MultiChangePath_Path.PreviewTextInput += Drawings_TbPath_OnPreviewTextInput;
                if (tb != null) tb.TextChanged += Drawings_TbPath_OnTextChanged;
                MultiChangePath_Path.ItemsSource = pathes.Where(x => x.StartsWith("Чертежи/"));
                // fill items
                MultiChangePath_LvItems.ItemsSource = null;
                var lst = new List<DwgBaseItemWithSelector>();
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = _dwgBaseItems.Count;
                ProgressBar.Value = 0;
                var index = 1;
                foreach (var dwgBaseItem in _dwgBaseItems)
                {
                    if (!dwgBaseItem.IsBlock)
                        lst.Add(new DwgBaseItemWithSelector { Selected = false, Item = dwgBaseItem });
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                    Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                    index++;
                }
                MultiChangePath_LvItems.ItemsSource = lst;
            }
            // clear progress
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
            Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
        }
        
        private void MultiChangePath_Accept_OnClick(object sender, RoutedEventArgs e)
        {
            if (MultiChangePath_Path.Text.Equals("Блоки/") | MultiChangePath_Path.Text.Equals("Чертежи/"))
            {
                ModPlusAPI.Windows.MessageBox.Show("Нельзя указывать только основную группу!");
                MultiChangePath_Path.Focus();
                return;
            }
            var selectedItems = new List<DwgBaseItem>();
            foreach (DwgBaseItemWithSelector item in MultiChangePath_LvItems.Items)
            {
                if (item.Selected)
                {
                    selectedItems.Add(item.Item);
                }
            }
            if (!selectedItems.Any())
            {
                ModPlusAPI.Windows.MessageBox.Show("Вы ничего не выбрали!"); return;
            }
            if (!ModPlusAPI.Windows.MessageBox.ShowYesNo(
                "Внимание! Внесенные изменения нельзя отменить!" + Environment.NewLine + "Продолжить?",
                MessageBoxIcon.Question)) return;

            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = _dwgBaseItems.Count;
            ProgressBar.Value = 0;
            var index = 1;
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                foreach (var selectedItem in selectedItems)
                {
                    if (dwgBaseItem.Equals(selectedItem))
                        dwgBaseItem.Path = MultiChangePath_Path.Text;
                }
                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                index++;
            }
            // was changed
            UserBaseChanged = true;
            // save
            DwgBaseHelpers.SerializerToXml(_dwgBaseItems, _userDwgBaseFile);
            DwgBaseHelpers.DeSeializerFromXml(_userDwgBaseFile, out _dwgBaseItems);
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы завершена");
            ModPlusAPI.Windows.MessageBox.Show("Обработка элементов базы завершена");
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
            Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
            // refill
            MultiChangePath_FillData();
        }

        private sealed class DwgBaseItemWithSelector : INotifyPropertyChanged
        {
            private bool _selected;
            public bool Selected { get { return _selected; } set { _selected = value; OnPropertyChanged(nameof(Selected)); } }

            public DwgBaseItem Item { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region Statistic
        private void Statistic_BtGet_OnClick(object sender, RoutedEventArgs e)
        {
            Statistic_TbStat.Text = string.Empty;
            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = _dwgBaseItems.Count;
            ProgressBar.Value = 0;
            //
            var authors = new List<string>();
            var sourceFiles = new List<string>();
            var sourceFilesNotFound = new List<string>();
            var blks = 0;
            var drws = 0;
            var dynBlks = 0;
            var annotBlks = 0;
            var is3dblks = 0;
            var hasSpecAttr = 0;
            var index = 1;
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                if (dwgBaseItem.IsBlock)
                {
                    blks++;
                    if (dwgBaseItem.IsDynamicBlock) dynBlks++;
                    if (dwgBaseItem.Is3Dblock) is3dblks++;
                    if (dwgBaseItem.IsAnnotative) annotBlks++;
                    if (dwgBaseItem.HasAttributesForSpecification) hasSpecAttr++;
                }
                else
                {
                    drws++;
                }
                if (!authors.Contains(dwgBaseItem.Author))
                    authors.Add(dwgBaseItem.Author);
                var sourceFile = Path.Combine(_dwgBaseFolder, dwgBaseItem.SourceFile);
                if (File.Exists(sourceFile))
                {
                    if (!sourceFiles.Contains(dwgBaseItem.SourceFile))
                        sourceFiles.Add(dwgBaseItem.SourceFile);
                }
                else
                {
                    if (!sourceFilesNotFound.Contains(dwgBaseItem.SourceFile))
                        sourceFilesNotFound.Add(dwgBaseItem.SourceFile);
                }
                //
                //if (index % 10 == 0 | index == _dwgBaseItems.Count - 1)
                //{
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty,
                        "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                    Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background,
                        System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double) index);
                //}
                index++;
            }
            // result
            var resultStr = new StringBuilder();
            resultStr.AppendLine("Пользовательская база содержит " + _dwgBaseItems.Count + " элементов.");
            resultStr.AppendLine("Из них:");
            resultStr.AppendLine("Блоков: " + blks);
            resultStr.AppendLine("Чертежей: " + drws);
            if (blks > 0)
            {
                resultStr.AppendLine("================================================");
                resultStr.AppendLine("Среди " + blks + " блока(ов) имеется:");
                if (dynBlks > 0)
                    resultStr.AppendLine("     Динамических блоков: " + dynBlks);
                if (annotBlks > 0)
                    resultStr.AppendLine("     Аннотативных блоков: " + annotBlks);
                if (is3dblks > 0)
                    resultStr.AppendLine("     Трехмерных блоков: " + is3dblks);
                if (hasSpecAttr > 0)
                    resultStr.AppendLine("     Блоков, имеющих атрибуты для спецификации: " + hasSpecAttr);
            }
            if (authors.Any())
            {
                resultStr.AppendLine("================================================");
                resultStr.AppendLine("Пользовательская база наполнена " + authors.Count + " автором(ами):");
                foreach (var author in authors)
                {
                    resultStr.AppendLine("     " + author);
                }
            }
            if (sourceFiles.Any())
            {
                resultStr.AppendLine("================================================");
                resultStr.AppendLine("Существующих dwg-файлов, используемых базой: " + sourceFiles.Count);
            }
            if (sourceFilesNotFound.Any())
            {
                resultStr.AppendLine("================================================");
                resultStr.AppendLine("Среди ссылок на dwg-файлы найдено " + sourceFilesNotFound.Count + " недействительных ссылок. Т.е. файлы-источники отсутсвуют!");
            }

            Statistic_TbStat.Text = resultStr.ToString();
            // clear progress
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
            Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
        }
        #endregion
        #region UnusedFiles
        private void UnusedFiles_BtSearch_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
                var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
                UnusedFiles_LvFiles.ItemsSource = null;
                // Load plugin base
                DwgBaseHelpers.DeSeializerFromXml(_mpDwgBaseFile, out List<DwgBaseItem> pluginDwgBaseItems);
                // Creat full one list
                var allDwgBaseItems = _dwgBaseItems.Concat(pluginDwgBaseItems).ToList();

                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = allDwgBaseItems.Count;
                ProgressBar.Value = 0;
                var index = 1;
                var unusedFiles = new List<UnusedFile>();
                var filesInItems = new List<string>();
                foreach (var baseItem in allDwgBaseItems)
                {
                    var file = Path.Combine(_dwgBaseFolder, baseItem.SourceFile.Replace("/", "\\"));
                    if (!filesInItems.Contains(file))
                        filesInItems.Add(file);
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty,
                        "Обработка элементов базы: " + index + "/" + allDwgBaseItems.Count);
                    Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background,
                        System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                    index++;
                }
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
                var filesInBaseDirectory = Directory.GetFiles(_dwgBaseFolder, "*.dwg", SearchOption.AllDirectories).ToList();
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = filesInBaseDirectory.Count;
                ProgressBar.Value = 0;
                index = 1;
                foreach (var fileInDir in filesInBaseDirectory)
                {
                    if (!filesInItems.Contains(fileInDir))
                    {
                        var fi = new FileInfo(fileInDir);
                        unusedFiles.Add(new UnusedFile
                        {
                            Selected = false,
                            FileName = fi.Name,
                            FullFileName = fi.FullName
                        });
                    }
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка файлов базы: " + index + "/" + filesInBaseDirectory.Count);
                    Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                    index++;
                }
                if (unusedFiles.Any())
                {
                    UnusedFiles_BtDelete.Visibility = Visibility.Visible;
                    UnusedFiles_LvFiles.Visibility = Visibility.Visible;
                    UnusedFiles_TbDelInfo.Visibility = Visibility.Visible;
                    UnusedFiles_LvFiles.ItemsSource = unusedFiles;
                }
                else
                {
                    ModPlusAPI.Windows.MessageBox.Show("Неиспользуемых файлов не найдено!");
                }
                // clear progress
                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        // deleting
        private void UnusedFiles_BtDelete_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUnusedFiles = new List<UnusedFile>();
                foreach (UnusedFile file in UnusedFiles_LvFiles.Items)
                {
                    if (file.Selected) selectedUnusedFiles.Add(file);
                }
                if (selectedUnusedFiles.Any())
                {
                    var count = 0;
                    var filesNotDeleted = new List<UnusedFile>();
                    if (ModPlusAPI.Windows.MessageBox.ShowYesNo(
                        "Выбранные файлы будут удалены безвозратно!" + Environment.NewLine + "Продолжить?",
                        MessageBoxIcon.Question))
                    {
                        foreach (var file in selectedUnusedFiles)
                        {
                            if (File.Exists(file.FullFileName))
                            {
                                try
                                {
                                    File.Delete(file.FullFileName);
                                    // delete bak's and images
                                    var fi = new FileInfo(file.FullFileName);
                                    var images = file.FullFileName + " icons";
                                    if (Directory.Exists(images)) Directory.Delete(images, true);
                                    if (fi.DirectoryName != null)
                                    {
                                        var bak = Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(file.FullFileName) + ".bak");
                                        try
                                        {
                                            File.Delete(bak);
                                        }
                                        catch
                                        {
                                            // ignored
                                        }
                                    }

                                    count++;
                                }
                                catch (IOException)
                                {
                                    filesNotDeleted.Add(file);
                                }
                            }
                        }
                        ModPlusAPI.Windows.MessageBox.Show("Удалено файлов: " + count);
                    }
                    if (filesNotDeleted.Any())
                    {
                        var filesStr = string.Empty;
                        foreach (var file in filesNotDeleted)
                        {
                            filesStr += file.FullFileName + Environment.NewLine;
                        }
                        ModPlusAPI.Windows.MessageBox.Show("Не удалось удалить файлы:" + Environment.NewLine + filesStr +
                                      "К файлам нет доступа. Возможно они открыты");
                    }
                    // visibility
                    UnusedFiles_BtDelete.Visibility = Visibility.Collapsed;
                    UnusedFiles_LvFiles.Visibility = Visibility.Collapsed;
                    UnusedFiles_TbDelInfo.Visibility = Visibility.Collapsed;
                    UnusedFiles_LvFiles.ItemsSource = null;
                }
                else
                {
                    ModPlusAPI.Windows.MessageBox.Show("Вы ничего не выбрали!");
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        private sealed class UnusedFile : INotifyPropertyChanged
        {
            private bool _selected;
            public bool Selected { get { return _selected; } set { _selected = value; OnPropertyChanged(nameof(Selected)); } }

            public string FileName { get; set; }
            public string FullFileName { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region UnusedItems
        private void UnusedItems_BtSearch_OnClick(object sender, RoutedEventArgs e)
        {
            var unusedItems = new List<DwgBaseItemWithSelector>();
            try
            {
                var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
                var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
                UnusedItems_LvFiles.ItemsSource = null;

                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = _dwgBaseItems.Count;
                ProgressBar.Value = 0;
                var index = 1;

                foreach (var baseItem in _dwgBaseItems)
                {
                    var file = Path.Combine(_dwgBaseFolder, baseItem.SourceFile.Replace("/", "\\"));
                    if (!File.Exists(file))
                        unusedItems.Add(new DwgBaseItemWithSelector
                        {
                            Selected = false,
                            Item = baseItem
                        });
                    Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                    Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                    index++;
                }

                if (unusedItems.Any())
                {
                    UnusedItems_BtDelete.Visibility = Visibility.Visible;
                    UnusedItems_LvFiles.Visibility = Visibility.Visible;
                    UnusedItems_LvFiles.ItemsSource = unusedItems;
                }
                else
                {
                    ModPlusAPI.Windows.MessageBox.Show("Подходящих элементов не найдено!");
                }
                // clear progress
                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        private void UnusedItems_BtDelete_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUnusedItems = new List<DwgBaseItem>();
                foreach (DwgBaseItemWithSelector item in UnusedItems_LvFiles.Items)
                {
                    if (item.Selected) selectedUnusedItems.Add(item.Item);
                }
                if (selectedUnusedItems.Any())
                {
                    //var count = 0;
                    if (ModPlusAPI.Windows.MessageBox.ShowYesNo(
                        "Выбранные элементы будут удалены безвозратно!" + Environment.NewLine + "Продолжить?",
                        MessageBoxIcon.Question))
                    {
                        var removed = _dwgBaseItems.RemoveAll(x => selectedUnusedItems.Contains(x));
                        // resave
                        DwgBaseHelpers.SerializerToXml(_dwgBaseItems, _userDwgBaseFile);
                        DwgBaseHelpers.DeSeializerFromXml(_userDwgBaseFile, out _dwgBaseItems);
                        ModPlusAPI.Windows.MessageBox.Show("Удалено элементов: " + removed);
                    }
                    // visibility
                    UnusedItems_BtDelete.Visibility = Visibility.Collapsed;
                    UnusedItems_LvFiles.Visibility = Visibility.Collapsed;
                    UnusedItems_LvFiles.ItemsSource = null;
                }
                else
                {
                    ModPlusAPI.Windows.MessageBox.Show("Вы ничего не выбрали!");
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        #endregion
        #region MultiChangeSourceAuthor

        private void MultiChangeSourceAuthor_FillData()
        {
            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);

            // fill items
            MultiChangeSourceAuthor_LvItems.ItemsSource = null;
            var lst = new List<DwgBaseItemWithSelector>();
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = _dwgBaseItems.Count;
            ProgressBar.Value = 0;
            var index = 1;
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                lst.Add(new DwgBaseItemWithSelector { Selected = false, Item = dwgBaseItem });
                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                index++;
            }
            MultiChangeSourceAuthor_LvItems.ItemsSource = lst;
            // clear progress
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
            Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
        }
        // on accept
        private void MultiChangeSourceAuthor_BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MultiChangeSourceAuthor_TbSource.Text) &
                string.IsNullOrEmpty(MultiChangeSourceAuthor_TbAuthor.Text))
            {
                ModPlusAPI.Windows.MessageBox.Show("Укажите хотя бы одно значение (Источник или Добавил) для замены");
                return;
            }
            var selectedItems = new List<DwgBaseItem>();
            foreach (DwgBaseItemWithSelector item in MultiChangeSourceAuthor_LvItems.Items)
            {
                if (item.Selected)
                {
                    selectedItems.Add(item.Item);
                }
            }
            if (!selectedItems.Any())
            {
                ModPlusAPI.Windows.MessageBox.Show("Вы ничего не выбрали!"); return;
            }
            if (!ModPlusAPI.Windows.MessageBox.ShowYesNo(
                "Внимание! Внесенные изменения нельзя отменить!" + Environment.NewLine + "Продолжить?",
                MessageBoxIcon.Question)) return;

            var updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
            var updatePtDelegate = new UpdateProgressTextDelegate(ProgressText.SetValue);
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = _dwgBaseItems.Count;
            ProgressBar.Value = 0;
            var index = 1;
            foreach (var dwgBaseItem in _dwgBaseItems)
            {
                foreach (var selectedItem in selectedItems)
                {
                    if (dwgBaseItem.Equals(selectedItem))
                    {
                        if(!string.IsNullOrEmpty(MultiChangeSourceAuthor_TbSource.Text))
                            dwgBaseItem.Source = MultiChangeSourceAuthor_TbSource.Text;
                        if (!string.IsNullOrEmpty(MultiChangeSourceAuthor_TbAuthor.Text))
                            dwgBaseItem.Author = MultiChangeSourceAuthor_TbAuthor.Text;
                    }
                }
                Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы: " + index + "/" + _dwgBaseItems.Count);
                Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, (double)index);
                index++;
            }
            // was changed
            UserBaseChanged = true;
            // save
            DwgBaseHelpers.SerializerToXml(_dwgBaseItems, _userDwgBaseFile);
            DwgBaseHelpers.DeSeializerFromXml(_userDwgBaseFile, out _dwgBaseItems);
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, "Обработка элементов базы завершена");
            ModPlusAPI.Windows.MessageBox.Show("Обработка элементов базы завершена");
            Dispatcher.Invoke(updatePtDelegate, DispatcherPriority.Background, TextBlock.TextProperty, string.Empty);
            Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0.0);
            // refill
            MultiChangeSourceAuthor_FillData();
        }
        #endregion
    }

    public class ProgressByTimer
    {
        
    }
}
