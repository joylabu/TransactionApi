using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TransactionApi.Models
{
    public class TransactionRequest
    {
        [Required]
        [StringLength(50)]
        public string PartnerKey { get; set; }

        [Required]
        [StringLength(50)]
        public string PartnerRefNo { get; set; }

        [Required]
        [StringLength(50)]
        public string PartnerPassword { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long TotalAmount { get; set; }

        [JsonPropertyName("items")]
        public List<ItemDetail> Items { get; set; }

        [Required]
        public string Timestamp { get; set; }

        [Required]
        public string Sig { get; set; }
    }

    public class ItemDetail
    {
        [Required]
        [StringLength(50)]
        public string PartnerItemRef { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(1, 5)]
        public int Qty { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long UnitPrice { get; set; }
    }
}
