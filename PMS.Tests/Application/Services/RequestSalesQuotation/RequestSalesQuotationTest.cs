using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using PMS.Application.DTOs.RequestSalesQuotation;
using PMS.Application.Services.Notification;
using PMS.Application.Services.RequestSalesQuotation;
using PMS.Core.Domain.Entities;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.RequestSalesQuotation;
using PMS.Data.UnitOfWork;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.RequestSalesQuotation
{
    [TestFixture]
    public class RequestSalesQuotationTest : ServiceTestBase
    {
        private Mock<IUnitOfWork> _uowMock;
        private Mock<IRequestSalesQuotationRepository> _rsqRepoMock;
        private Mock<INotificationService> _notifyMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<RequestSalesQuotationService>> _loggerMock;
        private Mock<ICustomerProfileRepository> _customerProfileRepoMock;
        private Mock<IProductRepository> _productRepoMock;

        private RequestSalesQuotationService _service;

        [SetUp]
        public void Setup()
        {
            base.BaseSetup();

            _uowMock = new Mock<IUnitOfWork>();
            _rsqRepoMock = new Mock<IRequestSalesQuotationRepository>();
            _notifyMock = new Mock<INotificationService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<RequestSalesQuotationService>>();
            _customerProfileRepoMock = new Mock<ICustomerProfileRepository>();
            _productRepoMock = new Mock<IProductRepository>();

            _uowMock.Setup(u => u.RequestSalesQuotation)
                    .Returns(_rsqRepoMock.Object);

            _uowMock.Setup(u => u.CustomerProfile)
                .Returns(_customerProfileRepoMock.Object);

            _uowMock.Setup(u => u.Product)
                .Returns(_productRepoMock.Object);

            //_uowMock.Setup(u => u.CommitAsync())
            //        .ReturnsAsync(1);

            //_uowMock.Setup(u => u.BeginTransactionAsync())
            //        .Returns(Task.CompletedTask);

            //_uowMock.Setup(u => u.CommitTransactionAsync())
            //        .Returns(Task.CompletedTask);

            //_uowMock.Setup(u => u.RollbackTransactionAsync())
            //        .Returns(Task.CompletedTask);

            //UnitOfWorkMock.Setup(u => u.RequestSalesQuotation).Returns(_rsqRepoMock.Object);
            //UnitOfWorkMock.Setup(u => u.CustomerProfile).Returns(_customerProfileRepoMock.Object);

            //UnitOfWorkMock.Setup(u => u.CustomerProfile.GetByIdAsync(
            //    It.IsAny<int>(),
            //    It.IsAny<Func<IQueryable<CustomerProfile>, IQueryable<CustomerProfile>>>()))
            //    .Returns<int, Func<IQueryable<CustomerProfile>, IQueryable<CustomerProfile>>>(
            //        (id, include) => _customerProfileRepoMock.Object.GetByIdAsync(id, include));

            _service = new RequestSalesQuotationService(
                _uowMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _notifyMock.Object
            );
        }

        [Test]
        public async Task CreateRequestSalesQuotation_Should_Return_Error_When_ProductIdList_Is_Empty()
        {
            var dto = new CreateRsqDTO
            {
                ProductIdList = new List<int>(),
                Status = 1
            };

            var customerProfile = new CustomerProfile
            {
                Id = 1,
            };

            _customerProfileRepoMock.Setup(r => r.Query())
                .Returns(new[] { customerProfile}.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateRequestSalesQuotation(dto, "1");

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Bạn phải chọn ít nhất một sản phẩm"));
        }


        [Test]
        public async Task CreateRequestSalesQuotation_Should_Return_Error_When_ProductIdList_Contain_Product_Not_Existing()
        {
            var dto = new CreateRsqDTO
            {
                ProductIdList = new List<int>()
                {
                    -1,2,3
                },
                Status = 1
            };

            var customerProfile = new CustomerProfile
            {
                Id = 1,
            };

            var productList = new List<Core.Domain.Entities.Product>()
            {
                new Core.Domain.Entities.Product
                {
                    ProductID = 1,
                    ProductName = "A",
                    MinQuantity = 1,
                    MaxQuantity = 10,
                    TotalCurrentQuantity = 5,
                    Status = true,
                    Unit = "Lọ",
                },
                new Core.Domain.Entities.Product
                {
                    ProductID = 2,
                    ProductName = "B",
                    MinQuantity = 1,
                    MaxQuantity = 10,
                    TotalCurrentQuantity = 5,
                    Status = true,
                    Unit = "Lọ",
                },
                new Core.Domain.Entities.Product
                {
                    ProductID = 3,
                    ProductName = "C",
                    MinQuantity = 1,
                    MaxQuantity = 10,
                    TotalCurrentQuantity = 5,
                    Status = true,
                    Unit = "Lọ",
                }
            };

            _customerProfileRepoMock.Setup(r => r.Query())
                .Returns(new[] { customerProfile }.AsQueryable().ToMockDbSet().Object);

            _productRepoMock.Setup(r => r.Query())
                .Returns(productList.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateRequestSalesQuotation(dto, "1");

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Sản phẩm số 1 không tồn tại."));
        }

        [Test]
        public async Task CreateRequestSalesQuotation_Should_Return_Success()
        {
            var dto = new CreateRsqDTO
            {
                ProductIdList = new List<int>()
                {
                    1,2,3
                },
                Status = 1
            };

            var customerProfile = new CustomerProfile
            {
                Id = 1,
            };

            var productList = new List<Core.Domain.Entities.Product>()
            {
                new Core.Domain.Entities.Product
                {
                    ProductID = 1,
                    ProductName = "A",
                    MinQuantity = 1,
                    MaxQuantity = 10,
                    TotalCurrentQuantity = 5,
                    Status = true,
                    Unit = "Lọ",
                },
                new Core.Domain.Entities.Product
                {
                    ProductID = 2,
                    ProductName = "B",
                    MinQuantity = 1,
                    MaxQuantity = 10,
                    TotalCurrentQuantity = 5,
                    Status = true,
                    Unit = "Lọ",
                },
                new Core.Domain.Entities.Product
                {
                    ProductID = 3,
                    ProductName = "C",
                    MinQuantity = 1,
                    MaxQuantity = 10,
                    TotalCurrentQuantity = 5,
                    Status = true,
                    Unit = "Lọ",
                }
            };

            _customerProfileRepoMock.Setup(r => r.Query())
                .Returns(new[] { customerProfile }.AsQueryable().ToMockDbSet().Object);

            _productRepoMock.Setup(r => r.Query())
                .Returns(productList.AsQueryable().ToMockDbSet().Object);

            var result = await _service.CreateRequestSalesQuotation(dto, "1");

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.Message, Is.EqualTo("Tạo yêu cầu báo giá thành công"));
        }
    }
}
