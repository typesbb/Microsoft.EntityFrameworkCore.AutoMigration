using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Microsoft.EntityFrameworkCore.AutoMigration.Test
{
    public class TextDbContext : DbContext
    {
        public DbSet<Widget> Widgets { get; private init; } = null!;
        public TextDbContext(DbContextOptions<TextDbContext> options)
            : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseAutoMigration();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>(e =>
            {
                e.Property(widget => widget.Id).ValueGeneratedNever();

                e.OwnsMany(widget => widget.Children);
            });
        }
    }
    public class Widget
    {
        public Widget(int id, DateTime createTime, IEnumerable<Child> children)
        {
            Id = id;
            CreateTime = createTime;
            Children = children.ToHashSet();
        }
        private Widget(int id)
        {
            Id = id;
        }

        //// EF calls this when materialising the Widget
        //private Widget(int id)
        //{
        //    Id = id;

        //    // Strategically add another Widget between loading Widget 1 and loading its Children
        //    using var dbContext = new MyDbContext();
        //    dbContext.Widgets.Add(new(id: 2, children: [new("New")]));
        //    dbContext.SaveChanges();
        //}

        public int Id { get; private init; }
        public DateTime CreateTime { get; private init; }

        public HashSet<Child> Children { get; private init; } = [];

        public override string ToString() =>
            $"Widget {Id} with children: [{string.Join(',', Children.Select(c => c.Name))}]";
    }

    public record Child(string Name)
    {
        public int WidgetId { get; private set; }
        public string Name { get; private set; }
        public string Name1 { get; private set; }
    }

}
