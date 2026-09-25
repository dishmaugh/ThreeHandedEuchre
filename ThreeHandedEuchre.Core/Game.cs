namespace ThreeHandedEuchre.Core;

public sealed class Game
{
    public GameState State { get; }

    private readonly Random _random = new();

    private int _currentBidderPosition;
    private int _bidsThisRound;

    public Player CurrentBidder =>
        State.Players[_currentBidderPosition];

    public bool BiddingRoundComplete =>
        _bidsThisRound >= State.Players.Count;


    public Game()
    {
        State = new GameState();
    }

     public bool CurrentBidderShouldOrderUp()
    {
        return AiPlayer.ShouldOrderUp(
            CurrentBidder,
            State.UpCard!,
            State.DealerPosition);
    }

    public void StartFirstBiddingRound()
    {
        _currentBidderPosition =
            (State.DealerPosition + 1) % State.Players.Count;

        _bidsThisRound = 0;
    }

    public void StartSecondBiddingRound()
    {
        _currentBidderPosition =
            (State.DealerPosition + 1) % State.Players.Count;

        _bidsThisRound = 0;
    }

    public void AdvanceBidder()
    {
        _bidsThisRound++;

        _currentBidderPosition =
            (_currentBidderPosition + 1) % State.Players.Count;
    }

    public bool ProcessCurrentAiBid()
    {
        Player player = CurrentBidder;

        bool orderedUp = AiPlayer.ShouldOrderUp(
            player,
            State.UpCard!,
            State.DealerPosition);

        if (orderedUp)
        {
            State.OrderUp(player.Position);
            return true;
        }

        _currentBidderPosition =
            (_currentBidderPosition + 1) % State.Players.Count;

        return false;
    }
}