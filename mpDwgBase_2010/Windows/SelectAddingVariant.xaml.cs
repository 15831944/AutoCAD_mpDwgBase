using System.Windows;
using System.Windows.Input;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;

namespace mpDwgBase.Windows
{
    /// <summary>
    /// Логика взаимодействия для SelectAddingVariant.xaml
    /// </summary>
    public partial class SelectAddingVariant
    {
        public string Variant = "None";

        public SelectAddingVariant()
        {
            InitializeComponent();
            this.OnWindowStartUp();
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
