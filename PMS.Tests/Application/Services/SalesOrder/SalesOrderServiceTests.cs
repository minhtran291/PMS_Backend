using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PMS.Application.DTOs.SalesOrder;
using PMS.Application.Services.SalesOrder;
using PMS.Application.Services.VNpay;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.Repositories.SalesOrderDetailsRepository;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.Repositories.SalesQuotation;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.SalesOrder
{
    public class SalesOrderServiceTests : ServiceTestBase
    {
        private ISalesOrderService _service;
        private Mock<ISalesQuotationRepository> _sqRepoMock;
        private Mock<ISalesOrderRepository> _soRepoMock;
        private Mock<ISalesOrderDetailsRepository> _sodRepoMock;
        private Mock<IVnPayService> _vnPayServiceMock;

        // capture created data
        private readonly List<Core.Domain.Entities.SalesOrder> _createdOrders = new();
        private readonly List<SalesOrderDetails> _createdDetails = new();

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            _sqRepoMock = new Mock<ISalesQuotationRepository>();
            _soRepoMock = new Mock<ISalesOrderRepository>();
            _sodRepoMock = new Mock<ISalesOrderDetailsRepository>();
            _vnPayServiceMock = new Mock<IVnPayService>();

            UnitOfWorkMock.Setup(u => u.SalesQuotation).Returns(_sqRepoMock.Object);
            UnitOfWorkMock.Setup(u => u.SalesOrder).Returns(_soRepoMock.Object);
            UnitOfWorkMock.Setup(u => u.SalesOrderDetails).Returns(_sodRepoMock.Object);

            UnitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _soRepoMock.Setup(r => r.AddAsync(It.IsAny<Core.Domain.Entities.SalesOrder>()))
                .Callback<Core.Domain.Entities.SalesOrder>(o =>
                {
                    if (o.SalesOrderId == 0)
                        o.SalesOrderId = _createdOrders.Count + 1;
                    _createdOrders.Add(o);
                })
                .Returns(Task.CompletedTask);

            _soRepoMock.Setup(r => r.Update(It.IsAny<Core.Domain.Entities.SalesOrder>()))
                .Callback<Core.Domain.Entities.SalesOrder>(o =>
                {
                    var idx = _createdOrders.FindIndex(x => x.SalesOrderId == o.SalesOrderId);
                    if (idx >= 0) _createdOrders[idx] = o;
                });

            _sodRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<SalesOrderDetails>>()))
                .Callback<IEnumerable<SalesOrderDetails>>(ds => _createdDetails.AddRange(ds))
                .Returns(Task.CompletedTask);

            _service = new SalesOrderService(
                UnitOfWorkMock.Object,
                MapperMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<SalesOrderService>>(),
                NotificationServiceMock!.Object,
                _vnPayServiceMock.Object);
        }

        private static (List<PMS.Core.Domain.Entities.Product> products, List<LotProduct> lots, List<TaxPolicy> taxes, Core.Domain.Entities.SalesQuotation sq) BuildExistingData()
        {
            // mimic existing rows from screenshots
            var products = new List<PMS.Core.Domain.Entities.Product>
            {
                new PMS.Core.Domain.Entities.Product { ProductID = 1, ProductName = "Paracetamol 500mg", Unit = "Hộp", CategoryID = 1, MinQuantity = 10, MaxQuantity = 100, TotalCurrentQuantity = 50, Status = true},
                new PMS.Core.Domain.Entities.Product { ProductID = 2, ProductName = "Amoxicillin 500mg", Unit = "Vỉ", CategoryID = 2, MinQuantity = 5, MaxQuantity = 80, TotalCurrentQuantity = 40, Status = true }
            };

            var taxes = new List<TaxPolicy>
            {
                new TaxPolicy { Id = 1, Name = "VAT 10%", Rate = 0.10m, Status = true },
                new TaxPolicy { Id = 3, Name = "VAT 0%", Rate = 0.00m, Status = true }
            };

            var lots = new List<LotProduct>
            {
                new LotProduct { LotID = 1,  InputDate = DateTime.Today, SalePrice = 15000, InputPrice = 12000, ExpiredDate = DateTime.Today.AddMonths(14), LotQuantity = 810, SupplierID = 1, ProductID = 1, Product = products[0], WarehouselocationID = 1, LastCheckedDate = DateTime.Today },
                new LotProduct { LotID = 2,  InputDate = DateTime.Today, SalePrice = 18000, InputPrice = 15000, ExpiredDate = DateTime.Today.AddMonths(15), LotQuantity = 835, SupplierID = 1, ProductID = 2, Product = products[1], WarehouselocationID = 2, LastCheckedDate = DateTime.Today }
            };

            var sq = new Core.Domain.Entities.SalesQuotation
            {
                Id = 10,
                QuotationCode = "SQ-10",
                ExpiredDate = DateTime.Today.AddDays(5),
                DepositPercent = 20m,
                DepositDueDays = 7,
                SalesQuotaionDetails = new List<SalesQuotaionDetails>
                {
                    new SalesQuotaionDetails { Id = 101, SqId = 10, LotId = 1, ProductId = 1, Product = products[0], LotProduct = lots[0], TaxPolicy = taxes[0], TaxId = taxes[0].Id },
                    new SalesQuotaionDetails { Id = 102, SqId = 10, LotId = 2, ProductId = 2, Product = products[1], LotProduct = lots[1], TaxPolicy = taxes[1], TaxId = taxes[1].Id }
                }
            };

            return (products, lots, taxes, sq);
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_ValidData_ShouldCreateDraftAndDetails()
        {
            var (_, _, _, sq) = BuildExistingData();
            var sqQueryable = new List<Core.Domain.Entities.SalesQuotation> { sq }.AsQueryable();
            var sqDbSet = MockHelper.GetMockDbSet(sqQueryable);
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 10,
                CreateBy = "USER-001",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new SalesOrderDetailsRequestDTO{ LotId = 1, Quantity = 2 }, // 15000*(1+0.1)=16500 -> 33000
                    new SalesOrderDetailsRequestDTO{ LotId = 2, Quantity = 1 }  // 18000*(1+0)  =18000 -> 18000
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(_createdOrders.Count, Is.EqualTo(1));
            Assert.That(_createdDetails.Count, Is.EqualTo(2));

            var created = _createdOrders.Single();
            Assert.That(created.SalesOrderStatus, Is.EqualTo(SalesOrderStatus.Draft));
            Assert.That(created.CreateBy, Is.EqualTo("USER-001"));
            Assert.That(created.TotalPrice, Is.EqualTo(33000m + 18000m)); // 51000
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_MissingCreateBy_Should400()
        {
            var (_, _, _, sq) = BuildExistingData();
            var sqDbSet = MockHelper.GetMockDbSet(new List<Core.Domain.Entities.SalesQuotation> { sq }.AsQueryable());
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 10,
                CreateBy = " ",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new SalesOrderDetailsRequestDTO{ LotId = 1, Quantity = 1 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("CreateBy"));
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_EmptyDetails_Should400()
        {
            var (_, _, _, sq) = BuildExistingData();
            var sqDbSet = MockHelper.GetMockDbSet(new List<Core.Domain.Entities.SalesQuotation> { sq }.AsQueryable());
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 10,
                CreateBy = "USER-001",
                Details = new List<SalesOrderDetailsRequestDTO>()
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Details"));
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_NegativeQuantity_Should400()
        {
            var (_, _, _, sq) = BuildExistingData();
            var sqDbSet = MockHelper.GetMockDbSet(new List<Core.Domain.Entities.SalesQuotation> { sq }.AsQueryable());
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 10,
                CreateBy = "USER-001",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new SalesOrderDetailsRequestDTO{ LotId = 1, Quantity = -1 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Quantity âm"));
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_LotNotInQuotation_Should400()
        {
            var (_, _, _, sq) = BuildExistingData();
            var sqDbSet = MockHelper.GetMockDbSet(new List<Core.Domain.Entities.SalesQuotation> { sq }.AsQueryable());
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 10,
                CreateBy = "USER-001",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new SalesOrderDetailsRequestDTO{ LotId = 999, Quantity = 1 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("không thuộc báo giá"));
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_QuotationNotFound_Should404()
        {
            var sqDbSet = MockHelper.GetMockDbSet(new List<Core.Domain.Entities.SalesQuotation>().AsQueryable());
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 123,
                CreateBy = "USER-001",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new SalesOrderDetailsRequestDTO{ LotId = 1, Quantity = 1 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Does.Contain("Không tìm thấy SalesQuotation"));
        }

        [Test]
        public async Task CreateDraftFromSalesQuotationAsync_QuotationExpired_Should400()
        {
            var sq = new Core.Domain.Entities.SalesQuotation
            {
                Id = 20,
                QuotationCode = "SQ-EX",
                ExpiredDate = DateTime.Today.AddDays(-1),
                SalesQuotaionDetails = new List<SalesQuotaionDetails>()
            };
            var sqDbSet = MockHelper.GetMockDbSet(new List<Core.Domain.Entities.SalesQuotation> { sq }.AsQueryable());
            _sqRepoMock.Setup(r => r.Query()).Returns(sqDbSet.Object);

            var req = new SalesOrderRequestDTO
            {
                SalesQuotationId = 20,
                CreateBy = "USER-001",
                Details = new List<SalesOrderDetailsRequestDTO>
                {
                    new SalesOrderDetailsRequestDTO{ LotId = 1, Quantity = 1 }
                }
            };

            var result = await _service.CreateDraftFromSalesQuotationAsync(req);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("đã hết hạn"));
        }
    }
}


