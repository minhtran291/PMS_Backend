using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.Application.DTOs.StockExportOrder;
using PMS.Application.Services.Notification;
using PMS.Application.Services.StockExportOrder;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.Repositories.StockExportOrder;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Tests.Application.Services.StockExportOrderTest
{
    public class CreateStockExportOrderTest : ServiceTestBase
    {
        private Mock<IUnitOfWork> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<StockExportOrderService>> _loggerMock;
        private Mock<INotificationService> _notifyMock;

        private Mock<IStockExportOrderRepository> _seoRepo;
        private Mock<ISalesOrderRepository> _soRepo;

        private StockExportOrderService _service;

        [SetUp]
        public void SetUp()
        {
            base.BaseSetup();

            _uowMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<StockExportOrderService>>();
            _notifyMock = new Mock<INotificationService>();

            _seoRepo = new Mock<IStockExportOrderRepository>();
            _soRepo = new Mock<ISalesOrderRepository>();

            _uowMock.Setup(u => u.StockExportOrder)
                .Returns(_seoRepo.Object);

            _uowMock.Setup(u => u.SalesOrder)
                .Returns(_soRepo.Object);

            _service = new StockExportOrderService(
                _uowMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _notifyMock.Object
                );
        }


        [Test]
        public async Task CreateStockExportOrder_Should_Returns_Error_When_Sales_Order_Not_Found()
        {
            var userId = "user123";
            var createUser = "user456";

            var details = new List<StockExportOrderDetailsDTO>()
            {
                new StockExportOrderDetailsDTO(){LotId = 1, Quantity = 100},
                new StockExportOrderDetailsDTO(){LotId = 2, Quantity = 50}
            };

            var seo = new StockExportOrderDTO()
            {
                SalesOrderId = -1,
                DueDate = new DateTime(2025, 11, 13),
                Details = details,
            };

            var so = new Core.Domain.Entities.SalesOrder()
            {
                SalesOrderId = 1,
                IsDeposited = true,
                CreateBy = createUser,
                SalesOrderExpiredDate = new DateTime(2025, 12, 30),
                SalesOrderDetails = new List<Core.Domain.Entities.SalesOrderDetails>()
                {
                    new Core.Domain.Entities.SalesOrderDetails(){LotId = 1, Quantity = 100},
                    new Core.Domain.Entities.SalesOrderDetails(){ LotId = 2, Quantity = 50},
                }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new[] { so }.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(seo, userId);

            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("Không tìm thấy đơn hàng mua"));
        }

        [Test]
        public async Task CreateStockExportOrder_Should_Returns_Error_When_DueDate_Invalid()
        {
            var userId = "user123";
            var createUser = "user456";

            var details = new List<StockExportOrderDetailsDTO>()
            {
                new StockExportOrderDetailsDTO(){LotId = 1, Quantity = 100},
                new StockExportOrderDetailsDTO(){LotId = 2, Quantity = 50}
            };

            var seo = new StockExportOrderDTO()
            {
                SalesOrderId = 1,
                DueDate = new DateTime(2025, 11, 13),
                Details = details,
            };

            var so = new Core.Domain.Entities.SalesOrder()
            {
                SalesOrderId = 1,
                IsDeposited = true,
                CreateBy = createUser,
                SalesOrderExpiredDate = new DateTime(2025, 12, 30),
                SalesOrderDetails = new List<Core.Domain.Entities.SalesOrderDetails>()
                {
                    new Core.Domain.Entities.SalesOrderDetails(){LotId = 1, Quantity = 100},
                    new Core.Domain.Entities.SalesOrderDetails(){ LotId = 2, Quantity = 50},
                }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new[] { so }.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(seo, userId);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Thời hạn yêu cầu xuất kho không được nhỏ hơn hôm nay"));
        }

        [Test]
        public async Task CreateStockExportOrder_Should_Returns_Error_When_Details_Empty()
        {
            var userId = "user123";
            var createUser = "user456";

            var details = new List<StockExportOrderDetailsDTO>()
            {
                new StockExportOrderDetailsDTO(){LotId = 1, Quantity = 100},
                new StockExportOrderDetailsDTO(){LotId = 2, Quantity = 50}
            };

            var seo = new StockExportOrderDTO()
            {
                SalesOrderId = 1,
                DueDate = new DateTime(2025, 12, 30),
                Details = new List<StockExportOrderDetailsDTO>(),
            };

            var so = new Core.Domain.Entities.SalesOrder()
            {
                SalesOrderId = 1,
                IsDeposited = true,
                CreateBy = createUser,
                SalesOrderExpiredDate = new DateTime(2025, 12, 30),
                SalesOrderDetails = new List<Core.Domain.Entities.SalesOrderDetails>()
                {
                    new Core.Domain.Entities.SalesOrderDetails(){LotId = 1, Quantity = 100},
                    new Core.Domain.Entities.SalesOrderDetails(){ LotId = 2, Quantity = 50},
                }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new[] { so }.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(seo, userId);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Chi tiết lệnh yêu cầu xuất kho phải có ít nhất 1 sản phẩm"));
        }

        [Test]
        public async Task CreateStockExportOrder_Should_Returns_Error_When_Details_Have_LotId_Not_Existing()
        {
            var userId = "user123";
            var createUser = "user456";

            var details = new List<StockExportOrderDetailsDTO>()
            {
                new StockExportOrderDetailsDTO(){LotId = 0, Quantity = 100},
            };

            var seo = new StockExportOrderDTO()
            {
                SalesOrderId = 1,
                DueDate = new DateTime(2025, 12, 30),
                Details = details,
            };

            var so = new Core.Domain.Entities.SalesOrder()
            {
                SalesOrderId = 1,
                IsDeposited = true,
                CreateBy = createUser,
                SalesOrderExpiredDate = new DateTime(2025, 12, 30),
                SalesOrderDetails = new List<Core.Domain.Entities.SalesOrderDetails>()
                {
                    new Core.Domain.Entities.SalesOrderDetails(){LotId = 1, Quantity = 100},
                    new Core.Domain.Entities.SalesOrderDetails(){ LotId = 2, Quantity = 50},
                }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new[] { so }.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(seo, userId);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Có lô không tồn tại hoặc không thuộc phạm vi của đơn hàng"));
        }

        [Test]
        public async Task CreateStockExportOrder_Should_Returns_Success()
        {
            var userId = "user123";
            var createUser = "user456";

            var details = new List<StockExportOrderDetailsDTO>()
            {
                new StockExportOrderDetailsDTO(){LotId = 1, Quantity = 100},
                new StockExportOrderDetailsDTO(){LotId = 2, Quantity = 50},
            };

            var seo = new StockExportOrderDTO()
            {
                SalesOrderId = 1,
                DueDate = new DateTime(2025, 12, 30),
                Details = details,
            };

            var so = new Core.Domain.Entities.SalesOrder()
            {
                SalesOrderId = 1,
                IsDeposited = true,
                CreateBy = createUser,
                SalesOrderExpiredDate = new DateTime(2025, 12, 30),
                SalesOrderDetails = new List<Core.Domain.Entities.SalesOrderDetails>()
                {
                    new Core.Domain.Entities.SalesOrderDetails(){LotId = 1, Quantity = 100},
                    new Core.Domain.Entities.SalesOrderDetails(){ LotId = 2, Quantity = 50},
                }
            };

            var seoInDB = new List<Core.Domain.Entities.StockExportOrder>();

            _soRepo.Setup(r => r.Query())
                .Returns(new[] { so }.AsQueryable().ToMockDbSet().Object);

            _seoRepo.Setup(r => r.Query())
                .Returns(seoInDB.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateAsync(seo, userId);

            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.Message, Is.EqualTo("Tạo lệnh yêu cầu xuất kho thành công"));
        }
    }
}
