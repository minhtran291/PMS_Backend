using Moq;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Enums;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SalesOrder
{
    [TestFixture]
    public class ApproveSalesOrderAsyncTests : ServiceTestBase
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

            var result = await _service.ApproveSalesOrderAsync(1, "staff1");
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task NotSendStatus_Returns400()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 2,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5)
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.ApproveSalesOrderAsync(2, "staff1");
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi", result.Message);
        }

        [Test]
        public async Task Success_UpdatesStatusToApproved_Returns200()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 3,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5)
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.ApproveSalesOrderAsync(3, "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.Is<PMS.Core.Domain.Entities.SalesOrder>(x => x.SalesOrderStatus == SalesOrderStatus.Approved)), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitAsync(), Times.AtLeastOnce);
            NotificationServiceMock!.Verify(n => n.SendNotificationToCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PMS.Core.Domain.Enums.NotificationType>()), Times.Once);
        }

        [Test]
        public async Task ExpiredOrder_Returns400()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 4,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(-1) // Hết hạn
            };

            _soRepo.Setup(r => r.Query())
                   .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }
                            .ToAsyncQueryable()
                            .ToMockDbSet().Object);

            var result = await _service.ApproveSalesOrderAsync(4, "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("hết hạn", result.Message, "Message phải báo hết hạn đơn hàng");
        }

        [Test]
        public async Task DifferentStaffApprovesOrder_Success()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 5,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "anotherCustomer",
                SalesOrderExpiredDate = DateTime.Today.AddDays(5)
            };

            _soRepo.Setup(r => r.Query())
                   .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }
                            .ToAsyncQueryable()
                            .ToMockDbSet().Object);

            var result = await _service.ApproveSalesOrderAsync(5, "staffA");

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.Is<PMS.Core.Domain.Entities.SalesOrder>(x => x.SalesOrderStatus == SalesOrderStatus.Approved)), Times.Once);
        }

        [Test]
        public async Task AlreadyApproved_Returns400()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 6,
                SalesOrderStatus = SalesOrderStatus.Approved,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3)
            };

            _soRepo.Setup(r => r.Query())
                   .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }
                            .ToAsyncQueryable()
                            .ToMockDbSet().Object);

            var result = await _service.ApproveSalesOrderAsync(6, "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi!", result.Message);
        }

        [Test]
        public async Task AlreadyRejected_Returns400()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 7,
                SalesOrderStatus = SalesOrderStatus.Rejected,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3)
            };

            _soRepo.Setup(r => r.Query())
                   .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }
                            .ToAsyncQueryable()
                            .ToMockDbSet().Object);

            var result = await _service.ApproveSalesOrderAsync(7, "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Chỉ được chấp nhận hoặc từ chối đơn hàng có trạng thái đã gửi!", result.Message);
        }

        [Test]
        public async Task NotificationFailsButOrderStillApproved()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 8,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3)
            };

            _soRepo.Setup(r => r.Query())
                   .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }
                            .ToAsyncQueryable()
                            .ToMockDbSet().Object);

            NotificationServiceMock!
                .Setup(n => n.SendNotificationToCustomerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>()))
                .ThrowsAsync(new Exception("Notification error"));

            var result = await _service.ApproveSalesOrderAsync(8, "staff1");

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.Is<PMS.Core.Domain.Entities.SalesOrder>(x => x.SalesOrderStatus == SalesOrderStatus.Approved)), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitAsync(), Times.AtLeastOnce);
        }

    }
}


