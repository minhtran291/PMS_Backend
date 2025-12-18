using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.Application.DTOs.SalesQuotation;
using PMS.Application.Hub;
using PMS.Application.Services.ExternalService;
using PMS.Application.Services.Notification;
using PMS.Application.Services.SalesQuotation;
using PMS.Data.Repositories.SalesQuotation;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Tests.Application.Services.SalesQuotation
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

            _uowMock.Setup(u => u.SalesQuotation)
                .Returns(_sqRepo.Object);

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

        //[Test]
        //public async Task CreateSalesQuotation_Should_Return_Error_When_Expired_Date_Invalid()
        //{
        //    var detailsList = new List<SalesQuotationDetailsDTO>()
        //    {
        //        new SalesQuotationDetailsDTO
        //        {

        //        }
        //    };

        //    var dto = new CreateSalesQuotationDTO
        //    {
        //        RsqId = 1,
        //        NoteId = 1,
        //        ExpiredDate = "16/12/2025",
        //        DepositPercent = 20,
        //        DepositDueDays = 3,
        //        ExpectedDeliveryDate = 30,
        //        Status = 0,
        //        Details =
        //    };
        //}
    }
}
