namespace PMS.Application.DTOs.GRN
{
    public class MonthlyImportWithProductsDto
    {
        public int Month { get; set; }
        public int TotalQuantity { get; set; }
        public List<ProductImportPercentageDto> Products { get; set; } = new();
    }
}