using System.Collections.Generic;

namespace ThaleMagnus.TheftOfTheWinterStar.Framework
{
    internal class SaveData
    {
        public int DataVersion { get; set; }
        public int DungeonYear { get; set; }
        public ArenaStage ArenaStage { get; set; } = ArenaStage.NotTriggered;
        public bool DidProjectilePuzzle { get; set; }
        public bool BeatBoss { get; set; }
        public bool ClaimedBossPresent { get; set; }
        public bool PushPuzzleSolved { get; set; }
        public SavedTilePosition? PushPuzzleTarget { get; set; }
        public HashSet<string> ClaimedRewards { get; set; } = new();
        public HashSet<string> UnlockedDoors { get; set; } = new();
        public HashSet<string> SolvedItemPuzzles { get; set; } = new();
        public HashSet<string> BombedLocations { get; set; } = new();
        public HashSet<string> InsertedBossKeyHalves { get; set; } = new();
        public List<SavedTilePosition> PushPuzzleBlocks { get; set; } = new();
    }

    internal class SavedTilePosition
    {
        public SavedTilePosition()
        {
        }

        public SavedTilePosition(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }
}
