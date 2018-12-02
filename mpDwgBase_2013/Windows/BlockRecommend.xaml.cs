namespace mpDwgBase.Windows
{
    using System.Windows;

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
    }
}
