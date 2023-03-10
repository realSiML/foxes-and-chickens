namespace ppm_foxes_and_chickens.Models;

public enum CellType
{
    Common,
    Target,
    Reached

}

public enum CellStatus
{
    Default,
    Selected,
    CanMoveTo,
}

public sealed class Cell
{
    public static int SIZE { get; } = 64;
    public Vector2 Position { get; set; }
    public Animal? Animal { get; set; }
    public Rectangle Rectangle => new((int)Position.X, (int)Position.Y, SIZE, SIZE);
    public CellType Type { get; set; }
    public CellStatus Status { get; set; }

}