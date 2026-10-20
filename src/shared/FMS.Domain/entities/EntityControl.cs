namespace FMS.Domain.entities
{
    public partial class EntityControl
    {
        public Guid Id { get; set; }


        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        public bool SystemIsDeleted { get; set; } = false;
        public DateTime? SystemDateDeleted { get; set; } = null;
    }
}