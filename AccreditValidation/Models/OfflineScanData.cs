namespace AccreditValidation.Models
{
    using AccreditValidation;
    using SQLite;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class OfflineScanData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string? Barcode { get; set; }

        [Required]
        public string? AreaId { get; set; }

        public string? AreaName { get; set; }

        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        public Enums.ValidationMode Mode { get; set; } = Enums.ValidationMode.Online;

        public Enums.ValidationDirection Direction { get; set; } = Enums.ValidationDirection.In;

        public DateTime? ScannedDateTime { get; set; }

        public int ValidationResult { get; set; }
        
        [Ignore]    
        [ForeignKey("Barcode")]
        public virtual Badge? Badge { get; set; }
    }
}
