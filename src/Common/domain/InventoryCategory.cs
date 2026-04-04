using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.domain
{
    public class InventoryCategory
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // z.B. "Pumpen", "Schläuche"

        public string Description { get; set; } = string.Empty;

        public ICollection<InventoryItem> Items { get; set; }
            = new List<InventoryItem>();

        public InventoryCategory()
        {
            Id = Guid.NewGuid();
        }
    }
}