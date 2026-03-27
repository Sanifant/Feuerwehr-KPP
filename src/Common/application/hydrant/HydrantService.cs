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

        public void CreateHydrant(Hydrant hydrant)
        {
            _repo.Add(hydrant);
        }

        public Task<List<Hydrant>> GetAll()
        {
            return Task.FromResult(new List<Hydrant>(_repo.GetAll()));
        }

        public Hydrant GetById(Guid id)
        {
            return _repo.GetById(id);
        }

        public void UpdateHydrant(Hydrant hydrant)
        {
            _repo.Update(hydrant);
        }

        public void DeleteHydrant(Guid id)
        {
            _repo.Delete(id);
        }
    }
}
