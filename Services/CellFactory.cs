using BenMakesGames.PlayPlayMini.Attributes.DI;
using ppm_foxes_and_chickens.Models;

namespace ppm_foxes_and_chickens.Services;

[AutoRegister(Lifetime.Singleton)]
public sealed class CellFactory
{
    public Cell CreateCell(Vector2 position, CellType type, Animal? animal)
    {
        return new Cell()
        {
            Position = new Vector2(position.X, position.Y),
            Type = type,
            Animal = animal
        };
    }
}