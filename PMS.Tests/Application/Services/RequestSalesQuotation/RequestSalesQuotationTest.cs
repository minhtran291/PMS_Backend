using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Application.Services.Notification;
using PMS.Application.Services.RequestSalesQuotation;
using PMS.Core.Domain.Entities;
using PMS.Core.Domain.Enums;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.RequestSalesQuotation;
using PMS.Data.UnitOfWork;
using System.Linq.Expressions;

namespace PMS.Tests.Application.Services.RequestSalesQuotation
{
    [TestFixture]
    public class RequestSalesQuotationTest
    {
        private Mock<IUnitOfWork> _uowMock;
        private Mock<IRequestSalesQuotationRepository> _rsqRepoMock;
        private Mock<INotificationService> _notifyMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<RequestSalesQuotationService>> _loggerMock;
        private Mock<ICustomerProfileRepository> _customerProfileRepoMock;


        private RequestSalesQuotationService _service;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _rsqRepoMock = new Mock<IRequestSalesQuotationRepository>();
            _notifyMock = new Mock<INotificationService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<RequestSalesQuotationService>>();
            _customerProfileRepoMock = new Mock<ICustomerProfileRepository>();

            _uowMock.Setup(u => u.RequestSalesQuotation)
                    .Returns(_rsqRepoMock.Object);

            _uowMock.Setup(u => u.CustomerProfile)
                .Returns(_customerProfileRepoMock.Object);

            _uowMock.Setup(u => u.CommitAsync())
                    .ReturnsAsync(1);

            _uowMock.Setup(u => u.BeginTransactionAsync())
                    .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.CommitTransactionAsync())
                    .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.RollbackTransactionAsync())
                    .Returns(Task.CompletedTask);

            _service = new RequestSalesQuotationService(
                _uowMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _notifyMock.Object
            );

            var fakeProfiles = new List<CustomerProfile>
            {
                new CustomerProfile { Id = 1 }   // hoặc id profile liên quan đến test
            };
        }

        [Test]
        public async Task CreateRequestSalesQuotation_Should_Return_Error_When_ProductIdList_Is_Empty()
        {
            // Arrange
            var dto = new CreateRsqDTO
            {
                ProductIdList = new List<int>(),
                Status = 1
            };

            // Act
            var result = await _service.CreateRequestSalesQuotation(dto, "1");

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Is.EqualTo("Tạo yêu cầu báo giá thất bại"));

            _rsqRepoMock.Verify(r => r.AddAsync(It.IsAny<Core.Domain.Entities.RequestSalesQuotation>()), Times.Never);
            _notifyMock.Verify(n => n.SendNotificationToRolesAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationType>()), Times.Never);
        }
    }
}
