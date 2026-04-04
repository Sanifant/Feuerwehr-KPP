using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.infrastructure
{
    /// <summary>
    /// Repository implementation for inventory item data access.
    /// </summary>
    public class InventoryRepository : IInventoryRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryRepository"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public InventoryRepository(AppDbContext dbContext)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public void Add(InventoryItem item)
        {
            _context.InventoryItems.Add(item);
            _context.SaveChanges();
        }

        public void Delete(Guid id)
        {
            _context.InventoryItems.Remove(new InventoryItem { Id = id });
            _context.SaveChanges();
        }

        public IEnumerable<InventoryItem> GetAll()
        {
            return _context.InventoryItems.ToList();
        }

        public IEnumerable<InventoryItem> GetByCategory(Guid category)
        {
            return _context.InventoryItems
                .Where(i => i.CategoryId != null && i.CategoryId == category)
                .ToList();
        }

        public InventoryItem GetById(Guid id)
        {
            return _context.InventoryItems.Find(id) 
                ?? throw new InvalidOperationException($"InventoryItem mit ID {id} wurde nicht gefunden.");
        }

        public InventoryItemRelationship GetRelationshipById(Guid id)
        {
            return _context.InventoryItemRelationships
                .Include(r => r.RelationshipLabel)
                .FirstOrDefault(r => r.Id == id) 
                ?? throw new InvalidOperationException($"InventoryItemRelationship mit ID {id} wurde nicht gefunden.");
        }

        public IEnumerable<InventoryItemRelationship> GetRelationships()
        {
            return _context.InventoryItemRelationships
                .Include(r => r.RelationshipLabel)
                .ToList();
        }

        public void Update(InventoryItem item)
        {
            if(_context.InventoryItems.Any(i => i.Id == item.Id))
            {
                _context.InventoryItems.Update(item);
                _context.SaveChanges();
            }
            else
            {
                throw new InvalidOperationException($"InventoryItem mit ID {item.Id} existiert nicht und kann nicht aktualisiert werden.");
            }
        }
    }
}
