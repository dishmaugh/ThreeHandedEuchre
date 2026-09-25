namespace ThreeHandedEuchre.Core;

public static class AiPlayer
{
    public static bool ShouldOrderUp(
        Player player,
        Card upCard,
        int dealerPosition)
    {
        Suit proposedTrump = upCard.Suit;
        bool isDealer = player.Position == dealerPosition;

        List<Card> candidateHand = [.. player.Hand];

        if (isDealer)
        {
            candidateHand.Add(upCard);

            Card discard = ChooseDiscard(
                candidateHand,
                proposedTrump);

            candidateHand.Remove(discard);
        }

        double expectedTricks =
            EstimateTricks(candidateHand, proposedTrump);

        double bestAlternative =
            EstimateBestAlternative(candidateHand, proposedTrump);

        // For now, require approximately three expected tricks.
        // A strong alternative suit makes passing somewhat more attractive.
        double threshold = isDealer ? 2.5 : 3.0;

        if (bestAlternative >= 3.0)
            threshold += 0.25;

        return expectedTricks >= threshold;
    }

    public static Suit? ChooseTrump(
        Player player,
        Suit rejectedSuit)
    {
        Suit? bestSuit = null;
        double bestExpectedTricks = 0;

        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            if (suit == rejectedSuit)
                continue;

            double expectedTricks = EstimateTricks(
                player.Hand,
                suit);

            if (expectedTricks > bestExpectedTricks)
            {
                bestExpectedTricks = expectedTricks;
                bestSuit = suit;
            }
        }

        return bestExpectedTricks >= 3.0
            ? bestSuit
            : null;
    }

    public static double EstimateTricks(
        IReadOnlyList<Card> hand,
        Suit trump)
    {
        double tricks = 0;

        List<Card> trumpCards = hand
            .Where(card =>
                EuchreRules.GetEffectiveSuit(card, trump) == trump)
            .ToList();

        bool hasRight = trumpCards.Any(card =>
            EuchreRules.IsRightBower(card, trump));

        bool hasLeft = trumpCards.Any(card =>
            EuchreRules.IsLeftBower(card, trump));

        // Right bower cannot lose.
        if (hasRight)
            tricks += 1.0;

        // Left bower is very strong by itself and effectively
        // guaranteed when protected by the right.
        if (hasLeft)
            tricks += hasRight ? 1.0 : 0.9;

        // Evaluate the remaining trump cards.
        foreach (Card card in trumpCards)
        {
            if (EuchreRules.IsRightBower(card, trump) ||
                EuchreRules.IsLeftBower(card, trump))
            {
                continue;
            }

            tricks += card.Rank switch
            {
                Rank.Ace => 0.85,
                Rank.King => 0.65,
                Rank.Queen => 0.45,
                Rank.Ten => 0.25,
                _ => 0
            };
        }

        // Multiple trump cards support one another:
        // high trump can pull opposing trump, while lower trump
        // become more useful after stronger cards are exhausted.
        int trumpCount = trumpCards.Count;

        if (trumpCount >= 2)
            tricks += 0.20;

        if (trumpCount >= 3)
            tricks += 0.30;

        if (trumpCount >= 4)
            tricks += 0.30;

        // Evaluate non-trump suits as groups rather than treating
        // every high card as an independent trick.
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            if (suit == trump)
                continue;

            List<Card> suitCards = hand
                .Where(card =>
                    EuchreRules.GetEffectiveSuit(card, trump) == suit)
                .OrderByDescending(card => card.Rank)
                .ToList();

            if (suitCards.Count == 0)
                continue;

            tricks += EstimateOffSuitTricks(suitCards);
        }

        return Math.Min(5.0, tricks);
    }

    private static double EstimateOffSuitTricks(
        IReadOnlyList<Card> cards)
    {
        bool hasAce = cards.Any(card => card.Rank == Rank.Ace);
        bool hasKing = cards.Any(card => card.Rank == Rank.King);

        if (!hasAce)
            return 0;

        // An ace is a plausible trick, but long suits reduce its
        // independence because several cards compete for the same
        // opportunity and opponents may become void.
        double tricks = cards.Count switch
        {
            1 => 0.85,
            2 => 0.75,
            3 => 0.60,
            _ => 0.45
        };

        // A protected king can sometimes become another winner
        // after the ace is played.
        if (hasKing && cards.Count <= 2)
            tricks += 0.20;

        return tricks;
    }

    private static double EstimateBestAlternative(
        IReadOnlyList<Card> hand,
        Suit proposedTrump)
    {
        double best = 0;

        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            if (suit == proposedTrump)
                continue;

            double estimate = EstimateTricks(hand, suit);

            if (estimate > best)
                best = estimate;
        }

        return best;
    }

    public static Card ChooseDiscard(
        IReadOnlyList<Card> hand,
        Suit trump)
    {
        if (hand.Count != 6)
            throw new ArgumentException(
                "Dealer must have six cards when choosing a discard.",
                nameof(hand));

        Card? bestDiscard = null;
        double bestRemainingTricks = double.MinValue;

        foreach (Card candidate in hand)
        {
            List<Card> remaining = hand
                .Where(card => !ReferenceEquals(card, candidate))
                .ToList();

            double expectedTricks = EstimateTricks(remaining, trump);

            if (expectedTricks > bestRemainingTricks)
            {
                bestRemainingTricks = expectedTricks;
                bestDiscard = candidate;
            }
        }

        return bestDiscard!;
    }
}