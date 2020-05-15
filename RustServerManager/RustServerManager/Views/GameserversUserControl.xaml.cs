using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RustServerManager.Views
{
    /// <summary>
    /// Interaction logic for ViewUserControl.xaml
    /// </summary>
    public partial class GameserversUserControl : UserControl
    {
        public GameserversUserControl(ViewModels.GameserversViewModel viewViewModel)
        {
            this.DataContext = viewViewModel;

            InitializeComponent();
        }

        private void CommandToSay_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void CommandToRcon_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
