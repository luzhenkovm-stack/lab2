namespace Lab02.Interfaces;

// Задание 1. Базовые интерфейсы
public interface IMovable
{
    void Move(int x, int y);
}

public sealed class Point : IMovable
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public Point(int x = 0, int y = 0) => (X, Y) = (x, y);
    public void Move(int x, int y) => (X, Y) = (x, y);
    public override string ToString() => $"Точка: ({X}, {Y})";
}

public interface IDrawable
{
    void Draw();
}

public sealed class Circle : IDrawable, IShape
{
    public double Radius { get; }
    public Circle(double radius) => Radius = radius > 0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));
    public void Draw() => Console.WriteLine($"Рисуем круг радиуса {Radius}");
    public double GetArea() => Math.PI * Radius * Radius;
    public double GetPerimeter() => 2 * Math.PI * Radius;
}

public sealed class Rectangle : IDrawable, IShape
{
    public double Width { get; }
    public double Height { get; }
    public Rectangle(double width, double height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Стороны должны быть положительными.");
        Width = width; Height = height;
    }
    public void Draw() => Console.WriteLine($"Рисуем прямоугольник {Width} × {Height}");
    public double GetArea() => Width * Height;
    public double GetPerimeter() => 2 * (Width + Height);
}

public static class DrawingService
{
    public static void DrawAll(IEnumerable<IDrawable> figures)
    {
        foreach (IDrawable figure in figures) figure.Draw();
    }
}

// Задание 2. Интерфейсы и наследование
public interface IShape
{
    double GetArea();
    double GetPerimeter();
}

public interface I3DShape : IShape
{
    double GetVolume();
}

public sealed class Cube : I3DShape
{
    public double Side { get; }
    public Cube(double side) => Side = side > 0 ? side : throw new ArgumentOutOfRangeException(nameof(side));
    public double GetArea() => 6 * Side * Side;
    public double GetPerimeter() => 12 * Side; // Сумма длин 12 рёбер куба.
    public double GetVolume() => Side * Side * Side;
}

public static class ShapeInfoPrinter
{
    public static void PrintShapeInfo(IShape shape)
    {
        Console.WriteLine($"Площадь: {shape.GetArea():F2}; периметр: {shape.GetPerimeter():F2}");
    }
}

// Задание 3. Сначала «толстый» интерфейс для демонстрации проблемы ISP.
public interface IDevice
{
    void Print(string document);
    void Scan();
    void Fax(string number);
}

// Демонстрационная реализация старого интерфейса: неподдерживаемые операции пустые.
public sealed class OldPrinter : IDevice
{
    public void Print(string document) => Console.WriteLine($"Печать: {document}");
    public void Scan() { }
    public void Fax(string number) { }
}

public sealed class OldScanner : IDevice
{
    public void Print(string document) { }
    public void Scan() => Console.WriteLine("Сканирование документа");
    public void Fax(string number) { }
}

// Разделённые интерфейсы.
public interface IPrinter { void Print(string document); }
public interface IScanner { void Scan(); }
public interface IFax { void Fax(string number); }

public sealed class Printer : IPrinter
{
    public void Print(string document) => Console.WriteLine($"Печать: {document}");
}
public sealed class Scanner : IScanner
{
    public void Scan() => Console.WriteLine("Сканирование документа");
}
public sealed class MultifunctionDevice : IPrinter, IScanner, IFax
{
    public void Print(string document) => Console.WriteLine($"МФУ печатает: {document}");
    public void Scan() => Console.WriteLine("МФУ сканирует документ");
    public void Fax(string number) => Console.WriteLine($"МФУ отправляет факс на номер {number}");
}

// Задание 4. Полиморфизм и логирование.
public interface IPayable { void Pay(decimal amount); }
public sealed class CreditCard : IPayable
{
    public void Pay(decimal amount) => Console.WriteLine($"Оплата картой: {amount:C}");
}
public sealed class Cash : IPayable
{
    public void Pay(decimal amount) => Console.WriteLine($"Оплата наличными: {amount:C}");
}
public static class PaymentProcessor
{
    public static void ProcessPayment(IPayable method, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        method.Pay(amount);
    }
}

public interface ILogger { void Log(string message); }
public sealed class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"[LOG] {message}");
}
public sealed class FileLogger : ILogger
{
    private readonly string _path;
    public FileLogger(string path) => _path = path;
    public void Log(string message) => File.AppendAllText(_path, $"{DateTime.Now:O} [LOG] {message}{Environment.NewLine}");
}
public static class WorkService
{
    public static void DoWork(ILogger logger)
    {
        logger.Log("Работа началась.");
        logger.Log("Работа успешно завершена.");
    }
}

public static class InterfaceDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Часть 1. Интерфейсы ===");
        var point = new Point(1, 2); point.Move(10, 20); Console.WriteLine(point);
        List<IDrawable> figures = [new Circle(3), new Rectangle(4, 5)];
        DrawingService.DrawAll(figures);
        ShapeInfoPrinter.PrintShapeInfo((IShape)figures[0]);
        var cube = new Cube(3);
        Console.WriteLine($"Куб: площадь поверхности={cube.GetArea():F2}, сумма рёбер={cube.GetPerimeter():F2}, объём={cube.GetVolume():F2}");
        IPrinter printer = new MultifunctionDevice(); printer.Print("Отчёт.pdf");
        IScanner scanner = new MultifunctionDevice(); scanner.Scan();
        IFax fax = new MultifunctionDevice(); fax.Fax("+70000000000");
        PaymentProcessor.ProcessPayment(new CreditCard(), 1500m);
        PaymentProcessor.ProcessPayment(new Cash(), 500m);
        WorkService.DoWork(new ConsoleLogger());
        WorkService.DoWork(new FileLogger("work.log"));
    }
}
