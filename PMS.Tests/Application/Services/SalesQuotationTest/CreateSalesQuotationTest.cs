using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.Hub;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.SalesQuotation;
using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.LotProductRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.RequestSalesQuotation;
using PMS.Data.Repositories.SalesQuotation;
using PMS.Data.Repositories.SalesQuotationNote;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.TaxPolicy;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Tests.Application.Services.SalesQuotationTest
{
    [TestFixture]
    public class CreateSalesQuotationTest : ServiceTestBase
    {
        private Mock<IUnitOfWork> _uowMock;
        private Mock<ISalesQuotationRepository> _sqRepo;
        private Mock<INotificationService> _notifyMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<SalesQuotationService>> _loggerMock;
        private Mock<IPdfService> _pdfMock;
        private Mock<IEmailService> _emailMock;
        private Mock<IHubContext<SalesQuotationHub>> _hubMock;

        private Mock<IStaffProfileRepository> _staffRepo;
        private Mock<IRequestSalesQuotationRepository> _rsqRepo;
        private Mock<ISalesQuotationNoteRepository> _sqNoteRepo;
        private Mock<IProductRepository> _productRepo;
        private Mock<ILotProductRepository> _lotRepo;
        private Mock<ITaxPolicyRepository> _taxRepo;

        private SalesQuotationService _service;

        [SetUp]
        public void SetUp()
        {
            base.BaseSetup();

            _uowMock = new Mock<IUnitOfWork>();
            _sqRepo =  new Mock<ISalesQuotationRepository>();
            _notifyMock = new Mock<INotificationService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<SalesQuotationService>>();
            _pdfMock = new Mock<IPdfService>();
            _emailMock = new Mock<IEmailService>();
            _hubMock = new Mock<IHubContext<SalesQuotationHub>>();
            _staffRepo = new Mock<IStaffProfileRepository>();
            _rsqRepo = new Mock<IRequestSalesQuotationRepository>();
            _sqNoteRepo = new Mock<ISalesQuotationNoteRepository>();
            _productRepo = new Mock<IProductRepository>();
            _lotRepo = new Mock<ILotProductRepository>();
            _taxRepo = new Mock<ITaxPolicyRepository>();

            _uowMock.Setup(u => u.SalesQuotation)
                .Returns(_sqRepo.Object);

            _uowMock.Setup(u => u.StaffProfile)
                .Returns(_staffRepo.Object);

            _uowMock.Setup(u => u.RequestSalesQuotation)
                .Returns(_rsqRepo.Object);

            _uowMock.Setup(u => u.SalesQuotationNote)
                .Returns(_sqNoteRepo.Object);

            _uowMock.Setup(u => u.Product)
                .Returns(_productRepo.Object);

            _uowMock.Setup(u => u.LotProduct)
                .Returns(_lotRepo.Object);

            _uowMock.Setup(u => u.TaxPolicy)
                .Returns(_taxRepo.Object);

            _service = new SalesQuotationService(
                    _uowMock.Object,
                    _mapperMock.Object,
                    _loggerMock.Object,
                    _pdfMock.Object,
                    _emailMock.Object,
                    _notifyMock.Object,
                    _hubMock.Object
                );
        }

        [Test]
        public async Task CreateSalesQuotation_Should_Return_Error_When_Expired_Date_Invalid()
        {
            var detailsList = new List<SalesQuotationDetailsDTO>()
            {
                new SalesQuotationDetailsDTO
                {
                    LotId = 1,
                    TaxId = 1,
                    ProductId = 1,
                    Note = "",
                },
                new SalesQuotationDetailsDTO
                {
                    LotId = 2,
                    TaxId = 2,
                    ProductId = 1,
                    Note = "",
                },
                new SalesQuotationDetailsDTO
                {
                    LotId = 3,
                    TaxId = 3,
                    ProductId = 1,
                    Note = "",
                },
            };

            var staffProfile = new StaffProfile
            {
                Id = 1,
            };

            var listRSQDetail = new List<Core.Domain.Entities.RequestSalesQuotationDetails>()
            {
                new RequestSalesQuotationDetails() {ProductId = 1}
            };

            var listLot = new List<LotProduct>()
            {
                new LotProduct(){LotID = 1, InputPrice = 22000, ProductID = 1},
                new LotProduct(){LotID = 2, InputPrice = 24000, ProductID = 1},
                new LotProduct(){LotID = 3, InputPrice = 26000, ProductID = 1},
            };

            var rsq = new Core.Domain.Entities.RequestSalesQuotation
            {
                Id = 1,
                Status = Core.Domain.Enums.RequestSalesQuotationStatus.Sent,
                RequestSalesQuotationDetails = listRSQDetail
            };

            var note = new Core.Domain.Entities.SalesQuotationNote
            {
                Id = 1,
                IsActive = true,
            };

            var listTax = new List<TaxPolicy>()
            {
                new TaxPolicy(){Id = 1, Status = true},
                new TaxPolicy(){Id = 2, Status = true},
                new TaxPolicy(){Id = 3, Status = true},
            };

            var product = new Core.Domain.Entities.Product
            {
                ProductID = 1,
                ProductName = "A",
                Unit = "Lọ",
                MinQuantity = 10,
                MaxQuantity = 10,
                TotalCurrentQuantity = 5,
                Status = true,
            };

            var dto = new CreateSalesQuotationDTO
            {
                RsqId = 1,
                NoteId = 1,
                ExpiredDate = new DateTime(2025, 12, 16),
                DepositPercent = 20,
                DepositDueDays = 3,
                ExpectedDeliveryDate = 30,
                Status = 0,
                Details = detailsList,
            };

            _staffRepo.Setup(r => r.Query())
                .Returns(new[] { staffProfile }.AsQueryable().ToMockDbSet().Object);

            _rsqRepo.Setup(r => r.Query())
                .Returns(new[] { rsq }.AsQueryable().ToMockDbSet().Object);

            _sqNoteRepo.Setup(r => r.Query())
                .Returns(new[] { note }.AsQueryable().ToMockDbSet().Object);

            _productRepo.Setup(r => r.Query())
                .Returns(new[] { product }.AsQueryable().ToMockDbSet().Object);

            _lotRepo.Setup(r => r.Query())
                .Returns(listLot.AsQueryable().ToMockDbSet().Object);

            _taxRepo.Setup(r => r.Query())
                .Returns(listTax.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateSalesQuotationAsync(dto, "1");

            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Ngày hết hạn cho báo giá không được nhỏ hơn hôm nay"));
        }
    }
}
