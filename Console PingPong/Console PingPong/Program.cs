using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Console_PingPong
{
    internal class Program
    {
        private static GameProcessor _gameProcessor;
        private static int _tickDelay = 20;

        static void Main(string[] args)
        {
            _gameProcessor = new GameProcessor();
            _gameProcessor.Start();

            while (true)
            {
                GameUpdate();

                Thread.Sleep(_tickDelay);
            }

            #pragma warning disable CS0162 // Обнаружен недостижимый код
            _gameProcessor.Shutdown();
            #pragma warning restore CS0162 // Обнаружен недостижимый код
        }

        static void GameUpdate()
        {
            _gameProcessor.Update();
        }
    }

    class ResourceLoader
    {
        private const string _ballFileName = "Ball";
        private const string _plankFileName = "Plank";

        public Shape GetBallShape()
        {
            return ExtractShapeFromImage(_ballFileName);
        }
        public Shape GetPlankShape()
        {
            return ExtractShapeFromImage(_plankFileName);
        }

        private Shape ExtractShapeFromImage(string fileName)
        {
            char[,] image = LoadImagefromTxt(fileName);
            List<Shape.Point> points = new List<Shape.Point>();
            Vector2 relativeCenter = new Vector2(image.GetLength(0) / 2, image.GetLength(1) / 2);

            for (int i = 0; i < image.GetLength(0); i++)
            {
                for (int j = 0; j < image.GetLength(1); j++)
                {
                    Vector2 position = new Vector2(j - relativeCenter.X, i - relativeCenter.Y);
                    char symbol = image[i, j];

                    points.Add(new Shape.Point(position, symbol));
                }
            }

            Shape shape = new Shape(points.ToArray());
            return shape;
        }

        public char[,] LoadImagefromTxt(string fileName)
        {
            string[] newFile = File.ReadAllLines($"{fileName}.txt");
            char[,] image = new char[newFile.Length, newFile[0].Length];

            for (int i = 0; i < image.GetLength(0); i++)
            {
                for (int j = 0; j < image.GetLength(1); j++)
                {
                    image[i, j] = newFile[i][j];
                }
            }

            return image;
        }
    }

    class GameProcessor
    {
        private ResourceLoader _resourceLoader = new ResourceLoader();
        private List<Entity> _entities = new List<Entity>();
        private Grid _grid;
        private Ball _ball;
        private Plank _leftPlank;
        private Plank _rightPlank;

        private int _leftPlayerScore = 0;
        private int _rightPlayerScore = 0;

        public void Start()
        {
            _grid = new Grid(100, 35, '#');
            _ball = new Ball(new Vector2(_grid.xLeght / 2, _grid.yLeght / 2), _resourceLoader.GetBallShape());
            _leftPlank = new Plank(new Vector2(5, _grid.yLeght / 2), _resourceLoader.GetPlankShape(), _grid.yLeght, 87, 83);
            _rightPlank = new Plank(new Vector2(_grid.xLeght - 5, _grid.yLeght / 2), _resourceLoader.GetPlankShape(), _grid.yLeght, 38, 40);

            _entities.Add(_ball);
            _entities.Add(_leftPlank);
            _entities.Add(_rightPlank);

            Console.SetWindowSize(_grid.xLeght + 1, _grid.yLeght + 1);
            Console.Title = "Console Pong";
        }

        public void Update()
        {
            Console.Clear();

            CheckBallCollisions();

            DrawCenterLine('|');
            DisplayPlayersScore(5, 1);

            foreach (var entity in _entities)
            {
                entity.Update();
                DrawObject(entity);
            }
        }

        public void Shutdown()
        {
            foreach (var entity in _entities)
            {
                entity.Destroy();
            }
        }

        private void DrawObject(Transformable transform)
        {
            foreach (var point in transform.Shape.Points)
            {
                try { Graphic.DrawChar((int)transform.Position.X + (int)point.Position.X, (int)transform.Position.Y + (int)point.Position.Y, point.Symbol); }
                catch { return; }
            }
        }

        private void CheckBallCollisions()
        {
            foreach (var point in _ball.Shape.Points)
            {
                if (point.Symbol == ' ')
                    continue;

                if (_ball.GetGridPosition().X + point.Position.X <= 0 || _ball.GetGridPosition().X + point.Position.X >= _grid.xLeght)
                {
                    Goal();
                    _ball.InvertHorizontalDirection();
                }

                if (_ball.GetGridPosition().Y + point.Position.Y <= 0 || _ball.GetGridPosition().Y + point.Position.Y >= _grid.yLeght)
                    _ball.InvertVerticalDirection();

                foreach (var entity in _entities)
                {
                    if (entity == _ball)
                        continue;

                    Shape entityShape = entity.Shape;

                    foreach (var entityPoint in entityShape.Points)
                    {
                        if (entityPoint.Symbol == ' ')
                            continue;

                        if (new Vector2(_ball.GetGridPosition().X, (int)_ball.GetGridPosition().Y) + point.Position == entity.GetGridPosition() + entityPoint.Position)
                        {
                            _ball.ChangeHorizontalDirection();
                        }
                    }
                }
            }
        }

        private void Goal()
        {
            if(_ball.Position.X > _grid.xLeght / 2)
            {
                _leftPlayerScore += 1;
            }
            else
            {
                _rightPlayerScore += 1;
            }

            Console.Beep();
            _ball.Position = new Vector2(_grid.xLeght / 2, _ball.Position.Y);
        }

        private void DisplayPlayersScore(int xOffset, int yOffset)
        {
            int xRelative = _grid.xLeght / 2;

            Graphic.DrawPaintedString(new Vector2(xRelative - xOffset, yOffset), _leftPlayerScore.ToString(), ConsoleColor.Blue);
            Graphic.DrawPaintedString(new Vector2(xRelative + xOffset, yOffset), _rightPlayerScore.ToString(), ConsoleColor.Blue);
        }

        private void DrawCenterLine(char symbol)
        { 
            int x = _grid.xLeght / 2;

            for (int i = 0; i < _grid.yLeght; i++)
            {
                if((i % 2) == 0)
                {
                    Graphic.DrawChar(x, i, symbol);
                }
            }
        }
    }

    static class Graphic
    {
        public static void DrawPaintedString(Vector2 position, string symbols, ConsoleColor color)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            DrawString(position, symbols);
            Console.ForegroundColor = old;
        }

        public static void DrawPaintedChar(Vector2 position, char symbol, ConsoleColor color)
        {
            DrawPaintedString(position, symbol.ToString(), color);
        }

        public static void DrawString(Vector2 position, string symbols)
        {
            Console.SetCursorPosition((int)position.X, (int)position.Y);
            Console.Write(symbols);
        }

        public static void DrawString(int x, int y, string symbols)
        {
            DrawString(new Vector2(x, y), symbols);
        }

        public static void DrawChar(int x, int y, char symbol)
        {
            DrawString(x, y, symbol.ToString());
        }
    }

    class Transformable
    {
        public Vector2 Position;
        public Shape Shape;

        public Vector2 GetGridPosition()
        {
            return new Vector2((int)Position.X, (int)Position.Y);
        }
    }

    public class Shape
    {
        public Point[] Points;

        public Shape(Point[] points)
        {
            Points = points;
        }

        public struct Point
        {
            public Vector2 Position;
            public char Symbol;

            public Point(Vector2 position, char symbol)
            {
                Position = position;
                Symbol = symbol;
            }
        }

        public static Shape BuildBaseShape(char symbol)
        {
            return new Shape(new Point[] { new Point(Vector2.Zero, symbol) });
        }
    }

    class Entity : Transformable
    {
        public Action Updated;
        public Action Destroyed;

        private Vector2 _spawnPosition;

        public Entity(Vector2 spawnPosition, Shape shape)
        {
            _spawnPosition = spawnPosition;
            Position = _spawnPosition;
            Shape = shape;
        }

        public void Update()
        {
            Updated?.Invoke();
        }

        public void Destroy()
        {
            Destroyed?.Invoke();
        }
    }

    class Ball : Entity
    {
        private Vector2 _direction;

        public Ball(Vector2 spawnPosition, Shape shape) : base(spawnPosition, shape)
        {
            Updated += Move;
            Destroyed += OnDestroy;

            SetRandomDirection();
        }

        #region DirectionFuctions
        public void InvertHorizontalDirection()
        {
            _direction = new Vector2(_direction.X * -1, _direction.Y);
        }

        public void InvertVerticalDirection()
        {
            _direction = new Vector2(_direction.X, _direction.Y * -1);
        }

        public void ChangeHorizontalDirection()
        {
            float x = _direction.X * -1;
            float y = 0;

            while (y >= -0.4f && y <= 0.4f)
            {
                y = FloatRandom.GetRandomNumber(-1, 1);
            }

            _direction = new Vector2(x, y);
        }

        public void ChangeVerticalDirection()
        {
            Random rand = new Random();
            float x = 1;

            while (x == 0)
            {
                x = rand.Next(-1, 1);
            }

            float y = _direction.Y * -1;
            _direction = new Vector2(x, y);
        }

        private void SetRandomDirection()
        {
            Random rand = new Random();
            float x = 1;
            float y = 0;

            while (x == 0)
            {
                x = rand.Next(-1, 1);
            }

            y = FloatRandom.GetRandomNumber(-1, 1);

            _direction = new Vector2(x, y);  
        }
        #endregion

        private void Move()
        {
            Position += new Vector2(_direction.X, _direction.Y);
        }

        private void OnDestroy()
        {
            Updated -= Move;
            Destroyed -= OnDestroy;
        }
    }

    class Plank : Entity
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);

        private int _upKey;
        private int _downKey;
        private int _maxY;

        public Plank(Vector2 spawnPosition, Shape shape, int maxY, int upKey, int downKey) : base(spawnPosition, shape)
        {
            Updated += TryMove;
            Destroyed += OnDestroy;

            _upKey = upKey;
            _downKey = downKey;
            _maxY = maxY;
        }

        private void TryMove()
        {
            bool canUp = true;
            bool canDown = true;

            foreach (var point in Shape.Points)
            {
                if (Position.Y + point.Position.Y <= 0)
                    canUp = false;
                if (Position.Y + point.Position.Y >= _maxY)
                    canDown = false;
            }

            if ((GetAsyncKeyState(_upKey) & 0x8000) > 0 && canUp == true)
                Move(-1);
            else if ((GetAsyncKeyState(_downKey) & 0x8000) > 0 && canDown == true)
                Move(1);
        }

        private void Move(int yDirection)
        {
            Position += new Vector2(0, yDirection);
        }

        private void OnDestroy()
        {
            Updated -= TryMove;
            Destroyed -= OnDestroy;
        }
    }

    class Grid
    {
        public int xLeght { get; private set; }
        public int yLeght { get; private set; }
        public char BorderSymbol { get; private set; }

        public Grid(int xLeght, int yLeght, char borderSymbol)
        {
            this.xLeght = xLeght;
            this.yLeght = yLeght;
            BorderSymbol = borderSymbol;
        }
    }

    static class FloatRandom
    {
        static Random random = new Random();

        public static float GetRandomNumber(double minimum, double maximum)
        {
            float result = (float)(random.NextDouble() * (maximum - minimum) + minimum);
            return result;
        }
    }
}
