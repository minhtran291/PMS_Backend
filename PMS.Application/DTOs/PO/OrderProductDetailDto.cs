namespace PMS.Application.DTOs.PO
{
    public class OrderProductDetailDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string DVT { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPriceTotal { get; set; }
        public string Description { get; set; }
        public decimal Tax { get; set; }
        public DateTime ExpiredDate { get; set; }
        public int POID { get; set; }
    }
}