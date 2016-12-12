using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace sudoku
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MaxCount = 9;
        private const double OutlineThickness = 5.0;
        private const double LineThickness = 0.5;
        private const double BoxLineThickness = 1.0;
        private readonly TimeSpan PutDuration = TimeSpan.FromSeconds(0.5);

        private SudokuData data;
        private TextBlock[,] texts;
        private TextBlock cursor;
        private Random r = new Random((int)DateTime.Now.Ticks);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void PutNumber(int n, int x, int y, bool delay = false, bool isReadOnly = false)
        {
            var text = this.texts[x, y];
            text.Foreground = isReadOnly ? Brushes.Violet : Brushes.Thistle;
            text.Text = n > 0 ? $"{n}" : text.Text;

            var previewText = new FormattedText(text.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize, text.Foreground);

            text.SetValue(Canvas.LeftProperty, OutlineThickness + x * this.CellSize + (this.CellSize - previewText.Width) / 2);
            text.SetValue(Canvas.TopProperty, OutlineThickness + y * this.CellSize + (this.CellSize - previewText.Height) / 2);
            var scaleTransform = text.RenderTransform as ScaleTransform
                ?? new ScaleTransform(1.0, 1.0, previewText.Width / 2, previewText.Height / 2);
            text.RenderTransform = scaleTransform;

            DoubleAnimation drop = new DoubleAnimation()
            {
                Duration = new Duration(PutDuration),
                BeginTime = delay ? TimeSpan.FromSeconds(this.r.NextDouble()) : TimeSpan.Zero,
                DecelerationRatio = 1.0,
            };

            var show = drop.Clone();

            if (n > 0)
            {
                drop.From = 10.0;
                drop.To = 1.0;

                show.From = 0.0;
                show.To = 1.0;
            }
            else
            {
                drop.From = 1.0;
                drop.To = 10.0;

                show.From = 1.0;
                show.To = 0.0;
            }

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, drop);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, drop);
            text.BeginAnimation(UIElement.OpacityProperty, show);
        }

        private double CellSize { get; set; }

        private void DrawLines()
        {
            // draw outer border
            Brush lineBrush = Brushes.Black;

            this.CellSize = (canvas.ActualHeight - 2 * OutlineThickness) / MaxCount;
            for (int i = 1; i < MaxCount; i++)
            {
                double offset = this.CellSize * i + OutlineThickness;
                Func<double, double, double, double, Line> MakeLine = (x1, y1, x2, y2) =>
                {
                    return new Line() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = Brushes.Gray, StrokeThickness = i % 3 == 0 ? BoxLineThickness : LineThickness, StrokeLineJoin = PenLineJoin.Bevel };
                };

                double start = OutlineThickness;

                this.canvas.Children.Add(MakeLine(offset, start, offset, this.canvas.ActualHeight - start));
                this.canvas.Children.Add(MakeLine(start, offset, this.canvas.ActualHeight - start, offset));
            }

            this.texts = new TextBlock[MaxCount, MaxCount];

            var numberList = Enumerable.Range(0, MaxCount).Select(x => Enumerable.Range(0, MaxCount)
                .Select(y => new { X = x, Y = y })).SelectMany(a => a)
                .Select(co => this.texts[co.X, co.Y] = new TextBlock()
                {
                    FontSize = 25.0,
                    TextAlignment = TextAlignment.Center,
                })
                .ToList();

            numberList.ForEach(t => this.canvas.Children.Add(t));

            this.cursor = new TextBlock() { FontSize = 30.0, TextAlignment = TextAlignment.Center };
            this.canvas.Children.Add(this.cursor);
            this.canvas.Cursor = Cursors.None;
            this.Cursor = Cursors.None;
            this.cursor.Text = $"{this.CurrentNumber}";
            Canvas.SetTop(this.cursor, Mouse.GetPosition(this.canvas).Y);
            Canvas.SetLeft(this.cursor, Mouse.GetPosition(this.canvas).X);
        }

        private void InitGame()
        {
            this.data = new SudokuData(Difficulty.Basic);

            var numberList = Enumerable.Range(0, MaxCount).Select(x => Enumerable.Range(0, MaxCount)
                .Select(y => new { X = x, Y = y })).SelectMany(a => a)
                .Select(co => new { X = co.X, Y = co.Y, N = this.data.Table[co.X, co.Y], Readonly = this.data.Readonly[co.X, co.Y] })
                .ToList();

            numberList.ForEach(cell => this.PutNumber(cell.N, cell.X, cell.Y, true, cell.Readonly));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // draw lines;
            this.DrawLines();
            this.InitGame();
        }

        public int CurrentNumber { get; private set; } = 1;

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.CurrentNumber = ((this.CurrentNumber - 1 + e.Delta / 120) % 9 + 9) % 9 + 1;
            this.currentNumber.Text = $"Current {this.CurrentNumber}";
            this.cursor.Text = $"{this.CurrentNumber}";
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(this.canvas);

            if (e.ButtonState == MouseButtonState.Released) return;
            int current = this.CurrentNumber;

            if (p.X > OutlineThickness && p.X < this.canvas.ActualWidth - OutlineThickness
                && p.Y > OutlineThickness && p.Y < this.canvas.ActualHeight - OutlineThickness)
            {
                int x = (int)(p.X / this.CellSize);
                int y = (int)(p.Y / this.CellSize);

                switch (e.ChangedButton)
                {
                    case MouseButton.Right:
                        if (this.data.Put(0, x, y) == PutResult.Cleared)
                        {
                            this.PutNumber(0, x, y);
                        }
                        else
                        {
                            // show occupied;
                        }
                        break;

                    case MouseButton.Left:
                        var result = this.data.Put(current, x, y);
                        switch (result)
                        {
                            case PutResult.Complete:
                                this.PutNumber(current, x, y);
                                // show complete;
                                this.complete.IsEnabled = true;
                                this.complete.Foreground = Brushes.Plum;
                                this.complete.FontSize = 25;
                                break;

                            case PutResult.Occupied:
                                // show occupied;
                                this.error.IsEnabled = true;
                                this.error.Foreground = Brushes.Red;
                                break;

                            case PutResult.OK:
                                this.PutNumber(current, x, y);
                                // show OK
                                break;

                            default:
                                // error;
                                break;
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Canvas.SetTop(this.cursor, e.GetPosition(this.canvas).Y);
            Canvas.SetLeft(this.cursor, e.GetPosition(this.canvas).X);
        }
    }
}
