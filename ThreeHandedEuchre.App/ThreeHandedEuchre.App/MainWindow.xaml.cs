using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
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
        None,
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

        Title = "Three-Handed Euchre";

        HumanCard0.CardClicked += HumanCard_Clicked;
        HumanCard1.CardClicked += HumanCard_Clicked;
        HumanCard2.CardClicked += HumanCard_Clicked;
        HumanCard3.CardClicked += HumanCard_Clicked;
        HumanCard4.CardClicked += HumanCard_Clicked;
        HumanCard5.CardClicked += HumanCard_Clicked;

        AppWindow.Resize(new SizeInt32(1152, 768));

        _game = new Game();
    }

    private void HideActionButtons()
    {
        ActionButton1.Visibility = Visibility.Collapsed;
        ActionButton2.Visibility = Visibility.Collapsed;
        ActionButton3.Visibility = Visibility.Collapsed;
        ActionButton4.Visibility = Visibility.Collapsed;
    }

    private async void HumanCard_Clicked(object? sender, EventArgs e)
    {
        CardView clickedCard = (CardView)sender!;
        Card? card = clickedCard.Card;

        if (card is null)
            return;

        if (_actionMode == ActionMode.Play)
        {
            Suit? ledSuit = null;

            if (_game.State.CurrentTrick.Count > 0)
            {
                ledSuit = EuchreRules.GetEffectiveSuit(
                    _game.State.CurrentTrick[0].Card,
                    _game.State.Trump!.Value);
            }

            // Must follow suit if possible.
            if (ledSuit.HasValue)
            {
                bool canFollowSuit = _game.State.Players[0].Hand.Any(c =>
                    EuchreRules.GetEffectiveSuit(
                        c,
                        _game.State.Trump!.Value) == ledSuit.Value);

                if (canFollowSuit &&
                    EuchreRules.GetEffectiveSuit(
                        card,
                        _game.State.Trump!.Value) != ledSuit.Value)
                {
                    GameMessage.Text = "You must follow suit.";
                    return;
                }
            }

            _game.State.PlayCard(0, card);

            ShowHumanHand();

            PlayedCard1.SetCard(card);
            PlayedCard1.ShowFace();
            PlayedCard1.Visibility = Visibility.Visible;

            GameMessage.Text = "Player 1 played.";

            _actionMode = ActionMode.None;

            await Task.Delay(750);

            GameMessage.Text = "";

            if (_game.State.CurrentTrick.Count == 3)
            {
                await CompleteTrick();
                return;
            }

            int nextPlayerPosition = 1;

            await PlayNextAiCard(nextPlayerPosition);

            return;
        }

        if (_actionMode == ActionMode.Discard)
        {
            _game.State.DiscardFromHumanHand(card);

            _actionMode = ActionMode.Play;

            ShowHumanHand();

            GameMessage.Text =
                $"Discarded {card.Rank} of {card.Suit}.";
            GameMessage.Visibility = Visibility.Visible;

            await Task.Delay(1000);

            StartHandPlay();
        }
    }

    private async void ActionButton1_Click(object sender, RoutedEventArgs e)
    {
        HideActionButtons();

        switch (_actionMode)
        {
            case ActionMode.Play:
                await StartGame();
                break;

            case ActionMode.FirstRoundBid:
                await HumanPickup();
                break;

            case ActionMode.SecondRoundBid:
                await HumanPassSecondRound();
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
        HideActionButtons();

        if (_actionMode == ActionMode.FirstRoundBid)
        {
            await HumanPassFirstRound();
            return;
        }

        if (_actionMode == ActionMode.SecondRoundBid)
        {
            await HumanCallTrump(ActionButton2);
        }
    }

    private async void ActionButton3_Click(object sender, RoutedEventArgs e)
    {
        HideActionButtons();

        if (_actionMode == ActionMode.SecondRoundBid)
        {
            await HumanCallTrump(ActionButton3);
        }
    }

    private async void ActionButton4_Click(object sender, RoutedEventArgs e)
    {
        HideActionButtons();

        if (_actionMode == ActionMode.SecondRoundBid)
        {
            await HumanCallTrump(ActionButton4);
        }
    }

    private void UpdateScores()
    {
        Player player1 = _game.State.Players[0];
        Player player2 = _game.State.Players[1];
        Player player3 = _game.State.Players[2];

        Player1ScoreText.Text = FormatPlayerScore(player1);
        Player2ScoreText.Text = FormatPlayerScore(player2);
        Player3ScoreText.Text = FormatPlayerScore(player3);
    }

    private static string FormatPlayerScore(Player player)
    {
        string role = player.Position switch
        {
            0 => "Human",
            1 => "AI-1",
            2 => "AI-2",
            _ => ""
        };

        string score = player.Score.ToString();

        if (player.TricksWon > 0)
            score += $"-{player.TricksWon}";

        return $"{player.Name} ({role}): {score}";
    }

    private async Task CompleteTrick()
    {
        Player winner = _game.State.CompleteTrick();
        UpdateScores();

        GameMessage.Text = $"{winner.Name} wins the trick.";
        GameMessage.Visibility = Visibility.Visible;

        await Task.Delay(1250);

        PlayedCard1.Clear();
        PlayedCard2.Clear();
        PlayedCard3.Clear();

        PlayedCard1.Visibility = Visibility.Collapsed;
        PlayedCard2.Visibility = Visibility.Collapsed;
        PlayedCard3.Visibility = Visibility.Collapsed;

        if (_game.State.HandComplete)
        {
            _game.State.ScoreHand();

            UpdateScores();

            GameMessage.Text = "Hand complete.";
            GameMessage.Visibility = Visibility.Visible;

            await Task.Delay(1500);

            // Game ends when someone reaches 10 points.
            int highScore = _game.State.Players.Max(player => player.Score);

            if (highScore >= 10)
            {
                List<Player> leaders = _game.State.Players
                    .Where(player => player.Score == highScore)
                    .ToList();

                if (leaders.Count == 1)
                {
                    Player gameWinner = leaders[0];

                    foreach (Player player in _game.State.Players)
                        player.TricksWon = 0;

                    UpdateScores();

                    GameMessage.Text = $"{gameWinner.Name} wins the game!";
                    GameMessage.Visibility = Visibility.Visible;

                    _actionMode = ActionMode.PlayAgain;

                    ActionButton1.Content = "Play Again";
                    ActionButton1.IsEnabled = true;
                    ActionButton1.Visibility = Visibility.Visible;
                    ActionButton2.Visibility = Visibility.Collapsed;

                    return;
                }
            }

            await Redeal();
            return;
        }

        GameMessage.Text = "Playing next trick...";

        await Task.Delay(500);

        await PlayAiLead();
    }

    private async Task HumanCallTrump(Button button)
    {
        if (_actionMode != ActionMode.SecondRoundBid)
            return;

        if (!Enum.TryParse<Suit>(button.Content?.ToString(), out Suit suit))
            return;

        _game.State.SetTrump(suit, 0);

        ShowCaller();

        GameMessage.Text = $"Player 1 calls {suit}.";

        ActionButton1.IsEnabled = false;
        ActionButton2.IsEnabled = false;
        ActionButton3.IsEnabled = false;
        ActionButton4.IsEnabled = false;

        await Task.Delay(1000);

        ActionButton1.Visibility = Visibility.Collapsed;
        ActionButton2.Visibility = Visibility.Collapsed;
        ActionButton3.Visibility = Visibility.Collapsed;
        ActionButton4.Visibility = Visibility.Collapsed;

        // Bidding is finished. Playing the hand comes next.
        StartHandPlay();
    }

    private void EnableLegalHumanCards()
    {
        CardView[] cardViews =
        [
            HumanCard0,
            HumanCard1,
            HumanCard2,
            HumanCard3,
            HumanCard4
        ];

        Player human = _game.State.Players[0];

        Suit? ledSuit = null;

        if (_game.State.CurrentTrick.Count > 0)
        {
            ledSuit = EuchreRules.GetEffectiveSuit(
                _game.State.CurrentTrick[0].Card,
                _game.State.Trump!.Value);
        }

        bool mustFollowSuit =
            ledSuit.HasValue &&
            human.Hand.Any(card =>
                EuchreRules.GetEffectiveSuit(
                    card,
                    _game.State.Trump!.Value) == ledSuit.Value);

        for (int i = 0; i < human.Hand.Count; i++)
        {
            bool legal =
                !mustFollowSuit ||
                EuchreRules.GetEffectiveSuit(
                    human.Hand[i],
                    _game.State.Trump!.Value) == ledSuit!.Value;

            cardViews[i].IsEnabled = true;
            cardViews[i].Opacity = legal ? 1.0 : 0.35;
        }
    }

    private async Task HumanPickup()
    {
        int dealerPosition = _game.State.DealerPosition;
        Player dealer = _game.State.Players[dealerPosition];

        Card? discard = null;
        int discardIndex = -1;

        // If an AI is the dealer, determine which card it will discard
        // before OrderUp changes the hand.
        if (dealerPosition != 0)
        {
            List<Card> candidateHand = [.. dealer.Hand];

            if (_game.State.UpCard is not null)
                candidateHand.Add(_game.State.UpCard);

            discard = AiPlayer.ChooseDiscard(
                candidateHand,
                _game.State.UpCard!.Suit);

            discardIndex = dealer.Hand
                .ToList()
                .FindIndex(card => ReferenceEquals(card, discard));
        }

        _game.State.OrderUp(0);

        GameMessage.Text = "Player 1 orders it up.";
        GameMessage.Visibility = Visibility.Visible;

        ActionButton1.IsEnabled = false;
        ActionButton2.IsEnabled = false;

        await Task.Delay(1000);

        // AI dealer: show its discard and the up-card replacement.
        if (dealerPosition != 0)
        {
            await ShowAiDealerSwap(dealerPosition, discardIndex);

            ShowCaller();
            GameMessage.Visibility = Visibility.Collapsed;

            // Gameplay will begin here next.
            StartHandPlay();
            return;
        }

        // Human dealer: OrderUp has given Player 1 the sixth card.
        // Stop here and let the human choose the discard.
        _actionMode = ActionMode.Discard;

        ShowCaller();

        UpCard.Clear();

        GameMessage.Text = "Choose a card to discard.";
        GameMessage.Visibility = Visibility.Visible;

        ShowHumanHand();
    }

    private async Task Redeal()
    {
        _game.State.AdvanceDealer();
        _game.State.Deal();

        UpdateScores();

        DealerText.Text = $"Dealer: {_game.State.Dealer.Name}";
        TrumpText.Text = "Trump: --";

        GameMessage.Text = "Dealing...";
        GameMessage.Visibility = Visibility.Visible;

        ShowHumanHand();
        ShowAiHands();

        UpCard.SetCard(_game.State.UpCard!);
        UpCard.ShowBack();
        UpCard.Visibility = Visibility.Visible;

        await Task.Delay(1500);

        ShowUpCard();

        _actionMode = ActionMode.FirstRoundBid;

        _game.StartFirstBiddingRound();

        await ProcessFirstRoundBidder();
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
        foreach (Player player in _game.State.Players)
        {
            player.Score = 0;
            player.TricksWon = 0;
        }

        UpdateScores();

        ActionButton1.IsEnabled = false;

        GameMessage.Text = "Shuffling...";
        GameMessage.Visibility = Visibility.Visible;

        await Task.Delay(2000);

        // Real game begins here.
        _game = new Game();
        _game.State.Deal();

        DealerText.Text = $"Dealer: {_game.State.Dealer.Name}";
        TrumpText.Text = "Trump: --";

        UpCard.SetCard(_game.State.UpCard!);
        UpCard.ShowBack();
        UpCard.Visibility = Visibility.Visible;

        GameMessage.Text = "Dealing...";

        Player1Hand.Visibility = Visibility.Visible;
        Player2Hand.Visibility = Visibility.Visible;
        Player3Hand.Visibility = Visibility.Visible;
        UpCard.SetCard(_game.State.UpCard!);
        UpCard.ShowBack();

        await Task.Delay(2000);

        GameMessage.Visibility = Visibility.Collapsed;

        ShowHumanHand();

        ShowAiHands();
        ShowUpCard();

        // Cards are dealt and revealed. Begin bidding.
        GameMessage.Text = "Starting...";
        GameMessage.Visibility = Visibility.Visible;

        await Task.Delay(1500);

        _actionMode = ActionMode.FirstRoundBid;

        _game.StartFirstBiddingRound();

        await ProcessFirstRoundBidder();
    }

    private void ShowAiHands()
    {
        CardView[] player2Cards =
        [
            Player2Card0,
            Player2Card1,
            Player2Card2,
            Player2Card3,
            Player2Card4
            ];

        CardView[] player3Cards =
        [
            Player3Card0,
            Player3Card1,
            Player3Card2,
            Player3Card3,
            Player3Card4
            ];

        Player player2 = _game.State.Players[1];
        Player player3 = _game.State.Players[2];

        foreach (CardView cardView in player2Cards)
            cardView.Clear();

        foreach (CardView cardView in player3Cards)
            cardView.Clear();

        for (int i = 0; i < player2.Hand.Count; i++)
        {
            player2Cards[i].SetCard(player2.Hand[i]);

            if (ReferenceEquals(player2.Hand[i], _game.State.KnownUpCard))
                player2Cards[i].ShowFace();
            else
                player2Cards[i].ShowBack();
        }

        for (int i = 0; i < player3.Hand.Count; i++)
        {
            player3Cards[i].SetCard(player3.Hand[i]);

            if (ReferenceEquals(player3.Hand[i], _game.State.KnownUpCard))
                player3Cards[i].ShowFace();
            else
                player3Cards[i].ShowBack();
        }
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
            int dealerPosition = _game.State.DealerPosition;
            Player dealer = _game.State.Players[dealerPosition];

            Card? discard = null;
            int discardIndex = -1;

            if (dealerPosition != 0)
            {
                List<Card> candidateHand = [.. dealer.Hand];

                if (_game.State.UpCard is not null)
                    candidateHand.Add(_game.State.UpCard);

                discard = AiPlayer.ChooseDiscard(
                    candidateHand,
                    _game.State.UpCard!.Suit);

                discardIndex = dealer.Hand
                    .ToList()
                    .FindIndex(card => ReferenceEquals(card, discard));
            }

            _game.State.OrderUp(player.Position);

            GameMessage.Text = $"{player.Name} ordered up.";

            ShowCaller();

            if (_game.State.DealerPosition == 0)
            {
                UpCard.Clear();

                ShowHumanHand();

                _actionMode = ActionMode.Discard;

                GameMessage.Text = "Choose a card to discard.";
                GameMessage.Visibility = Visibility.Visible;

                ActionButton1.IsEnabled = false;
                ActionButton2.IsEnabled = false;

                return;
            }

            // AI dealer: show the discard disappearing and the
            // known up card taking its place.
            await ShowAiDealerSwap(dealerPosition, discardIndex);

            await Task.Delay(1000);

            StartHandPlay();

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

    private async Task PlayAiLead()
    {
        int playerPosition = _game.State.LeaderPosition;

        // Player 1 is human.
        if (playerPosition == 0)
        {
            _actionMode = ActionMode.Play;
            GameMessage.Text = "Choose a card to play.";
            GameMessage.Visibility = Visibility.Visible;
            EnableLegalHumanCards();
            return;
        }

        Player player = _game.State.Players[playerPosition];

        Card card = player.ChooseCard(
            null,
            _game.State.Trump!.Value,
            Random.Shared);

        _game.State.PlayCard(playerPosition, card);

        ShowAiHands();

        CardView cardView = GetPlayedCardView(playerPosition);

        cardView.SetCard(card);
        cardView.ShowFace();
        cardView.Visibility = Visibility.Visible;

        await Task.Delay(750);

        int nextPlayerPosition = (playerPosition + 1) % 3;

        if (nextPlayerPosition == 0)
        {
            _actionMode = ActionMode.Play;
            GameMessage.Text = "Choose a card to play.";
            GameMessage.Visibility = Visibility.Visible;
            EnableLegalHumanCards();
            return;
        }

        await PlayNextAiCard(nextPlayerPosition);
    }

    private async Task PlayNextAiCard(int playerPosition)
    {
        Player player = _game.State.Players[playerPosition];

        Suit ledSuit =
            EuchreRules.GetEffectiveSuit(
                _game.State.CurrentTrick[0].Card,
                _game.State.Trump!.Value);

        Card card = player.ChooseCard(
            ledSuit,
            _game.State.Trump.Value,
            Random.Shared);

        _game.State.PlayCard(playerPosition, card);

        ShowAiHands();

        CardView cardView = GetPlayedCardView(playerPosition);

        cardView.SetCard(card);
        cardView.ShowFace();
        cardView.Visibility = Visibility.Visible;

        await Task.Delay(750);

        // All three cards have now been played.
        if (_game.State.CurrentTrick.Count == 3)
        {
            await CompleteTrick();
            return;
        }

        int nextPlayerPosition = (playerPosition + 1) % 3;

        // Human is next.
        if (nextPlayerPosition == 0)
        {
            _actionMode = ActionMode.Play;

            ShowHumanHand();
            EnableLegalHumanCards();

            GameMessage.Text = "Choose a card to play.";
            GameMessage.Visibility = Visibility.Visible;

            return;
        }

        // Another AI is next.
        await PlayNextAiCard(nextPlayerPosition);
    }

    private CardView GetPlayedCardView(int playerPosition)
    {
        return playerPosition switch
        {
            0 => PlayedCard1,
            1 => PlayedCard2,
            2 => PlayedCard3,
            _ => throw new ArgumentOutOfRangeException(nameof(playerPosition))
        };
    }

    private async Task HumanPassSecondRound()
    {
        GameMessage.Text = "Player 1 passes";

        ActionButton1.IsEnabled = false;
        ActionButton2.IsEnabled = false;
        ActionButton3.IsEnabled = false;
        ActionButton4.IsEnabled = false;

        await Task.Delay(1000);

        _game.AdvanceBidder();

        if (_game.BiddingRoundComplete)
        {
            GameMessage.Text = "Nobody called trump.";

            await Task.Delay(1500);

            await Redeal();
            return;
        }

        await ProcessSecondRoundBidder();
    }

    private void StartHandPlay()
    {
        _game.State.StartHandPlay();

        ShowCaller();

        UpCard.Clear();
        UpCard.Visibility = Visibility.Collapsed;

        HideActionButtons();

        GameMessage.Text = "Playing hand...";

        _ = PlayAiLead();
    }

    private void ShowCaller()
    {
        if (!_game.State.CallerPosition.HasValue)
            return;

        Player caller =
            _game.State.Players[_game.State.CallerPosition.Value];

        DealerText.Text = $"Called: {caller.Name}";
        TrumpText.Text = $"Trump: {_game.State.Trump}";
    }

    private async Task ShowAiDealerSwap(
        int dealerPosition,
        int discardIndex)
    {
        CardView[] dealerCards = dealerPosition == 1
            ?
            [
                Player2Card0,
            Player2Card1,
            Player2Card2,
            Player2Card3,
            Player2Card4
            ]
            :
            [
                Player3Card0,
            Player3Card1,
            Player3Card2,
            Player3Card3,
            Player3Card4
            ];

        // Briefly remove the discarded card.
        if (discardIndex >= 0)
        {
            dealerCards[discardIndex].Clear();
            await Task.Delay(750);
        }

        // Redisplay the dealer's current five-card hand face down.
        Player dealer = _game.State.Players[dealerPosition];

        for (int i = 0; i < dealer.Hand.Count; i++)
        {
            dealerCards[i].SetCard(dealer.Hand[i]);
            dealerCards[i].ShowBack();
        }

        // The up card was public information, so leave its
        // replacement position face up.
        if (discardIndex >= 0)
            dealerCards[discardIndex].ShowFace();

        UpCard.Clear();
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

            ShowCaller();

            GameMessage.Text =
                $"{player.Name} calls {SuitName(chosenTrump.Value)}.";

            await Task.Delay(1000);

            StartHandPlay();
            return;
        }

        GameMessage.Text = $"{player.Name}: Pass";

        await Task.Delay(1000);

        _game.AdvanceBidder();

        if (_game.BiddingRoundComplete)
        {
            GameMessage.Text = "Nobody called trump.";

            ActionButton1.IsEnabled = false;
            ActionButton2.IsEnabled = false;
            ActionButton3.IsEnabled = false;
            ActionButton4.IsEnabled = false;

            await Task.Delay(1500);

            await Redeal();
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
        CardView[] humanCards =
        [
            HumanCard0,
            HumanCard1,
            HumanCard2,
            HumanCard3,
            HumanCard4,
            HumanCard5
            ];

        Player human = _game.State.Players[0];

        for (int i = 0; i < humanCards.Length; i++)
        {
            // Reset any legal-play visual state left from the previous trick/hand.
            humanCards[i].IsEnabled = true;
            humanCards[i].Opacity = 1.0;

            if (i < human.Hand.Count)
            {
                humanCards[i].SetCard(human.Hand[i]);
                humanCards[i].ShowFace();
                humanCards[i].Visibility = Visibility.Visible;
            }
            else
            {
                humanCards[i].Clear();
                humanCards[i].Visibility = Visibility.Collapsed;
            }
        }
    }
}