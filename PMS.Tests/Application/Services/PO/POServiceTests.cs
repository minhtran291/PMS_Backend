using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PMS.API.Services.POService;
using PMS.Application.DTOs.PO;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.PO;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Core.Domain.Identity;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;

namespace PMS.Tests.Services
{
    [TestFixture]
    public class POServiceTests : ServiceTestBase
    {
        private Mock<IPdfService> _pdfServiceMock;
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private IPOService _poService;

        private List<PurchasingOrder> _poData;
        private List<User> _userData;
        private List<Quotation> _quotationData;
        private List<Supplier> _supplierData;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();

            _pdfServiceMock = new Mock<IPdfService>();
            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.Setup(x => x.WebRootPath).Returns("wwwroot");


            _userData = new List<User>
            {
                new User { Id = "USER-001", UserName = "john_doe", FullName = "John Doe" },
                new User { Id = "USER-002", UserName = "jane_smith", FullName = "Jane Smith" }
            };

            _supplierData = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Supplier A" },
                new Supplier { Id = 2, Name = "Supplier B" }
            };

            _quotationData = new List<Quotation>
            {
                new Quotation { QID = 101, SupplierID = 1, QuotationExpiredDate = new DateTime(2025, 1, 15),SendDate = new DateTime(2025, 1, 1) , PRFQID=1},
                new Quotation
            {
                QID = 102,
                SupplierID = 2,
                SendDate = new DateTime(2025, 1, 2),
                QuotationExpiredDate = new DateTime(2025, 1, 16),
        
            }
            };

            _poData = new List<PurchasingOrder>
            {
                new PurchasingOrder
                {
                    POID = 1,
                    QID = 101,
                    Total = 1000000,
                    Deposit = 400000,
                    Debt = 600000,
                    Status = PurchasingOrderStatus.deposited,
                    OrderDate = new DateTime(2025, 1, 1),
                    PaymentDate = new DateTime(2025, 1, 5),
                    UserId = "USER-001",
                    PaymentBy = "USER-002",
                    User = _userData[0],
                    Quotations = _quotationData[0],
                    PurchasingOrderDetails = new List<PurchasingOrderDetail>
                    {
                        new PurchasingOrderDetail
                        {
                            PODID = 1,
                            ProductName = "Product X",
                            Quantity = 10,
                            UnitPrice = 100000,
                            UnitPriceTotal = 1000000,
                            DVT = "Cái",
                            Description = "Test product"
                        }
                    }
                }
            };

            // Setup UnitOfWork mocks
            var poDbSet = _poData.AsQueryable().ToMockDbSet();
            var userDbSet = _userData.AsQueryable().ToMockDbSet();
            var quotationDbSet = _quotationData.AsQueryable().ToMockDbSet();
            var supplierDbSet = _supplierData.AsQueryable().ToMockDbSet();

            UnitOfWorkMock.Setup(x => x.PurchasingOrder.Query()).Returns(poDbSet.Object);
            UnitOfWorkMock.Setup(x => x.Users.Query()).Returns(userDbSet.Object);
            UnitOfWorkMock.Setup(x => x.Quotation.Query()).Returns(quotationDbSet.Object);
            UnitOfWorkMock.Setup(x => x.Supplier.Query()).Returns(supplierDbSet.Object);

            UnitOfWorkMock.Setup(x => x.PurchasingOrder.Update(It.IsAny<PurchasingOrder>()))
                .Callback<PurchasingOrder>(po => _poData[_poData.FindIndex(p => p.POID == po.POID)] = po);

            // UserManager mock
            UserManagerMock.Setup(x => x.FindByIdAsync("USER-002"))
                .ReturnsAsync(_userData[1]);

            // Mapper mock
            MapperMock.Setup(m => m.Map<It.IsAnyType>(It.IsAny<object>()))
                .Returns((Type t, object src) => src);


            NotificationServiceMock = new Mock<INotificationService>();
            NotificationServiceMock
            .Setup(x => x.SendNotificationToRolesAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),   
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>()
            ))
            .Returns(Task.CompletedTask);

            // Create service
            _poService = new POService(
                UnitOfWorkMock.Object,
                MapperMock.Object,
                _webHostEnvironmentMock.Object,
                _pdfServiceMock.Object,
                NotificationServiceMock.Object);
        }



        [Test]
        public async Task DepositedPOAsync_ValidPayment_ShouldUpdateDepositAndStatus()
        {
            // Arrange
            var updateDto = new POUpdateDTO { paid = 300000 };

            // Act
            var result = await _poService.DepositedPOAsync("USER-002", 1, updateDto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data.Status, Is.EqualTo(PurchasingOrderStatus.deposited));
            Assert.That(result.Data.Debt, Is.EqualTo(300000));
            Assert.That(result.Data.PaymentBy, Is.EqualTo("jane_smith"));

            UnitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task DepositedPOAsync_PaymentExceedsTotal_ShouldReturn400()
        {
            // Arrange
            var updateDto = new POUpdateDTO { paid = 2000000 };

            // Act
            var result = await _poService.DepositedPOAsync("USER-002", 1, updateDto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Contains.Substring("vượt quá tổng giá trị"));
        }

        [Test]
        public async Task DepositedPOAsync_NonExistentPO_ShouldReturn404()
        {
            // Act
            var result = await _poService.DepositedPOAsync("USER-002", 999, new POUpdateDTO { paid = 100 });

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }



        [Test]
        public async Task ViewDetailPObyID_InvalidId_ShouldReturn404()
        {
            // Act
            var result = await _poService.ViewDetailPObyID(999);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task DebtAccountantPOAsync_ValidPayment_ShouldUpdateDebtAndStatus()
        {
            // Arrange
            var updateDto = new POUpdateDTO { paid = 300000 };

            // Act
            var result = await _poService.DebtAccountantPOAsync("USER-002", 1, updateDto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data.Status, Is.EqualTo(PurchasingOrderStatus.paid));
            Assert.That(result.Data.Debt, Is.EqualTo(300000));

            UnitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Test]
        public async Task DebtAccountantPOAsync_PaymentCompletesDebt_ShouldSetCompletedStatus()
        {
            // Arrange
            var updateDto = new POUpdateDTO { paid = 600000 };

            // Act
            var result = await _poService.DebtAccountantPOAsync("USER-002", 1, updateDto);

            // Assert
            Assert.That(result.Data.Status, Is.EqualTo(PurchasingOrderStatus.compeleted));
            Assert.That(result.Data.Debt, Is.EqualTo(0));
        }



        [Test]
        public async Task ChangeStatusAsync_InvalidTransition_ShouldReturn400()
        {
            // Arrange: Current status is 'deposited', try to go to 'sent' → invalid
            var po = _poData[0];
            po.Status = PurchasingOrderStatus.deposited;

            // Act
            var result = await _poService.ChangeStatusAsync("USER-001", 1, PurchasingOrderStatus.sent);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Contains.Substring("Không thể chuyển trạng thái"));
        }

        [Test]
        public async Task GeneratePOPaymentPdfAsync_ValidPO_ShouldReturnPdfBytes()
        {
            // Arrange
            _pdfServiceMock.Setup(x => x.GeneratePdfFromHtml(It.IsAny<string>()))
                .Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 }); 

            // Act
            var pdfBytes = await _poService.GeneratePOPaymentPdfAsync(1);

            // Assert
            Assert.That(pdfBytes, Is.Not.Null);
            Assert.That(pdfBytes.Length, Is.GreaterThan(0));
            _pdfServiceMock.Verify(x => x.GeneratePdfFromHtml(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void GeneratePOPaymentPdfAsync_InvalidPO_ShouldThrowException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _poService.GeneratePOPaymentPdfAsync(999));

            Assert.That(ex.Message, Contains.Substring("Không tìm thấy đơn hàng"));
        }
    }
}