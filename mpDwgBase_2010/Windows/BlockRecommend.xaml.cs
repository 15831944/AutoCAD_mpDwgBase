using System.Windows;
using System.Windows.Input;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;

namespace mpDwgBase.Windows
{
    public partial class BlockRecommend 
    {
        public BlockRecommend()
        {
            InitializeComponent();
            this.OnWindowStartUp();
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
