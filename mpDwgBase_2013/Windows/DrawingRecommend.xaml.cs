namespace mpDwgBase.Windows
{
    using System.Windows;

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
    }
}
