using static ThreeHandedEuchre.Core.GameState;

namespace ThreeHandedEuchre.Core;

public static class EuchreRules
{
    public static bool IsRightBower(Card card, Suit trump)
    {
        return card.Rank == Rank.Jack &&
               card.Suit == trump;
    }

    public static bool IsLeftBower(Card card, Suit trump)
    {
        return card.Rank == Rank.Jack &&
               card.Suit == SameColorSuit(trump);
    }

    public static Suit GetEffectiveSuit(Card card, Suit trump)
    {
        return IsLeftBower(card, trump)
            ? trump
            : card.Suit;
    }

    private static Suit SameColorSuit(Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => Suit.Spades,
            Suit.Spades => Suit.Clubs,
            Suit.Diamonds => Suit.Hearts,
            Suit.Hearts => Suit.Diamonds,
            _ => throw new ArgumentOutOfRangeException(nameof(suit))
        };
    }

    public static PlayedCard GetTrickWinner(
        IReadOnlyList<PlayedCard> trick,
        Suit ledSuit,
        Suit trump)
    {
        return trick
            .OrderByDescending(play => GetTrickValue(play.Card, ledSuit, trump))
            .First();
    }

    private static int GetTrickValue(Card card, Suit ledSuit, Suit trump)
    {
        // Right bower
        if (card.Rank == Rank.Jack && card.Suit == trump)
            return 100;

        // Left bower
        if (IsLeftBower(card, trump))
            return 99;

        Suit effectiveSuit = GetEffectiveSuit(card, trump);

        // Trump beats everything else.
        if (effectiveSuit == trump)
            return 80 + GetRankValue(card.Rank);

        // Then cards of the suit that was led.
        if (effectiveSuit == ledSuit)
            return 40 + GetRankValue(card.Rank);

        // Off-suit cards cannot win.
        return GetRankValue(card.Rank);
    }

    private static int GetRankValue(Rank rank)
    {
        return rank switch
        {
            Rank.Ace => 5,
            Rank.King => 4,
            Rank.Queen => 3,
            Rank.Jack => 2,
            Rank.Ten => 1,
            _ => 0
        };
    }
}