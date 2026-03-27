using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.domain;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.infrastructure
{
    /// <summary>
    /// Repository implementation for hydrant data access.
    /// </summary>
    public class HydrantRepository : IHydrantRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="HydrantRepository"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public HydrantRepository(AppDbContext dbContext)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public void Add(Hydrant hydrant)
        {
            _context.Hydrants.Add(hydrant);
            _context.SaveChanges();
        }

        public void Delete(Guid id)
        {
            _context.Hydrants.Remove(new Hydrant { Id = id });
            _context.SaveChanges();
        }

        public IEnumerable<Hydrant> GetAll()
        {
            return _context.Hydrants.ToList();
        }

        public Hydrant GetById(Guid id)
        {
            return _context.Hydrants.Find(id)
                ?? throw new InvalidOperationException($"Hydrant mit ID {id} wurde nicht gefunden.");
        }

        public void Update(Hydrant hydrant)
        {
            if (_context.Hydrants.Any(h => h.Id == hydrant.Id))
            {
                _context.Hydrants.Update(hydrant);
                _context.SaveChanges();
            }
            else
            {
                throw new InvalidOperationException($"Hydrant mit ID {hydrant.Id} existiert nicht und kann nicht aktualisiert werden.");
            }
        }
    }
}
