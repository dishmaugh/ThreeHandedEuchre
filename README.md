# Three-Handed Euchre

A three-player Euchre card game for Windows, built with C#/.NET 10 and WinUI 3.

## About

This project implements a playable three-handed version of Euchre with one human player and two computer-controlled players. It began as an exploration of Euchre strategy and grew into a complete Windows game.

The current version intentionally uses relatively simple computer players. The goal is a casual, playable game rather than an opponent optimized to defeat the human player.

## Rules

- Three players play individually.
- The deck contains 20 cards: 10, Jack, Queen, King, and Ace in each suit.
- Each player receives five cards, with the remaining five forming the kitty.
- The top card of the kitty is turned up for the first round of bidding.
- The right bower (Jack of trump) is the highest trump.
- The left bower (Jack of the same color as trump) is the second-highest trump.
- Players must follow the effective suit when able.
- The caller must take at least three tricks to make the contract.

### Scoring

- Caller takes 3 or 4 tricks: 1 point.
- Caller takes all 5 tricks: 4 points.
- If the caller is set, each opponent receives 2 points.
- The game ends when a player has at least 10 points and is the unique high scorer. A tie for the high score continues into another hand.

## Technology

- C#
- .NET 10
- WinUI 3
- Windows App SDK

The solution separates the game rules/state from the WinUI application:

- `ThreeHandedEuchre.Core` — cards, rules, game state, bidding, scoring, and AI behavior.
- `ThreeHandedEuchre.App` — Windows user interface and game interaction.

## Building

Open `ThreeHandedEuchre.slnx` in Visual Studio with the .NET 10 and WinUI/Windows App SDK development components installed.

Build and run the application from Visual Studio.

Packaging and signing are environment-specific and are not required to build or run the source project during development.

## Status

Version 1 is complete and playable. Future changes may include targeted bug fixes or AI experiments, but the intentionally simple AI is suitable for casual play.
