namespace mpDwgBase.Models
{
    using System.Windows;

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