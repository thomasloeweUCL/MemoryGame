using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MemoryGame.ViewModels;

namespace MemoryGame.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Denne linje er afgørende. 
            // Den opretter en ny GameViewModel og fortæller vinduet,
            // at det er dén, alle bindings skal hente data fra.
            DataContext = new GameViewModel();
        }
    }
}