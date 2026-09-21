namespace ThreeHandedEuchre.Core;

public sealed class Deck
{
    private readonly List<Card> _cards = [];

    public IReadOnlyList<Card> Cards => _cards;

    public Deck()
    {
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(rank, suit));
            }
        }
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);

            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
            throw new InvalidOperationException("The deck is empty.");

        Card card = _cards[0];
        _cards.RemoveAt(0);

        return card;
    }
}