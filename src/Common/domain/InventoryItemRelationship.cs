using System;
using System.ComponentModel.DataAnnotations;

namespace de.openelp.feuerwehr.domain
{
    public class InventoryItemRelationship
    {
        public Guid Id { get; set; }

        // FK → das "übergeordnete" Item
        public Guid ParentItemId { get; set; }
        public InventoryItem ParentItem { get; set; } = null!;

        // FK → das "untergeordnete" Item 
        public Guid ChildItemId { get; set; }
        public InventoryItem ChildItem { get; set; } = null!;

        public Guid RelationshipLabelId { get; set; }
        public ItemRelationshipLabel RelationshipLabel { get; set; } = null!;

        public InventoryItemRelationship()
        {
            Id = Guid.NewGuid();
        }
    }
}