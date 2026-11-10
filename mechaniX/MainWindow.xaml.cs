using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace mechaniX;

public partial class MainWindow : Window
{
    private double? span;
    private double? force;
    private double? forcePosition;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Calculate();
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        Calculate();
    }

    private void DiagramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawDiagram();
    }

    private void Calculate()
    {
        if (!TryReadPositiveValue(SpanInput.Text, out var currentSpan))
        {
            ShowInputError("Die Stützweite l muss eine Zahl größer als 0 sein.");
            return;
        }

        if (!TryReadPositiveValue(ForceInput.Text, out var currentForce))
        {
            ShowInputError("Die Einzelkraft F muss eine Zahl größer als 0 sein.");
            return;
        }

        if (!TryReadNonNegativeValue(PositionInput.Text, out var currentPosition))
        {
            ShowInputError("Die Position x muss eine Zahl ab 0 sein.");
            return;
        }

        if (currentPosition > currentSpan)
        {
            ShowInputError("Die Position x muss zwischen A und B liegen (0 ≤ x ≤ l).");
            return;
        }

        span = currentSpan;
        force = currentForce;
        forcePosition = currentPosition;

        var reactionA = currentForce * ((currentSpan - currentPosition) / currentSpan);
        var reactionB = currentForce * (currentPosition / currentSpan);
        ReactionAOutput.Text = $"R_A = {Format(reactionA)} kN";
        ReactionBOutput.Text = $"R_B = {Format(reactionB)} kN";
        EquilibriumOutput.Text =
            $"Kontrolle: R_A + R_B = {Format(reactionA + reactionB)} kN = F. " +
            "Beide Auflagerreaktionen wirken nach oben.";
        ErrorText.Text = string.Empty;
        DrawDiagram();
    }

    private void ShowInputError(string message)
    {
        span = null;
        force = null;
        forcePosition = null;
        ReactionAOutput.Text = "—";
        ReactionBOutput.Text = "—";
        EquilibriumOutput.Text = "Bitte Eingaben prüfen und erneut berechnen.";
        ErrorText.Text = message;
        DrawDiagram();
    }

    private static bool TryReadPositiveValue(string text, out double value)
    {
        return TryReadNonNegativeValue(text, out value) && value > 0;
    }

    private static bool TryReadNonNegativeValue(string text, out double value)
    {
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture, out value)
               && double.IsFinite(value)
               && value >= 0;
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.GetCultureInfo("de-DE"));
    }

    private void DrawDiagram()
    {
        DiagramCanvas.Children.Clear();
        if (span is not double currentSpan || force is not double currentForce ||
            forcePosition is not double currentPosition)
        {
            return;
        }

        var width = DiagramCanvas.ActualWidth;
        if (width < 150)
        {
            return;
        }

        const double left = 44;
        const double rightMargin = 44;
        const double beamY = 100;
        var right = width - rightMargin;
        var beamWidth = right - left;
        var loadX = left + beamWidth * (currentPosition / currentSpan);

        AddLine(left, beamY, right, beamY, "#172B4D", 5);
        DrawPinSupport(left, beamY);
        DrawRollerSupport(right, beamY);
        AddText("A", left - 5, beamY + 27, 16, "#24344D", true);
        AddText("B", right - 5, beamY + 34, 16, "#24344D", true);

        AddArrow(loadX, beamY - 55, loadX, beamY - 5, "#D64545", 3);
        AddText($"F = {Format(currentForce)} kN", loadX - 45, beamY - 82, 110, "#B42318", true);
        AddArrow(left, beamY + 89, left, beamY + 43, "#2868C7", 2.5);
        AddArrow(right, beamY + 89, right, beamY + 43, "#2868C7", 2.5);
        AddText("R_A", left + 8, beamY + 60, 42, "#2868C7", true);
        AddText("R_B", right - 50, beamY + 60, 42, "#2868C7", true);

        var dimensionY = beamY + 112;
        AddDimension(left, loadX, dimensionY, $"x = {Format(currentPosition)} m");
        AddDimension(left, right, dimensionY + 26, $"l = {Format(currentSpan)} m");
    }

    private void DrawPinSupport(double x, double y)
    {
        AddPolygon(new Point(x - 14, y + 20), new Point(x + 14, y + 20),
            new Point(x, y + 2), "#718198");
        AddLine(x - 19, y + 22, x + 19, y + 22, "#718198", 2);
        for (var offset = -14; offset <= 10; offset += 8)
        {
            AddLine(x + offset, y + 23, x + offset - 6, y + 29, "#AAB5C4", 1);
        }
    }

    private void DrawRollerSupport(double x, double y)
    {
        AddPolygon(new Point(x - 14, y + 16), new Point(x + 14, y + 16),
            new Point(x, y + 2), "#718198");
        var roller = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(ColorFromHex("#718198")),
            StrokeThickness = 1.5
        };
        DiagramCanvas.Children.Add(roller);
        Canvas.SetLeft(roller, x - 4);
        Canvas.SetTop(roller, y + 17);
        AddLine(x - 19, y + 27, x + 19, y + 27, "#718198", 2);
        for (var offset = -14; offset <= 10; offset += 8)
        {
            AddLine(x + offset, y + 28, x + offset - 6, y + 34, "#AAB5C4", 1);
        }
    }

    private void AddDimension(double start, double end, double y, string label)
    {
        AddLine(start, y, end, y, "#8A97A9", 1);
        AddLine(start, y - 5, start, y + 5, "#8A97A9", 1);
        AddLine(end, y - 5, end, y + 5, "#8A97A9", 1);
        AddText(label, (start + end) / 2 - 48, y + 5, 96, "#66758A", true);
    }

    private void AddArrow(double startX, double startY, double endX, double endY,
        string color, double thickness)
    {
        AddLine(startX, startY, endX, endY, color, thickness);
        var direction = new Vector(endX - startX, endY - startY);
        direction.Normalize();
        var perpendicular = new Vector(-direction.Y, direction.X);
        var tip = new Point(endX, endY);
        var basePoint = tip - direction * 10;
        AddPolygon(tip, basePoint + perpendicular * 4, basePoint - perpendicular * 4, color);
    }

    private void AddLine(double x1, double y1, double x2, double y2, string color, double thickness)
    {
        DiagramCanvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(ColorFromHex(color)),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private void AddPolygon(Point first, Point second, Point third, string color)
    {
        DiagramCanvas.Children.Add(new Polygon
        {
            Points = new PointCollection { first, second, third },
            Fill = new SolidColorBrush(ColorFromHex(color)),
            Stroke = new SolidColorBrush(ColorFromHex(color))
        });
    }

    private void AddText(string text, double x, double y, double width, string color, bool bold)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            FontSize = 12,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = new SolidColorBrush(ColorFromHex(color)),
            TextAlignment = TextAlignment.Center
        };
        DiagramCanvas.Children.Add(label);
        Canvas.SetLeft(label, Math.Max(0, Math.Min(x, DiagramCanvas.ActualWidth - width)));
        Canvas.SetTop(label, y);
    }

    private static Color ColorFromHex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex)!;
    }
}
