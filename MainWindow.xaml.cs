using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Space_Invaders
{
    public partial class MainWindow : Window
    {
        bool goLeft, goRight;
        List<Rectangle> itemsToRemove = new List<Rectangle>();
        int enemyImage = 0;
        int bulletTime = 0;
        int bulletTimeLimit = 100;
        int totalEnemies = 0;
        bool gameOver = false;

        DispatcherTimer gameTimer = new DispatcherTimer();
        ImageBrush playerSkin = new ImageBrush();

        // 🔹 Movement Speeds
        int playerSpeed = 30;
        int enemySpeed = 60;
        int enemyBulletSpeed = 60;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Console.WriteLine("MainWindow constructor called.");

                gameTimer.Tick += GameLoop;
                gameTimer.Interval = TimeSpan.FromMilliseconds(20);

                try
                {
                    playerSkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/player.png"));
                    Player.Fill = playerSkin;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load player image: {ex.Message}", "Resource Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                myCanvas.Focus();
                MakeEnemies(100); // start with 100 enemies
                gameTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in MainWindow constructor: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window loaded successfully.");
        }

        private void RestartGame()
        {
            gameOver = false;
            goLeft = goRight = false;

            // Reset speeds
            playerSpeed = 25;
            enemySpeed = 50;
            enemyBulletSpeed = 50;

            totalEnemies = 0;

            // ✅ Remove only game elements (Player, enemies, bullets), not labels
            foreach (var item in myCanvas.Children.OfType<UIElement>().ToList())
            {
                if (item is Rectangle rect && (string)rect.Tag == "enemy" ||
                    item is Rectangle rect2 && (string)rect2.Tag == "bullet" ||
                    item is Rectangle rect3 && (string)rect3.Tag == "enemyBullet" ||
                    item == Player)
                {
                    myCanvas.Children.Remove(item);
                }
            }

            // Re-add player safely
            if (!myCanvas.Children.Contains(Player))
            {
                myCanvas.Children.Add(Player);
            }
            Canvas.SetLeft(Player, myCanvas.ActualWidth / 2 - Player.Width / 2);
            Canvas.SetTop(Player, myCanvas.ActualHeight - Player.Height - 20);

            MakeEnemies(100);

            // ✅ Reset label content
            EnemiesLeft.Content = "Enemies Left: " + totalEnemies;

            gameTimer.Start();
        }


        private void GameLoop(object sender, EventArgs e)
        {
            Rect playerHitBox = new Rect(Canvas.GetLeft(Player), Canvas.GetTop(Player), Player.Width, Player.Height);
            EnemiesLeft.Content = "Enemies Left: " + totalEnemies;

            // 🔹 Player movement
            if (goLeft && Canvas.GetLeft(Player) > 0)
            {
                Canvas.SetLeft(Player, Canvas.GetLeft(Player) - playerSpeed);
            }
            if (goRight && Canvas.GetLeft(Player) + Player.Width < myCanvas.ActualWidth)
            {
                Canvas.SetLeft(Player, Canvas.GetLeft(Player) + playerSpeed);
            }

            // Enemy bullet timer
            bulletTime += 5;
            if (bulletTime >= bulletTimeLimit)
            {
                var enemies = myCanvas.Children.OfType<Rectangle>().Where(x => (string)x.Tag == "enemy").ToList();
                if (enemies.Any())
                {
                    var randomEnemy = enemies[new Random().Next(enemies.Count)];
                    EnemyBulletMaker(Canvas.GetLeft(randomEnemy) + randomEnemy.Width / 2, Canvas.GetTop(randomEnemy) + randomEnemy.Height);
                    bulletTime = 0;
                }
            }

            itemsToRemove.Clear();
            foreach (var x in myCanvas.Children.OfType<Rectangle>().ToList())
            {
                if ((string)x.Tag == "bullet")
                {
                    Canvas.SetTop(x, Canvas.GetTop(x) - 5);
                    if (Canvas.GetTop(x) < 10) itemsToRemove.Add(x);

                    Rect bullet = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    foreach (var y in myCanvas.Children.OfType<Rectangle>().Where(y => (string)y.Tag == "enemy"))
                    {
                        Rect enemy = new Rect(Canvas.GetLeft(y), Canvas.GetTop(y), y.Width, y.Height);
                        if (bullet.IntersectsWith(enemy))
                        {
                            itemsToRemove.Add(x);
                            itemsToRemove.Add(y);
                            totalEnemies--;
                            if (totalEnemies <= 0) showGameOver("You Win! All enemies have been defeated!");
                        }
                    }
                }

                if ((string)x.Tag == "enemy")
                {
                    Canvas.SetLeft(x, Canvas.GetLeft(x) + enemySpeed);
                    if (Canvas.GetLeft(x) > myCanvas.ActualWidth - x.Width)
                    {
                        Canvas.SetLeft(x, 0);
                        Canvas.SetTop(x, Canvas.GetTop(x) + x.Height + 10);
                    }

                    Rect enemyHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    if (playerHitBox.IntersectsWith(enemyHitBox)) showGameOver("Game Over! You have been defeated!");
                }

                if ((string)x.Tag == "enemyBullet")
                {
                    Canvas.SetTop(x, Canvas.GetTop(x) + enemyBulletSpeed);
                    if (Canvas.GetTop(x) > myCanvas.ActualHeight) itemsToRemove.Add(x);

                    Rect enemyBulletHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    if (enemyBulletHitBox.IntersectsWith(playerHitBox))
                    {
                        itemsToRemove.Add(x);
                        myCanvas.Children.Remove(Player);
                        showGameOver("Game Over! You have been defeated!");
                    }
                }
            }

            foreach (Rectangle i in itemsToRemove) myCanvas.Children.Remove(i);

            // 🔹 Difficulty scaling
            if (totalEnemies < 50)
            {
                enemySpeed = 80;
                enemyBulletSpeed = 80;
                playerSpeed = 40;
            }
            else
            {
                enemySpeed = 50;
                enemyBulletSpeed = 50;
                playerSpeed = 25;
            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left) goLeft = true;
            if (e.Key == Key.Right) goRight = true;
        }

        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left) goLeft = false;
            if (e.Key == Key.Right) goRight = false;

            if (e.Key == Key.Space)
            {
                Rectangle newBullet = new Rectangle
                {
                    Tag = "bullet",
                    Height = 20,
                    Width = 5,
                    Fill = Brushes.White,
                    Stroke = Brushes.Red
                };
                Canvas.SetLeft(newBullet, Canvas.GetLeft(Player) + Player.Width / 2);
                Canvas.SetTop(newBullet, Canvas.GetTop(Player) - newBullet.Height);
                myCanvas.Children.Add(newBullet);
            }

            if (e.Key == Key.Enter && gameOver) RestartGame();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            myCanvas.Width = e.NewSize.Width;
            myCanvas.Height = e.NewSize.Height;
            Console.WriteLine($"Window resized: {e.NewSize.Width} x {e.NewSize.Height}");
        }

        private void EnemyBulletMaker(double x, double y)
        {
            Rectangle enemyBullet = new Rectangle
            {
                Tag = "enemyBullet",
                Height = 40,
                Width = 15,
                Fill = Brushes.Yellow,
                Stroke = Brushes.Black
            };
            Canvas.SetLeft(enemyBullet, x);
            Canvas.SetTop(enemyBullet, y);
            myCanvas.Children.Add(enemyBullet);
        }

        private void MakeEnemies(int limit)
        {
            int left = 0;
            totalEnemies = limit;
            for (int i = 0; i < limit; i++)
            {
                ImageBrush enemySkin = new ImageBrush();
                Rectangle newEnemy = new Rectangle
                {
                    Tag = "enemy",
                    Height = 50,
                    Width = 50,
                    Fill = enemySkin
                };

                Canvas.SetTop(newEnemy, 30);
                Canvas.SetLeft(newEnemy, left);
                myCanvas.Children.Add(newEnemy);
                left += 60;

                enemyImage++;
                if (enemyImage > 8) enemyImage = 1;

                try
                {
                    switch (enemyImage)
                    {
                        case 1: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader1.gif")); break;
                        case 2: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader2.gif")); break;
                        case 3: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader3.gif")); break;
                        case 4: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader4.gif")); break;
                        case 5: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader5.gif")); break;
                        case 6: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader6.gif")); break;
                        case 7: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader7.gif")); break;
                        case 8: enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader8.gif")); break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load enemy image {enemyImage}: {ex.Message}", "Resource Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void showGameOver(string msg)
        {
            gameOver = true;
            gameTimer.Stop();
            EnemiesLeft.Content = $"Enemies Left: {totalEnemies} {msg} Press Enter to Play Again";
        }
    }
}
