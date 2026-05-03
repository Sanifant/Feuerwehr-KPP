using de.openelp.feuerwehr.domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.application.hydrant
{
    public class HydrantService
    {
        private readonly IHydrantRepository _repo;

        public HydrantService(IHydrantRepository repo)
        {
            _repo = repo;
        }

        public virtual void CreateHydrant(Hydrant hydrant)
        {
            _repo.Add(hydrant);
        }

        public virtual Task<List<Hydrant>> GetAll()
        {
            return Task.FromResult(new List<Hydrant>(_repo.GetAll()));
        }

        public virtual Hydrant? GetById(Guid id)
        {
            return _repo.GetById(id);
        }

        public virtual void UpdateHydrant(Hydrant hydrant)
        {
            _repo.Update(hydrant);
        }

        public virtual void DeleteHydrant(Guid id)
        {
            _repo.Delete(id);
        }
    }
}
