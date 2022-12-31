using Autofac;
using BenMakesGames.PlayPlayMini;
using BenMakesGames.PlayPlayMini.Model;
using ppm_foxes_and_chickens.GameStates;

// TODO: any pre-req setup, ex:
/*
 * var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
 * var appDataGameDirectory = @$"{appData}{Path.DirectorySeparatorChar}ppm_foxes_and_chickens";
 * 
 * Directory.CreateDirectory(appDataGameDirectory);
 */

var gsmBuilder = new GameStateManagerBuilder();

gsmBuilder
    .SetWindowSize(1920 / 4, 1920 / 4, 2)
    .SetInitialGameState<Startup>()

    // TODO: set a better window title
    .SetWindowTitle("ppm_foxes_and_chickens")

    // TODO: add any resources needed (refer to PlayPlayMini documentation for more info)
    .AddAssets(new IAsset[]
    {
        new FontMeta("Font","Font",11,23),
        new FontMeta("Test","font-test",33,14),
        // new PictureMeta(...)
        new SpriteSheetMeta("Fox","fox",32,32),
        new SpriteSheetMeta("Chicken","chicken",32,32),
        new SpriteSheetMeta("Cell","cell",64,64),
        // new SongMeta(...)
        // new SoundEffectMeta(...)
    })


    // TODO: any additional service registration (refer to PlayPlayMini and/or Autofac documentation for more info)
    .AddServices(s =>
    {

    })
;

gsmBuilder.Run();