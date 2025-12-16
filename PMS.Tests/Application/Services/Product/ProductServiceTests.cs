using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using Moq;
using PMS.Application.DTOs.Product;
using PMS.Application.Services.Category;
using PMS.Application.Services.Product;
using PMS.Core.Domain.Entities;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.Product
{
    [TestFixture]
    public class ProductServiceTests : ServiceTestBase
    {
        private ProductService _productService;
        private IWebHostEnvironment _webEnv;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();
            _productService = new ProductService(UnitOfWorkMock.Object, MapperMock.Object, _webEnv);
        }

        [Test]
        public async Task AddProductAsync_NullProduct_ShouldReturn500()
        {

            var result = await _productService.AddProductAsync(null);


            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("không được để trống"));
            Assert.False(result.Data);
        }


        [Test]
        public async Task AddProductAsync_MinQuantityGreaterThanMax_ShouldReturn200()
        {
            // Arrange
            var dto = new ProductDTOView
            {
                ProductName = "Sản phẩm test",
                MinQuantity = 10,
                MaxQuantity = 5,
                CategoryID = 1,
                Unit = "Hộp",
                Image = null,
                ImageA = null,
                ImageB = null,
                ImageC = null,
                ImageD = null,
                ImageE = null,
                ProductDescription = "abc",
                Status = true,
            };

            // Act
            var result = await _productService.AddProductAsync(dto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Số lượng tối thiểu"));
            Assert.False(result.Data);
        }

        [Test]
        public async Task AddProductAsync_CategoryNotFound_ShouldReturn200()
        {
            // Arrange
            var dto = new ProductDTOView
            {
                ProductName = "Sản phẩm test",
                MinQuantity = 1,
                MaxQuantity = 5,
                CategoryID = 999,
                Unit = "Hộp",
                Image = null,
                ProductDescription = "abc",
                Status = true,

            };

            var emptyCategory = new List<PMS.Core.Domain.Entities.Category>().AsQueryable().ToAsyncQueryable();
            var categorySet = MockHelper.ToMockDbSet(emptyCategory);
            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(categorySet.Object);

            // Act
            var result = await _productService.AddProductAsync(dto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Danh mục không tồn tại"));
            Assert.False(result.Data);
        }

        [Test]
        public async Task AddProductAsync_ProductNameExists_ShouldReturn200()
        {
            // Arrange
            var dto = new ProductDTOView
            {
                ProductName = "Trùng tên",
                MinQuantity = 1,
                MaxQuantity = 10,
                CategoryID = 1,
                Unit = "Hộp",
                Image = null,
                ProductDescription = "abc",
                Status = true,
            };

            var categories = new List<PMS.Core.Domain.Entities.Category> { new PMS.Core.Domain.Entities.Category { CategoryID = 1, Name  = "Cat1" } }.ToAsyncQueryable();
            var existingProducts = new List<PMS.Core.Domain.Entities.Product> { new PMS.Core.Domain.Entities.Product { ProductName = "Trùng tên", MinQuantity = 1,
                MaxQuantity = 10,
                TotalCurrentQuantity = 10,
                CategoryID = 1,
                Unit = "Hộp",
                Image = "",
                ProductDescription = "abc",
                Status = true,
            } }.ToAsyncQueryable();

            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(categories.ToMockDbSet().Object);
            UnitOfWorkMock.Setup(x => x.Product.Query()).Returns(existingProducts.ToMockDbSet().Object);

            // Act
            var result = await _productService.AddProductAsync(dto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Tên trùng"));
            Assert.False(result.Data);
        }

        [Test]
        public async Task AddProductAsync_ValidData_ShouldReturnSuccess()
        {
            // Arrange
            var dto = new ProductDTOView
            {
                ProductName = "Sản phẩm mới",
                Unit = "Cái",
                ProductDescription = "Mô tả",
                CategoryID = 1,
                MinQuantity = 1,
                MaxQuantity = 10,
                Image = null,
                Status = true
            };

            var categories = new List<PMS.Core.Domain.Entities.Category> { new PMS.Core.Domain.Entities.Category { CategoryID = 1, Name= "Cat1" } }.ToAsyncQueryable();
            var products = new List<PMS.Core.Domain.Entities.Product>().ToAsyncQueryable();

            var categorySet = MockHelper.ToMockDbSet(categories);
            var productSet = MockHelper.ToMockDbSet(products);

            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(categorySet.Object);
            UnitOfWorkMock.Setup(x => x.Product.Query()).Returns(productSet.Object);
            UnitOfWorkMock.Setup(x => x.Product.AddAsync(It.IsAny<PMS.Core.Domain.Entities.Product>())).Returns(Task.CompletedTask);

            // Act
            var result = await _productService.AddProductAsync(dto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Data, Is.True);
            Assert.That(result.Message, Does.Contain("Thêm mới sản phẩm thành công"));
            UnitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }
    }

}
