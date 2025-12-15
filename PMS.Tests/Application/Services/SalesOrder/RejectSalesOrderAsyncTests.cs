using Moq;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SalesOrder
{
    [TestFixture]
    public class RejectSalesOrderAsyncTests : ServiceTestBase
    {
        private SalesOrderService _service;
        private Mock<ISalesOrderRepository> _soRepo;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            _soRepo = new Mock<ISalesOrderRepository>();
            UnitOfWorkMock.SetupGet(x => x.SalesOrder).Returns(_soRepo.Object);

            _service = new SalesOrderService(
                UnitOfWorkMock.Object,
                MapperMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<SalesOrderService>>(),
                NotificationServiceMock!.Object,
                Mock.Of<PMS.Application.Services.VNpay.IVnPayService>()
            );
        }

        [Test]
        public async Task NotFound_Returns404()
        {
            _soRepo.Setup(r => r.Query()).Returns(new List<PMS.Core.Domain.Entities.SalesOrder>().ToAsyncQueryable().ToMockDbSet().Object);
            var result = await _service.RejectSalesOrderAsync(new RejectSalesOrderRequestDTO { SalesOrderId = 10, Reason = "r" }, "staff1");
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task NotSendStatus_Returns400()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 11,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5),
                CustomerDebts = new CustomerDebt { Id = 1, SalesOrderId = 11, CustomerId = "cust" }
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.RejectSalesOrderAsync(new RejectSalesOrderRequestDTO { SalesOrderId = 11, Reason = "r" }, "staff1");
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi", result.Message);
        }

        [Test]
        public async Task EmptyReason_Returns400()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 12,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5),
                CustomerDebts = new CustomerDebt { Id = 1, SalesOrderId = 12, CustomerId = "cust" }
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.RejectSalesOrderAsync(new RejectSalesOrderRequestDTO { SalesOrderId = 12, Reason = " " }, "staff1");
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Vui lòng nhập lý do từ chối", result.Message);
        }

        [Test]
        public async Task Success_UpdatesStatusAndDebt_Returns200()
        {
            var debt = new CustomerDebt { Id = 1, SalesOrderId = 13, CustomerId = "cust", DebtAmount = 50, status = CustomerDebtStatus.UnPaid };
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 13,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5),
                CustomerDebts = debt
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.RejectSalesOrderAsync(new RejectSalesOrderRequestDTO { SalesOrderId = 13, Reason = "not acceptable" }, "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.Is<PMS.Core.Domain.Entities.SalesOrder>(x =>
                x.SalesOrderStatus == SalesOrderStatus.Rejected &&
                x.RejectReason == "not acceptable" &&
                x.CustomerDebts.status == CustomerDebtStatus.Disable &&
                x.CustomerDebts.DebtAmount == 0m
            )), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitAsync(), Times.AtLeastOnce);
            NotificationServiceMock!.Verify(n => n.SendNotificationToCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PMS.Core.Domain.Enums.NotificationType>()), Times.Once);
        }

        [Test]
        public async Task CommitThrowsException_Returns500()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 15,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5),
                CustomerDebts = new CustomerDebt { Id = 1, SalesOrderId = 15, CustomerId = "cust" }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            UnitOfWorkMock.Setup(x => x.CommitAsync())
                .ThrowsAsync(new Exception("DB error"));

            var result = await _service.RejectSalesOrderAsync(
                new RejectSalesOrderRequestDTO { SalesOrderId = 15, Reason = "reason" },
                "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(500));
        }

    }
}


