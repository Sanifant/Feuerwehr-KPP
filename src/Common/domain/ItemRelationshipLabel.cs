using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.domain
{
    public class ItemRelationshipLabel
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // z.B. "Zubehör", "Ersatzteil"

        public string Description { get; set; } = string.Empty;

        public ICollection<InventoryItemRelationship> Relationships { get; set; }
            = new List<InventoryItemRelationship>();

        public ItemRelationshipLabel()
        {
            Id = Guid.NewGuid();
        }
    }
}