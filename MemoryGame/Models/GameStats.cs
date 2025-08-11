using System;

namespace MemoryGame.Models
{
    public class GameStats
    {
        public string PlayerName { get; set; }
        public int Moves { get; set; }
        public TimeSpan GameTime { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}