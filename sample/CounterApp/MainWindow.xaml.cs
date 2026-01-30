using Knotty.Core;
using System.Windows;

namespace CounterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent ();

            this.DataContext = new MainStore ();
        }

        private void CounterAdd_Click(object sender, RoutedEventArgs e)
        {

            KnottyBus.Send (new MainIntent.Increment ());
        }

        private void CounterMinus_Click(object sender, RoutedEventArgs e)
        {
            KnottyBus.Send (new MainIntent.Decrement ());
        }

        private void CounterReset_Click(object sender, RoutedEventArgs e)
        {
            KnottyBus.Send (new MainIntent.ResetAsync ());
        }
    }
}