using BenMakesGames.PlayPlayMini;
using BenMakesGames.PlayPlayMini.Services;
using ppm_foxes_and_chickens.Models;
using ppm_foxes_and_chickens.Services;

namespace ppm_foxes_and_chickens.GameStates;

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
    private List<Cell> QueueCells { get; set; }
    private List<Fox> Foxes { get; set; }
    private List<Chicken> Chickens { get; set; }

    private readonly Random Random = new();

    private const int GRID_DIM = 7;
    private readonly int GRID_MARGIN;
    private readonly int CELL_SIZE = Cell.SIZE;

    private int Wins { get; set; }
    private int Loses { get; set; }

    bool isCompTurn;
    bool isMoving;

    public Playing(GraphicsManager graphics, GameStateManager gsm, CellFactory cellFactory, MouseManager mouse, FoxFactory foxFactory, ChickenFactory chickenFactory)
    {
        Graphics = graphics;
        GSM = gsm;
        Mouse = mouse;
        Mouse.UseSystemCursor();

        CellFactory = cellFactory;
        FoxFactory = foxFactory;
        ChickenFactory = chickenFactory;

        Cells = new Cell[GRID_DIM][];
        TargetCells = new List<Cell>(9);
        CanMoveToCells = new List<Cell>(4);
        QueueCells = new();
        Foxes = new List<Fox>(2);
        Chickens = new List<Chicken>(20);
        isMoving = false;
        isCompTurn = false;

        GRID_MARGIN = (Graphics.Width - CELL_SIZE * 7) / 2;
        if (GRID_MARGIN < 0) GRID_MARGIN = 0;

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

                var cellX = GRID_MARGIN + (j * CELL_SIZE);
                var cellY = GRID_MARGIN + (i * CELL_SIZE);

                Cells[i][j] = CellFactory.CreateCell
                (
                    new Vector2(cellX, cellY),
                    celltype,
                    null
                );

                if (celltype == CellType.Target) TargetCells.Add(Cells[i][j]);
            }
        }
    }

    public void RestartGame()
    {
        Foxes = new(2);
        Chickens = new(20);
        QueueCells = new();

        // Скрытые клетки.
        var IdxsForHidden = new int[] { 0, 1, 5, 6 };

        for (int i = 0; i < GRID_DIM; i++)
        {
            for (int j = 0; j < GRID_DIM; j++)
            {
                // Скрытые клетки.
                if (IdxsForHidden.Contains(i) && IdxsForHidden.Contains(j)) continue;

                Animal? animal;
                var cellX = GRID_MARGIN + (j * CELL_SIZE);
                var cellY = GRID_MARGIN + (i * CELL_SIZE);

                if (i == 2 & (j == 2 || j == 4))
                {
                    animal = FoxFactory.CreateFox(new Vector2(cellX, cellY), (i, j));
                    Foxes.Add((Fox)animal);
                }
                else if (i >= 3)
                {
                    animal = ChickenFactory.CreateChicken(new Vector2(cellX, cellY), (i, j));
                    Chickens.Add((Chicken)animal);
                }
                else
                    animal = null;

                Cells[i][j].Animal = animal;
            }
        }
    }

    public override void ActiveInput(GameTime gameTime)
    {
        if (isMoving || isCompTurn) return;

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

                    QueueCells = new();

                    var animal = cell.Animal;

                    // Если не выбрана: выбирает если только Курица
                    if (SelectedCell is null)
                    {
                        if (animal is not Chicken) return;
                        SelectAt(i, j);
                    }
                    // Если выбрана:
                    else
                    {
                        // Доступные ходы
                        if (CanMoveToCells.Contains(cell))
                        {
                            var selectedAnimal = SelectedCell.Animal;
                            if (selectedAnimal is not null)
                            {
                                var newPos = new Vector2(cell.Position.X, cell.Position.Y);

                                if (SelectedCell.Animal is not null)
                                    MoveAnimalTo(SelectedCell.Animal, (i, j), newPos);

                                isCompTurn = true;
                            }
                            CancelSelection();
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

        // Вертикаль: Только вверх
        var row = i - 1;

        if (row >= 0 && row < GRID_DIM && row != i)
        {
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
        MovingLogic();
        CompLogic();

        if (Chickens.Count < 9)
        {
            GSM.ChangeState<EndGameMenu, EndGameMenuConfig>(new(this, "You lose :("));
        }

        foreach (var cell in TargetCells)
        {
            if (cell.Animal is not Chicken) return;
        }

        GSM.ChangeState<EndGameMenu, EndGameMenuConfig>(new(this, "You win! :)"));
    }

    public void CompLogic()
    {
        if (!isCompTurn) return;

        foreach (var Fox in Foxes)
        {
            MakeQueueFor(Fox, new Queue<(int, int)>());
        }

        var (fox, foxQueue) = Foxes[0].Queue.Count > Foxes[1].Queue.Count ? (Foxes[0], Foxes[0].Queue) : (Foxes[1], Foxes[1].Queue);

        if (foxQueue.Count == 0)
        {
            // Передвижение.
            var randIdx = Random.Next(2);
            var fox1 = Foxes[randIdx];
            var fox2 = fox1 == Foxes[0] ? Foxes[1] : Foxes[0];

            var (i_1, j_1) = fox1.Index;
            var (i_2, j_2) = fox2.Index;

            // Вверх ⬆️
            if (i_1 > 0 && !IsOccupied(i_1 - 1, j_1))
            {
                MoveAnimalTo(fox1, (i_1 - 1, j_1), Cells[i_1 - 1][j_1].Position);
            }
            else if (i_2 > 0 && !IsOccupied(i_2 - 1, j_2))
            {
                MoveAnimalTo(fox2, (i_2 - 1, j_2), Cells[i_2 - 1][j_2].Position);
            }
            // В сторону ⬅️➡️
            else if (fox1.MovePreference == MovePreference.Left && !IsOccupied(i_1, j_1 - 1))
            {
                MoveAnimalTo(fox1, (i_1, j_1 - 1), Cells[i_1][j_1 - 1].Position);
            }
            else if (fox1.MovePreference == MovePreference.Right && !IsOccupied(i_1, j_1 + 1))
            {
                MoveAnimalTo(fox1, (i_1, j_1 + 1), Cells[i_1][j_1 + 1].Position);
            }
            else if (fox2.MovePreference == MovePreference.Left && !IsOccupied(i_2, j_2 - 1))
            {
                MoveAnimalTo(fox2, (i_2, j_2 - 1), Cells[i_2][j_2 - 1].Position);
            }
            else if (fox2.MovePreference == MovePreference.Right && !IsOccupied(i_2, j_2 + 1))
            {
                MoveAnimalTo(fox2, (i_2, j_2 + 1), Cells[i_2][j_2 + 1].Position);
            }
            else if (fox1.MovePreference == MovePreference.None && (!IsOccupied(i_1, j_1 - 1) || !IsOccupied(i_1, j_1 + 1)))
            {
                var rand = Random.Next(2);
                if (rand == 0) rand = -1;

                if (!IsOccupied(i_1, j_1 + rand)) MoveAnimalTo(fox1, (i_1, j_1 + rand), Cells[i_1][j_1 + rand].Position);
                else MoveAnimalTo(fox1, (i_1, j_1 - rand), Cells[i_1][j_1 - rand].Position);
            }
            else if (fox2.MovePreference == MovePreference.None && (!IsOccupied(i_2, j_2 - 1) || !IsOccupied(i_2, j_2 + 1)))
            {
                var rand = Random.Next(2);
                if (rand == 0) rand = -1;

                if (!IsOccupied(i_2, j_2 + rand)) MoveAnimalTo(fox2, (i_2, j_2 + rand), Cells[i_2][j_2 + rand].Position);
                else MoveAnimalTo(fox2, (i_2, j_2 - rand), Cells[i_2][j_2 - rand].Position);
            }
            // Вниз ⬇️
            else if (i_1 < GRID_DIM && !IsOccupied(i_1 + 1, j_1))
            {
                MoveAnimalTo(fox1, (i_1 + 1, j_1), Cells[i_1 + 1][j_1].Position);
            }
            else if (i_2 < GRID_DIM && !IsOccupied(i_2 + 1, j_2))
            {
                MoveAnimalTo(fox2, (i_2 + 1, j_2), Cells[i_2 + 1][j_2].Position);
            }
        }
        else
        {
            var length = foxQueue.Count;
            QueueCells.Add(Cells[fox.Index.Item1][fox.Index.Item2]);
            for (int k = 0; k < length; k += 2)
            {
                // 1. Удаляем Chicken
                var (i, j) = foxQueue.Dequeue();
                Chickens.Remove(Chickens.Single(c => c.Index == (i, j)));
                Cells[i][j].Animal = null;

                // 2. Свободная клетка: Рисуем цифры
                (i, j) = foxQueue.Dequeue();
                QueueCells.Add(Cells[i][j]);

                // Передвигаем Fox
                if (foxQueue.Count == 0) MoveAnimalTo(fox, (i, j), Cells[i][j].Position);
            }
            // Очищаем очереди
            foreach (var Fox in Foxes)
            {
                Fox.Queue = new Queue<(int, int)>(0);
            }
        }

        isCompTurn = false;
    }

    public void MakeQueueFor(Fox fox, Queue<(int, int)> tempQueue)
    {
        var (i, j) = fox.Index;

        for (int k = -1; k <= 1; k++)
        {
            if (k == 0) continue;
            // Горизонталь
            if (!(j + 2 * k < 0 || j + 2 * k >= GRID_DIM || Cells[i][j + k] is null || Cells[i][j + 2 * k] is null))
                if (!tempQueue.Contains((i, j + k)) && Cells[i][j + k].Animal is Chicken && Cells[i][j + 2 * k].Animal is null)
                {
                    tempQueue.Enqueue((i, j + k));
                    tempQueue.Enqueue((i, j + 2 * k));
                    fox.Index = (i, j + 2 * k);
                    var chicken = Cells[i][j + k].Animal;
                    Cells[i][j + k].Animal = null;
                    MakeQueueFor(fox, new(tempQueue));
                    Cells[i][j + k].Animal = chicken;
                    fox.Index = (i, j);
                    tempQueue = new();
                }
            // Вертикаль
            if (i + 2 * k < 0 || i + 2 * k >= GRID_DIM || Cells[i + k][j] is null || Cells[i + 2 * k][j] is null) continue;
            if (!tempQueue.Contains((i + k, j)) && Cells[i + k][j].Animal is Chicken && Cells[i + 2 * k][j].Animal is null)
            {
                tempQueue.Enqueue((i + k, j));
                tempQueue.Enqueue((i + 2 * k, j));
                fox.Index = (i + 2 * k, j);
                var chicken = Cells[i + k][j].Animal;
                Cells[i + k][j].Animal = null;
                MakeQueueFor(fox, new(tempQueue));
                Cells[i + k][j].Animal = chicken;
                fox.Index = (i, j);
                tempQueue = new();
            }
        }

        if (tempQueue.Count > fox.Queue.Count) fox.Queue = new(tempQueue);

    }

    public bool IsOccupied((int, int) index)
    {
        var (i, j) = index;
        if (Cells[i][j] is not null && Cells[i][j].Animal is not null) return true;

        return false;
    }

    public bool IsOccupied(int i, int j)
    {
        if (Cells[i][j] is null || Cells[i][j].Animal is not null) return true;

        return false;
    }

    public void MoveAnimalTo(Animal animal, (int, int) index, Vector2 position)
    {
        var lastCell = GetCellByAnimal(animal);
        (animal.Position, animal.Index) = (position, index);
        lastCell.Animal = null;

        var newCell = GetCellByAnimal(animal);
        newCell.Animal = animal;
    }

    public Cell GetCellByAnimal(Animal animal)
    {
        var (i, j) = animal.Index;
        return Cells[i][j];
    }

    public void MovingLogic()
    {
        if (isMoving)
        {

            // ? Логика отрисовки передвижения
        }
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


        foreach (var cell in TargetCells)
        {
            if (cell.Animal is Chicken) cell.Type = CellType.Reached;
            else if (cell.Type == CellType.Reached) cell.Type = CellType.Target;
        }

        for (int i = 0; i < GRID_DIM; i++)
        {
            for (int j = 0; j < GRID_DIM; j++)
            {
                DrawCell(Cells[i][j]);
            }
        }

        var CH = Graphics.Fonts["Font"].CharacterHeight;
        var CW = Graphics.Fonts["Font"].CharacterWidth;
        for (int i = 0; i < QueueCells.Count - 1; i++)
        {
            var cell = QueueCells[i];
            Graphics.DrawText("Font", (int)cell.Position.X + (CELL_SIZE - CW) / 2, (int)cell.Position.Y + (CELL_SIZE - CH) / 2, i.ToString(), Color.Black);
        }

        foreach (var fox in Foxes)
        {
            DrawFox(fox);
        }

        foreach (var chick in Chickens)
        {
            DrawChicken(chick);
        }

        Graphics.DrawText("Font", 4, 4, $"Wins:  {Wins}", Color.Black);
        Graphics.DrawText("Font", 4, 27, $"Loses: {Loses}", Color.Black);
    }

    public override void Enter()
    {
        RestartGame();
    }

    public override void Leave()
    {
        if (Chickens.Count < 9) Loses++;
        else Wins++;
    }

    public void DrawCell(Cell cell)
    {
        if (cell is null) return;

        Color color;
        if (cell.Status == CellStatus.Default)
            color = cell.Type switch
            {
                CellType.Target => Color.LightGreen,
                CellType.Reached => Color.Gold,
                _ => Color.White,
            };
        else
            color = cell.Status switch
            {
                CellStatus.Selected => Color.LightSteelBlue,
                CellStatus.CanMoveTo => Color.LightCyan,
                _ => Color.White,
            };

        Graphics.DrawSprite("Cell", (int)cell.Position.X, (int)cell.Position.Y, 0, color);
    }

    public void DrawFox(Fox fox)
    {
        Graphics.DrawSpriteRotatedAndScaled("Fox", (int)fox.Position.X, (int)fox.Position.Y - Animal.POSITION_OFFSET / 2, 0, 0f, 2f, Color.White);
    }

    public void DrawChicken(Chicken chicken)
    {
        Graphics.DrawSprite("Chicken", (int)chicken.Position.X - Animal.POSITION_OFFSET / 2, (int)chicken.Position.Y - Animal.POSITION_OFFSET / 2, 0);
    }
}