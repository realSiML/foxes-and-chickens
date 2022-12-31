namespace ppm_foxes_and_chickens.Models;

public sealed class Fox : Animal
{
    public Queue<(int, int)> Queue { get; set; } = new();
    public MovePreference MovePreference => Index.Item2 switch
    {
        < 3 => MovePreference.Right,
        > 3 => MovePreference.Left,
        _ => MovePreference.None,
    };

}

public enum MovePreference
{
    Left,
    Right,
    None
}

