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

namespace TwinCatAdsTool.Gui.Views
{
    /// <summary>
    /// Interaction logic for ConnectionCabView.xaml
    /// </summary>
    public partial class ConnectionCabView : UserControl
    {
        public ConnectionCabView()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.evopro-ag.de");
        }
    }
}
