using System.Windows;
using System.Windows.Input;

namespace mpDwgBase.Windows
{
    public partial class BlockRecommend 
    {
        public BlockRecommend()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("mpDwgBase", "h37");
        }

        private void BtClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BlockRecommend_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape) Close();
        }
    }
}
