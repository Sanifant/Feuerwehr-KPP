using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace de.openelp.feuerwehr.Api.UnitTests.Mocks
{
    internal class HydrantRespositoryMock : IHydrantRepository
    {
        public List<Hydrant> Hydrants { get; set; } = new List<Hydrant>();

        public void Add(Hydrant hydrant)
        {
            Hydrants.Add(hydrant);
        }

        public void Delete(Guid id)
        {
            var hydrant = Hydrants.Find(h => h.Id == id);
            if (hydrant != null)
            {
                Hydrants.Remove(hydrant);
            }
        }

        public IEnumerable<Hydrant> GetAll()
        {
            return Hydrants;
        }

        public Hydrant? GetById(Guid id)
        {
            return Hydrants.Find(h => h.Id == id);
        }

        public void Update(Hydrant hydrant)
        {
            var existingHydrant = Hydrants.Find(h => h.Id == hydrant.Id);
            if (existingHydrant != null)
            {
                Hydrants.Remove(existingHydrant);
                Hydrants.Add(hydrant);
            }
        }
    }
}
