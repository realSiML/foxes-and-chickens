using Microsoft.Xna.Framework;
using BenMakesGames.PlayPlayMini;
using BenMakesGames.PlayPlayMini.Services;
using ppm_foxes_and_chickens.Models;
using ppm_foxes_and_chickens.Services;

namespace ppm_foxes_and_chickens;

// sealed classes execute faster than non-sealed, so always seal your game states!
public sealed class Playing : GameState
{
    private GraphicsManager Graphics { get; }
    private MouseManager Mouse { get; }
    private GameStateManager GSM { get; }
    private CellFactory CellFactory { get; }
    private FoxFactory FoxFactory { get; }
    private ChickenFactory ChickenFactory { get; }

    private Cell[][] Cells { get; }
    private Cell? SelectedCell { get; set; }
    private List<Cell> TargetCells { get; set; }
    private List<Cell> CanMoveToCells { get; set; }
    private List<Fox> Foxes { get; }
    private List<Chicken> Chickens { get; }

    const int GRID_DIM = 7;
    const int GRID_MARGIN = 10;
    const int ANIMAL_OFFSET = 32;
    readonly int CELL_SIZE = Cell.SIZE;
    bool isMoving;

    public Playing(GraphicsManager graphics, GameStateManager gsm, CellFactory cellFactory, MouseManager mouse, FoxFactory foxFactory, ChickenFactory chickenFactory)
    {
        Graphics = graphics;
        GSM = gsm;
        Mouse = mouse;
        mouse.UseSystemCursor();

        CellFactory = cellFactory;
        FoxFactory = foxFactory;
        ChickenFactory = chickenFactory;

        Cells = new Cell[GRID_DIM][];
        TargetCells = new List<Cell>(9);
        CanMoveToCells = new List<Cell>(4);
        Foxes = new List<Fox>(2);
        Chickens = new List<Chicken>(20);

        // Скрытые клетки.
        var IdxsForHidden = new int[] { 0, 1, 5, 6 };
        // Выйгрышные клетки.
        var IdxsForTarget_i = new int[] { 0, 1, 2 };
        var IdxsForTarget_j = new int[] { 2, 3, 4 };

        for (int i = 0; i < GRID_DIM; i++)
        {
            Cells[i] = new Cell[GRID_DIM];
            for (int j = 0; j < GRID_DIM; j++)
            {
                // Скрытые клетки.
                if (IdxsForHidden.Contains(i) && IdxsForHidden.Contains(j)) continue;

                // Выйгрышные клетки.
                CellType celltype;
                if (IdxsForTarget_i.Contains(i) && IdxsForTarget_j.Contains(j))
                    celltype = CellType.Target;
                else
                    celltype = CellType.Common;

                Animal? animal;
                var cellX = GRID_MARGIN + (j * CELL_SIZE);
                var cellY = GRID_MARGIN + (i * CELL_SIZE);

                if (i == 2 & (j == 2 || j == 4))
                {
                    animal = FoxFactory.CreateFox(new Vector2(cellX + ANIMAL_OFFSET, cellY + ANIMAL_OFFSET));
                    Foxes.Add((Fox)animal);
                }
                else if (i >= 3)
                {
                    animal = ChickenFactory.CreateChicken(new Vector2(cellX + ANIMAL_OFFSET, cellY + ANIMAL_OFFSET));
                    Chickens.Add((Chicken)animal);
                }
                else
                    animal = null;

                Cells[i][j] = CellFactory.CreateCell
                (
                    new Vector2(cellX, cellY),
                    celltype,
                    animal
                );

                if (celltype == CellType.Target) TargetCells.Add(Cells[i][j]);
            }
        }
    }

    // overriding lifecycle methods is optional; feel free to delete any overrides you're not using.
    // note: you do NOT need to call the `base.` for lifecycle methods. so save some CPU cycles,
    // and don't call them :P

    public override void ActiveInput(GameTime gameTime)
    {
        if (isMoving) return;

        if (Mouse.LeftClicked)
        {
            var MouseRectangle = new Rectangle(Mouse.X, Mouse.Y, 1, 1);

            for (int i = 0; i < GRID_DIM; i++)
            {
                for (int j = 0; j < GRID_DIM; j++)
                {
                    var cell = Cells[i][j];
                    if (cell is null) continue; // "Спрятанная" клетка

                    if (!MouseRectangle.Intersects(cell.Rectangle)) continue;

                    var animal = cell.Animal;

                    // Если не выбрана: выбирает если только Курица
                    if (SelectedCell is null)
                    {
                        if (animal is Fox || animal is null) continue;
                        SelectAt(i, j);
                    }
                    // Если выбрана:
                    else
                    {
                        // Доступные ходы
                        if (CanMoveToCells.Contains(cell))
                        {
                            if (SelectedCell.Animal is not null)
                            {
                                // if (SelectedCell.Status == CellStatus.Reached)
                                //     SelectedCell.Status = CellStatus.Default;

                                var newPos = new Vector2(cell.Position.X + ANIMAL_OFFSET, cell.Position.Y + ANIMAL_OFFSET);
                                SelectedCell.Animal.Position = newPos;

                                (SelectedCell.Animal, cell.Animal) = (cell.Animal, SelectedCell.Animal);


                            }
                            CancelSelection();
                            if (TargetCells.Contains(cell)) cell.Status = CellStatus.Reached;
                        }
                        // Другие Курицы
                        else if (animal is Chicken)
                        {
                            CancelSelection();
                            if (SelectedCell != cell)
                                SelectAt(i, j);
                        }
                        // Остальное (для сброса)
                        else
                            CancelSelection();
                    }
                }
            }
        }
    }

    private void SelectAt(int i, int j)
    {
        SelectedCell = Cells[i][j];
        Cells[i][j].Status = CellStatus.Selected;
        // Перебираем соседние клетки
        for (int k = -1; k <= 1; k++)
        {
            // Вертикаль
            var row = i + k;

            if (row < 0 || row >= GRID_DIM || row == i) continue;

            var c = Cells[row][j];
            if (c is not null && c.Animal is null)
            {
                c.Status = CellStatus.CanMoveTo;
                CanMoveToCells.Add(c);
            }
        }

        for (int k = -1; k <= 1; k++)
        {

            // Горизонталь
            var col = j + k;

            if (col < 0 || col >= GRID_DIM || col == j) continue;

            var c = Cells[i][col];
            if (c is not null && c.Animal is null)
            {
                c.Status = CellStatus.CanMoveTo;
                CanMoveToCells.Add(c);
            }
        }
    }

    private void CancelSelection()
    {
        if (SelectedCell is not null)
        {
            SelectedCell.Status = CellStatus.Default;
            SelectedCell = null;
        }

        for (int i = CanMoveToCells.Count - 1; i >= 0; i--)
        {
            CanMoveToCells[i].Status = CellStatus.Default;
            CanMoveToCells.RemoveAt(i);
        }
    }

    public override void ActiveUpdate(GameTime gameTime)
    {
        if (!isMoving) return;

        // ? Логика передвижения
    }

    public override void AlwaysUpdate(GameTime gameTime)
    {
    }

    public override void ActiveDraw(GameTime gameTime)
    {

        Mouse.ActiveDraw(gameTime);
    }

    public override void AlwaysDraw(GameTime gameTime)
    {
        // TODO: draw game scene (refer to PlayPlayMini documentation for more info)
        Graphics.Clear(Color.LightSlateGray);


        for (int i = 0; i < GRID_DIM; i++)
        {
            for (int j = 0; j < GRID_DIM; j++)
            {
                DrawCell(Cells[i][j]);
            }
        }

        foreach (var fox in Foxes)
        {
            DrawFox(fox);
        }

        foreach (var chick in Chickens)
        {
            DrawChicken(chick);
        }
    }

    public void DrawCell(Cell cell)
    {
        if (cell is null) return;

        Color color;
        if (cell.Status == CellStatus.Default)
            color = cell.Type switch
            {
                CellType.Target => Color.LightGreen,
                _ => Color.White,
            };
        else
            color = cell.Status switch
            {
                CellStatus.Selected => Color.LightSteelBlue,
                CellStatus.CanMoveTo => Color.LightCyan,
                CellStatus.Reached => Color.Gold,
                _ => Color.White,
            };

        Graphics.DrawSprite("Cell", (int)cell.Position.X, (int)cell.Position.Y, 0, color);
    }

    public void DrawFox(Fox fox)
    {
        Graphics.DrawSpriteRotatedAndScaled("Fox", (int)fox.Position.X, (int)fox.Position.Y - ANIMAL_OFFSET / 2, 0, 0f, 2f, Color.White);
    }

    public void DrawChicken(Chicken chicken)
    {
        Graphics.DrawSprite("Chicken", (int)chicken.Position.X - ANIMAL_OFFSET / 2, (int)chicken.Position.Y - ANIMAL_OFFSET / 2, 0);
    }


}