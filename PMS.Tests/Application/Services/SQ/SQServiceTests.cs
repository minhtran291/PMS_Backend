using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.API.Services.QuotationService;
using PMS.Application.Services.Product;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SQ
{
    public class SQServiceTests : ServiceTestBase
    {
        private QuotationService _quotationService;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();
            _quotationService = new QuotationService(UnitOfWorkMock.Object, MapperMock.Object);
        }

        [Test]
        public async Task GetAllQuotationAsync_ValidData_ShouldReturnSuccess()
        {

            var now = DateTime.Now;
            var quotations = new List<Quotation>
            {
                new Quotation { QID = 1, SupplierID = 1, SendDate = now.AddDays(-2), QuotationExpiredDate = now.AddDays(2) },
                new Quotation { QID = 2, SupplierID = 2, SendDate = now.AddDays(-5), QuotationExpiredDate = now.AddDays(-1) }
            }.AsQueryable();

            var suppliers = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Supplier A" },
                new Supplier { Id = 2, Name = "Supplier B" }
            }.AsQueryable();

            var quotationDbSet = MockHelper.GetMockDbSet(quotations);
            var supplierDbSet = MockHelper.GetMockDbSet(suppliers);

            UnitOfWorkMock.Setup(u => u.Quotation.Query()).Returns(quotationDbSet.Object);
            UnitOfWorkMock.Setup(u => u.Supplier.Query()).Returns(supplierDbSet.Object);


            var result = await _quotationService.GetAllQuotationAsync();


            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data.Count(), Is.EqualTo(2));
            Assert.That(result.Message, Does.Contain("thành công"));
            Assert.That(result.Data.First(q => q.QID == 1).Status, Is.EqualTo(SupplierQuotationStatus.InDate));
            Assert.That(result.Data.First(q => q.QID == 2).Status, Is.EqualTo(SupplierQuotationStatus.OutOfDate));
        }


        [Test]
        public async Task GetAllQuotationAsync_NoData_ShouldReturn404()
        {

            var quotationDbSet = MockHelper.GetMockDbSet(new List<Quotation>().AsQueryable());
            var supplierDbSet = MockHelper.GetMockDbSet(new List<Supplier>().AsQueryable());

            UnitOfWorkMock.Setup(u => u.Quotation.Query()).Returns(quotationDbSet.Object);
            UnitOfWorkMock.Setup(u => u.Supplier.Query()).Returns(supplierDbSet.Object);


            var result = await _quotationService.GetAllQuotationAsync();


            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Does.Contain("Không có báo giá"));
        }



        [Test]
        public async Task GetAllQuotationsWithActiveDateAsync_ValidData_ShouldReturnSuccess()
        {

            var now = DateTime.Now;
            var quotations = new List<Quotation>
            {
                new Quotation { QID = 1, SupplierID = 1, SendDate = now.AddDays(-1), QuotationExpiredDate = now.AddDays(1) },
                new Quotation { QID = 2, SupplierID = 2, SendDate = now.AddDays(-3), QuotationExpiredDate = now.AddDays(-2) }
            }.AsQueryable();

            var suppliers = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "NCC A" },
                new Supplier { Id = 2, Name = "NCC B" }
            }.AsQueryable();

            var quotationDbSet = MockHelper.GetMockDbSet(quotations);
            var supplierDbSet = MockHelper.GetMockDbSet(suppliers);

            UnitOfWorkMock.Setup(u => u.Quotation.Query()).Returns(quotationDbSet.Object);
            UnitOfWorkMock.Setup(u => u.Supplier.Query()).Returns(supplierDbSet.Object);


            var result = await _quotationService.GetAllQuotationsWithActiveDateAsync();


            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data.Count, Is.EqualTo(2));
            Assert.That(result.Data.First(q => q.QID == 1).Status, Is.EqualTo(SupplierQuotationStatus.InDate));
            Assert.That(result.Data.First(q => q.QID == 2).Status, Is.EqualTo(SupplierQuotationStatus.OutOfDate));
        }


        [Test]
        public async Task GetQuotationByIdAsync_ValidId_ShouldReturnSuccess()
        {

            var now = DateTime.Now;

            var quotation = new Quotation
            {
                QID = 100,
                SupplierID = 1,
                SendDate = now.AddDays(-1),
                QuotationExpiredDate = now.AddDays(3),
                QuotationDetails = new List<QuotationDetail>
        {
            new QuotationDetail
            {
                ProductID = 1,
                ProductName = "Paracetamol",
                ProductDescription = "Thuốc giảm đau",
                ProductUnit = "Hộp",
                UnitPrice = 20000,
                ProductDate = now,
                QID = 100,
                Tax=5
            }
        }
            };

            var supplier = new Supplier { Id = 1, Name = "Supplier A" };

            var quotationDbSet = MockHelper.GetMockDbSet(new List<Quotation> { quotation }.AsQueryable());
            var supplierDbSet = MockHelper.GetMockDbSet(new List<Supplier> { supplier }.AsQueryable());

            UnitOfWorkMock.Setup(u => u.Quotation.Query()).Returns(quotationDbSet.Object);
            UnitOfWorkMock.Setup(u => u.Supplier.Query()).Returns(supplierDbSet.Object);


            var result = await _quotationService.GetQuotationByIdAsync(100);


            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data.QID, Is.EqualTo(100));
            Assert.That(result.Data.SupplierName, Is.EqualTo("Supplier A"));
            Assert.That(result.Data.QuotationDetailDTOs.Count, Is.EqualTo(1));
        }


        [Test]
        public async Task GetQuotationByIdAsync_NotFound_ShouldReturn404()
        {

            var quotationDbSet = MockHelper.GetMockDbSet(new List<Quotation>().AsQueryable());
            var supplierDbSet = MockHelper.GetMockDbSet(new List<Supplier>().AsQueryable());

            UnitOfWorkMock.Setup(u => u.Quotation.Query()).Returns(quotationDbSet.Object);
            UnitOfWorkMock.Setup(u => u.Supplier.Query()).Returns(supplierDbSet.Object);


            var result = await _quotationService.GetQuotationByIdAsync(99);


            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Does.Contain("Không tìm thấy báo giá"));
        }

    }
}
