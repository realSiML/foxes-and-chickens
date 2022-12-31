namespace ppm_foxes_and_chickens.Models;

public abstract class Animal
{
    public static readonly int POSITION_OFFSET = 32;
    private Vector2 position;

    public Vector2 Position
    {
        get => position; set
        {
            position = value + new Vector2(Animal.POSITION_OFFSET, Animal.POSITION_OFFSET);
        }
    }
    public (int, int) Index { get; set; }

}