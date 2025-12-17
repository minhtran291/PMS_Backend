using Moq;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.Repositories.CustomerDebtRepo;
//using PMS.Data.Repositories.SalesOrderDetails;
using PMS.Data.Repositories.SalesOrderDetailsRepository;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SalesOrder
{
    [TestFixture]
    public class UpdateDraftQuantitiesAsyncTests : ServiceTestBase
    {
        private SalesOrderService _service;
        private Mock<ISalesOrderRepository> _soRepo;
        private Mock<ISalesOrderDetailsRepository> _sodRepo;
        private Mock<ICustomerDebtRepository> _debtRepo;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            _soRepo = new Mock<ISalesOrderRepository>();
            _sodRepo = new Mock<ISalesOrderDetailsRepository>();
            _debtRepo = new Mock<ICustomerDebtRepository>();

            UnitOfWorkMock.SetupGet(x => x.SalesOrder).Returns(_soRepo.Object);
            UnitOfWorkMock.SetupGet(x => x.SalesOrderDetails).Returns(_sodRepo.Object);
            UnitOfWorkMock.SetupGet(x => x.CustomerDebt).Returns(_debtRepo.Object);

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
        public async Task NullPayload_Returns400()
        {
            var result = await _service.UpdateDraftQuantitiesAsync(null!);
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task InvalidSalesOrderId_Returns400()
        {
            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 0,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO> { new() { ProductId = 1, LotId = 1, Quantity = 1 } }
            });
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task EmptyDetails_Returns400()
        {
            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO>()
            });
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task NegativeQuantity_Returns400()
        {
            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO> { new() { ProductId = 1, LotId = 1, Quantity = -1 } }
            });
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task OrderNotFound_Returns404()
        {
            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder>().ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 5,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO> { new() { ProductId = 1, LotId = 1, Quantity = 1 } }
            });
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task OrderNotDraft_Returns400()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 6,
                SalesOrderStatus = SalesOrderStatus.Approved,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(7),
                SalesOrderDetails = new List<SalesOrderDetails>()
            };
            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 6,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO> { new() { ProductId = 1, LotId = 1, Quantity = 1 } }
            });
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Chỉ được phép sửa đơn hàng ở trạng thái nháp.", result.Message);
        }

        [Test]
        public async Task DraftExpired_Returns400()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 7,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(-1),
                SalesOrderDetails = new List<SalesOrderDetails>()
            };
            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 7,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO> { new() { ProductId = 1, LotId = 1, Quantity = 1 } }
            });
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("đã hết hạn", result.Message);
        }

        [Test]
        public async Task Success_RecalculatesTotal_UpdatesDebt_Returns200()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 8,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3),
                PaidAmount = 0m,
                SalesOrderDetails = new List<SalesOrderDetails>
                {
                    new() { SalesOrderId = 8, LotId = 1, Quantity = 2, UnitPrice = 10m, SubTotalPrice = 20m },
                    new() { SalesOrderId = 8, LotId = 2, Quantity = 1, UnitPrice = 30m, SubTotalPrice = 30m }
                }
            };

            var debt = new CustomerDebt
            {
                Id = 1,
                SalesOrderId = 8,
                CustomerId = "customer1",
                DebtAmount = 0m,
                status = CustomerDebtStatus.UnPaid
            };

            _soRepo.Setup(r => r.Query()).Returns(new List<Core.Domain.Entities.SalesOrder> { so }.ToAsyncQueryable().ToMockDbSet().Object);
            _debtRepo.Setup(r => r.Query()).Returns(new List<CustomerDebt> { debt }.ToAsyncQueryable().ToMockDbSet().Object);

            var result = await _service.UpdateDraftQuantitiesAsync(new SalesOrderUpdateDTO
            {
                SalesOrderId = 8,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO>
                {
                    new() { ProductId = 1, LotId = 1, Quantity = 5 },
                    new() { ProductId = 2, LotId = 2, Quantity = 1 }
                }
            });

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.IsAny<Core.Domain.Entities.SalesOrder>()), Times.Once);
            _debtRepo.Verify(r => r.Update(It.IsAny<CustomerDebt>()), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Test]
        public async Task NoCustomerDebt_StillReturns200_AndDoesNotUpdateDebt()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 9,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3),
                PaidAmount = 0m,
                SalesOrderDetails = new List<SalesOrderDetails>
            {
                new() { SalesOrderId = 9, LotId = 1, Quantity = 2, UnitPrice = 10m, SubTotalPrice = 20m }
            }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new List<Core.Domain.Entities.SalesOrder> { so }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            // Không có CustomerDebt nào
            _debtRepo.Setup(r => r.Query())
                .Returns(new List<CustomerDebt>().ToAsyncQueryable().ToMockDbSet().Object);

            var dto = new SalesOrderUpdateDTO
            {
                SalesOrderId = 9,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO>
            {
                new() { ProductId = 1, LotId = 1, Quantity = 2 }
            }
            };

            var result = await _service.UpdateDraftQuantitiesAsync(dto);

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.IsAny<Core.Domain.Entities.SalesOrder>()), Times.Once);
            _debtRepo.Verify(r => r.Update(It.IsAny<CustomerDebt>()), Times.Never);
        }

        [Test]
        public async Task CommitThrowsException_Returns500_AndRollbacks()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 10,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3),
                PaidAmount = 0m,
                SalesOrderDetails = new List<SalesOrderDetails>
            {
                new() { SalesOrderId = 10, LotId = 1, Quantity = 1, UnitPrice = 10m, SubTotalPrice = 10m }
            }
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new List<Core.Domain.Entities.SalesOrder> { so }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            _debtRepo.Setup(r => r.Query())
                .Returns(new List<CustomerDebt>().ToAsyncQueryable().ToMockDbSet().Object);

            // Ép CommitAsync ném lỗi
            UnitOfWorkMock.Setup(x => x.CommitAsync())
                .ThrowsAsync(new Exception("DB commit failed"));

            var dto = new SalesOrderUpdateDTO
            {
                SalesOrderId = 10,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO>
            {
                new() { ProductId = 1, LotId = 1, Quantity = 2 }
            }
            };

            var result = await _service.UpdateDraftQuantitiesAsync(dto);

            Assert.That(result.StatusCode, Is.EqualTo(500));
            StringAssert.Contains("Có lỗi xảy ra khi cập nhật số lượng đơn nháp", result.Message);
            UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Test]
        public async Task QuantityZero_IsAccepted_Returns200()
        {
            var so = new Core.Domain.Entities.SalesOrder
            {
                SalesOrderId = 11,
                SalesOrderStatus = SalesOrderStatus.Draft,
                CreateBy = "customer1",
                SalesOrderExpiredDate = DateTime.Today.AddDays(3),
                PaidAmount = 0m,
                SalesOrderDetails = new List<SalesOrderDetails>
        {
            new() { SalesOrderId = 11, LotId = 1, Quantity = 2, UnitPrice = 10m, SubTotalPrice = 20m }
        }
            };

            var debt = new CustomerDebt
            {
                Id = 2,
                SalesOrderId = 11,
                CustomerId = "customer1",
                DebtAmount = 0m,
                status = CustomerDebtStatus.UnPaid
            };

            _soRepo.Setup(r => r.Query())
                .Returns(new List<Core.Domain.Entities.SalesOrder> { so }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            _debtRepo.Setup(r => r.Query())
                .Returns(new List<CustomerDebt> { debt }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            var dto = new SalesOrderUpdateDTO
            {
                SalesOrderId = 11,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsUpdateDTO>
        {
            new() { ProductId = 1, LotId = 1, Quantity = 0 } 
        }
            };

            var result = await _service.UpdateDraftQuantitiesAsync(dto);

            Assert.That(result.StatusCode, Is.EqualTo(200));
            _soRepo.Verify(r => r.Update(It.IsAny<Core.Domain.Entities.SalesOrder>()), Times.Once);
            _debtRepo.Verify(r => r.Update(It.IsAny<CustomerDebt>()), Times.Once);
        }


    }
}


