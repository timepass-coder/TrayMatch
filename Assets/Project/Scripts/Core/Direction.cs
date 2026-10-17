namespace TrayMatch.Core
{
    // Grid space: x grows to the right, y grows DOWN (row 0 is the top row), same as the prototype.
    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }

    public static class DirectionExtensions
    {
        public static int Dx(this Direction d) => d == Direction.Right ? 1 : d == Direction.Left ? -1 : 0;

        public static int Dy(this Direction d) => d == Direction.Down ? 1 : d == Direction.Up ? -1 : 0;
    }
}