using System;
using System.Collections.Generic;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace FallingStarsGame
{
    public partial class MainForm : Form
    {
        private Timer gameTimer; //основной таймер
        private Timer speedTimer; //таймер увелечения скорости падения
        private Timer freezeTimer; //таймер заморозки

        private List<FallingObject> stars = new List<FallingObject>(); //список звезд
        private List<FallingObject> iceBlocks = new List<FallingObject>(); //список льдинок

        private Random rand = new Random();

        private int score = 0; //счёт
        private int missedStars = 0; //счетчик пропущенных звезд
        private float starSpeed = 2f; //скорость звезды
        private bool isFrozen = false; //заморозка

        private SoundPlayer starHitSound; //звук звезды
        private SoundPlayer iceHitSound; //звук льдинки
        private SoundPlayer missSound; //звук промаха

        private bool isMenuVisible = true;
        private bool isSoundOn = true; //вкл/выкл звука
        private float difficultyMultiplier = 1f;

        private const int maxMissedStars = 3;

        private Rectangle easyRect = new Rectangle(100, 200, 250, 50); //кнока на легкого уровня сложности
        private Rectangle mediumRect = new Rectangle(100, 270, 250, 50); //для среднего
        private Rectangle hardRect = new Rectangle(100, 340, 250, 50); //для сложного
        private Rectangle soundRect = new Rectangle(100, 420, 250, 50); //для звука

        enum ObjectType
        {
            Star,
            Ice
        }

        class FallingObject
        {
            public PointF Position;
            public float Speed;
            public ObjectType Type;
            public float Size;
        }

        public MainForm()
        {
            InitializeComponent();
            
            //запрет на изменения окна
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            //инициализация проигрывателей
            starHitSound = new SoundPlayer("sounds/star_hit.wav");
            iceHitSound = new SoundPlayer("sounds/ice_hit.wav");
            missSound = new SoundPlayer("sounds/miss.wav");

            //загружаем звуки заранее
            starHitSound.LoadAsync();
            iceHitSound.LoadAsync();
            missSound.LoadAsync();

            this.DoubleBuffered = true;
            this.Width = 450;
            this.Height = 650;
            this.Text = "Falling Stars";

            //настройка таймера игры
            gameTimer = new Timer();
            gameTimer.Interval = 30;
            gameTimer.Tick += GameTimer_Tick;

            //таймер увеличения скорости
            speedTimer = new Timer();
            speedTimer.Interval = 5000; //каждые 5с
            speedTimer.Tick += SpeedTimer_Tick;

            //таймер заморозки
            freezeTimer = new Timer();
            freezeTimer.Interval = 3000; //3с
            freezeTimer.Tick += FreezeTimer_Tick;

            this.Paint += MainForm_Paint;
            this.MouseDown += MainForm_MouseDown;

            StartGame();
        }

        private void StartGame()
        {
            score = 0;
            missedStars = 0;
            starSpeed = 2f * difficultyMultiplier;
            stars.Clear();
            iceBlocks.Clear();
            isFrozen = false;

            gameTimer.Start();
            speedTimer.Start();
        }

        private void DrawMenu(Graphics graph)
        {
            graph.Clear(Color.DarkSlateBlue);
            var font = new Font("Roboto", 22, FontStyle.Bold);
            var fontSmall = new Font("Roboto", 14, FontStyle.Regular);
            var title = "Выберите сложность";
            var titleSize = graph.MeasureString(title, font);
            graph.DrawString(title, font, Brushes.White, (this.ClientSize.Width - titleSize.Width) / 2, 100);

            //кнопки уровней
            DrawMenuButton(graph, easyRect, "Лёгко (0.5x)");
            DrawMenuButton(graph, mediumRect, "Нормально (1x)");
            DrawMenuButton(graph, hardRect, "Сложно (2x)");

            //кнопка звука
            var soundText = isSoundOn ? "Звук: ВКЛ" : "Звук: ВЫКЛ";
            DrawMenuButton(graph, soundRect, soundText);
        }

        private void DrawMenuButton(Graphics graph, Rectangle rect, string text)
        {
            graph.FillRectangle(Brushes.MediumSlateBlue, rect);
            graph.DrawRectangle(Pens.White, rect);
            var font = new Font("Roboto", 16, FontStyle.Bold);
            var size = graph.MeasureString(text, font);
            graph.DrawString(text, font, Brushes.White, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
        }


        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (isMenuVisible)
                return;

            if (!isFrozen)
            {
                //добавляем новые звезды и льдинки
                if (rand.NextDouble() < 0.07) //7% шанс добавить звезду
                {
                    stars.Add(new FallingObject
                    {
                        Position = new PointF(rand.Next(20, this.ClientSize.Width - 20), -20),
                        Speed = starSpeed,
                        Type = ObjectType.Star,
                        Size = 20
                    });
                }
                if (rand.NextDouble() < 0.02) //2% шанс добавить льдинку
                {
                    iceBlocks.Add(new FallingObject
                    {
                        Position = new PointF(rand.Next(20, this.ClientSize.Width - 20), -20),
                        Speed = starSpeed,
                        Type = ObjectType.Ice,
                        Size = 20
                    });
                }

                //обновляем позиции звёзд
                for (int i = stars.Count - 1; i >= 0; i--)
                {
                    stars[i].Position = new PointF(stars[i].Position.X, stars[i].Position.Y + stars[i].Speed);
                    if (stars[i].Position.Y > this.ClientSize.Height)
                    {
                        stars.RemoveAt(i);
                        missedStars++;
                        if (missedStars >= maxMissedStars)
                        {
                            GameOver();
                            return;
                        }
                    }
                }

                //обновляем позиции льдинок
                for (int i = iceBlocks.Count - 1; i >= 0; i--)
                {
                    iceBlocks[i].Position = new PointF(iceBlocks[i].Position.X, iceBlocks[i].Position.Y + iceBlocks[i].Speed);
                    if (iceBlocks[i].Position.Y > this.ClientSize.Height)
                        iceBlocks.RemoveAt(i);
                }
            }

            this.Invalidate();
        }

        //увеличиваем скорость
        private void SpeedTimer_Tick(object sender, EventArgs e)
        {
            if (!isFrozen)
                starSpeed += 0.5f;
        }

        //снимаем заморозку
        private void FreezeTimer_Tick(object sender, EventArgs e)
        {
            isFrozen = false;
            freezeTimer.Stop();
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics graph = e.Graphics;

            if (isMenuVisible)
            {
                DrawMenu(graph);
                return;
            }

            //звёзды
            foreach (var star in stars)
                DrawStar(graph, star.Position, star.Size, Brushes.Gold);

            //льдинки
            foreach (var ice in iceBlocks)
                DrawIce(graph, ice.Position, ice.Size, Brushes.LightBlue);

            //отображение счёта
            var scoreText = $"Score: {score}";
            var font = new Font("Roboto", 14, FontStyle.Bold);
            var sizeText = graph.MeasureString(scoreText, font);
            graph.DrawString(scoreText, font, Brushes.White, this.ClientSize.Width - sizeText.Width - 10, 10);

            //отображение промахов
            var missText = $"Missed: {missedStars}/{maxMissedStars}";
            graph.DrawString(missText, font, Brushes.Red, 10, 10);
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (isMenuVisible)
            {
                if (easyRect.Contains(e.Location))
                {
                    difficultyMultiplier = 0.5f;
                    isMenuVisible = false;
                    StartGame();
                }
                else if (mediumRect.Contains(e.Location))
                {
                    difficultyMultiplier = 1f;
                    isMenuVisible = false;
                    StartGame();
                }
                else if (hardRect.Contains(e.Location))
                {
                    difficultyMultiplier = 2f;
                    isMenuVisible = false;
                    StartGame();
                }
                else if (soundRect.Contains(e.Location))
                {
                    isSoundOn = !isSoundOn;
                    this.Invalidate();
                }
                return;
            }

                //проверка звёзд
                for (int i = stars.Count - 1; i >= 0; i--)
            {
                if (IsPointInCircle(e.Location, stars[i].Position, stars[i].Size / 2))
                {
                    score += 10;
                    stars.RemoveAt(i);
                    if (isSoundOn)
                        starHitSound.Play();
                    return;
                }
            }

            //проверка льдинок
            for (int i = iceBlocks.Count - 1; i >= 0; i--)
            {
                if (IsPointInCircle(e.Location, iceBlocks[i].Position, iceBlocks[i].Size / 2))
                {
                    iceBlocks.RemoveAt(i);
                    if (isSoundOn) 
                        iceHitSound.Play();
                    FreezeTime();
                    return;
                }
            }

            if (isSoundOn)
                missSound.Play();
        }

        private void FreezeTime()
        {
            if (!isFrozen)
            {
                isFrozen = true;
                freezeTimer.Start();
            }
            else
            {
                //продлеваем заморозку, если она уже есть
                freezeTimer.Stop();
                freezeTimer.Start();
            }
        }

        private void GameOver()
        {
            gameTimer.Stop();
            speedTimer.Stop();
            freezeTimer.Stop();

            MessageBox.Show($"Your score: {score}", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            isMenuVisible = true;
            this.Invalidate();
            StartGame();
        }

        private bool IsPointInCircle(Point p, PointF center, float radius)
        {
            var dx = p.X - center.X;
            var dy = p.Y - center.Y;
            return dx * dx + dy * dy <= radius * radius;
        }

        private void DrawStar(Graphics graph, PointF center, float size, Brush brush)
        {
            //фигура звезды
            PointF[] points = new PointF[10];
            var angle = -Math.PI / 2;
            var step = Math.PI / 5;

            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? size / 2 : size / 2 / 2.5f;
                points[i] = new PointF(
                    center.X + (float)(r * Math.Cos(angle)),
                    center.Y + (float)(r * Math.Sin(angle))
                );
                angle += step;
            }

            graph.FillPolygon(brush, points);
            graph.DrawPolygon(Pens.DarkGoldenrod, points);
        }

        private void DrawIce(Graphics graph, PointF center, float size, Brush brush)
        {
            //фигура льдинки
            RectangleF ice = new RectangleF(center.X - size / 2, center.Y - size / 2, size, size);
            Color color = Color.FromArgb(180, ((SolidBrush)brush).Color);
            using (SolidBrush sBrush = new SolidBrush(color))
                graph.FillRectangle(sBrush, ice);
            graph.DrawRectangle(Pens.Blue, ice.X, ice.Y, ice.Width, ice.Height);
        }
    }
}
