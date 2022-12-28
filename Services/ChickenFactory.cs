using BenMakesGames.PlayPlayMini.Attributes.DI;
using ppm_foxes_and_chickens.Models;

namespace ppm_foxes_and_chickens.Services;

[AutoRegister(Lifetime.Singleton)]
public sealed class ChickenFactory
{
    public Chicken CreateChicken(Vector2 position)
    {
        return new Chicken()
        {
            Position = new Vector2(position.X, position.Y)
        };
    }
}