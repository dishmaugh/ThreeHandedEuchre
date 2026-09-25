using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using ThreeHandedEuchre.Core;

namespace ThreeHandedEuchre.App
{
    public sealed partial class CardView : UserControl
    {
        private Card? _card;

        public Card? Card => _card;

        public event EventHandler? CardClicked;

        public CardView()
        {
            InitializeComponent();
            Clear();
        }

        private void CardView_Tapped(
            object sender,
            Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CardClicked?.Invoke(this, EventArgs.Empty);
        }

        public void SetCard(Card card)
        {
            _card = card;

            RankText.Text = RankTextFor(card.Rank);

            string symbol = SuitTextFor(card.Suit);

            SmallSuitText.Text = symbol;
            SuitText.Text = symbol;

            var color = IsRedSuit(card.Suit)
                ? Colors.DarkRed
                : Colors.Black;

            var brush = new SolidColorBrush(color);

            RankText.Foreground = brush;
            SmallSuitText.Foreground = brush;
            SuitText.Foreground = brush;
        }

        public void ShowFace()
        {
            if (_card is null)
                return;

            CardBack.Visibility = Visibility.Collapsed;
            CardFace.Visibility = Visibility.Visible;
        }

        public void ShowBack()
        {
            CardFace.Visibility = Visibility.Collapsed;
            CardBack.Visibility = Visibility.Visible;
        }

        public void Clear()
        {
            _card = null;

            RankText.Text = "";
            SmallSuitText.Text = "";
            SuitText.Text = "";

            CardFace.Visibility = Visibility.Collapsed;
            CardBack.Visibility = Visibility.Collapsed;
        }

        private static string RankTextFor(Rank rank) =>
            rank switch
            {
                Rank.Ten => "10",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                Rank.Ace => "A",
                _ => ""
            };

        private static string SuitTextFor(Suit suit) =>
            suit switch
            {
                Suit.Clubs => "♣",
                Suit.Diamonds => "♦",
                Suit.Hearts => "♥",
                Suit.Spades => "♠",
                _ => ""
            };

        private static bool IsRedSuit(Suit suit) =>
            suit == Suit.Hearts || suit == Suit.Diamonds;
    }
}