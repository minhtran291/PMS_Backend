using Moq;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
//using PMS.Data.Repositories.SalesOrderDetails;
using PMS.Data.Repositories.SalesOrderDetailsRepository;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SalesOrder
{
    [TestFixture]
    public class DeleteDraftAsyncTests : ServiceTestBase
    {
        private SalesOrderService _service;
        private Mock<ISalesOrderRepository> _soRepo;
        private Mock<ISalesOrderDetailsRepository> _sodRepo;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            _soRepo = new Mock<ISalesOrderRepository>();
            _sodRepo = new Mock<ISalesOrderDetailsRepository>();

            UnitOfWorkMock.SetupGet(x => x.SalesOrder).Returns(_soRepo.Object);
            UnitOfWorkMock.SetupGet(x => x.SalesOrderDetails).Returns(_sodRepo.Object);

            UnitOfWorkMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);

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
            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder>().ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.DeleteDraftAsync(1);
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task NotDraft_Returns400()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 7,
                SalesOrderStatus = SalesOrderStatus.Send,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(7)
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.DeleteDraftAsync(7);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Chỉ được xoá đơn ở trạng thái nháp", result.Message);
        }

        [Test]
        public async Task DraftWithDetails_RemovesDetailsAndOrder_Returns200()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 9,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(7),
                SalesOrderDetails = new List<SalesOrderDetails>
                {
                    new() { SalesOrderId = 9, LotId = 1, Quantity = 1, UnitPrice = 10, SubTotalPrice = 10 },
                    new() { SalesOrderId = 9, LotId = 2, Quantity = 2, UnitPrice = 5, SubTotalPrice = 10 }
                }
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.DeleteDraftAsync(9);

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _sodRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<SalesOrderDetails>>()), Times.Once);
            _soRepo.Verify(r => r.Remove(It.IsAny<Core.Domain.Entities.SalesOrder>()), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Test]
        public async Task DraftWithoutDetails_RemovesOnlyOrder_Returns200()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 10,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(7),
                SalesOrderDetails = new List<SalesOrderDetails>() 
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.DeleteDraftAsync(10);

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _sodRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<SalesOrderDetails>>()), Times.Never);
            _soRepo.Verify(r => r.Remove(It.IsAny<PMS.Core.Domain.Entities.SalesOrder>()), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Test]
        public async Task CommitThrowsException_Returns500_AndRollbacks()
        {
            var so = new PMS.Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 11,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(7),
                SalesOrderDetails = new List<SalesOrderDetails>()
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new List<PMS.Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            // Ép CommitAsync ném exception
            UnitOfWorkMock.Setup(x => x.CommitAsync())
                .ThrowsAsync(new Exception("DB error"));

            var result = await _service.DeleteDraftAsync(11);

            Assert.That(result.StatusCode, Is.EqualTo(500));
            StringAssert.Contains("xoá bản nháp đơn hàng", result.Message);
            UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }


    }
}


