using static ThreeHandedEuchre.Core.GameState;

namespace ThreeHandedEuchre.Core;

public sealed class Game
{
    public GameState State { get; }

    private readonly Random _random = new();

    public Game()
    {
        State = new GameState();
    }

    public void Start()
    {
        while (!State.Players.Any(player => player.Score >= 10))
        {
            State.Deal();

            bool trumpSelected =
                RunFirstBiddingRound() ||
                RunSecondBiddingRound();

            if (!trumpSelected)
            {
                // Misdeal: nobody selected trump.
                State.AdvanceDealer();
                continue;
            }

            PlayHand();

            State.AdvanceDealer();
        }
    }

    private bool RunFirstBiddingRound()
    {
        int firstPlayer =
            (State.DealerPosition + 1) % State.Players.Count;

        for (int offset = 0;
             offset < State.Players.Count;
             offset++)
        {
            int position =
                (firstPlayer + offset) % State.Players.Count;

            Player player = State.Players[position];

            if (AiPlayer.ShouldOrderUp(
                player,
                State.UpCard!,
                State.DealerPosition))
            {
                State.OrderUp(player.Position);
                return true;
            }
        }

        return false;
    }

    private bool RunSecondBiddingRound()
    {
        Suit rejectedSuit = State.UpCard!.Suit;

        int firstPlayer =
            (State.DealerPosition + 1) % State.Players.Count;

        for (int offset = 0;
             offset < State.Players.Count;
             offset++)
        {
            int position =
                (firstPlayer + offset) % State.Players.Count;

            Player player = State.Players[position];

            Suit? chosenTrump =
                AiPlayer.ChooseTrump(player, rejectedSuit);

            if (chosenTrump.HasValue)
            {
                State.SetTrump(
                    chosenTrump.Value,
                    player.Position);

                return true;
            }
        }

        return false;
    }

    private void PlayHand()
    {
        if (State.Trump is null)
            throw new InvalidOperationException("Cannot play without trump.");

        int leaderPosition =
            (State.DealerPosition + 1) % State.Players.Count;

        int[] tricksWon = new int[State.Players.Count];

        for (int trickNumber = 0; trickNumber < 5; trickNumber++)
        {
            List<PlayedCard> trick =
                State.PlayTrick(leaderPosition, _random);

            Suit ledSuit =
                EuchreRules.GetEffectiveSuit(
                    trick[0].Card,
                    State.Trump.Value);

            PlayedCard winner =
                EuchreRules.GetTrickWinner(
                    trick,
                    ledSuit,
                    State.Trump.Value);

            tricksWon[winner.Player.Position]++;

            leaderPosition = winner.Player.Position;
        }

        ScoreHand(tricksWon);
    }

    private void ScoreHand(int[] tricksWon)
    {
        if (State.CallerPosition is null)
            throw new InvalidOperationException("No player called trump.");

        int callerPosition = State.CallerPosition.Value;
        int callerTricks = tricksWon[callerPosition];

        if (callerTricks == 5)
        {
            State.Players[callerPosition].Score += 4;
        }
        else if (callerTricks >= 3)
        {
            State.Players[callerPosition].Score += 1;
        }
        else
        {
            int opponentPoints = callerTricks == 0 ? 10 : 2;

            foreach (Player player in State.Players)
            {
                if (player.Position != callerPosition)
                    player.Score += opponentPoints;
            }
        }
    }
}