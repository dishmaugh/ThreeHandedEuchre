using Microsoft.UI.Xaml;
using ThreeHandedEuchre.Core;
using Windows.Graphics;

namespace ThreeHandedEuchre.App;

public sealed partial class MainWindow : Window
{
    private readonly Game _game;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.Resize(new SizeInt32(1152, 768));

        _game = new Game();
        _game.Start();
    }
}