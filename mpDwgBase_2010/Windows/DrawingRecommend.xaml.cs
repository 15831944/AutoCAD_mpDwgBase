using System.Windows;
using System.Windows.Input;

namespace mpDwgBase.Windows
{
    public partial class DrawingRecommend
    {
        public DrawingRecommend()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("mpDwgBase", "h62");
        }

        private void BtClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DrawingRecommend_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
