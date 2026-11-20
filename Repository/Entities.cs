namespace Lab02.Repository;

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public sealed class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PassHash { get; set; } = string.Empty;
}
