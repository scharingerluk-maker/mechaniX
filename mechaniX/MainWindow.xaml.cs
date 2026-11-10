using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace mechaniX;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ForceEntry> forces = new();
    private double? span;
    private double reactionAx;
    private double reactionAy;
    private double reactionBy;

    public MainWindow()
    {
        InitializeComponent();
        ForceList.ItemsSource = forces;
        forces.Add(new ForceEntry("Kraft 1", "2", "10", "-90"));
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Calculate();
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        Calculate();
    }

    private void AddForceButton_Click(object sender, RoutedEventArgs e)
    {
        if (forces.Count >= 5)
        {
            ErrorText.Text = "Es können höchstens fünf Kräfte eingegeben werden.";
            return;
        }

        var position = TryReadPositiveValue(SpanInput.Text, out var currentSpan)
            ? Format(currentSpan / 2)
            : "0";
        forces.Add(new ForceEntry($"Kraft {forces.Count + 1}", position, "10", "-90"));
        ErrorText.Text = string.Empty;
    }

    private void RemoveForceButton_Click(object sender, RoutedEventArgs e)
    {
        if (forces.Count <= 1)
        {
            ErrorText.Text = "Mindestens eine Kraft muss im System bleiben.";
            return;
        }

        if (sender is Button { Tag: ForceEntry entry })
        {
            forces.Remove(entry);
            UpdateForceLabels();
            ErrorText.Text = string.Empty;
        }
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

        if (forces.Count is < 1 or > 5)
        {
            ShowInputError("Das System muss zwischen einer und fünf Kräften enthalten.");
            return;
        }

        var parsedForces = new List<AppliedForce>(forces.Count);
        for (var index = 0; index < forces.Count; index++)
        {
            var entry = forces[index];
            if (!TryReadNonNegativeValue(entry.Position, out var position))
            {
                ShowInputError($"Die Position x von Kraft {index + 1} muss eine Zahl ab 0 sein.");
                return;
            }

            if (position > currentSpan)
            {
                ShowInputError($"Die Position x von Kraft {index + 1} muss zwischen 0 und l liegen.");
                return;
            }

            if (!TryReadPositiveValue(entry.Magnitude, out var magnitude))
            {
                ShowInputError($"Die Kraft F von Kraft {index + 1} muss größer als 0 sein.");
                return;
            }

            if (!TryReadSignedValue(entry.Angle, out var angle))
            {
                ShowInputError($"Der Winkel von Kraft {index + 1} muss eine gültige Zahl sein.");
                return;
            }

            var radians = Math.IEEERemainder(angle, 360) * (Math.PI / 180);
            var horizontal = magnitude * Math.Cos(radians);
            var vertical = magnitude * Math.Sin(radians);
            if (!double.IsFinite(horizontal) || !double.IsFinite(vertical))
            {
                ShowInputError($"Die Komponenten von Kraft {index + 1} sind zu groß.");
                return;
            }

            parsedForces.Add(new AppliedForce(position, magnitude, angle, horizontal, vertical));
        }

        var totalHorizontal = parsedForces.Sum(applied => applied.Horizontal);
        var totalVertical = parsedForces.Sum(applied => applied.Vertical);
        var totalMomentOverSpan = parsedForces.Sum(applied =>
            (applied.Position / currentSpan) * applied.Vertical);
        var currentReactionBy = -totalMomentOverSpan;
        var currentReactionAy = -totalVertical - currentReactionBy;
        var currentReactionAx = -totalHorizontal;
        if (!double.IsFinite(currentReactionAx) || !double.IsFinite(currentReactionAy) ||
            !double.IsFinite(currentReactionBy))
        {
            ShowInputError("Die resultierenden Auflagerkräfte sind zu groß.");
            return;
        }

        span = currentSpan;
        reactionAx = currentReactionAx;
        reactionAy = currentReactionAy;
        reactionBy = currentReactionBy;
        ReactionAxOutput.Text = $"Aₓ = {Format(reactionAx)}";
        ReactionAyOutput.Text = $"Aᵧ = {Format(reactionAy)}";
        ReactionBOutput.Text = $"Bᵧ = {Format(reactionBy)}";
        EquilibriumOutput.Text =
            $"Kontrolle: ΣFₓ = {Format(reactionAx + totalHorizontal)} kN, " +
            $"ΣFᵧ = {Format(reactionAy + reactionBy + totalVertical)} kN. " +
            "Vorzeichen: positiv nach rechts/oben.";
        ErrorText.Text = string.Empty;
        DrawDiagram();
    }

    private void ShowInputError(string message)
    {
        span = null;
        ReactionAxOutput.Text = "Aₓ = —";
        ReactionAyOutput.Text = "Aᵧ = —";
        ReactionBOutput.Text = "Bᵧ = —";
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
        return TryReadSignedValue(text, out value) && value >= 0;
    }

    private static bool TryReadSignedValue(string text, out double value)
    {
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float,
                   CultureInfo.InvariantCulture, out value)
               && double.IsFinite(value);
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.GetCultureInfo("de-DE"));
    }

    private void UpdateForceLabels()
    {
        for (var index = 0; index < forces.Count; index++)
        {
            forces[index].Label = $"Kraft {index + 1}";
        }
        ForceList.Items.Refresh();
    }

    private void DrawDiagram()
    {
        DiagramCanvas.Children.Clear();
        if (span is not double currentSpan)
        {
            return;
        }

        var width = DiagramCanvas.ActualWidth;
        if (width < 150)
        {
            return;
        }

        const double left = 56;
        const double rightMargin = 56;
        const double beamY = 145;
        const double arrowLength = 42;
        var right = width - rightMargin;
        var beamWidth = right - left;

        AddLine(left, beamY, right, beamY, "#172B4D", 5);
        DrawPinSupport(left, beamY);
        DrawRollerSupport(right, beamY);
        AddText("A", left - 5, beamY + 28, 16, "#24344D", true);
        AddText("B", right - 5, beamY + 36, 16, "#24344D", true);

        for (var index = 0; index < forces.Count; index++)
        {
            if (!TryReadNonNegativeValue(forces[index].Position, out var position) ||
                !TryReadPositiveValue(forces[index].Magnitude, out var magnitude) ||
                !TryReadSignedValue(forces[index].Angle, out var angle) ||
                position > currentSpan)
            {
                continue;
            }

            var radians = Math.IEEERemainder(angle, 360) * (Math.PI / 180);
            var loadX = left + beamWidth * (position / currentSpan);
            var endX = loadX + arrowLength * Math.Cos(radians);
            var endY = beamY - arrowLength * Math.Sin(radians);
            var color = index % 2 == 0 ? "#D64545" : "#D97706";
            AddArrow(loadX, beamY, endX, endY, color, 2.5);
            AddText($"F{index + 1} {Format(magnitude)} kN · {Format(angle)}°",
                endX - 56, endY - 22 - (index % 2) * 15, 112, color, true);
        }

        DrawReaction(left, beamY, reactionAx, reactionAy, true, "#2868C7");
        DrawReaction(right, beamY, 0, reactionBy, false, "#2868C7");
        AddText("Aₓ, Aᵧ", left - 24, beamY + 48, 62, "#2868C7", true);
        AddText("Bᵧ (nur vertikal)", right - 76, beamY + 55, 112, "#2868C7", true);
        AddDimension(left, right, beamY + 91, $"l = {Format(currentSpan)} m");
    }

    private void DrawReaction(double x, double y, double horizontal, double vertical,
        bool drawHorizontal, string color)
    {
        if (drawHorizontal && Math.Abs(horizontal) > 1e-10)
        {
            var direction = Math.Sign(horizontal);
            AddArrow(x, y, x + direction * 34, y, color, 2);
        }

        if (Math.Abs(vertical) > 1e-10)
        {
            var direction = Math.Sign(vertical);
            AddArrow(x, y, x, y - direction * 34, color, 2);
        }
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
        AddText(label, (start + end) / 2 - 60, y + 5, 120, "#66758A", true);
    }

    private void AddArrow(double startX, double startY, double endX, double endY,
        string color, double thickness)
    {
        AddLine(startX, startY, endX, endY, color, thickness);
        var direction = new Vector(endX - startX, endY - startY);
        direction.Normalize();
        var perpendicular = new Vector(-direction.Y, direction.X);
        var tip = new Point(endX, endY);
        var basePoint = tip - direction * 9;
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
            FontSize = 11,
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

    private sealed class AppliedForce(
        double position,
        double magnitude,
        double angle,
        double horizontal,
        double vertical)
    {
        public double Position { get; } = position;
        public double Magnitude { get; } = magnitude;
        public double Angle { get; } = angle;
        public double Horizontal { get; } = horizontal;
        public double Vertical { get; } = vertical;
    }

    public sealed class ForceEntry(string label, string position, string magnitude, string angle)
    {
        public string Label { get; set; } = label;
        public string Position { get; set; } = position;
        public string Magnitude { get; set; } = magnitude;
        public string Angle { get; set; } = angle;
    }
}
