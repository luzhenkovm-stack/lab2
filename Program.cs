using Lab02.Interfaces;
using Lab02.Repository;
using Microsoft.EntityFrameworkCore;

var runInterfaceDemo = args.Contains("--interfaces-demo", StringComparer.OrdinalIgnoreCase);
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--interfaces-demo").ToArray());
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=lab02.db"));
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

// Создание таблиц SQLite при первом запуске.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/", () => Results.Ok(new { message = "Lab02 REST API работает", endpoints = new[] { "POST /user", "GET /user/{id}", "PUT /user/{id}", "DELETE /user/{id}", "GET/POST /books" } }));

// Часть 3. REST API для таблицы User. Пароль принимается только как готовый хеш.
app.MapPost("/user", async (CreateUserRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Login) || request.Login.Length > 50)
        return Results.BadRequest(new { error = "Логин обязателен и должен содержать не более 50 символов." });
    if (string.IsNullOrWhiteSpace(request.PassHash))
        return Results.BadRequest(new { error = "PassHash обязателен. Передавайте хеш, а не пароль." });
    if (await db.Users.AnyAsync(u => u.Login == request.Login))
        return Results.Conflict(new { error = "Пользователь с таким логином уже существует." });

    var user = new User { Login = request.Login.Trim(), PassHash = request.PassHash };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/user/{user.Id}", new UserResponse(user.Id, user.Login));
});

app.MapGet("/user/{id:int}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    return user is null ? Results.NotFound(new { error = "Пользователь не найден." }) : Results.Ok(new UserResponse(user.Id, user.Login));
});

app.MapPut("/user/{id:int}", async (int id, UpdateUserRequest request, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound(new { error = "Пользователь не найден." });
    if (string.IsNullOrWhiteSpace(request.Login) || request.Login.Length > 50 || string.IsNullOrWhiteSpace(request.PassHash))
        return Results.BadRequest(new { error = "Укажите корректные Login и PassHash." });
    if (await db.Users.AnyAsync(u => u.Login == request.Login && u.Id != id))
        return Results.Conflict(new { error = "Этот логин уже занят." });
    user.Login = request.Login.Trim();
    user.PassHash = request.PassHash;
    await db.SaveChangesAsync();
    return Results.Ok(new UserResponse(user.Id, user.Login));
});

app.MapDelete("/user/{id:int}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound(new { error = "Пользователь не найден." });
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Пользователь удалён." });
});

// Часть 2. Демонстрация репозитория книг (CRUD и специализированный поиск).
app.MapGet("/books", async (IBookRepository repo, CancellationToken ct) => Results.Ok(await repo.GetAllAsync(ct)));
app.MapGet("/books/{id:int}", async (int id, IBookRepository repo, CancellationToken ct) =>
    await repo.GetByIdAsync(id, ct) is { } book ? Results.Ok(book) : Results.NotFound());
app.MapPost("/books", async (Book book, IBookRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author) || book.Price < 0)
        return Results.BadRequest(new { error = "Укажите название, автора и неотрицательную цену." });
    book.Id = 0;
    await repo.AddAsync(book, ct);
    return Results.Created($"/books/{book.Id}", book);
});
app.MapPut("/books/{id:int}", async (int id, Book input, IBookRepository repo, CancellationToken ct) =>
{
    var book = await repo.GetByIdAsync(id, ct);
    if (book is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Author) || input.Price < 0)
        return Results.BadRequest(new { error = "Некорректные данные книги." });
    book.Title = input.Title; book.Author = input.Author; book.Price = input.Price;
    repo.Update(book);
    return Results.Ok(book);
});
app.MapDelete("/books/{id:int}", async (int id, IBookRepository repo, CancellationToken ct) =>
{
    if (await repo.GetByIdAsync(id, ct) is null) return Results.NotFound();
    await repo.DeleteAsync(id, ct);
    return Results.Ok(new { message = "Книга удалена." });
});
app.MapGet("/books/by-author/{author}", (string author, IBookRepository repo) => Results.Ok(repo.FindByAuthor(author)));

// Для демонстрации части 1 запустите: dotnet run -- --interfaces-demo
if (runInterfaceDemo) InterfaceDemo.Run();

app.Run();

public record CreateUserRequest(string Login, string PassHash);
public record UpdateUserRequest(string Login, string PassHash);
public record UserResponse(int Id, string Login);
