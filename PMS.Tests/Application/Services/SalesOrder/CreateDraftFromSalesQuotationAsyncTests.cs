using Moq;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.Services.SalesOrder;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.Repositories.SalesOrderDetailsRepository;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.Repositories.SalesQuotation;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SalesOrder
{
    [TestFixture]
    public class CreateDraftFromSalesQuotationAsyncTests : ServiceTestBase
    {
        private SalesOrderService _service;
        private Mock<ISalesQuotationRepository> _sqRepo;
        private Mock<ISalesOrderRepository> _soRepo;
        private Mock<ISalesOrderDetailsRepository> _sodRepo;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            _sqRepo = new Mock<ISalesQuotationRepository>();
            _soRepo = new Mock<ISalesOrderRepository>();
            _sodRepo = new Mock<ISalesOrderDetailsRepository>();

            UnitOfWorkMock.SetupGet(x => x.SalesQuotation).Returns(_sqRepo.Object);
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
        public async Task MissingCreateBy_Returns400()
        {
            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 1,
                CreateBy = " ",
                Details = new List<SalesOrderDetailsRequestDTO> { new() { LotId = 1, Quantity = 1 } }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Người tạo đơn hàng là bắt buộc", result.Message);
        }

        [Test]
        public async Task EmptyDetails_Returns400()
        {
            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO>()
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Danh sách chi tiết trống", result.Message);
        }

        [Test]
        public async Task SalesQuotationNotFound_Returns404()
        {
            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 999,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO> { new() { LotId = 1, Quantity = 1 } }
            };

            var empty = new List<SalesQuotation>().ToAsyncQueryable();
            _sqRepo.Setup(r => r.Query()).Returns(empty.ToMockDbSet().Object);

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task SalesQuotationExpired_Returns400()
        {
            var sq = Mock.Of<SalesQuotation>(q =>
               q.Id == 1 &&
               q.ExpiredDate == DateTime.Now.Date.AddDays(-1) &&
               q.SalesQuotaionDetails == new List<SalesQuotaionDetails>()
           );

            _sqRepo.Setup(r => r.Query())
                .Returns(new List<SalesQuotation> { sq }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO> { new() { LotId = 1, Quantity = 1 } }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Báo giá đã hết hạn", result.Message);
        }

        [Test]
        public async Task NegativeQuantity_Returns400()
        {
            // minimal SQ detail so it passes "lot belongs to quotation" check
            var sqDetail = Mock.Of<SalesQuotaionDetails>(d =>
                d.LotId == 1 &&
                d.Product == Mock.Of<PMS.Core.Domain.Entities.Product>()
            );

            var sq = Mock.Of<SalesQuotation>(q =>
                q.Id == 1 &&
                q.ExpiredDate == DateTime.Now.Date.AddDays(3) &&
                q.SalesQuotaionDetails == new List<SalesQuotaionDetails> { sqDetail }
            );

            _sqRepo.Setup(r => r.Query())
                .Returns(new List<SalesQuotation> { sq }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO> { new() { LotId = 1, Quantity = -5 } }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("số lượng không hợp lệ", result.Message);
        }

        [Test]
        public async Task DetailLotNotInQuotation_Returns400()
        {
            var sqDetail = Mock.Of<SalesQuotaionDetails>(d =>
               d.LotId == 1 &&
               d.Product == Mock.Of<PMS.Core.Domain.Entities.Product>()
           );

            var sq = Mock.Of<SalesQuotation>(q =>
                q.Id == 1 &&
                q.ExpiredDate == DateTime.Now.Date.AddDays(3) &&
                q.SalesQuotaionDetails == new List<SalesQuotaionDetails> { sqDetail }
            );

            _sqRepo.Setup(r => r.Query())
                .Returns(new List<SalesQuotation> { sq }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO> { new() { LotId = 999, Quantity = 1 } }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            StringAssert.Contains("Có sản phẩm không thuộc báo giá hiện tại của bạn, vui lòng kiểm tra lại!", result.Message);
        }

        [Test]
        public async Task ValidRequest_CreatesDraftAndDetails_Returns201()
        {
            // Lot 11: SalePrice=100, VAT=10%
            var lot1 = Mock.Of<LotProduct>(lp =>
                lp.LotID == 11 &&
                lp.SalePrice == 100m &&
                lp.LotQuantity == 100 &&
                lp.Product == Mock.Of<PMS.Core.Domain.Entities.Product>()
            );
            var tax10 = Mock.Of<TaxPolicy>(t => t.Id == 1 && t.Rate == 0.10m);

            // Lot 22: SalePrice=50, VAT=null/0%
            var lot2 = Mock.Of<LotProduct>(lp =>
                lp.LotID == 22 &&
                lp.SalePrice == 50m &&
                lp.LotQuantity == 200 &&
                lp.Product == Mock.Of<PMS.Core.Domain.Entities.Product>()
            );

            var d1 = Mock.Of<SalesQuotaionDetails>(d =>
                d.Id == 1 &&
                d.LotId == 11 &&
                d.LotProduct == lot1 &&
                d.Product == lot1.Product &&
                d.TaxPolicy == tax10 &&
                d.SqId == 5
            );

            var d2 = Mock.Of<SalesQuotaionDetails>(d =>
                d.Id == 2 &&
                d.LotId == 22 &&
                d.LotProduct == lot2 &&
                d.Product == lot2.Product &&
                d.TaxPolicy == null &&
                d.SqId == 5
            );

            var sq = Mock.Of<SalesQuotation>(q =>
                q.Id == 5 &&
                q.ExpiredDate == DateTime.Now.Date.AddDays(5) &&
                q.DepositPercent == 0.2m &&
                q.SalesQuotaionDetails == new List<SalesQuotaionDetails> { d1, d2 }
            );

            _sqRepo.Setup(r => r.Query())
                .Returns(new List<SalesQuotation> { sq }
                    .ToAsyncQueryable()
                    .ToMockDbSet().Object);

            _soRepo.Setup(r => r.AddAsync(It.IsAny<PMS.Core.Domain.Entities.SalesOrder>()))
                .Returns(Task.CompletedTask);

            _sodRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<SalesOrderDetails>>()))
                .Returns(Task.CompletedTask);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 5,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new() { LotId = 11, Quantity = 2 },
                    new() { LotId = 22, Quantity = 3 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.Data, Is.Not.Null);

            _soRepo.Verify(r => r.AddAsync(It.IsAny<PMS.Core.Domain.Entities.SalesOrder>()), Times.Once);
            _sodRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<SalesOrderDetails>>()), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Test]
        public async Task QuantityZero_ValidDetail_Returns201()
        {
            var sqDetails = new List<SalesQuotaionDetails>
            {
                Mock.Of<SalesQuotaionDetails>(d =>
                    d.LotId == 1 &&
                    d.LotProduct == Mock.Of<LotProduct>(lp =>
                        lp.SalePrice == 100 &&
                        lp.Product == Mock.Of<PMS.Core.Domain.Entities.Product>() &&
                        lp.LotQuantity == 100
                    ) &&
                    d.TaxPolicy == null
                )
            };

            var sq = Mock.Of<SalesQuotation>(q =>
                q.Id == 1 &&
                q.ExpiredDate == DateTime.Now.Date.AddDays(3) &&
                q.SalesQuotaionDetails == sqDetails
            );

            _sqRepo.Setup(r => r.Query())
                .Returns(new List<SalesQuotation> { sq }
                .ToAsyncQueryable()
                .ToMockDbSet().Object);

            _soRepo.Setup(r => r.AddAsync(It.IsAny<PMS.Core.Domain.Entities.SalesOrder>()))
                .Returns(Task.CompletedTask);

            _sodRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<SalesOrderDetails>>()))
                .Returns(Task.CompletedTask);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 1,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new() { LotId = 1, Quantity = 0 } 
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(201));
            _soRepo.Verify(r => r.AddAsync(It.IsAny<PMS.Core.Domain.Entities.SalesOrder>()), Times.Once);
            _sodRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<SalesOrderDetails>>()), Times.Once);
        }

        [Test]
        public async Task MissingLotProductProduct_StillCreates_Returns201()
        {
            var lot = Mock.Of<LotProduct>(lp =>
                lp.SalePrice == 50 &&
                lp.Product == null &&         
                lp.LotQuantity == 50
            );

            var detail = Mock.Of<SalesQuotaionDetails>(d =>
                d.LotId == 20 &&
                d.LotProduct == lot &&
                d.TaxPolicy == null
            );

            var sq = Mock.Of<SalesQuotation>(q =>
                q.Id == 2 &&
                q.ExpiredDate == DateTime.Now.AddDays(3) &&
                q.SalesQuotaionDetails == new List<SalesQuotaionDetails> { detail }
            );

            _sqRepo.Setup(r => r.Query())
                .Returns(new List<SalesQuotation> { sq }
                .ToAsyncQueryable()
                .ToMockDbSet().Object);

            _soRepo.Setup(r => r.AddAsync(It.IsAny<PMS.Core.Domain.Entities.SalesOrder>())).Returns(Task.CompletedTask);
            _sodRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<SalesOrderDetails>>())).Returns(Task.CompletedTask);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 2,
                CreateBy = "customer1",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new() { LotId = 20, Quantity = 2 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(201));
        }

    }
}


