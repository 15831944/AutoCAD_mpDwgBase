using System;
using System.Collections.Generic;
using ModPlusAPI.Interfaces;

namespace mpDwgBase
{
    public class Interface : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.AutoCAD;
        public string Name => "mpDwgBase";
        public string AvailProductExternalVersion => "2016";
        public string FullClassName => string.Empty;
        public string AppFullClassName => string.Empty;
        public Guid AddInId => Guid.Empty;
        public string LName => "База dwg";
        public string Description => "База различных блоков и чертежей";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
        public bool CanAddToRibbon => true;
        public string FullDescription => "База dwg позволяет вставлять как блоки, так и готовые чертежи (например, узлы). База dwg наполняется пользователями. Для работы функции необходим доступ в internet";
        public string ToolTipHelpImage => string.Empty;
        public List<string> SubFunctionsNames => new List<string>();
        public List<string> SubFunctionsLames => new List<string>();
        public List<string> SubDescriptions => new List<string>();
        public List<string> SubFullDescriptions => new List<string>();
        public List<string> SubHelpImages => new List<string>();
        public List<string> SubClassNames => new List<string>();
    }
    public class VersionData
    {
        public const string FuncVersion = "2016";
    }
}