using System;

namespace PuzzleRoom.Puzzles.PowerGrid
{
    [Flags]
    public enum ConnectionDirection
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }
}
