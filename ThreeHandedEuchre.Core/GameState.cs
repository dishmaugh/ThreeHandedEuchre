namespace ThreeHandedEuchre.Core;

public sealed class GameState
{
    private readonly List<Player> _players =
        [
            new Player(0, "Player 1"),
            new Player(1, "Player 2"),
            new Player(2, "Player 3")
        ];

    private readonly List<Card> _kitty = [];

    public IReadOnlyList<Player> Players => _players;
    public IReadOnlyList<Card> Kitty => _kitty;

    public int DealerPosition { get; private set; }

    public Card? UpCard { get; private set; }

    public Card? KnownUpCard { get; private set; }

    public Suit? Trump { get; private set; }

    public int? CallerPosition { get; private set; }

    public Player? Caller =>
        CallerPosition.HasValue
            ? _players[CallerPosition.Value]
            : null;

    public Player Dealer =>
        _players[DealerPosition];

    public sealed record PlayedCard(Player Player, Card Card);

    private readonly List<PlayedCard> _currentTrick = [];

    public IReadOnlyList<PlayedCard> CurrentTrick => _currentTrick;

    public int LeaderPosition { get; private set; }

    public bool HandComplete =>
        _players.Sum(player => player.TricksWon) == 5;

    public GameState()
    {
        DealerPosition = Random.Shared.Next(_players.Count);
    }

    public void StartHandPlay()
    {
        LeaderPosition = (DealerPosition + 1) % _players.Count;
        _currentTrick.Clear();
    }

    public PlayedCard PlayCard(int playerPosition, Card card)
    {
        Player player = _players[playerPosition];

        player.RemoveCard(card);

        var playedCard = new PlayedCard(player, card);
        _currentTrick.Add(playedCard);

        return playedCard;
    }

    public void Deal()
    {
        Deck deck = new();
        deck.Shuffle();

        foreach (Player player in _players)
            player.ClearHand();

        _kitty.Clear();
        UpCard = null;
        KnownUpCard = null;

        foreach (Player player in _players)
        {
            player.TricksWon = 0;
        }

        int firstPlayer = (DealerPosition + 1) % _players.Count;

        // Five rotations around the table.
        for (int round = 0; round < 5; round++)
        {
            for (int offset = 0; offset < _players.Count; offset++)
            {
                int position = (firstPlayer + offset) % _players.Count;
                _players[position].AddCard(deck.Draw());
            }
        }

        // Five cards remain after 15 have been dealt.
        while (deck.Cards.Count > 0)
            _kitty.Add(deck.Draw());

        UpCard = _kitty[0];
    }

    public void ScoreHand()
    {
        if (!CallerPosition.HasValue)
            throw new InvalidOperationException("There is no caller.");

        Player caller = _players[CallerPosition.Value];

        if (caller.TricksWon == 5)
        {
            caller.Score += 4;
            return;
        }

        if (caller.TricksWon >= 3)
        {
            caller.Score += 1;
            return;
        }

        foreach (Player player in _players)
        {
            if (player.Position != caller.Position)
                player.Score += 2;
        }
    }

    public Player CompleteTrick()
    {
        if (_currentTrick.Count != 3)
            throw new InvalidOperationException("The trick is not complete.");

        Suit ledSuit = EuchreRules.GetEffectiveSuit(
            _currentTrick[0].Card,
            Trump!.Value);

        PlayedCard winningPlay = EuchreRules.GetTrickWinner(
            _currentTrick,
            ledSuit,
            Trump.Value);

        Player winner = winningPlay.Player;

        winner.TricksWon++;

        LeaderPosition = winner.Position;

        _currentTrick.Clear();

        return winner;
    }

    public void SetTrump(
        Suit trump,
        int callerPosition)
    {
        Trump = trump;
        CallerPosition = callerPosition;
        UpCard = null;
    }

    public Card? OrderUp(int callerPosition)
    {
        if (UpCard is null)
            throw new InvalidOperationException("There is no up card.");

        Card upCard = UpCard;
        KnownUpCard = upCard;
        Suit trump = upCard.Suit;
        Player dealer = _players[DealerPosition];

        Trump = trump;
        CallerPosition = callerPosition;

        _kitty.Remove(upCard);

        // Human dealer will choose the discard manually later.
        if (DealerPosition == 0)
        {
            dealer.AddCard(upCard);
            UpCard = null;
            return null;
        }

        // AI dealer temporarily has six cards so it can choose
        // the best five-card hand.
        dealer.AddCard(upCard);

        Card discard = AiPlayer.ChooseDiscard(
            dealer.Hand,
            trump);

        int discardIndex = dealer.Hand
            .ToList()
            .FindIndex(card => ReferenceEquals(card, discard));

        dealer.RemoveCard(discard);
        AddToKitty(discard);

        // If the AI discarded one of its original five cards,
        // move the up card into that card's position.
        if (!ReferenceEquals(discard, upCard))
        {
            dealer.RemoveCard(upCard);
            dealer.InsertCard(discardIndex, upCard);
        }

        UpCard = null;

        return discard;
    }

    public void DiscardFromHumanHand(Card card)
    {
        Player human = _players[0];

        human.RemoveCard(card);
        AddToKitty(card);
    }

    public void AddToKitty(Card card)
    {
        _kitty.Add(card);
    }

    public void AdvanceDealer()
    {
        DealerPosition = (DealerPosition + 1) % _players.Count;
    }
}