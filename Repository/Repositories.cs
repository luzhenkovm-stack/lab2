using Microsoft.EntityFrameworkCore;

namespace Lab02.Repository;

public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Delete(int id);
}

public interface IAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class GenericRepository<T>(AppDbContext context) : IRepository<T>, IAsyncRepository<T> where T : class
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public T? GetById(int id) => DbSet.Find(id);
    public IEnumerable<T> GetAll() => DbSet.AsNoTracking().ToList();
    public void Add(T entity) { DbSet.Add(entity); Context.SaveChanges(); }
    public void Update(T entity) { DbSet.Update(entity); Context.SaveChanges(); }
    public void Delete(int id)
    {
        var entity = DbSet.Find(id);
        if (entity is not null) { DbSet.Remove(entity); Context.SaveChanges(); }
    }
    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => DbSet.FindAsync([id], cancellationToken).AsTask();
    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) => DbSet.AsNoTracking().ToListAsync(cancellationToken);
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    { await DbSet.AddAsync(entity, cancellationToken); await Context.SaveChangesAsync(cancellationToken); }
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet.FindAsync([id], cancellationToken);
        if (entity is not null) DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}

public interface IBookRepository : IRepository<Book>, IAsyncRepository<Book>
{
    IEnumerable<Book> FindByAuthor(string author);
    IEnumerable<Book> FindByPriceRange(decimal min, decimal max);
    Task<List<Book>> FindByAuthorAsync(string author, CancellationToken cancellationToken = default);
}

public sealed class BookRepository(AppDbContext context) : GenericRepository<Book>(context), IBookRepository
{
    public IEnumerable<Book> FindByAuthor(string author) => DbSet.AsNoTracking().Where(b => b.Author.Contains(author)).ToList();
    public IEnumerable<Book> FindByPriceRange(decimal min, decimal max) => DbSet.AsNoTracking().Where(b => b.Price >= min && b.Price <= max).ToList();
    public Task<List<Book>> FindByAuthorAsync(string author, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking().Where(b => b.Author.Contains(author)).ToListAsync(cancellationToken);
}

public interface IUnitOfWork : IDisposable
{
    IBookRepository Books { get; }
    IRepository<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IBookRepository? _books;
    private IRepository<User>? _users;
    public IBookRepository Books => _books ??= new BookRepository(context);
    public IRepository<User> Users => _users ??= new GenericRepository<User>(context);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
    public void Dispose() => context.Dispose();
}
