using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.Application.DTOs.GoodsIssueNote;
using PMS.Application.Services.GoodsIssueNote;
using PMS.Application.Services.Notification;
using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.GoodsIssueNote;
using PMS.Data.Repositories.LotProductRepository;
using PMS.Data.Repositories.StockExportOrder;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Tests.Application.Services.GoodsIssueNoteTest
{
    public class CreateGoodsIssueNote : ServiceTestBase
    {
        private Mock<IUnitOfWork> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<GoodsIssueNoteService>> _loggerMock;
        private Mock<INotificationService> _notifyMock;

        private Mock<IGoodsIssueNoteRepository> _ginRepo;
        private Mock<IStockExportOrderRepository> _seoRepo;
        private Mock<ILotProductRepository> _lotRepo;

        private GoodsIssueNoteService _service;

        [SetUp]
        public void SetUp()
        {
            base.BaseSetup();

            _uowMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GoodsIssueNoteService>>();
            _notifyMock = new Mock<INotificationService>();

            _ginRepo = new Mock<IGoodsIssueNoteRepository>();
            _seoRepo = new Mock<IStockExportOrderRepository>();
            _lotRepo = new Mock<ILotProductRepository>();

            _uowMock.Setup(u => u.GoodsIssueNote)
                .Returns(_ginRepo.Object);

            _uowMock.Setup(u => u.StockExportOrder)
                .Returns(_seoRepo.Object);

            _uowMock.Setup(u => u.LotProduct)
                .Returns(_lotRepo.Object);

            _service = new GoodsIssueNoteService(
                _uowMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _notifyMock.Object
                );
        }

        [Test]
        public async Task CreateGoodsIssueNote_Should_Return_Error_When_seoId_Not_Found()
        {
            string userId = "user123";

            var seo = new Core.Domain.Entities.StockExportOrder
            {
                Id = 1,
                StockExportOrderCode = "SEO123",
                CreateBy = "Sales01",
                Status = Core.Domain.Enums.StockExportOrderStatus.Sent,
                StockExportOrderDetails =
                {
                    new Core.Domain.Entities.StockExportOrderDetails {LotId = 1, Quantity = 50},
                    new Core.Domain.Entities.StockExportOrderDetails {LotId = 2, Quantity = 100},
                }
            };

            var dto = new CreateGoodsIssueNoteDTO
            {
                StockExportOrderId = -1,
            };

            var ginList = new List<Core.Domain.Entities.GoodsIssueNote>();

            _seoRepo.Setup(r => r.Query())
                .Returns(new[] { seo }.AsQueryable().ToMockDbSet().Object);

            _ginRepo.Setup(r => r.Query())
                .Returns(ginList.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(dto, userId);

            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("Lệnh xuất kho không tồn tại"));
        }

        [Test]
        public async Task CreateGoodsIssueNote_Should_Return_Error_When_seoId_Not_Existing()
        {
            string userId = "user123";

            var seo = new Core.Domain.Entities.StockExportOrder
            {
                Id = 1,
                StockExportOrderCode = "SEO123",
                CreateBy = "Sales01",
                Status = Core.Domain.Enums.StockExportOrderStatus.Sent,
                StockExportOrderDetails =
                {
                    new Core.Domain.Entities.StockExportOrderDetails {LotId = 1, Quantity = 50},
                    new Core.Domain.Entities.StockExportOrderDetails {LotId = 2, Quantity = 100},
                }
            };

            var dto = new CreateGoodsIssueNoteDTO
            {
                StockExportOrderId = 0,
            };

            var ginList = new List<Core.Domain.Entities.GoodsIssueNote>();

            _seoRepo.Setup(r => r.Query())
                .Returns(new[] { seo }.AsQueryable().ToMockDbSet().Object);

            _ginRepo.Setup(r => r.Query())
                .Returns(ginList.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(dto, userId);

            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("Lệnh xuất kho không tồn tại"));
        }

        [Test]
        public async Task CreateGoodsIssueNote_Should_Return_Success()
        {
            string userId = "user123";

            var warehouse = new Warehouse
            {
                Id = 1,
                Name = "Kho A",
                Address = "Ha Noi"
            };

            var location = new WarehouseLocation
            {
                Id = 1,
                Warehouse = warehouse,
                WarehouseId = warehouse.Id,
                LocationName = "Khu thuoc A1"
            };

            var product = new Core.Domain.Entities.Product
            {
                ProductID = 1,
                ProductName = "Paracetamol",
                Unit = "Lọ",
                MinQuantity = 10,
                MaxQuantity = 100,
                TotalCurrentQuantity = 50,
                Status = true,
            };

            var lotInDB = new List<LotProduct>
            {
                new LotProduct
                {
                    LotID = 1,
                    LotQuantity = 50,
                    SalePrice = 10000,
                    InputPrice = 9000,
                    ExpiredDate = new DateTime(2025, 12, 30),
                    SupplierID = 1,
                    ProductID = 1,
                    Product = product,
                    WarehouseLocation = location
                },
                new LotProduct
                {
                    LotID = 2,
                    LotQuantity = 100,
                    SalePrice = 15000,
                    InputPrice = 13000,
                    ExpiredDate = new DateTime(2025, 12, 30),
                    SupplierID = 1,
                    ProductID = 1,
                    Product = product,
                    WarehouseLocation = location
                }
            };

            var seo = new Core.Domain.Entities.StockExportOrder
            {
                Id = 1,
                StockExportOrderCode = "SEO123",
                CreateBy = "Sales01",
                Status = Core.Domain.Enums.StockExportOrderStatus.Sent,
                StockExportOrderDetails =
                {
                    new StockExportOrderDetails
                    {
                        LotId = 1,
                        Quantity = 50,
                        LotProduct = lotInDB[0]
                    },
                    new StockExportOrderDetails
                    {
                        LotId = 2,
                        Quantity = 100,
                        LotProduct = lotInDB[1]
                    }
                }
            };

            var dto = new CreateGoodsIssueNoteDTO
            {
                StockExportOrderId = 1,
            };

            var ginList = new List<Core.Domain.Entities.GoodsIssueNote>();

            _seoRepo.Setup(r => r.Query())
                .Returns(new[] { seo }.AsQueryable().ToMockDbSet().Object);

            _ginRepo.Setup(r => r.Query())
                .Returns(ginList.AsQueryable().ToMockDbSet().Object);

            _lotRepo.Setup(r => r.Query())
                .Returns(lotInDB.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(dto, userId);

            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.Message, Is.EqualTo("Tạo phiếu xuất kho thành công"));
        }
    }
}
