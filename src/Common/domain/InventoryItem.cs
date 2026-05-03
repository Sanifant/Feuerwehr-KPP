using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace de.openelp.feuerwehr.domain
{
    public class InventoryItem
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;


        public string Description { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }           // nullable → Item muss nicht kategorisiert sein
        public InventoryCategory? Category { get; set; }

        public int Quantity { get; set; }

        public string Condition { get; set; } = string.Empty;

        public DateOnly PurchaseDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public DateOnly NextInspectionDate { get; set; }


        public ICollection<InventoryItemRelationship> ChildRelationships { get; set; }
            = new List<InventoryItemRelationship>();

        public InventoryItem() { 

            this.Id = Guid.NewGuid();
        }
    }
}
