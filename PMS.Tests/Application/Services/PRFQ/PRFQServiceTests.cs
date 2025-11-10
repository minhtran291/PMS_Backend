using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OfficeOpenXml;
using PMS.API.Services.PRFQService;
using PMS.Application.DTOs.PO;
using PMS.Application.DTOs.PRFQ;
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
        private Mock<IDistributedCache> _cacheMock;
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
            _cacheMock = new Mock<IDistributedCache>();

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
                _cacheMock.Object,
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
            var supplier = new Supplier { Id = TestSupplierId, Status = SupplierStatus.Active, Name="anhtester@gmail.com" };

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            _supplierRepoMock.Setup(r => r.Query())
                .Returns(new[] { supplier }.ToAsyncQueryable());

            var products = new[]
            {
                new Product
                {
                    ProductID = 201,
                    Status = true,
                    MaxQuantity = 2000,
                    MinQuantity = 10,
                    TotalCurrentQuantity = 30,
                    ProductName = "abc",
                    Unit = "Hộp"
                }
            };

            _productRepoMock.Setup(r => r.Query())
                .Returns(MockHelper.MockDbSet(products).Object);


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
                new Product { ProductID = 201, Status = false,MaxQuantity=2000,
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

            _productRepoMock.Setup(r => r.Query())
                .Returns(MockHelper.MockDbSet(products).Object);

            UserManagerMock.Setup(m => m.FindByIdAsync(TestUserId)).ReturnsAsync(user);

            _supplierRepoMock.Setup(r => r.Query())
            .Returns(MockHelper.MockDbSet(new[] { supplier }).Object);
            //
            var addedPrfq = new PurchasingRequestForQuotation { PRFQID = 999 };
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


        [Test]
        public async Task CreatePRFQAsync_ValidData_ShouldReturnSuccess()
        {
            var userId = "user123";
            var supplierId = 1;
            var productIds = new List<int> { 10, 20 };

            var user = new User { Id = userId, FullName = "TestUser" };
            var supplier = new Supplier { Id = supplierId, Name = "Supplier A", Status = SupplierStatus.Active, Email = "supplier@mail.com" };
            var products = new List<Product>
            {
                new Product { ProductID = 10, ProductName = "Prod1", Status = true, Unit = "Hộp", MaxQuantity=2000, MinQuantity=100, TotalCurrentQuantity=20 },
                new Product { ProductID = 20, ProductName = "Prod2", Status = true, Unit = "Hộp", MaxQuantity=2000, MinQuantity=100, TotalCurrentQuantity=20 }
            };


            UserManagerMock.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);


            var supplierDbSet = MockHelper.GetMockDbSet(new List<Supplier> { supplier }.AsQueryable());
            UnitOfWorkMock.Setup(u => u.Supplier.Query()).Returns(supplierDbSet.Object);


            var productDbSet = MockHelper.GetMockDbSet(products.AsQueryable());
            UnitOfWorkMock.Setup(u => u.Product.Query()).Returns(productDbSet.Object);


            UnitOfWorkMock.Setup(u => u.PurchasingRequestForQuotation.AddAsync(It.IsAny<PurchasingRequestForQuotation>()))
                .Callback<PurchasingRequestForQuotation>(p => p.PRFQID = 99)
                .Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.PurchasingRequestProduct.AddAsync(It.IsAny<PurchasingRequestProduct>()))
                .Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);


            var fullPrfq = new PurchasingRequestForQuotation
            {
                PRFQID = 99,
                TaxCode = "123456789",
                MyPhone = "0909123456",
                MyAddress = "HCM",
                Supplier = supplier,
                User = user,
                PRPS = productIds.Select(pid => new PurchasingRequestProduct
                {
                    ProductID = pid,
                    Product = products.First(p => p.ProductID == pid)
                }).ToList()
            };
            UnitOfWorkMock.Setup(u => u.PurchasingRequestForQuotation.GetByIdAsync(
                    It.IsAny<int>(),
                    It.IsAny<Func<IQueryable<PurchasingRequestForQuotation>, IQueryable<PurchasingRequestForQuotation>>>()))
                .ReturnsAsync(fullPrfq);


            EmailServiceMock.Setup(e => e.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);


            var result = await _prfqService.CreatePRFQAsync(
                userId, supplierId, "123456789", "0909123456", "HCM", productIds, PRFQStatus.Sent);


            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data, Is.EqualTo(99));
            EmailServiceMock.Verify(e => e.SendEmailWithAttachmentAsync(
                supplier.Email, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
            UnitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));
        }


        [Test]
        public async Task ConvertExcelToPurchaseOrderAsync_ValidExcel_ShouldReturnSuccess()
        {


            var tempFile = Path.GetTempFileName();

            using (var package = new ExcelPackage(new FileInfo(tempFile)))
            {
                var ws = package.Workbook.Worksheets.Add("Sheet1");

                ws.Cells[2, 1].Value = 1; // STT
                ws.Cells[2, 2].Value = 1; // PRFQID
                ws.Cells[4, 4].Value = 10; // QID
                ws.Cells[4, 6].Value = "TestSupplier"; // Supplier name
                ws.Cells[5, 6].Value = "supplier@email.com"; // supplier email
                ws.Cells[7, 2].Value = DateTime.Today; // OrderDate
                ws.Cells[7, 4].Value = DateTime.Today.AddDays(3); // ExpectedDate

                // Product line
                ws.Cells[11, 2].Value = 1001; // ProductID
                ws.Cells[11, 4].Value = "Desc";
                ws.Cells[11, 5].Value = "PCS";
                ws.Cells[11, 6].Value = "10";
                ws.Cells[11, 7].Value = DateTime.Today.AddMonths(1);

                package.Save();
            }

            var tempFileBytes = Encoding.UTF8.GetBytes(tempFile);
            _cacheMock.Setup(x => x.GetAsync("ExcelKey1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(tempFileBytes);

            var input = new PurchaseOrderInputDto
            {
                ExcelKey = "ExcelKey1",
                status = PurchasingOrderStatus.sent,
                Details = new List<PurchaseOrderDetailInput>
            {
                new PurchaseOrderDetailInput { STT = 1, Quantity = 5 }
            }
            };


            _cacheMock.Setup(x => x.GetAsync("ExcelKey1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempFileBytes);


            var user = new User { Id = "u1", UserName = "Tester" };
            UserManagerMock.Setup(x => x.FindByIdAsync("u1")).ReturnsAsync(user);
            var userSet = MockHelper.MockDbSet(new List<User> { user });
            UnitOfWorkMock.Setup(x => x.Users.Query()).Returns(userSet.Object);


            var supplierList = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "TestSupplier", Email = "supplier@email.com" }
            };
            var supplierDbSet = MockHelper.MockDbSet(supplierList);
            UnitOfWorkMock.Setup(x => x.Supplier.Query()).Returns(supplierDbSet.Object);


            var products = new List<Product>
            {
            new Product
            {
                ProductID = 1001,
                ProductName = "Prod A",
                MaxQuantity = 2000,
                MinQuantity = 100,
                Status = true,
                TotalCurrentQuantity = 12,
                Unit = "Hộp"
            }
            };
            var productSet = MockHelper.MockDbSet(products);
            UnitOfWorkMock.Setup(x => x.Product.Query()).Returns(productSet.Object);


            var quotationList = new List<Quotation>
            {
                new Quotation
                {
                    QID = 10,
                    SupplierID = 1,
                    QuotationExpiredDate=new DateTime(2030, 12, 5),
                    SendDate=new DateTime(2025/11/10),        
                }
            };
            var quotationSet = MockHelper.MockDbSet(quotationList);
            UnitOfWorkMock.Setup(x => x.Quotation.Query()).Returns(quotationSet.Object);
            UnitOfWorkMock.Setup(x => x.Quotation.AddAsync(It.IsAny<Quotation>())).Returns(Task.CompletedTask);


            var poSet = MockHelper.MockDbSet(new List<PurchasingOrder>());
            UnitOfWorkMock.Setup(x => x.PurchasingOrder.Query()).Returns(poSet.Object);
            UnitOfWorkMock.Setup(x => x.PurchasingOrder.AddAsync(It.IsAny<PurchasingOrder>()))
                .Callback<PurchasingOrder>(po => po.POID = 99)
                .Returns(Task.CompletedTask);

            var poDetailSet = MockHelper.MockDbSet(new List<PurchasingOrderDetail>());
            UnitOfWorkMock.Setup(x => x.PurchasingOrderDetail.AddRange(It.IsAny<IEnumerable<PurchasingOrderDetail>>()));


            var quotationDetailSet = MockHelper.MockDbSet(new List<QuotationDetail>());
            UnitOfWorkMock.Setup(x => x.QuotationDetail.AddRange(It.IsAny<IEnumerable<QuotationDetail>>()));


            EmailServiceMock.Setup(x => x.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            NotificationServiceMock.Setup(x => x.SendNotificationToRolesAsync(
                It.IsAny<string>(), It.IsAny<List<string>>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>()))
                .Returns(Task.CompletedTask);


            var result = await _prfqService.ConvertExcelToPurchaseOrderAsync("u1", input, PurchasingOrderStatus.sent);


            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Thành công"));

            UnitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);

            EmailServiceMock.Verify(x => x.SendEmailWithAttachmentAsync(
                "supplier@email.com",
                "Đơn hàng",
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.Is<string>(f => f.Contains("PO_"))),
                Times.Once);

            NotificationServiceMock.Verify(x => x.SendNotificationToRolesAsync(
                "u1", It.IsAny<List<string>>(),
                "Yêu cầu nhập hàng",
                It.IsAny<string>(),
                NotificationType.Reminder),
                Times.Once);

            File.Delete(tempFile);
        }

        [Test]
        public async Task ConvertExcelToPurchaseOrderAsync_FileMissing_ShouldReturnFail()
        {

            var input = new PurchaseOrderInputDto
            {
                ExcelKey = "MissingFile",
                status = PurchasingOrderStatus.sent,
                Details = new List<PurchaseOrderDetailInput>
        {
            new PurchaseOrderDetailInput { STT = 1, Quantity = 5 }
        }
            };


            _cacheMock.Setup(x => x.GetAsync("MissingFile", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);


            var user = new User { Id = "user", UserName = "Tester" };
            UserManagerMock.Setup(x => x.FindByIdAsync("user")).ReturnsAsync(user);


            var result = await _prfqService.ConvertExcelToPurchaseOrderAsync("user", input, PurchasingOrderStatus.sent);


            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Thất bại") 
                .IgnoreCase);

            UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}