using System;
using System.Windows;
using Knotty;

namespace CounterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IDisposable? _effectSubscription;

        public MainWindow()
        {
            InitializeComponent();

            var store = new MainStore();
            DataContext = store;

            // Effect 구독 - 10 단위 달성 시 MessageBox 표시
            _effectSubscription = store.Effects.Subscribe<CounterEffect>(HandleEffect);

            Unloaded += (s, e) => _effectSubscription?.Dispose();
        }

        private void HandleEffect(CounterEffect effect)
        {
            switch (effect)
            {
                case CounterEffect.Milestone milestone:
                    MessageBox.Show(
                        $"🎉 축하합니다! {milestone.Count}에 도달했습니다!",
                        "Milestone!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
            }
        }
    }
}