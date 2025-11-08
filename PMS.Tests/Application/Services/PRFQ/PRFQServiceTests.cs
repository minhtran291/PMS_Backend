using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OfficeOpenXml;
using PMS.API.Services.PRFQService;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.PurchasingRequestForQuotationRepository;
using PMS.Data.Repositories.PurchasingRequestProductRepository;
using PMS.Data.Repositories.Supplier;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;

namespace PMS.Tests.Services.Purchasing
{
    [TestFixture]
    public class PRFQServiceTests : ServiceTestBase
    {
        private PRFQService _prfqService;
        private Mock<IPurchasingRequestForQuotationRepository> _prfqRepoMock;
        private Mock<IPurchasingRequestProductRepository> _prpRepoMock;
        private Mock<ISupplierRepository> _supplierRepoMock;
        private Mock<IProductRepository> _productRepoMock;

        private const string TestUserId = "user-123";
        private const int TestSupplierId = 100;
        private readonly List<int> TestProductIds = new() { 201, 202 };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            ExcelPackage.License.SetNonCommercialPersonal("hoanganh");
        }
        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();


            _prfqRepoMock = new Mock<IPurchasingRequestForQuotationRepository>();
            _prpRepoMock = new Mock<IPurchasingRequestProductRepository>();
            _supplierRepoMock = new Mock<ISupplierRepository>();
            _productRepoMock = new Mock<IProductRepository>();

            UnitOfWorkMock.Setup(u => u.PurchasingRequestForQuotation).Returns(_prfqRepoMock.Object);
            UnitOfWorkMock.Setup(u => u.PurchasingRequestProduct).Returns(_prpRepoMock.Object);
            UnitOfWorkMock.Setup(u => u.Supplier).Returns(_supplierRepoMock.Object);
            UnitOfWorkMock.Setup(u => u.Product).Returns(_productRepoMock.Object);


            UnitOfWorkMock.Setup(u => u.PurchasingRequestForQuotation.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<Func<IQueryable<PurchasingRequestForQuotation>, IQueryable<PurchasingRequestForQuotation>>>()))
                .Returns<int, Func<IQueryable<PurchasingRequestForQuotation>, IQueryable<PurchasingRequestForQuotation>>>(
                    (id, include) => _prfqRepoMock.Object.GetByIdAsync(id, include));


            _prfqService = new PRFQService(
                UnitOfWorkMock.Object,
                MapperMock.Object,
                EmailServiceMock!.Object,
                Mock.Of<IDistributedCache>(),
                Mock.Of<ILogger<PRFQService>>(),
                NotificationServiceMock!.Object
            );
        }

        [Test]
        public async Task CreatePRFQAsync_UserNotFound_Returns404()
        {

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId))
                .ReturnsAsync((User)null);


            var result = await _prfqService.CreatePRFQAsync(
                TestUserId, TestSupplierId, "123456789", "0901234567", "Hà Nội", TestProductIds, PRFQStatus.Draft);


            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(404));
                Assert.That(result.Message, Is.EqualTo("User không tồn tại, không có quyền tạo"));
                Assert.That(result.Data, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CreatePRFQAsync_SupplierNotFound_Returns404()
        {
            // Arrange
            var user = new User { Id = TestUserId, FullName = "Nguyễn Văn A" };
            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);

            // Mock DbSet empty để giả lập supplier không tồn tại
            _supplierRepoMock.Setup(r => r.Query())
                .Returns(new List<Supplier>().AsQueryable().ToMockDbSet().Object);

            // Act
            var result = await _prfqService.CreatePRFQAsync(
                TestUserId, TestSupplierId, "123456789", "0901234567", "Hà Nội", TestProductIds, PRFQStatus.Draft);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(404));
                Assert.That(result.Message, Is.EqualTo("Supplier không tồn tại"));
            });
        }

        [Test]
        public async Task CreatePRFQAsync_ProductNotFound_Returns400()
        {

            var user = new User { Id = TestUserId, FullName = "Nguyễn Văn A" };
            var supplier = new Supplier { Id = TestSupplierId, Status = SupplierStatus.Active };

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            _supplierRepoMock.Setup(r => r.Query())
                .Returns(new[] { supplier }.ToAsyncQueryable());

            _productRepoMock.Setup(r => r.Query())
                .Returns(new[] { new Product { ProductID = 201, Status = true, MaxQuantity = 2000, MinQuantity = 10, TotalCurrentQuantity = 30, ProductName = "abc", Unit = "Hộp" } }
                    .ToAsyncQueryable());


            var result = await _prfqService.CreatePRFQAsync(
                TestUserId, TestSupplierId, "123456789", "0901234567", "Hà Nội", TestProductIds, PRFQStatus.Draft);


            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(400));
                Assert.That(result.Message, Is.EqualTo("Một số sản phẩm không tồn tại"));
            });
        }

        [Test]
        public async Task CreatePRFQAsync_InactiveProduct_Returns400()
        {

            var user = new User { Id = TestUserId, FullName = "Nguyễn Văn A" };
            var supplier = new Supplier { Id = TestSupplierId, Status = SupplierStatus.Active };
            var products = new[]
            {
                new Product { ProductID = 201, Status = true,                MaxQuantity=2000,
                MinQuantity=10,
                TotalCurrentQuantity=30,ProductName="abc",Unit="Hộp" },
                new Product { ProductID = 202, Status = false,MaxQuantity=2000,
                MinQuantity=10,
                TotalCurrentQuantity=30,ProductName="bcd",Unit="Hộp" }
            };

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            _supplierRepoMock.Setup(r => r.Query())
                .Returns(new[] { supplier }.ToAsyncQueryable());
            _productRepoMock.Setup(r => r.Query())
                .Returns(products.ToAsyncQueryable());

            var result = await _prfqService.CreatePRFQAsync(
                TestUserId, TestSupplierId, "123456789", "0901234567", "Hà Nội", TestProductIds, PRFQStatus.Draft);

            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(400));
                Assert.That(result.Message, Is.EqualTo("Một số sản phẩm không hoạt động"));
            });
        }

        [Test]
        public async Task CreatePRFQAsync_DraftStatus_Success_NoEmailSent()
        {

            var user = new User { Id = TestUserId, FullName = "Nguyễn Văn A" };
            var supplier = new Supplier { Id = TestSupplierId, Status = SupplierStatus.Active, Email = "ncc@test.com" };
            var products = TestProductIds.Select(id => new Product
            {
                ProductID = id,
                Status = true,
                ProductName = $"Sản phẩm {id}",
                Unit = "Cái",
                MaxQuantity = 2000,
                MinQuantity = 10,
                TotalCurrentQuantity = 30,
            }).ToList();

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);


            _supplierRepoMock.Setup(r => r.Query())
                .Returns(new[] { supplier }.AsQueryable().ToMockDbSet().Object);

            _productRepoMock.Setup(r => r.Query())
                .Returns(products.AsQueryable().ToMockDbSet().Object);

            var addedPrfq = new PurchasingRequestForQuotation { PRFQID = 0 };
            _prfqRepoMock.Setup(r => r.AddAsync(It.IsAny<PurchasingRequestForQuotation>()))
                .Callback<PurchasingRequestForQuotation>(e => e.PRFQID = 999)
                .Returns(Task.CompletedTask);

            var addedPrps = new List<PurchasingRequestProduct>();
            _prpRepoMock.Setup(r => r.AddAsync(It.IsAny<PurchasingRequestProduct>()))
                .Callback<PurchasingRequestProduct>(addedPrps.Add)
                .Returns(Task.CompletedTask);


            var result = await _prfqService.CreatePRFQAsync(
                TestUserId, TestSupplierId, "123456789", "0901234567", "Hà Nội", TestProductIds, PRFQStatus.Draft);


            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Data, Is.EqualTo(999));
                Assert.That(result.Message, Is.EqualTo("Tạo yêu cầu báo giá thành công."));

                _prfqRepoMock.Verify(r => r.AddAsync(It.IsAny<PurchasingRequestForQuotation>()), Times.Once);
                _prpRepoMock.Verify(r => r.AddAsync(It.IsAny<PurchasingRequestProduct>()), Times.Exactly(2));
                UnitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));

                EmailServiceMock.Verify(e => e.SendEmailWithAttachmentAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()),
                    Times.Never);
            });
        }


        [Test]
        public async Task CreatePRFQAsync_SentStatus_Success_EmailSent()
        {

            var user = new User { Id = TestUserId, FullName = "Nguyễn Văn A" };
            var supplier = new Supplier
            {
                Id = TestSupplierId,
                Status = SupplierStatus.Active,
                Email = "ncc@test.com",
                Name = "NCC ABC"
            };
            var products = TestProductIds.Select(id => new Product
            {
                ProductID = id,
                Status = true,
                ProductName = $"Sản phẩm {id}",
                Unit = "Cái",
                MaxQuantity = 2000,
                MinQuantity = 10,
                TotalCurrentQuantity = 30,
            }).ToList();

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);


            _supplierRepoMock.Setup(r => r.Query())
                .Returns(new[] { supplier }.AsQueryable().ToMockDbSet().Object);
            _productRepoMock.Setup(r => r.Query())
                .Returns(products.AsQueryable().ToMockDbSet().Object);

            var prfqId = 999;
            _prfqRepoMock.Setup(r => r.AddAsync(It.IsAny<PurchasingRequestForQuotation>()))
                .Callback<PurchasingRequestForQuotation>(e => e.PRFQID = prfqId)
                .Returns(Task.CompletedTask);

            _prpRepoMock.Setup(r => r.AddAsync(It.IsAny<PurchasingRequestProduct>()))
                .Returns(Task.CompletedTask);

            var fullPrfq = new PurchasingRequestForQuotation
            {
                PRFQID = prfqId,
                RequestDate = DateTime.Now,
                TaxCode = "123456789",
                MyPhone = "0901234567",
                MyAddress = "Hà Nội",
                User = user,
                Supplier = supplier,
                PRPS = products.Select(p => new PurchasingRequestProduct
                {
                    PRFQID = prfqId,
                    ProductID = p.ProductID,
                    Product = p
                }).ToList()
            };

            _prfqRepoMock.Setup(r => r.GetByIdAsync(
                prfqId,
                It.IsAny<Func<IQueryable<PurchasingRequestForQuotation>, IQueryable<PurchasingRequestForQuotation>>>()))
                .ReturnsAsync(fullPrfq);


            var result = await _prfqService.CreatePRFQAsync(
                TestUserId, TestSupplierId, "123456789", "0901234567", "Hà Nội", TestProductIds, PRFQStatus.Sent);


            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data, Is.EqualTo(prfqId));


            EmailServiceMock.Verify(e => e.SendEmailWithAttachmentAsync(
                supplier.Email,
                "Yêu cầu báo giá",
                "Kính gửi, đính kèm yêu cầu báo giá.",
                It.Is<byte[]>(b => b.Length > 0),
                $"PRFQ_{prfqId}.xlsx"
            ), Times.Once);
        }
    }
}