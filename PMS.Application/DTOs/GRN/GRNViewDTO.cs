using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Core.Domain.Entities;

namespace PMS.Application.DTOs.GRN
{
    public class GRNViewDTO
    {
        public int GRNID { get; set; }

        public required string Source { get; set; }

        public required DateTime CreateDate { get; set; }

        public required decimal Total { get; set; }

        public required string CreateBy { get; set; }

        public string? Description { get; set; }
        public string WarehouseName { get; set; }
        public string warehouse { get; set; }


        public int POID { get; set; }


        public virtual ICollection<GRNDetailViewDTO> GRNDetailViewDTO { get; set; } = new List<GRNDetailViewDTO>();

    }
}
