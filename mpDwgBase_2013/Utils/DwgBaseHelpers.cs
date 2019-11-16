namespace mpDwgBase.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Windows.Media.Imaging;
    using System.Xml;
    using System.Xml.Serialization;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Models;
    using ModPlusAPI.Windows;

    public static class DwgBaseHelpers
    {
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

        public static bool DeseializeFromXml(string fileName, out List<DwgBaseItem> listOfItems)
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
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                listOfItems = null;
                return false;
            }
        }

        public static string FindImageFile(DwgBaseItem selectedItem, string baseFolder)
        {
            var file = string.Empty;
            var dwgFile = new FileInfo(Path.Combine(baseFolder, selectedItem.SourceFile));

            if (Directory.Exists(dwgFile.DirectoryName))
            {
                file = Path.Combine(dwgFile.DirectoryName, dwgFile.Name + " icons", selectedItem.BlockName + ".bmp");
                if (File.Exists(file))
                {
                    return file;
                }

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
            if (!File.Exists(file))
            {
                return false;
            }

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
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

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
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return false;
            }
        }

        public static string GetBlkNameForInsertDrawing(string blkName, Database db)
        {
            var returnedBlkName = blkName.Replace(".dwg", string.Empty);
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                while (bt != null && bt.Has(returnedBlkName))
                {
                    returnedBlkName = blkName + "_" + DateTime.Now.Second;
                }
            }

            return returnedBlkName;
        }

        /// <summary>
        /// Удаление блоков из БД
        /// </summary>
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
                        {
                            btr.Erase(true);
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}