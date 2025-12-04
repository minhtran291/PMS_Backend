namespace PMS.Application.DTOs.GRN
{
    public class ProductImportPercentageDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Percentage { get; set; }
    }
}