using BenMakesGames.PlayPlayMini;
using BenMakesGames.PlayPlayMini.Services;
using Microsoft.Xna.Framework.Input;

namespace ppm_foxes_and_chickens.GameStates;

public sealed class EndGameMenu : GameState
{
    private GameStateManager GSM { get; }
    private GraphicsManager Graphics { get; }
    private KeyboardManager Keyboard { get; }
    private GameState PreviousState { get; }

    private string WinOrLoseText { get; }
    private bool IsHidden { get; set; }

    public EndGameMenu(GameStateManager gsm, GraphicsManager graphics, KeyboardManager keyboard, EndGameMenuConfig config)
    {
        GSM = gsm;
        Graphics = graphics;
        Keyboard = keyboard;
        PreviousState = config.PreviousState;

        WinOrLoseText = config.WinOrLose;
        IsHidden = false;
    }

    public override void AlwaysDraw(GameTime gameTime)
    {
        PreviousState.AlwaysDraw(gameTime);

        Graphics.DrawFilledRectangle(0, 0, Graphics.Width, Graphics.Height, new Color(0, 0, 0, 0.5f));

        const int windowWidth = 145;
        const int windowHeight = 100;

        var windowX = (Graphics.Width - windowWidth) / 2;
        var windowY = (Graphics.Height - windowWidth) / 2;

        if (!IsHidden)
        {
            Graphics.DrawFilledRectangle(windowX, windowY, windowWidth, windowHeight, Color.White);
            Graphics.DrawRectangle(windowX, windowY, windowWidth, windowHeight, Color.Black);
            Graphics.DrawTextWithWordWrap("Font", windowX + 4, windowY + 4, windowWidth - 8, WinOrLoseText + "  R - Retry    Q - Quit", Color.Black);
        }

    }

    public override void ActiveInput(GameTime gameTime)
    {
        if (Keyboard.PressedKey(Keys.Q))
        {
            GSM.Exit();
        }
        else if (Keyboard.PressedKey(Keys.R))
        {
            GSM.ChangeState(PreviousState);
        }
        else if (Keyboard.PressedKey(Keys.H))
        {
            IsHidden = IsHidden != true;
        }
    }

    public override void AlwaysUpdate(GameTime gameTime)
    {
        PreviousState.AlwaysUpdate(gameTime);
    }

}

public sealed record EndGameMenuConfig(GameState PreviousState, string WinOrLose);