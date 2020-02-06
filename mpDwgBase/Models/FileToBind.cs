namespace mpDwgBase.Models
{
    using ModPlusAPI.Mvvm;

    internal class FileToBind : VmBase
    {
        private bool _selected;

        public string FileName { get; set; }

        public string FullFileName { get; set; }

        public string SourceFile { get; set; }
        
        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Полный путь к директории
        /// </summary>
        public string FullDirectory { get; set; }

        /// <summary>
        /// Усеченный путь
        /// </summary>
        public string SubDirectory { get; set; }
    }
}