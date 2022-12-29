namespace ppm_foxes_and_chickens.Models;

public sealed class Fox : Animal
{
    public required Queue<(int, int)> Queue { get; init; }
}

