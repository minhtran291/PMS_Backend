using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using PMS.Application.DTOs.Category;
using PMS.Application.Services.Category;
using PMS.Tests.TestBase;

namespace PMS.Tests.Application.Services.Category
{
    [TestFixture]
    public class CategoryServiceTests : ServiceTestBase
    {
        private CategoryService _categoryService;

        [SetUp]
        public override void BaseSetup()
        {
            base.BaseSetup();
            _categoryService = new CategoryService(UnitOfWorkMock.Object, MapperMock.Object);
        }



        [Test]
        public async Task AddAsync_NullCategory_ShouldReturn500()
        {
            // Act
            var result = await _categoryService.AddAsync(null);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("kiểm tra lại"));
            Assert.False(result.Data);
        }

        [Test]
        public async Task AddAsync_CategoryNameAlreadyExists_ShouldReturn200()
        {
            // Arrange
            var dto = new CategoryDTO { Name = "Thuốc", Description = "Danh mục thuốc" };
            var existing = new List<PMS.Core.Domain.Entities.Category> { new PMS.Core.Domain.Entities.Category { Name = "Thuốc" } }.AsQueryable().ToAsyncQueryable();

            var mockSet = MockHelper.ToMockDbSet(existing);
            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(mockSet.Object);

            // Act
            var result = await _categoryService.AddAsync(dto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("đã tồn tại"));
            Assert.False(result.Data);
        }

        [Test]
        public async Task AddAsync_ValidData_ShouldReturnSuccess()
        {
            // Arrange
            var dto = new CategoryDTO { Name = "Thực phẩm", Description = "Mô tả test" };

            var categories = new List<PMS.Core.Domain.Entities.Category>().AsQueryable().ToAsyncQueryable();
            var mockSet = MockHelper.ToMockDbSet(categories);

            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(mockSet.Object);
            UnitOfWorkMock.Setup(x => x.Category.AddAsync(It.IsAny<PMS.Core.Domain.Entities.Category>())).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _categoryService.AddAsync(dto);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Thành công"));
            Assert.True(result.Data);
            UnitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }


        [Test]
        public async Task DeleteCategoriesWithNoReference_CategoryNotFound_ShouldReturn404()
        {
            // Arrange
            var emptyCate = new List<PMS.Core.Domain.Entities.Category>().AsQueryable().ToAsyncQueryable();
            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(emptyCate.ToMockDbSet().Object);

            // Act
            var result = await _categoryService.DeleteCategoriesWithNoReference(99);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Does.Contain("không tồn tại"));
            Assert.False(result.Success);
        }

        [Test]
        public async Task DeleteCategoriesWithNoReference_HasProducts_ShouldReturn400()
        {
            // Arrange
            var cateWithProduct = new PMS.Core.Domain.Entities.Category
            {
                CategoryID = 1,
                Name = "Danh mục test",
                Products = new List<PMS.Core.Domain.Entities.Product> { new PMS.Core.Domain.Entities.Product { ProductID = 1 , ProductName = "SP1", MaxQuantity=1000, MinQuantity=10, Status= true, TotalCurrentQuantity=200, Unit="Hộp" } }
            };
            var data = new List<PMS.Core.Domain.Entities.Category> { cateWithProduct }.AsQueryable().ToAsyncQueryable();

            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(data.ToMockDbSet().Object);

            // Act
            var result = await _categoryService.DeleteCategoriesWithNoReference(1);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("Không thể xóa"));
            Assert.False(result.Success);
        }

        [Test]
        public async Task DeleteCategoriesWithNoReference_NoProducts_ShouldReturnSuccess()
        {
            // Arrange
            var cate = new PMS.Core.Domain.Entities.Category
            {
                CategoryID = 2,
                Name = "Không có SP",
                Products = new List<PMS.Core.Domain.Entities.Product>() 
            };
            var data = new List<PMS.Core.Domain.Entities.Category> { cate }.AsQueryable().ToAsyncQueryable();

            UnitOfWorkMock.Setup(x => x.Category.Query()).Returns(data.ToMockDbSet().Object);
            UnitOfWorkMock.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _categoryService.DeleteCategoriesWithNoReference(2);

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Thành công"));
            Assert.True(result.Success);
            UnitOfWorkMock.Verify(x => x.Category.Remove(It.IsAny<PMS.Core.Domain.Entities.Category>()), Times.Once);
        }


        [Test]
        public async Task GetAllAsync_NoCategories_ShouldReturnMessageEmpty()
        {
            // Arrange
            UnitOfWorkMock
                .Setup(x => x.Category.GetAllAsync())
                .ReturnsAsync(new List<PMS.Core.Domain.Entities.Category>()); 

            // Act
            var result = await _categoryService.GetAllAsync();

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Hiện chưa có loại sản phẩm nào"));
            Assert.IsNull(result.Data);
        }

        [Test]
        public async Task GetAllAsync_HasCategories_ShouldReturnSuccess()
        {
            // Arrange
            var categories = new List<PMS.Core.Domain.Entities.Category>
        {
            new PMS.Core.Domain.Entities.Category { CategoryID = 1, Name = "Thuốc", Description = "Nhóm thuốc", Status = true },
            new PMS.Core.Domain.Entities.Category { CategoryID = 2, Name = "Dụng cụ", Description = "Thiết bị", Status = false }
        };

            UnitOfWorkMock
                .Setup(x => x.Category.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _categoryService.GetAllAsync();

            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Does.Contain("Thành công"));
            Assert.IsNotNull(result.Data);
            Assert.That(result.Data.Count(), Is.EqualTo(2));
            Assert.That(result.Data.First().Name, Is.EqualTo("Thuốc"));
        }

        [Test]
        public void GetAllAsync_WhenExceptionThrown_ShouldThrowException()
        {
            // Arrange
            UnitOfWorkMock
                .Setup(x => x.Category.GetAllAsync())
                .ThrowsAsync(new Exception("DB lỗi"));

            // Act + Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _categoryService.GetAllAsync());
            Assert.That(ex.Message, Does.Contain("Lỗi khi lấy loại sản phẩm"));
        }
    }
}

