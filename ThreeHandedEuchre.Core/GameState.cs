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

    public Suit? Trump { get; private set; }

    public int? CallerPosition { get; private set; }

    public Player? Caller =>
        CallerPosition.HasValue
            ? _players[CallerPosition.Value]
            : null;

    public Player Dealer =>
        _players[DealerPosition];

    public sealed record PlayedCard(Player Player, Card Card);

    public GameState()
    {
        DealerPosition = Random.Shared.Next(_players.Count);
    }

    public void Deal()
    {
        Deck deck = new();
        deck.Shuffle();

        foreach (Player player in _players)
            player.ClearHand();

        _kitty.Clear();
        UpCard = null;

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

    public void SetTrump(
        Suit trump,
        int callerPosition)
    {
        Trump = trump;
        CallerPosition = callerPosition;
        UpCard = null;
    }

    public void OrderUp(int callerPosition)
    {
        if (UpCard is null)
            throw new InvalidOperationException("There is no up card.");

        Suit trump = UpCard.Suit;
        Player dealer = _players[DealerPosition];

        dealer.AddCard(UpCard);

        Card discard = AiPlayer.ChooseDiscard(
            dealer.Hand,
            trump);

        dealer.RemoveCard(discard);

        Trump = trump;
        CallerPosition = callerPosition;

        _kitty.Remove(UpCard);
        _kitty.Add(discard);

        UpCard = null;
    }

    public List<PlayedCard> PlayTrick(int leaderPosition, Random random)
    {
        if (Trump is null)
            throw new InvalidOperationException("Trump has not been selected.");

        var trick = new List<PlayedCard>();
        Suit? ledSuit = null;

        for (int offset = 0; offset < _players.Count; offset++)
        {
            int position = (leaderPosition + offset) % _players.Count;
            Player player = _players[position];

            Card card = player.ChooseCard(ledSuit, Trump.Value, random);

            player.RemoveCard(card);
            trick.Add(new PlayedCard(player, card));

            ledSuit ??= EuchreRules.GetEffectiveSuit(card, Trump.Value);
        }

        return trick;
    }

    public void AdvanceDealer()
    {
        DealerPosition = (DealerPosition + 1) % _players.Count;
    }
}