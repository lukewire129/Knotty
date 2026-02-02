using System.Windows;

namespace DebounceApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainStore _store;
        public MainWindow()
        {
            InitializeComponent ();

            this.DataContext = _store = new MainStore ();
        }

        private void CounterAdd_Click(object sender, RoutedEventArgs e)
        {
            _store.Dispatch (new CounterIntent.Increment ());
        }

        private void CounterMinus_Click(object sender, RoutedEventArgs e)
        {
            _store.Dispatch (new CounterIntent.Decrement ());
        }

        private void CounterReset_Click(object sender, RoutedEventArgs e)
        {
            _store.Dispatch (new CounterIntent.Reset ());
        }
    }
}