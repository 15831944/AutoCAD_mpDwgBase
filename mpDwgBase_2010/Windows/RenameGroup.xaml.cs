using System.Windows;
using System.Windows.Input;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;

namespace mpDwgBase.Windows
{
    public partial class RenameGroup 
    {
        public RenameGroup()
        {
            InitializeComponent();
            this.OnWindowStartUp();
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
                ModPlusAPI.Windows.MessageBox.Show("Укажите новое имя группы");
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

        private void RenameLayout_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
            if (e.Key == Key.Return)
            {
                OnAccept();
            }
        }
        
    }
}