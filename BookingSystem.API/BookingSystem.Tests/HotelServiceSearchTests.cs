using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BookingSystem.Tests;

/// <summary>
/// Юнит-тесты поиска отелей: сервис обязан прокидывать все параметры
/// фильтрации и сортировки в репозиторий (регрессия на баг «null, null»).
/// </summary>
public class HotelServiceSearchTests
{
    private readonly Mock<IHotelRepository> _repo = new();
    private readonly Mock<ILoggingService> _logging = new();
    private readonly Mock<IHttpContextAccessor> _accessor = new();
    private readonly HotelService _service;

    public HotelServiceSearchTests()
    {
        _accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        _service = new HotelService(_repo.Object, _logging.Object, _accessor.Object);
        _repo.Setup(r => r.SearchHotelsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((Enumerable.Empty<Hotel>(), 0));
    }

    [Fact]
    public async Task SearchHotels_PassesLocationAndRatingToRepository()
    {
        await _service.SearchHotelsAsync(new HotelSearchDto
        {
            Location = "Минск", MinRating = 4.5m, PageNumber = 1, PageSize = 10,
        });

        _repo.Verify(r => r.SearchHotelsAsync(
            null, "Минск", 4.5m, null, null, null, 1, 10, "Rating"), Times.Once);
    }

    [Fact]
    public async Task SearchHotels_PassesPriceSortAndPaging()
    {
        await _service.SearchHotelsAsync(new HotelSearchDto
        {
            MaxPrice = 120m, SortBy = "Price", PageNumber = 2, PageSize = 5,
        });

        _repo.Verify(r => r.SearchHotelsAsync(
            null, null, null, null, null, 120m, 2, 5, "Price"), Times.Once);
    }

    [Fact]
    public async Task SearchHotels_ComputesPagination()
    {
        _repo.Setup(r => r.SearchHotelsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((Enumerable.Range(1, 7).Select(i => new Hotel { Id = i, Name = $"H{i}" }), 7));

        var result = await _service.SearchHotelsAsync(new HotelSearchDto { PageNumber = 2, PageSize = 3 });

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);   // страница 2 из 3
        Assert.True(result.HasPrevious);
    }

}
