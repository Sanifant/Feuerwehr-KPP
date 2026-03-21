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
        public string Name { get; set; }

        
        public string Description { get; set; } = string.Empty;

        public DateOnly PurchaseDate { get; set; }

        public string Location { get; set; }


        public InventoryItem() { 

            this.Id = Guid.NewGuid();
        }
    }
}
