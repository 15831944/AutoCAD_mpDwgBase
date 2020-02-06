using System.Windows;

namespace mpDwgBase.Windows
{
    public partial class RenameGroup 
    {
        private const string LangItem = "mpDwgBase";

        public RenameGroup()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h63");
            Loaded += RenameLayout_Loaded;
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            OnAccept();
        }

        private void BtCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        
        private void OnAccept()
        {
            if (string.IsNullOrEmpty(TbNewName.Text))
            {
                ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "msg71"));
                TbNewName.Focus();
            }
            else 
            {
                DialogResult = true;
            }
        }

        private void RenameLayout_Loaded(object sender, RoutedEventArgs e)
        {
            TbNewName.Focus();
            TbNewName.CaretIndex = TbNewName.Text.Length;
        }
    }
}