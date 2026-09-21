using Microsoft.UI.Xaml;
using ThreeHandedEuchre.Core;

namespace ThreeHandedEuchre.App;

public sealed partial class MainWindow : Window
{
    private readonly Game _game;

    public MainWindow()
    {
        InitializeComponent();

        _game = new Game();
        _game.Start();
    }
}