using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;

namespace Wpf_Labeln
{
    public partial class MainWindow : Window
    {
        private readonly Random _random = new();
        private readonly DispatcherTimer _timer = new();
        private readonly List<Rect> _occupiedAreas = new(); // Geänderte Sammlung für belegte Bereiche
        private readonly Dictionary<string, int> dict = new()
        {
            { "Button", 0 }, { "CheckBox", 1 }, { "ComboBox", 2 }, { "icon", 3 }, { "input", 4 }, { "label", 5 }, { "menu", 6 }, { "menuItem", 7 }, { "radio", 8 }, { "switch", 8 }, { "tabControl", 8 }, { "upDown", 8 },
        };

        public MainWindow()
        {
            InitializeComponent();

            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, e) =>
            {
                RandomizeControls();
                TakeScreenshot();
            };
            _timer.Start();
        }

        private void RandomizePosition(FrameworkElement control)
        {
            if (control.Parent is Grid grid)
            {
                double gridWidth = grid.ActualWidth;
                double gridHeight = grid.ActualHeight;

                Rect newPosition;
                int maxAttempts = 1000; // Maximale Versuche für eine passende Position
                int attempts = 0;

                do
                {
                    // Zufällige Position innerhalb des gültigen Bereichs
                    double x = _random.NextDouble() * (gridWidth - control.ActualWidth);
                    double y = _random.NextDouble() * (gridHeight - control.ActualHeight);

                    newPosition = new Rect(x, y, control.ActualWidth, control.ActualHeight);
                    attempts++;

                } while (_occupiedAreas.Any(area => area.IntersectsWith(newPosition)) && attempts < maxAttempts);

                try
                {
                    if (attempts < maxAttempts)
                    {
                        // Margin festlegen und Bereich speichern
                        control.Margin = new Thickness(newPosition.X, newPosition.Y, 50, 50);
                        _occupiedAreas.Add(newPosition);
                    }
                    else
                    {
                        // Wenn keine passende Position gefunden wird, ein neues zufälliges Layout berechnen
                        Console.WriteLine($"Keine passende Position für {control.Name} gefunden nach {maxAttempts} Versuchen.");
                        control.Margin = new Thickness(50, 50, 50, 50);  // Fallback-Position
                    }
                }
                catch (Exception ex)
                {
                    // Fehler abfangen und zum nächsten Element springen
                    Console.WriteLine($"Fehler beim Positionieren von {control.Name}: {ex.Message}. Weiter mit dem nächsten Element.");
                }
            }
        }

        private void RandomizeControls()
        {
            MainGrid.Children.Clear(); // Alle existierenden Controls entfernen
            _occupiedAreas.Clear(); // Zurücksetzen der belegten Bereiche

            // Alle Elemente aus dem Dictionary zufällig erzeugen und zum Grid hinzufügen
            foreach (var controlType in dict)
            {
                FrameworkElement control = null;
                switch (controlType.Key)
                {
                    case "Button":
                        control = button;
                        break;
                    case "CheckBox":
                        control = checkBox;
                        break;
                    case "ComboBox":
                        control = randomComboBox;
                        break;
                    case "icon":
                        control = new Button { Content = "Icon" };
                        break;
                    case "input":
                        control = textBox;
                        break;
                    case "label":
                        control = new Label { Content = $"Label {controlType.Value}" };
                        break;
                    case "menu":
                        control = new Menu();
                        break;
                    case "menuItem":
                        control = new MenuItem { Header = "MenuItem" };
                        break;
                    case "radio":
                        control = radioButton;
                        break;
                    case "switch":
                        control = new CheckBox { Content = "Switch" }; // Switch als CheckBox
                        break;
                    case "tabControl":
                        control = new TabControl();
                        break;
                    case "upDown":
                        control = new DoubleUpDown(); // Typargument hinzugefügt
                        break;
                    default:
                        continue;
                }

                // Wenn das Control nicht null ist und es nicht bereits im XAML vorhanden ist, dann zufällig platzieren
                if (control != null && !MainGrid.Children.Contains(control))
                {
                    MainGrid.Children.Add(control);
                    RandomizePosition(control); // Position zufällig setzen
                    RandomizeAppearance(control); // Erscheinung zufällig setzen
                }
            }
        }




        private void RandomizeAppearance(FrameworkElement control)
        {
            if (control is Control element)
            {
                // Zufällige Breite und Höhe
                element.Width = _random.Next(50, 200);
                element.Height = _random.Next(20, 100);

                // Zufällige Hintergrundfarbe
                element.Background = new SolidColorBrush(
                    Color.FromRgb((byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256))
                );

                // Zufällige Schriftfarbe und -größe
                if (element is ContentControl contentControl)
                {
                    contentControl.FontSize = _random.Next(12, 24);
                    contentControl.Foreground = new SolidColorBrush(
                        Color.FromRgb((byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256))
                    );
                }
            }
        }

        private void TakeScreenshot()
        {
            try
            {
                // Grenzen des MainGrid ermitteln
                var bounds = VisualTreeHelper.GetDescendantBounds(MainGrid);
                if (bounds.IsEmpty)
                {
                    Console.WriteLine("MainGrid hat keine sichtbaren Inhalte.");
                    return;
                }

                // RenderTargetBitmap erstellen
                var renderTarget = new RenderTargetBitmap(
                    (int)Math.Ceiling(bounds.Width),
                    (int)Math.Ceiling(bounds.Height),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                // Zeichnen des MainGrid in die Bitmap
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var brush = new VisualBrush(MainGrid);
                    context.DrawRectangle(brush, null, new Rect(new Point(), bounds.Size));
                }
                renderTarget.Render(visual);

                // Screenshot speichern
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string screenshotPath = Path.Combine("images", $"Screenshot_{timestamp}.png");
                string labelFilePath = Path.Combine("labels", $"Screenshot_{timestamp}.txt");

                // Verzeichnisse erstellen
                Directory.CreateDirectory("images");
                Directory.CreateDirectory("labels");

                // Screenshot speichern
                using (var fileStream = new FileStream(screenshotPath, FileMode.Create))
                {
                    var pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
                    pngEncoder.Save(fileStream);
                }

                // Bounding Boxen speichern
                using (StreamWriter writer = new StreamWriter(labelFilePath))
                {
                    foreach (var area in _occupiedAreas)
                    {
                        // Index für das Element bestimmen (Beispiel: Immer 0 für Standard, kann angepasst werden)
                        int index = 0; // Placeholder, hier kann Logik für die Zuordnung ergänzt werden

                        // Normalisierte Werte berechnen
                        double centerX = (area.X + area.Width / 2) / bounds.Width;
                        double centerY = (area.Y + area.Height / 2) / bounds.Height;
                        double width = area.Width / bounds.Width;
                        double height = area.Height / bounds.Height;

                        // Werte ins gewünschte Format schreiben
                        writer.WriteLine($"{index} {centerX:F6} {centerY:F6} {width:F6} {height:F6}");
                    }
                }

                Console.WriteLine($"Screenshot gespeichert: {screenshotPath}");
                Console.WriteLine($"Bounding-Box-Koordinaten gespeichert: {labelFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Speichern des Screenshots oder der Bounding-Box-Koordinaten: {ex.Message}");
            }
        }


    }
}
