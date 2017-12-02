#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using Visibility = System.Windows.Visibility;

namespace mpDwgBase.Windows
{
    public partial class DrawingWindow
    {
        private readonly bool IsEdit;
        public DwgBaseItem Item;
        private readonly string _mpDwgBaseFile;
        private readonly string _userDwgBaseFile;
        // Путь к папке с базами dwg плагина
        private readonly string _dwgBaseFolder;

        public DrawingWindow(string mpDwgBaseFile, string userDwgBaseFile, string dwgBaseFolder, bool isEdit)
        {
            InitializeComponent();
            this.OnWindowStartUp();
            _mpDwgBaseFile = mpDwgBaseFile;
            _userDwgBaseFile = userDwgBaseFile;
            _dwgBaseFolder = dwgBaseFolder;
            IsEdit = isEdit;
            //
            FillHelpImagesToPopUp();
        }
        private void FillHelpImagesToPopUp()
        {
            Uri uri = new Uri("pack://application:,,,/mpDwgBase_" + VersionData.FuncVersion + ";component/Resources/helpImages/helpImage_1.png", UriKind.RelativeOrAbsolute);
            helpImage_1.Source = BitmapFrame.Create(uri);
        }
        private void DrawingWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!IsEdit)
            {
                // visibility of button to load last data
                BtLoadLastInteredData.Visibility = CheckFileWithLastDataExists()
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                // set first item data
                Item.Path = "Чертежи/";
                // bind
                GridDrawingDetails.DataContext = Item;
            }
        }

        private void DrawingWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void TbPath_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && tb.CaretIndex < 8) e.Handled = true;
            if (tb != null && !tb.Text.StartsWith("Чертежи/"))
            {
                e.Handled = true;
            }
        }

        private void TbPath_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && !tb.Text.StartsWith("Чертежи/"))
            {
                Dispatcher.BeginInvoke(new Action(() => tb.Undo()));
            }
        }

        private void TbPath_OnTextInput(object sender, TextCompositionEventArgs e)
        {
            var cb = sender as ComboBox;
            var tb = cb?.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb != null && !tb.Text.StartsWith("Чертежи/"))
            {
                Dispatcher.BeginInvoke(new Action(() => tb.Undo()));
            }
        }
        // Выбрать dwg-файл для добавления 
        private void BtSelectDwgFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new Autodesk.AutoCAD.Windows.OpenFileDialog("Выбор чертежа для добавления в базу", _dwgBaseFolder, "dwg", "name",
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.NoFtpSites |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.NoShellExtensions |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.NoUrls |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.DefaultIsFolder |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.ForceDefaultFolder |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.DoNotTransferRemoteFiles);
                var needLoop = true;
                while (needLoop)
                {
                    var ofdresult = ofd.ShowDialog();
                    if (ofdresult == System.Windows.Forms.DialogResult.OK)
                    {
                        var selectedFile = ofd.Filename;
                        if (selectedFile.Contains(_dwgBaseFolder))
                        {
                            if (DwgBaseHelpers.Is2010DwgVersion(selectedFile))
                            {
                                if (!DwgBaseHelpers.HasProxyEntities(selectedFile))
                                {
                                    TbSourceFile.Text =
                                        DwgBaseHelpers.TrimStart(selectedFile, _dwgBaseFolder).TrimStart('\\');
                                    BtAccept.IsEnabled = true;
                                    needLoop = false;
                                }
                                else
                                    ModPlusAPI.Windows.MessageBox.Show("Выбранный файл содержит proxy-объекты и не может быть добавлен в базу!" + Environment.NewLine
                                        + "Удалите все proxy-объекты и повторите попытку");
                            }
                            else
                                ModPlusAPI.Windows.MessageBox.Show("Выбранный файл должен быть сохранен в версии AutoCAD 2010!");
                        }
                        else
                            ModPlusAPI.Windows.MessageBox.Show("Указанный файл должен находиться в подпапке папки " + _dwgBaseFolder);
                    }
                    else if (ofdresult == System.Windows.Forms.DialogResult.Cancel) return;
                    else needLoop = false;
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Focus();
            }
        }

        private void BtCopyDwgFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedFile = string.Empty;
                var selectedPath = string.Empty;
                var copiedFile = string.Empty;
                // Сначала нужно выбрать файл, проверив версию его
                var ofd = new Autodesk.AutoCAD.Windows.OpenFileDialog("Выбор чертежа для добавления в базу", _dwgBaseFolder, "dwg", "name",
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.NoFtpSites |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.NoShellExtensions |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.NoUrls |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.DefaultIsFolder |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.ForceDefaultFolder |
                    Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.DoNotTransferRemoteFiles);
                var needLoop = true;
                while (needLoop)
                {
                    var ofdresult = ofd.ShowDialog();
                    if (ofdresult == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedFile = ofd.Filename;
                        if (DwgBaseHelpers.Is2010DwgVersion(selectedFile))
                        {
                            if (!DwgBaseHelpers.HasProxyEntities(selectedFile))
                                needLoop = false;
                            else
                                ModPlusAPI.Windows.MessageBox.Show("Выбранный файл содержит proxy-объекты и не может быть добавлен в базу!" +
                                              Environment.NewLine
                                              + "Удалите все proxy-примитивы и повторите попытку");
                        }
                        else
                            ModPlusAPI.Windows.MessageBox.Show("Выбранный файл должен быть сохранен в версии AutoCAD 2010!");
                    }
                    else if (ofdresult == System.Windows.Forms.DialogResult.Cancel) return;
                    else needLoop = false;
                }
                // Теперь нужно указать папку для расположения файла
                var fbd = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = @"Укажите папку для копирования выбранного файла",
                    SelectedPath = _dwgBaseFolder,
                    ShowNewFolderButton = true
                };
                needLoop = true;
                while (needLoop)
                {
                    var fbdResult = fbd.ShowDialog();
                    if (fbdResult == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedPath = fbd.SelectedPath;
                        if (selectedPath.Contains(_dwgBaseFolder))
                        {
                            if (!selectedPath.Equals(_dwgBaseFolder))
                            {
                                var fi = new FileInfo(selectedFile);
                                copiedFile = System.IO.Path.Combine(selectedPath, fi.Name);
                                if (File.Exists(copiedFile))
                                {
                                    needLoop =
                                        !ModPlusAPI.Windows.MessageBox.ShowYesNo("В указанной папке уже есть файл " + fi.Name +
                                                       Environment.NewLine + "Заменить?", MessageBoxIcon.Question);
                                }
                                else needLoop = false;
                            }
                            else
                                ModPlusAPI.Windows.MessageBox.Show("Не стоит выбирать папку " + _dwgBaseFolder + Environment.NewLine +
                                              "Создайте подкаталог");
                        }
                        else
                            ModPlusAPI.Windows.MessageBox.Show("Выбранная папка должна находиться в подпапке папки " + _dwgBaseFolder);
                    }
                    else if (fbdResult == System.Windows.Forms.DialogResult.Cancel) return;
                    else needLoop = true;
                }
                // then copy file
                if (!string.IsNullOrEmpty(selectedFile) & !string.IsNullOrEmpty(selectedPath))
                    if (File.Exists(selectedFile))
                    {
                        File.Copy(selectedFile, copiedFile, true);
                        if (File.Exists(copiedFile))
                        {
                            TbSourceFile.Text = DwgBaseHelpers.TrimStart(copiedFile, _dwgBaseFolder).TrimStart('\\');
                            BtAccept.IsEnabled = true;
                        }
                    }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Focus();
            }
        }
        // on accept click
        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckEmptyData()) return;
            if (!IsEdit)
            {
                var allGood = true;
                if (CheckInteredItemData())
                {
                    allGood = ModPlusAPI.Windows.MessageBox.ShowYesNo("Добавить чертеж с такими данными?", MessageBoxIcon.Question);
                }
                if (allGood)
                {
                    SaveInteredData();
                    FillDwgBaseItemData();
                    Closed -= DrawingWindow_OnClosed;
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                FillDwgBaseItemData();
                Closed -= DrawingWindow_OnClosed;
                DialogResult = true;
                Close();
            }
        }
        // on cancel click
        private void BtCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        /// <summary>
        /// Проверка введенных данных по базе
        /// </summary>
        /// <returns>True - есть такие данные, false - нет таких данных</returns>
        private bool CheckInteredItemData()
        {
            var hasSame = false;
            #region Проверяем по базе плагина
            List<DwgBaseItem> mpDwgBaseItems;
            if (DwgBaseHelpers.DeSeializerFromXml(_mpDwgBaseFile, out mpDwgBaseItems))
            {
                foreach (var mpDwgBaseItem in mpDwgBaseItems)
                {
                    if (!mpDwgBaseItem.IsBlock)
                        if (mpDwgBaseItem.Name.Equals(Item.Name))
                        {
                            ModPlusAPI.Windows.MessageBox.Show("База dwg плагина содержит чертеж с именем " + Item.Name);
                            hasSame = true;
                        }
                }
            }
            else
            {
                ModPlusAPI.Windows.MessageBox.Show("Ошибка парсинга файла базы dwg плагина!");
                return false;
            }
            #endregion
            #region Проверяем по базе пользователя
            List<DwgBaseItem> userDwgBaseItems;
            if (DwgBaseHelpers.DeSeializerFromXml(_userDwgBaseFile, out userDwgBaseItems))
            {
                foreach (var userDwgBaseItem in userDwgBaseItems)
                {
                    if (!userDwgBaseItem.IsBlock)
                        if (userDwgBaseItem.Name.Equals(Item.Name))
                        {
                            ModPlusAPI.Windows.MessageBox.Show("Пользовательская база dwg содержит чертеж с именем " + Item.Name);
                            hasSame = true;
                        }
                }
            }
            else
            {
                ModPlusAPI.Windows.MessageBox.Show("Ошибка парсинга файла пользовательской базы dwg!");
                return false;
            }
            #endregion
            return hasSame;
        }
        /// <summary>
        /// Проверка введенных данных на наличие пустых полей
        /// </summary>
        /// <returns>true - все заполнено, false - требуется заполнение</returns>
        private bool CheckEmptyData()
        {
            if (string.IsNullOrEmpty(Item.Name))
            {
                ModPlusAPI.Windows.MessageBox.Show("Укажите отображаемое имя чертежа (Название)");
                TbName.Focus();
                return false;
            }
            if (Item.Path.Equals("Чертежи/"))
            {
                ModPlusAPI.Windows.MessageBox.Show("Укажите путь в каталоге");
                CbPath.Focus();
                return false;
            }
            if (!IsEdit)
            {
                var db = AcApp.DocumentManager.MdiActiveDocument.Database;
                if (ChkIsCurrentDwgFile.IsChecked != null && ChkIsCurrentDwgFile.IsChecked.Value)
                {
                    if (!db.Filename.Contains(_dwgBaseFolder))
                    {
                        var fi = new FileInfo(db.Filename);
                        if (File.Exists(db.Filename))
                            ModPlusAPI.Windows.MessageBox.Show(
                                "Установлена галочка \"Текущий файл\" для dwg-файла, однако текущий файл не расположен в каталоге dwg-базы!" +
                                Environment.NewLine + "Каталог dwg-базы - " + _dwgBaseFolder + Environment.NewLine +
                                "Каталог текущего файла - " + fi.DirectoryName +
                                Environment.NewLine + "Возможно текущий файл еще не сохранен!"
                            );
                        else
                            ModPlusAPI.Windows.MessageBox.Show(
                                "Установлена галочка \"Текущий\" для dwg-файла, однако не удалось распознать каталог расположения текущего файла" +
                                Environment.NewLine + "Возможно текущий файл не сохранен!"
                            );

                        return false;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Item.SourceFile))
                    {
                        ModPlusAPI.Windows.MessageBox.Show("Укажите dwg-файл чертежа");
                        return false;
                    }
                }
            }
            return true;
        }
        // Заполнение текущего элемента данными
        private void FillDwgBaseItemData()
        {
            // Данные о блоке заполнены при выборе блока
            // Проверка введенных данных уже произведена
            Item.IsBlock = false;
            Item.IsAnnotative = false;
            Item.IsDynamicBlock = false;
            Item.Name = TbName.Text.Trim();
            Item.Description = TbDescription.Text.Trim();
            Item.Path = CbPath.Text.Trim().TrimEnd('/');
            Item.SourceFile = TbSourceFile.Text;
            Item.Document = TbDocument.Text.Trim();
            Item.HasAttributesForSpecification = false;
            Item.PositionValue = string.Empty;
            Item.DesignationValue = string.Empty;
            Item.MassValue = string.Empty;
            Item.NoteValue = string.Empty;
            Item.NominationValue = string.Empty;

            Item.Source = TbSource.Text.Trim();
            Item.Author = TbAuthor.Text.Trim();
            Item.Is3Dblock = false;
        }
        private void ChkIsCurrentDwgFile_OnChecked(object sender, RoutedEventArgs e)
        {
            // нужно проверить версию файла
            var db = AcApp.DocumentManager.MdiActiveDocument.Database;
            if (ChkIsCurrentDwgFile.IsChecked != null && ChkIsCurrentDwgFile.IsChecked.Value)
            {
                if (!db.Filename.Contains(_dwgBaseFolder))
                {
                    var fi = new FileInfo(db.Filename);
                    if (File.Exists(db.Filename))
                        ModPlusAPI.Windows.MessageBox.Show(
                            "Текущий файл не расположен в каталоге dwg-базы!" +
                            Environment.NewLine + "Каталог dwg-базы - " + _dwgBaseFolder + Environment.NewLine +
                            "Каталог текущего файла - " + fi.DirectoryName +
                            Environment.NewLine + "Возможно текущий файл еще не сохранен!"
                            );
                    else
                        ModPlusAPI.Windows.MessageBox.Show(
                            "Не удалось распознать каталог расположения текущего файла" +
                            Environment.NewLine + "Возможно текущий файл не сохранен!"
                            );

                    ChkIsCurrentDwgFile.IsChecked = false;
                }
                else
                {
                    if (File.Exists(db.Filename))
                    {
                        if (db.LastSavedAsVersion != DwgVersion.AC1024)
                        {
                            ModPlusAPI.Windows.MessageBox.Show("Версия текущего файла не соответствует версии AutoCAD 2010!" +
                                          Environment.NewLine + "Так как плагин ModPlus работает с версии AutoCAD 2010 и выше," +
                                          Environment.NewLine + "файл нужно сохранить в версию AutoCAD 2010");
                            ChkIsCurrentDwgFile.IsChecked = false;
                        }
                    }
                    else
                    {
                        ModPlusAPI.Windows.MessageBox.Show("Не удалось распознать каталог расположения текущего файла" +
                            Environment.NewLine + "Возможно текущий файл не сохранен!");
                        ChkIsCurrentDwgFile.IsChecked = false;
                    }
                }
            }
        }

        #region last intered data
        public bool CheckFileWithLastDataExists()
        {
            var file = System.IO.Path.Combine(_dwgBaseFolder, "LastInteredUserDataForDrawing.xml");
            return File.Exists(file);
        }

        private void SaveInteredData()
        {
            var file = System.IO.Path.Combine(_dwgBaseFolder, "LastInteredUserDataForDrawing.xml");
            var xEl = new XElement("LastData");
            // Name
            xEl.SetAttributeValue("Name", TbName.Text);
            // Description
            xEl.SetAttributeValue("Description", TbDescription.Text);
            // document
            xEl.SetAttributeValue("Document", TbDocument.Text);
            // Author
            xEl.SetAttributeValue("Author", TbAuthor.Text);
            // Source
            xEl.SetAttributeValue("Source", TbSource.Text);
            // Path
            xEl.SetAttributeValue("Path", CbPath.Text);
            // Check if current file
            xEl.SetAttributeValue("IsCurrentDwgFile", ChkIsCurrentDwgFile.IsChecked != null && ChkIsCurrentDwgFile.IsChecked.Value);
            // Source file
            xEl.SetAttributeValue("SourceFile", TbSourceFile.Text);

            // save
            xEl.Save(file);
        }
        private void BtLoadLastInteredData_OnClick(object sender, RoutedEventArgs e)
        {
            var file = System.IO.Path.Combine(_dwgBaseFolder, "LastInteredUserDataForDrawing.xml");
            var xEl = XElement.Load(file);
            // Name 
            TbName.Text = xEl.Attribute("Name")?.Value;
            // Description
            TbDescription.Text = xEl.Attribute("Description")?.Value;
            // Document
            TbDocument.Text = xEl.Attribute("Document")?.Value;
            // Author
            TbAuthor.Text = xEl.Attribute("Author")?.Value;
            // Source
            TbSource.Text = xEl.Attribute("Source")?.Value;
            // Path
            CbPath.Text = xEl.Attribute("Path")?.Value;
            // ChkIsCurrentDwgFile
            ChkIsCurrentDwgFile.IsChecked = bool.TryParse(xEl.Attribute("IsCurrentDwgFile")?.Value, out bool b) && b; // false
            // Source File
            // ReSharper disable once AssignNullToNotNullAttribute
            var sf = System.IO.Path.Combine(_dwgBaseFolder, xEl.Attribute("SourceFile")?.Value);
            if (!string.IsNullOrEmpty(sf) && File.Exists(sf))
            {
                TbSourceFile.Text = xEl.Attribute("SourceFile")?.Value;
                BtAccept.IsEnabled = true;
            }
        }
        #endregion

        private void BtRecommend_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new DrawingRecommend();
            win.ShowDialog();
        }

        private void DrawingWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void DrawingWindow_OnClosed(object sender, EventArgs e)
        {
            if (IsEdit) return;
            if (!string.IsNullOrEmpty(TbName.Text) | !string.IsNullOrEmpty(TbDescription.Text) |
                !string.IsNullOrEmpty(TbAuthor.Text) | !string.IsNullOrEmpty(TbDocument.Text) |
                !string.IsNullOrEmpty(TbSource.Text) | !string.IsNullOrEmpty(TbSourceFile.Text))
                if (ModPlusAPI.Windows.MessageBox.ShowYesNo("Сохранить введенные данные как \"Последние введенные данные\"?",
                    MessageBoxIcon.Question))
                    SaveInteredData();
        }
    }
}
