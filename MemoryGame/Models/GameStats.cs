using System;

namespace MemoryGame.Models
{
    // Represents the statistics for a single completed game session.
    // This is a simple data object (POCO - Plain Old CLR Object) with no logic.
    public class GameStats
    {
        // The name of the player.
        public string PlayerName { get; set; }
        // The number of moves (pairs flipped) the player took to complete the game.
        public int Moves { get; set; }
        // The total time taken to complete the game.
        public TimeSpan GameTime { get; set; }
        // The date and time when the game was completed.
        public DateTime CompletedAt { get; set; }
    }
}