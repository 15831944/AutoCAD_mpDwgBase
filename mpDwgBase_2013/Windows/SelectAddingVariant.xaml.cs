using System.Windows;
using System.Windows.Input;

namespace mpDwgBase.Windows
{
    public partial class SelectAddingVariant
    {
        public string Variant = "None";

        public SelectAddingVariant()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("mpDwgBase", "h66");
        }

        private void BtAddBlock_OnClick(object sender, RoutedEventArgs e)
        {
            Variant = "Block";
            DialogResult = true;
        }

        private void BtAddDrawing_OnClick(object sender, RoutedEventArgs e)
        {
            Variant = "Drawing";
            DialogResult = true;
        }

        private void SelectAddingVariant_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
                Close();
        }
    }
}
