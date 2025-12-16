using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Moq;
using PMS.API.Services.GRNService;
using PMS.Application.Services.ExternalService;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Identity;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.GRN
{
    [TestFixture]
    public class GRNServiceTests : ServiceTestBase
    {
        private GRNService _grnService;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private Mock<IPdfService> _pdfServiceMock;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();


            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.SetupGet(x => x.WebRootPath).Returns("wwwroot");
            _pdfServiceMock = new Mock<IPdfService>();
            _pdfServiceMock.Setup(p => p.GeneratePdfFromHtml(It.IsAny<string>()))
            .Returns(new byte[] { 1, 2, 3 });

            _grnService = new GRNService(
                UnitOfWorkMock.Object,
                MapperMock.Object,
                _webHostEnvironmentMock.Object,
                _pdfServiceMock.Object
            );
        }


        [Test]
        public async Task GetAllGRN_ReturnsGRNList_Success()
        {
            var grnList = new List<GoodReceiptNote>
    {
        new GoodReceiptNote { GRNID = 1, Source = "Kho A", CreateBy = "user1", Total = 100, CreateDate = DateTime.Now, warehouseID = 1 },
        new GoodReceiptNote { GRNID = 2, Source = "Kho B", CreateBy = "user2", Total = 200, CreateDate = DateTime.Now, warehouseID = 1 }
    };

            var users = new List<User>
    {
        new User { Id = "user1", FullName = "Nguyen Van A" },
        new User { Id = "user2", FullName = "Le Thi B" }
    };

            var warehouseLocations = new List<WarehouseLocation>
    {
        new WarehouseLocation
        {
            Id = 1,
            LocationName = "Kho Khu A",
            Warehouse = new Warehouse { Name = "Tổng Kho", Address="hanoi" }
        }
    };

            UnitOfWorkMock.Setup(u => u.GoodReceiptNote.Query())
                .Returns(MockHelper.MockDbSet(grnList).Object);

            UnitOfWorkMock.Setup(u => u.Users.Query())
                .Returns(MockHelper.MockDbSet(users).Object);

            UnitOfWorkMock.Setup(u => u.WarehouseLocation.Query())
                .Returns(MockHelper.MockDbSet(warehouseLocations).Object);

            var result = await _grnService.GetAllGRN();

            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.Count, Is.EqualTo(2));
            Assert.That(result.Data[0].CreateBy, Is.EqualTo("Nguyen Van A"));
            Assert.That(result.Data[1].CreateBy, Is.EqualTo("Le Thi B"));
        }

        [Test]
        public async Task GetAllGRN_NoGRN_ReturnsEmptyMessage()
        {

            UnitOfWorkMock.Setup(u => u.GoodReceiptNote.Query()).Returns(MockHelper.MockDbSet(new List<GoodReceiptNote>()).Object);

            var result = await _grnService.GetAllGRN();

            Assert.That(result.Success, Is.False);
            Assert.That(result.Data, Is.Null);
            Assert.That(result.Message, Is.EqualTo("Hiện tại không có bất kỳ bản ghi nhập kho nào"));
        }

        [Test]
        public async Task GetGRNDetailAsync_InvalidGRN_Returns404()
        {

            UnitOfWorkMock.Setup(u => u.GoodReceiptNote.Query()).Returns(MockHelper.MockDbSet(new List<GoodReceiptNote>()).Object);


            var result = await _grnService.GetGRNDetailAsync(999);

            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("Không tìm thấy phiếu nhập kho có ID = 999"));
        }


        [Test]
        public async Task GeneratePDFGRNAsync_ValidGRN_ReturnsPdfBytes()
        {
            var grnId = 1;

            var grn = new GoodReceiptNote
            {
                GRNID = grnId,
                CreateBy = "user1",
                POID = 100,
                CreateDate = DateTime.Now,
                Source = "dhg",
                Total = 1000000,
                warehouseID = 10,
                Description = "Ghi chú test",
                PurchasingOrder = new PurchasingOrder
                {
                    POID = 100,
                    OrderDate = DateTime.Today,
                    QID = 204,
                    Quotations = new Quotation
                    {
                        QID = 204,
                        SupplierID = 20,
                        SendDate=DateTime.Today,
                        QuotationExpiredDate= DateTime.Now.AddDays(7),
                    }
                }
            };

            var supplier = new Supplier { Id = 20, Name = "NCC Test" };
            var user = new User { Id = "user1", FullName = "Nguyen Van A" };
            var warehouseLocation = new WarehouseLocation { Id = 10, LocationName = "Kho A", WarehouseId = 5 };
            var warehouse = new Warehouse
            {
                Id = 5,
                Name = "Kho Tổng",
                Address = "Hà Nội",
                WarehouseLocations = new List<WarehouseLocation> { warehouseLocation }
            };

            var detailList = new List<GoodReceiptNoteDetail>
    {
        new GoodReceiptNoteDetail
        {
            GRNDID = 1,
            GRNID = grnId,
            ProductID = 101,
            Quantity = 5,
            UnitPrice = 10,
            Product = new PMS.Core.Domain.Entities.Product
            {
                ProductID = 101,
                ProductName = "SP A",
                Unit = "Hộp",
                MaxQuantity=1000,
                MinQuantity=10,
                Status=true,
                TotalCurrentQuantity=9
            }
        }
    };

           
            UnitOfWorkMock.Setup(u => u.GoodReceiptNote.Query())
                .Returns(MockHelper.MockDbSet(new[] { grn }).Object);

            UnitOfWorkMock.Setup(u => u.Supplier.Query())
                .Returns(MockHelper.MockDbSet(new[] { supplier }).Object);

            UnitOfWorkMock.Setup(u => u.Users.Query())
                .Returns(MockHelper.MockDbSet(new[] { user }).Object);

            UnitOfWorkMock.Setup(u => u.WarehouseLocation.Query())
                .Returns(MockHelper.MockDbSet(new[] { warehouseLocation }).Object);

            UnitOfWorkMock.Setup(u => u.Warehouse.Query())
                .Returns(MockHelper.MockDbSet(new[] { warehouse }).Object);

            UnitOfWorkMock.Setup(u => u.PurchasingOrder.Query())
                .Returns(MockHelper.MockDbSet(new[] { grn.PurchasingOrder }).Object);

            UnitOfWorkMock.Setup(u => u.GoodReceiptNoteDetail.Query())
                .Returns(MockHelper.MockDbSet(detailList).Object);


            UnitOfWorkMock.Setup(u => u.PurchasingOrderDetail.Query())
                .Returns(MockHelper.MockDbSet(new List<PurchasingOrderDetail>
                {
            new PurchasingOrderDetail
            {
                PODID = 1,
                POID = 100,
                ProductID = 101,
                Quantity = 5,
                UnitPrice = 10
            }
                }).Object);


            _pdfServiceMock
                .Setup(p => p.GeneratePdfFromHtml(It.IsAny<string>()))
                .Returns(new byte[] { 1, 2, 3 });

            var result = await _grnService.GeneratePDFGRNAsync(grnId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(3));

            _pdfServiceMock.Verify(
                p => p.GeneratePdfFromHtml(It.Is<string>(html => html.Contains("PHIẾU NHẬP KHO"))),
                Times.Once);
        }


        [Test]
        public async Task CreateGoodReceiptNoteFromPOAsync_PO_NotFound_ShouldReturn404()
        {

            UnitOfWorkMock.Setup(x => x.PurchasingOrder.Query())
                .Returns(new List<PurchasingOrder>().AsQueryable().ToAsyncMockDbSet().Object);

            var result = await _grnService.CreateGoodReceiptNoteFromPOAsync("user1", 1, 10);


            Assert.False(result.Success);
            Assert.AreEqual(404, result.StatusCode);
            Assert.AreEqual("Đơn hàng không tồn tại.", result.Message);
        }


        [Test]
        public async Task CreateGoodReceiptNoteFromPOAsync_Quotation_NotFound_ShouldReturn404()
        {

            var poList = new List<PurchasingOrder>
        {
            new PurchasingOrder { POID = 1, Quotations = null, PurchasingOrderDetails = new List<PurchasingOrderDetail>(), OrderDate=new DateTime(2025, 11, 1), QID=99999 }
        };
            UnitOfWorkMock.Setup(x => x.PurchasingOrder.Query())
                .Returns(poList.AsQueryable().ToAsyncMockDbSet().Object);


            var result = await _grnService.CreateGoodReceiptNoteFromPOAsync("user1", 1, 10);


            Assert.False(result.Success);
            Assert.AreEqual(404, result.StatusCode);
            Assert.AreEqual("Báo giá không tồn tại.", result.Message);
        }

        [Test]
        public async Task CreateGoodReceiptNoteFromPOAsync_Supplier_NotFound_ShouldReturn404()
        {

            var poList = new List<PurchasingOrder>
        {
            new PurchasingOrder
            {
                POID = 1,
                Quotations = new Quotation { SupplierID = 100, QID=204,
                    QuotationExpiredDate= new DateTime(2026, 11, 1), SendDate=new DateTime(2025, 11, 1) },
                PurchasingOrderDetails = new List<PurchasingOrderDetail>(),
                OrderDate=new DateTime(2025, 11, 1),
                QID=204
            }
        };
            UnitOfWorkMock.Setup(x => x.PurchasingOrder.Query())
                .Returns(poList.AsQueryable().ToAsyncMockDbSet().Object);

            UnitOfWorkMock.Setup(x => x.Supplier.Query())
                .Returns(new List<Supplier>().AsQueryable().ToAsyncMockDbSet().Object);


            var result = await _grnService.CreateGoodReceiptNoteFromPOAsync("user1", 1, 10);


            Assert.False(result.Success);
            Assert.AreEqual(404, result.StatusCode);
            Assert.AreEqual("Không tồn tại nhà cung cấp.", result.Message);
        }      
    }
}
