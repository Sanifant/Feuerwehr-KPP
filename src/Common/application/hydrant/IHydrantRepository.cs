using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;

namespace de.openelp.feuerwehr.application.hydrant
{
    public interface IHydrantRepository
    {
        IEnumerable<Hydrant> GetAll();

        Hydrant? GetById(Guid id);

        void Add(Hydrant hydrant);

        void Update(Hydrant hydrant);

        void Delete(Guid id);
    }
}
