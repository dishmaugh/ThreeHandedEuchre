using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThreeHandedEuchre.Core;
using Windows.Graphics;

namespace ThreeHandedEuchre.App;

public sealed partial class MainWindow : Window
{
    private Game _game;

    private enum ActionMode
    {
        Play,
        FirstRoundBid,
        SecondRoundBid,
        Discard,
        PlayAgain
    }

    private ActionMode _actionMode = ActionMode.Play;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.Resize(new SizeInt32(1152, 768));

        _game = new Game();
        //_game.Start();
    }

    private async void ActionButton1_Click(object sender, RoutedEventArgs e)
    {
        switch (_actionMode)
        {
            case ActionMode.Play:
                await StartGame();
                break;

            case ActionMode.FirstRoundBid:
                await HumanPickup();
                break;

            case ActionMode.SecondRoundBid:
                // Later: Button 1 will select one of the available suits.
                break;

            case ActionMode.Discard:
                // Later: discard selection will actually be handled by cards.
                break;

            case ActionMode.PlayAgain:
                await StartGame();
                break;
        }
    }

    private async void ActionButton2_Click(object sender, RoutedEventArgs e)
    {
        if (_actionMode == ActionMode.FirstRoundBid)
        {
            await HumanPassFirstRound();
        }
    }

    private void ActionButton3_Click(object sender, RoutedEventArgs e)
    {
        // Used later in second-round bidding.
    }

    private void ActionButton4_Click(object sender, RoutedEventArgs e)
    {
        // Used later in second-round bidding.
    }

    private async Task HumanPickup()
    {
        // Human orders up the current up card.
        _game.State.OrderUp(0);

        GameMessage.Text = "Player 1 orders it up";
        GameMessage.Visibility = Visibility.Visible;

        ActionButton1.IsEnabled = false;
        ActionButton2.IsEnabled = false;

        await Task.Delay(1500);

        GameMessage.Visibility = Visibility.Collapsed;

        // We'll handle the dealer's discard next.
    }

    private async Task HumanPassFirstRound()
    {
        GameMessage.Text = "Player 1 passes";

        ActionButton1.IsEnabled = false;
        ActionButton2.IsEnabled = false;

        await Task.Delay(1500);

        _game.AdvanceBidder();

        if (_game.BiddingRoundComplete)
        {
            await StartSecondBiddingRound();
            return;
        }

        await ProcessFirstRoundBidder();
    }

    private async Task StartGame()
    {
        ActionButton1.IsEnabled = false;

        GameMessage.Text = "Shuffling...";
        GameMessage.Visibility = Visibility.Visible;

        await Task.Delay(2000);

        // Real game begins here.
        _game = new Game();
        _game.State.Deal();

        DealerText.Text = $"Dealer: {_game.State.Dealer.Name}";

        GameMessage.Text = "Dealing...";

        Player1Hand.Visibility = Visibility.Visible;
        Player2Hand.Visibility = Visibility.Visible;
        Player3Hand.Visibility = Visibility.Visible;
        UpCard.SetCard(_game.State.UpCard!);
        UpCard.ShowBack();

        await Task.Delay(2000);

        GameMessage.Visibility = Visibility.Collapsed;

        ShowHumanHand();
        ShowUpCard();

        // Cards are dealt and revealed. Begin bidding.
        GameMessage.Text = "Starting...";
        GameMessage.Visibility = Visibility.Visible;

        await Task.Delay(1500);

        _actionMode = ActionMode.FirstRoundBid;

        _game.StartFirstBiddingRound();

        await ProcessFirstRoundBidder();
    }

    private async Task ProcessFirstRoundBidder()
    {
        Player player = _game.CurrentBidder;

        // Player 1 is the human. Stop here and wait for a button click.
        if (player.Position == 0)
        {
            GameMessage.Text = "Your decision";

            ActionButton1.Content = "Pickup";
            ActionButton1.IsEnabled = true;
            ActionButton1.Visibility = Visibility.Visible;

            ActionButton2.Content = "Pass";
            ActionButton2.IsEnabled = true;
            ActionButton2.Visibility = Visibility.Visible;

            return;
        }

        // AI player gets a short thinking pause.
        GameMessage.Text = $"{player.Name} is thinking...";

        await Task.Delay(1000);

        bool pickup = _game.CurrentBidderShouldOrderUp();

        GameMessage.Text = pickup
            ? $"{player.Name}: Pickup"
            : $"{player.Name}: Pass";

        await Task.Delay(1000);

        if (pickup)
        {
            _game.State.OrderUp(player.Position);

            GameMessage.Text = $"{player.Name} ordered up.";

            // We'll handle the dealer's discard next.
            return;
        }

        _game.AdvanceBidder();

        if (_game.BiddingRoundComplete)
        {
            await StartSecondBiddingRound();
            return;
        }

        await ProcessFirstRoundBidder();
    }

    private async Task StartSecondBiddingRound()
    {
        GameMessage.Text = "Second round of bidding...";
        GameMessage.Visibility = Visibility.Visible;

        ActionButton1.IsEnabled = false;
        ActionButton2.IsEnabled = false;

        await Task.Delay(1000);

        UpCard.ShowBack();

        _actionMode = ActionMode.SecondRoundBid;

        _game.StartSecondBiddingRound();

        await ProcessSecondRoundBidder();
    }

    private async Task ProcessSecondRoundBidder()
    {
        Player player = _game.CurrentBidder;
        Suit rejectedSuit = _game.State.UpCard!.Suit;

        if (player.Position == 0)
        {
            GameMessage.Text = "Choose trump";

            ActionButton1.Content = "Pass";
            ActionButton1.IsEnabled = true;
            ActionButton1.Visibility = Visibility.Visible;

            Suit[] availableSuits = Enum.GetValues<Suit>()
                .Where(suit => suit != rejectedSuit)
                .ToArray();

            ActionButton2.Content = SuitName(availableSuits[0]);
            ActionButton2.IsEnabled = true;
            ActionButton2.Visibility = Visibility.Visible;

            ActionButton3.Content = SuitName(availableSuits[1]);
            ActionButton3.IsEnabled = true;
            ActionButton3.Visibility = Visibility.Visible;

            ActionButton4.Content = SuitName(availableSuits[2]);
            ActionButton4.IsEnabled = true;
            ActionButton4.Visibility = Visibility.Visible;

            return;
        }

        GameMessage.Text = $"{player.Name} is thinking...";

        await Task.Delay(1000);

        Suit? chosenTrump =
            AiPlayer.ChooseTrump(player, rejectedSuit);

        if (chosenTrump.HasValue)
        {
            _game.State.SetTrump(chosenTrump.Value, player.Position);

            GameMessage.Text =
                $"{player.Name} calls {SuitName(chosenTrump.Value)}.";

            await Task.Delay(1000);

            // Bidding is finished. Playing the hand comes next.
            return;
        }

        GameMessage.Text = $"{player.Name}: Pass";

        await Task.Delay(1000);

        _game.AdvanceBidder();

        if (_game.BiddingRoundComplete)
        {
            // All three passed twice. This is a misdeal/redeal.
            GameMessage.Text = "Nobody called trump.";
            return;
        }

        await ProcessSecondRoundBidder();
    }

    private static string SuitName(Suit suit) => suit switch
    {
        Suit.Clubs => "Clubs",
        Suit.Diamonds => "Diamonds",
        Suit.Hearts => "Hearts",
        Suit.Spades => "Spades",
        _ => "?"
    };

    private void ShowUpCard()
    {
        if (_game.State.UpCard is not Card card)
            return;

        UpCard.SetCard(card);
        UpCard.ShowFace();
    }

    private void ShowHumanHand()
    {
        Player1Hand.Children.Clear();

        Player human = _game.State.Players[0];

        foreach (Card card in human.Hand)
        {
            var cardView = new CardView();
            cardView.SetCard(card);
            cardView.ShowFace();

            Player1Hand.Children.Add(cardView);
        }
    }
}