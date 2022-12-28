using BenMakesGames.PlayPlayMini.Attributes.DI;
using ppm_foxes_and_chickens.Models;

namespace ppm_foxes_and_chickens.Services;

[AutoRegister(Lifetime.Singleton)]
public sealed class FoxFactory
{
    public Fox CreateFox(Vector2 position)
    {
        return new Fox()
        {
            Position = new Vector2(position.X, position.Y)
        };
    }
}