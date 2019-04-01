namespace mpDwgBase
{
    using Autodesk.AutoCAD.Windows.Data;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using Autodesk.AutoCAD.Runtime;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using System.ComponentModel;
    using System.Drawing;
    using System.Net;
    using System.Windows.Media.Imaging;
    using System.Xml;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.Internal;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    [Serializable]
    public class DwgBaseItem
    {
        [XmlAttribute]
        public string Author { get; set; }// Кто добавил блок в базу
        [XmlAttribute]
        public string Source { get; set; } // Источник
        [XmlAttribute]
        public string Name { get; set; }// Название блока (узла)
        [XmlAttribute]
        public string Path { get; set; }// Путь
        [XmlAttribute]
        public string SourceFile { get; set; }// Файл-источник
        [XmlAttribute]
        public string Description { get; set; }// Описание
        [XmlAttribute]
        public string Document { get; set; }// Нормативный документ (если есть)
        [XmlAttribute]
        public bool IsAnnotative { get; set; }// Аннотативный?
        [XmlAttribute]
        public bool IsDynamicBlock { get; set; }// Динамический?
        [XmlAttribute]
        public bool HasAttributesForSpecification { get; set; }// Есть ли атрибуты для спецификации
        [XmlAttribute]
        public bool IsBlock { get; set; }// Является ли блоком
        [XmlAttribute]
        public string BlockName { get; set; }// Имя блока в файле-исходнике
        [XmlAttribute]
        public bool Is3Dblock { get; set; }// Является ли блок трехмерным
        // specification
        [XmlAttribute]
        public string PositionValue { get; set; }// Атрибут - Обозначение
        [XmlAttribute]
        public string DesignationValue { get; set; }// Атрибут - Обозначение
        [XmlAttribute]
        public string NominationValue { get; set; }// Атрибут - Наименование
        [XmlAttribute]
        public string MassValue { get; set; }// Атрибут - масса
        [XmlAttribute]
        public string NoteValue { get; set; }// Атрибут - примечание
        //Значение атрибутов, входящих в сам блок
        [XmlElement(IsNullable = true)]
        public List<AttributeValue> AttributesValues { get; set; }
        //public List<string> AttributesValues { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var dwgBaseItem = (DwgBaseItem) obj;

            return
                BlockName == dwgBaseItem.BlockName &&
                Source == dwgBaseItem.Source &&
                SourceFile == dwgBaseItem.SourceFile &&
                Name == dwgBaseItem.Name &&
                Description == dwgBaseItem.Description &&
                Document == dwgBaseItem.Document &&
                Path == dwgBaseItem.Path;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }
    }

    public class AttributeValue
    {
        [XmlAttribute]
        public string Tag { get; set; }
        [XmlAttribute]
        public string Prompt { get; set; }
        [XmlAttribute]
        public string TextString { get; set; }
    }

    public class MpDwgBaseMain
    {

        [CommandMethod("ModPlus", "mpDwgBase", CommandFlags.Session)]
        public void MainFunction()
        {
            Statistic.SendCommandStarting(new ModPlusConnector());
            try
            {
                // Проверяем наличие папок
                var mpBaseFolder = Path.Combine(DwgBaseHelpers.GetTopDir(), "Data", "DwgBase");
                if (!Directory.Exists(mpBaseFolder))
                    Directory.CreateDirectory(mpBaseFolder);

                var win = new MpDwgBaseMainWindow();
                AcApp.ShowModalWindow(AcApp.MainWindow.Handle, win, false);

            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
    public static class DwgBaseHelpers
    {
        public static string GetTopDir()
        {
            using (var r = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus"))
            {
                return r?.GetValue("TopDir", string.Empty).ToString();
            }
        }
        /// <summary>
        /// Сериализация списка элементов в xml
        /// </summary>
        /// <param name="listOfItems">Список элементов</param>
        /// <param name="fileName">Имя файла</param>
        public static void SerializerToXml(List<DwgBaseItem> listOfItems, string fileName)
        {
            var formatter = new XmlSerializer(typeof(List<DwgBaseItem>));
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                formatter.Serialize(fs, listOfItems);
            }
        }
        public static bool DeSeializerFromXml(string fileName, out List<DwgBaseItem> listOfItems)
        {
            try
            {
                var formatter = new XmlSerializer(typeof(List<DwgBaseItem>));
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    listOfItems = formatter.Deserialize(fs) as List<DwgBaseItem>;
                    return true;
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
                listOfItems = null;
                return false;
            }
        }
        public static string FindImageFile(DwgBaseItem selectedItem, string baseFolder)
        {
            var file = string.Empty;
            var dwgfile = new FileInfo(Path.Combine(baseFolder, selectedItem.SourceFile));

            if (Directory.Exists(dwgfile.DirectoryName))
            {
                file = Path.Combine(dwgfile.DirectoryName, dwgfile.Name + " icons", selectedItem.BlockName + ".bmp");
                if (File.Exists(file)) return file;
                file = string.Empty;
            }
            return file;
        }
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        public static bool Is2010DwgVersion(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                var readLine = reader.ReadLine();
                return readLine != null && readLine.Substring(0, 6).Equals("AC1024");
            }
        }

        public static string TrimStart(string target, string trimString)
        {
            var result = target;
            while (result.StartsWith(trimString))
            {
                result = result.Substring(trimString.Length);
            }
            return result;
        }

        public static bool HasProxyEntities(string file)
        {
            if (!File.Exists(file)) return false;
            var db = new Database(false, true);
            db.ReadDwgFile(file, FileShare.Read, true, string.Empty);
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (var objectId in bt)
                {
                    var btr = tr.GetObject(objectId, OpenMode.ForRead) as BlockTableRecord;
                    foreach (var objId in btr)
                    {
                        var ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (ent.IsAProxy)
                            return true;
                    }
                }
            }
            return false;
        }

        //public static bool HasProxyEntities(string file)
        //{
        //    // Взято у Александра Ривилиса - http://adn-cis.org/forum/index.php?topic=1060.msg5017#msg5017
        //    if (!File.Exists(file)) return false;
        //    var db = new Database(false, true);
        //    db.ReadDwgFile(file, FileShare.Read, true, string.Empty);
        //    Handle firstHandle = db.BlockTableId.Handle;  // Первая метка объекта - метка таблицы блоков
        //    Handle lastHandle = db.Handseed;              // Следующая после последней метки
        //    int nObjects = db.ApproxNumObjects;           // Приблизительное количество объектов в базе
        //    string bufferLast = lastHandle.ToString();
        //    string bufferFirst = firstHandle.ToString();
        //    Int64 iLast = Int64.Parse(bufferLast, System.Globalization.NumberStyles.HexNumber);
        //    Int64 iFirst = Int64.Parse(bufferFirst, System.Globalization.NumberStyles.HexNumber);

        //    using (var tr = db.TransactionManager.StartTransaction())
        //    {
        //        for (Int64 i = iFirst; i < iLast && nObjects > 0; i++)
        //        {
        //            Handle h = new Handle(i);
        //            ObjectId id = ObjectId.Null;
        //            if (db.TryGetObjectId(h, out id))
        //            {
        //                try
        //                {
        //                    DBObject dbObj = tr.GetObject(id, OpenMode.ForRead, true, true);
        //                    if (dbObj != null && !dbObj.IsErased && dbObj.IsAProxy)
        //                    {
        //                        return true;
        //                    }
        //                    nObjects--;
        //                }
        //                catch
        //                {
        //                    //ignored
        //                }
        //            }
        //        }
        //        tr.Commit();
        //    }
        //    return false;
        //}
        public static bool GetConfigFileFromSite(out XmlDocument xmlDocument)
        {
            xmlDocument = new XmlDocument();
            try
            {
                const string url = "http://www.modplus.org/Downloads/ModPlus.xml";
                string xmlStr;
                using (var wc = new WebClient())
                {
                    xmlStr = wc.DownloadString(url);
                }
                xmlDocument.LoadXml(xmlStr);
                return true;
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
                return false;
            }
        }

        public static string GetBlkNameForInsertDrawing(string blkName, Database db)
        {

            var returnedBlkName = blkName.Replace(".dwg", "");
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                while (bt.Has(returnedBlkName))
                {
                    returnedBlkName = blkName + "_" + DateTime.Now.Second;
                }
            }
            return returnedBlkName;
        }
        /// <summary>
        /// Удаление блоков из БД
        /// </summary>
        /// <param name="blkNames"></param>
        /// <param name="doc"></param>
        /// <param name="db"></param>
        public static void RemoveBlocksFromDB(IEnumerable<string> blkNames, Document doc, Database db)
        {
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                var bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                foreach (var blkName in blkNames)
                {
                    if (bt != null && bt.Has(blkName))
                    {
                        var btr = tr.GetObject(bt[blkName], OpenMode.ForWrite) as BlockTableRecord;
                        if (btr != null && (btr.GetBlockReferenceIds(false, false).Count == 0 && !btr.IsLayout))
                            btr.Erase(true);
                    }
                }
                tr.Commit();
            }
        }
    }
    public static class BlockInsertion
    {
        private const string LangItem = "mpDwgBase";

        public static Dictionary<AttributeDefinition, AttributeReference> AppendAttribToBlock(Transaction tr, BlockReference blkref, List<string> atts)
        {
            var blkdef = (BlockTableRecord)tr.GetObject(blkref.BlockTableRecord, OpenMode.ForRead);
            int i = 0;
            if (blkdef.HasAttributeDefinitions)
            {
                var attribs = new Dictionary<AttributeDefinition, AttributeReference>();
                var attdefs = blkdef.Cast<ObjectId>()
                    .Select(id => tr.GetObject(id, OpenMode.ForRead))
                    .OfType<AttributeDefinition>()
                    .Where(attdef => !(attdef.Constant || attdef.Invisible));
                foreach (var attdef in attdefs)
                {
                    AttributeReference attref = new AttributeReference();
                    attref.SetAttributeFromBlock(attdef, blkref.BlockTransform);
                    if (i < atts.Count)
                        attref.TextString = atts[i];
                    else
                        attref.TextString = attdef.TextString;
                    i++;
                    blkref.AttributeCollection.AppendAttribute(attref);
                    tr.AddNewlyCreatedDBObject(attref, true);
                    attribs.Add(attdef, attref);
                }
                return attribs;
            }
            return null;
        }

        /// <summary>
        /// Вставка блока с атрибутами
        /// </summary>
        /// <param name="promptCounter">0 - только вставка, 1 - с поворотом</param>
        /// <param name="tr">Транзакция</param>
        /// <param name="db">База данных чертежа</param>
        /// <param name="ed">Editor</param>
        /// <param name="blkdefid">ObjectId блока</param>
        /// <param name="atts">Список имен атрибутов</param>
        /// <param name="isAnnotative">Аннотативность блока-исходника</param>
        /// <returns></returns>
        public static ObjectId InsertBlockRef(
            int promptCounter,
            Transaction tr,
            Database db,
            Editor ed,
            ObjectId blkdefid,
            List<string> atts,
            bool isAnnotative)
        {
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            BlockReference blkref = new BlockReference(Point3d.Origin, blkdefid);
            if (isAnnotative)
            {
                ObjectContextManager ocm = db.ObjectContextManager;
                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                ObjectContexts.AddContext(blkref, occ.CurrentContext);
            }
            ObjectId id = btr.AppendEntity(blkref);
            tr.AddNewlyCreatedDBObject(blkref, true);
            BlockRefJig jig = new BlockRefJig(blkref, AppendAttribToBlock(tr, blkref, atts));
            jig.SetPromptCounter(0);
            PromptResult res = ed.Drag(jig);
            if (res.Status == PromptStatus.OK)
            {
                if (promptCounter == 1)
                {
                    jig.SetPromptCounter(promptCounter);
                    res = ed.Drag(jig);
                    if (res.Status == PromptStatus.OK)
                    {
                        return id;
                    }
                }
                else return id;
            }
            blkref.Erase();
            return ObjectId.Null;
        }
        internal class BlockRefJig : EntityJig
        {
            Point3d m_Position, m_BasePoint;
            double m_Angle;
            int m_PromptCounter;
            Matrix3d m_Ucs;
            Matrix3d m_Mat;
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            Dictionary<AttributeDefinition, AttributeReference> m_Attribs;
            public BlockRefJig(BlockReference blkref, Dictionary<AttributeDefinition, AttributeReference> attribs)
            : base(blkref)
            {
                m_Position = new Point3d();
                m_Angle = 0;
                m_Ucs = ed.CurrentUserCoordinateSystem;
                m_Attribs = attribs;
                Update();
            }
            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                switch (m_PromptCounter)
                {
                    case 0:
                        {
                            JigPromptPointOptions jigOpts = new JigPromptPointOptions("\n" + Language.GetItem(LangItem, "msg13") + ":");
                            jigOpts.UserInputControls =
                            UserInputControls.Accept3dCoordinates |
                            UserInputControls.NoZeroResponseAccepted |
                            UserInputControls.NoNegativeResponseAccepted;
                            PromptPointResult res = prompts.AcquirePoint(jigOpts);
                            Point3d pnt = res.Value;
                            if (pnt != m_Position)
                            {
                                m_Position = pnt;
                                m_BasePoint = m_Position;
                            }
                            else
                                return SamplerStatus.NoChange;
                            if (res.Status == PromptStatus.Cancel)
                                return SamplerStatus.Cancel;

                            return SamplerStatus.OK;
                        }
                    case 1:
                        {
                            JigPromptAngleOptions jigOpts = new JigPromptAngleOptions("\n" + Language.GetItem(LangItem, "msg14") + ":");
                            jigOpts.UserInputControls =
                            UserInputControls.Accept3dCoordinates |
                            UserInputControls.NoNegativeResponseAccepted |
                            UserInputControls.GovernedByUCSDetect |
                            UserInputControls.UseBasePointElevation;
                            jigOpts.Cursor = CursorType.RubberBand;
                            jigOpts.UseBasePoint = true;
                            jigOpts.BasePoint = m_BasePoint;
                            PromptDoubleResult res = prompts.AcquireAngle(jigOpts);
                            double angleTemp = res.Value;
                            if (Math.Abs(angleTemp - m_Angle) > Autodesk.AutoCAD.Geometry.Tolerance.Global.EqualVector)
                                m_Angle = angleTemp;
                            else
                                return SamplerStatus.NoChange;
                            if (res.Status == PromptStatus.Cancel)
                                return SamplerStatus.Cancel;

                            return SamplerStatus.OK;
                        }
                    default:
                        return SamplerStatus.NoChange;
                }
            }
            protected sealed override bool Update()
            {
                try
                {
                    BlockReference blkref = (BlockReference)Entity;
                    blkref.Normal = Vector3d.ZAxis;
                    blkref.Position = m_Position.TransformBy(ed.CurrentUserCoordinateSystem);
                    blkref.Rotation = m_Angle;
                    blkref.TransformBy(m_Ucs);
                    if (m_Attribs != null)
                    {
                        m_Mat = blkref.BlockTransform;
                        foreach (KeyValuePair<AttributeDefinition, AttributeReference> att in m_Attribs)
                        {
                            AttributeReference attref = att.Value;
                            string s = attref.TextString;
                            attref.SetAttributeFromBlock(att.Key, m_Mat);
                            attref.TextString = s;
                        }
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }
            public void SetPromptCounter(int i)
            {
                if (i == 0 || i == 1)
                {
                    m_PromptCounter = i;
                }
            }
        }
        /// <summary>
        /// Добавление атрибутов для спецификации
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="blkRef"></param>
        /// <param name="dwgBaseItem"></param>
        public static void AddAttributesForSpecification(Transaction tr, BlockReference blkRef, DwgBaseItem dwgBaseItem)
        {
            if (HasAttributesForSpecification(tr, blkRef.ObjectId))
            {
                // apdate attributes
                foreach (ObjectId id in blkRef.AttributeCollection)
                {
                    var attr = tr.GetObject(id, OpenMode.ForWrite) as AttributeReference;
                    if (attr != null)
                    {
                        if (attr.Tag.ToLower().Equals("mp:позиция") ||
                            attr.Tag.ToLower().Equals("mp:position"))
                            attr.TextString = dwgBaseItem.PositionValue;
                        if (attr.Tag.ToLower().Equals("mp:обозначение") ||
                            attr.Tag.ToLower().Equals("mp:designation"))
                            attr.TextString = dwgBaseItem.DesignationValue;
                        if (attr.Tag.ToLower().Equals("mp:наименование") ||
                            attr.Tag.ToLower().Equals("mp:name"))
                            attr.TextString = dwgBaseItem.NominationValue;
                        if (attr.Tag.ToLower().Equals("mp:масса") ||
                            attr.Tag.ToLower().Equals("mp:mass"))
                            attr.TextString = dwgBaseItem.MassValue;
                        if (attr.Tag.ToLower().Equals("mp:примечание") ||
                            attr.Tag.ToLower().Equals("mp:note"))
                            attr.TextString = dwgBaseItem.NoteValue;
                    }
                }
            }
            else
            {
                // add attributes
                BlockTableRecord acBlkTblRec;
                if (blkRef.IsDynamicBlock) //get the real dynamic block name.
                {
                    acBlkTblRec = tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                }
                else acBlkTblRec = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                if (acBlkTblRec != null)
                {
                    //blkName = acBlkTblRec.Name;
                    var attdefs = AddAttrDefenitions(acBlkTblRec, blkRef, tr);
                    if (attdefs.Any())
                    {
                        foreach (ObjectId id in blkRef.AttributeCollection)
                        {
                            var attr = tr.GetObject(id, OpenMode.ForWrite) as AttributeReference;
                            if (attr != null)
                            {
                                if (attr.Tag.ToLower().Equals("mp:позиция") ||
                                    attr.Tag.ToLower().Equals("mp:position"))
                                    attr.TextString = dwgBaseItem.PositionValue;
                                if (attr.Tag.ToLower().Equals("mp:обозначение") ||
                                    attr.Tag.ToLower().Equals("mp:designation"))
                                    attr.TextString = dwgBaseItem.DesignationValue;
                                if (attr.Tag.ToLower().Equals("mp:наименование") ||
                                    attr.Tag.ToLower().Equals("mp:name"))
                                    attr.TextString = dwgBaseItem.NominationValue;
                                if (attr.Tag.ToLower().Equals("mp:масса") ||
                                    attr.Tag.ToLower().Equals("mp:mass"))
                                    attr.TextString = dwgBaseItem.MassValue;
                                if (attr.Tag.ToLower().Equals("mp:примечание") ||
                                    attr.Tag.ToLower().Equals("mp:note"))
                                    attr.TextString = dwgBaseItem.NoteValue;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверка, что блок имеет атрибуты для заполнения спецификации
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private static bool HasAttributesForSpecification(Transaction tr, ObjectId objectId)
        {
            var allowAttributesTags = new List<string> { "mp:position", "mp:designation", "mp:name", "mp:mass", "mp:note" };
            if (Language.RusWebLanguages.Contains(Language.CurrentLanguageName))
                allowAttributesTags = new List<string> { "mp:позиция", "mp:обозначение", "mp:наименование", "mp:масса", "mp:примечание" };

            var blk = tr.GetObject(objectId, OpenMode.ForRead) as BlockReference;
            if (blk != null)
            {
                // Если это блок
                if (blk.AttributeCollection.Count > 0)
                {
                    foreach (ObjectId id in blk.AttributeCollection)
                    {
                        var attr = tr.GetObject(id, OpenMode.ForRead) as AttributeReference;
                        if (allowAttributesTags.Contains(attr?.Tag.ToLower())) return true;
                    }
                }
            }
            return false;
        }
        // Add an attribute definition to the block
        private static List<AttributeDefinition> AddAttrDefenitions(BlockTableRecord acBlkTblRec, BlockReference blkRef, Transaction tr)
        {
            var attrDefs = new List<AttributeDefinition>();
            var allowAttributesTags = new List<string> { "mp:position", "mp:designation", "mp:name", "mp:mass", "mp:note" };
            if (Language.RusWebLanguages.Contains(Language.CurrentLanguageName))
                allowAttributesTags = new List<string> { "mp:позиция", "mp:обозначение", "mp:наименование", "mp:масса", "mp:примечание" };

            var allowAttributesPrompt = new List<string>
            {
                Language.GetItem(LangItem, "ad1"), Language.GetItem(LangItem, "ad2"),
                Language.GetItem(LangItem, "ad3"), Language.GetItem(LangItem, "ad4"), Language.GetItem(LangItem, "ad5")
            };

            for (var i = 0; i < allowAttributesTags.Count; i++)
            {
                using (var acAttDef = new AttributeDefinition())
                {
                    acAttDef.Position = new Point3d(0, 0, 0);
                    acAttDef.Invisible = true;
                    acAttDef.Prompt = allowAttributesPrompt[i];
                    acAttDef.Tag = allowAttributesTags[i];
                    acAttDef.TextString = string.Empty;
                    acBlkTblRec.AppendEntity(acAttDef);

                    var attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(acAttDef, blkRef.BlockTransform);
                    blkRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);

                    attrDefs.Add(acAttDef);
                }
            }
            return attrDefs;
        }
    }

    public class TreeViewModelItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        public TreeViewModelItem()
        {
            Items = new List<TreeViewModelItem>();
        }
        public List<TreeViewModelItem> Items { get; set; }

        private string _Name;
        public string Name
        {
            get => _Name;
            set { if (_Name != value) { _Name = value; OnPropertyChanged("Name"); } }
        }

        private TreeViewModelItem _parent;
        public TreeViewModelItem Parent
        {
            get => _parent;
            set { if (_parent != value) { _parent = value; OnPropertyChanged("Parent"); } }
        }

        public List<string> Children { get; set; }

        public string GetAncestry()
        {
            string x = string.Empty;
            if (Parent != null)
                x += Parent.GetAncestry() + "/";
            x += Name;
            return x;
        }
    }

    public static class ImageCreator
    {
#if !A2013
        public static string ImagePreviewFile(DwgBaseItem selectedItem, string baseFolder)
        {
            try
            {
                var imgFile = DwgBaseHelpers.FindImageFile(selectedItem, baseFolder);
                // Если файл есть
                if (!string.IsNullOrEmpty(imgFile))
                {
                    if (File.Exists(imgFile))
                        return imgFile;
                    return CreateImageFile(selectedItem, baseFolder);
                }
                return CreateImageFile(selectedItem, baseFolder);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            return string.Empty;
        }
        private static string CreateImageFile(DwgBaseItem selectedItem, string baseFolder)
        {
            string file;
            var sourceDb = new Database(false, true);
            // Read the DWG into a side database
            sourceDb.ReadDwgFile(Path.Combine(baseFolder, selectedItem.SourceFile), FileShare.Read, true, "");
            var dwgfile = new FileInfo(Path.Combine(baseFolder, selectedItem.SourceFile));
            
            // ReSharper disable once AssignNullToNotNullAttribute
            var iconPath = Path.Combine(dwgfile.DirectoryName, dwgfile.Name + " icons");
            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);

                file = ExtractThumbnails(iconPath, tr, bt[selectedItem.BlockName]);

                tr.Commit();
            }
            return file;
        }
        private static string ExtractThumbnails(string iconPath, Transaction tr, ObjectId id)
        {
            var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);

            if (!Directory.Exists(iconPath))
                Directory.CreateDirectory(iconPath);

            // Save the icon to our out directory

            var imgsrc = CMLContentSearchPreviews.GetBlockTRThumbnail(btr);
            var bmp =
              ImageSourceToGDI(
                imgsrc as System.Windows.Media.Imaging.BitmapSource
              );

            var fname = iconPath + "\\" + btr.Name + ".bmp";

            if (File.Exists(fname))
                File.Delete(fname);

            bmp.Save(fname);

            return fname;
        }
        // Helper function to generate an Image from a BitmapSource
        private static System.Drawing.Image ImageSourceToGDI(System.Windows.Media.Imaging.BitmapSource src)
        {
            var ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(src));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }
#endif
    }
}
