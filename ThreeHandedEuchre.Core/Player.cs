using ThreeHandedEuchre.Core;

public sealed class Player
{
    private readonly List<Card> _hand = [];

    public int Position { get; }
    public string Name { get; }
    public int Score { get; set; }
    public int TricksWon { get; set; }

    public IReadOnlyList<Card> Hand => _hand;

    public Player(int position, string name)
    {
        Position = position;
        Name = name;
    }

    internal void AddCard(Card card)
    {
        _hand.Add(card);
    }

    internal void RemoveCard(Card card)
    {
        _hand.Remove(card);
    }

    internal void ClearHand()
    {
        _hand.Clear();
    }

    public override string ToString()
    {
        return Name;
    }

    public Card ChooseCard(Suit? ledSuit, Suit trump, Random random)
    {
        if (ledSuit is null)
            return Hand[random.Next(Hand.Count)];

        var followingCards = Hand
            .Where(card => EuchreRules.GetEffectiveSuit(card, trump) == ledSuit.Value)
            .ToList();

        if (followingCards.Count > 0)
            return followingCards[random.Next(followingCards.Count)];

        return Hand[random.Next(Hand.Count)];
    }

    public void InsertCard(int index, Card card)
    {
        _hand.Insert(index, card);
    }
}