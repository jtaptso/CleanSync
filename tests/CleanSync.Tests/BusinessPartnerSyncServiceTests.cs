using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;
using CleanSync.Application.Services;
using CleanSync.Domain.Entities;
using CleanSync.Domain.Interfaces;
using Moq;
using Xunit;

namespace CleanSync.Tests;

public class BusinessPartnerSyncServiceTests
{
    private readonly Mock<IEcommerceBusinessPartnerService> _mockEcommerceService;
    private readonly Mock<ISapBusinessPartnerService> _mockSapService;
    private readonly Mock<IBusinessPartnerRepository> _mockPartnerRepository;
    private readonly Mock<ISyncLogRepository> _mockSyncLogRepository;
    private readonly BusinessPartnerSyncService _service;

    public BusinessPartnerSyncServiceTests()
    {
        _mockEcommerceService = new Mock<IEcommerceBusinessPartnerService>();
        _mockSapService = new Mock<ISapBusinessPartnerService>();
        _mockPartnerRepository = new Mock<IBusinessPartnerRepository>();
        _mockSyncLogRepository = new Mock<ISyncLogRepository>();

        _service = new BusinessPartnerSyncService(
            _mockEcommerceService.Object,
            _mockSapService.Object,
            _mockPartnerRepository.Object,
            _mockSyncLogRepository.Object);
    }

    [Fact]
    public async Task SyncFromEcommerceToSapAsync_NewCustomers_CreatesInSapAndSaves()
    {
        // Arrange
        var customers = new List<EcommerceCustomerDto>
        {
            new()
            {
                Id = "shopify_001",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                Phone = "+1-555-0100",
                Company = "Doe Corp",
                DefaultAddress = new EcommerceAddressDto
                {
                    Address1 = "123 Main St",
                    City = "New York",
                    Country = "US",
                    Zip = "10001"
                },
                Source = "Shopify"
            }
        };

        _mockEcommerceService.Setup(x => x.GetCustomersAsync()).ReturnsAsync(customers);
        _mockPartnerRepository.Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockPartnerRepository.Setup(x => x.GetByCardCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockSapService.Setup(x => x.CreateAsync(It.IsAny<SapBusinessPartnerDto>()))
            .ReturnsAsync(new SapBusinessPartnerDto { CardCode = "SH00001" });
        _mockPartnerRepository.Setup(x => x.AddAsync(It.IsAny<BusinessPartner>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncFromEcommerceToSapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        _mockSapService.Verify(x => x.CreateAsync(It.IsAny<SapBusinessPartnerDto>()), Times.Once);
        _mockPartnerRepository.Verify(x => x.AddAsync(It.Is<BusinessPartner>(p => p.CardCode == "SH00001")), Times.Once);
    }

    [Fact]
    public async Task SyncFromEcommerceToSapAsync_ExistingCustomer_UpdatesInSap()
    {
        // Arrange
        var existingPartner = new BusinessPartner
        {
            Id = 1,
            CardCode = "SH00001",
            ExternalId = "shopify_001",
            Source = "Shopify",
            SyncStatus = SyncStatus.Synced
        };

        var customers = new List<EcommerceCustomerDto>
        {
            new()
            {
                Id = "shopify_001",
                Email = "john.updated@example.com",
                FirstName = "John",
                LastName = "Doe",
                Source = "Shopify"
            }
        };

        _mockEcommerceService.Setup(x => x.GetCustomersAsync()).ReturnsAsync(customers);
        _mockPartnerRepository.Setup(x => x.GetByExternalIdAsync("shopify_001", "Shopify"))
            .ReturnsAsync(existingPartner);
        _mockSapService.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<SapBusinessPartnerDto>()))
            .ReturnsAsync(new SapBusinessPartnerDto { CardCode = "SH00001" });
        _mockPartnerRepository.Setup(x => x.UpdateAsync(It.IsAny<BusinessPartner>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncFromEcommerceToSapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.SuccessCount);
        _mockSapService.Verify(x => x.UpdateAsync("SH00001", It.IsAny<SapBusinessPartnerDto>()), Times.Once);
    }

    [Fact]
    public async Task SyncFromEcommerceToSapAsync_PartialFailure_ContinuesProcessing()
    {
        // Arrange
        var customers = new List<EcommerceCustomerDto>
        {
            new() { Id = "shopify_001", Email = "john@example.com", FirstName = "John", LastName = "Doe", Source = "Shopify" },
            new() { Id = "shopify_002", Email = "jane@example.com", FirstName = "Jane", LastName = "Smith", Source = "Shopify" }
        };

        _mockEcommerceService.Setup(x => x.GetCustomersAsync()).ReturnsAsync(customers);
        _mockPartnerRepository.Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockPartnerRepository.Setup(x => x.GetByCardCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        
        // First customer succeeds, second fails
        _mockSapService.SetupSequence(x => x.CreateAsync(It.IsAny<SapBusinessPartnerDto>()))
            .ReturnsAsync(new SapBusinessPartnerDto { CardCode = "SH00001" })
            .ThrowsAsync(new Exception("SAP Error"));
            
        _mockPartnerRepository.Setup(x => x.AddAsync(It.IsAny<BusinessPartner>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncFromEcommerceToSapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
    }

    [Fact]
    public async Task SyncFromEcommerceToSapAsync_EmptyEcommerce_ReturnsEmptyResult()
    {
        // Arrange
        _mockEcommerceService.Setup(x => x.GetCustomersAsync()).ReturnsAsync(new List<EcommerceCustomerDto>());
        _mockSyncLogRepository.Setup(x => x.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncFromEcommerceToSapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public async Task SyncFromEcommerceToSapAsync_ShopifyCustomer_GeneratesSHCode()
    {
        // Arrange
        var customer = new EcommerceCustomerDto
        {
            Id = "shopify_test",
            Email = "john@example.com",
            Source = "Shopify"
        };

        _mockEcommerceService.Setup(x => x.GetCustomersAsync()).ReturnsAsync(new List<EcommerceCustomerDto> { customer });
        _mockPartnerRepository.Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockPartnerRepository.Setup(x => x.GetByCardCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockSapService.Setup(x => x.CreateAsync(It.IsAny<SapBusinessPartnerDto>()))
            .ReturnsAsync(new SapBusinessPartnerDto { CardCode = "SH00001" });
        _mockPartnerRepository.Setup(x => x.AddAsync(It.IsAny<BusinessPartner>())).Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.AddAsync(It.IsAny<SyncLog>())).Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncLog>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncFromEcommerceToSapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        _mockPartnerRepository.Verify(
            x => x.AddAsync(It.Is<BusinessPartner>(p => p.CardCode.StartsWith("SH"))), 
            Times.Once);
    }

    [Fact]
    public async Task SyncFromEcommerceToSapAsync_AmazonCustomer_GeneratesAMZCode()
    {
        // Arrange
        var customer = new EcommerceCustomerDto
        {
            Id = "amazon_test",
            Email = "bob@amazon.com",
            Source = "Amazon"
        };

        _mockEcommerceService.Setup(x => x.GetCustomersAsync()).ReturnsAsync(new List<EcommerceCustomerDto> { customer });
        _mockPartnerRepository.Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockPartnerRepository.Setup(x => x.GetByCardCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((BusinessPartner?)null);
        _mockSapService.Setup(x => x.CreateAsync(It.IsAny<SapBusinessPartnerDto>()))
            .ReturnsAsync(new SapBusinessPartnerDto { CardCode = "AMZ00001" });
        _mockPartnerRepository.Setup(x => x.AddAsync(It.IsAny<BusinessPartner>())).Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.AddAsync(It.IsAny<SyncLog>())).Returns(Task.CompletedTask);
        _mockSyncLogRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncLog>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncFromEcommerceToSapAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        _mockPartnerRepository.Verify(
            x => x.AddAsync(It.Is<BusinessPartner>(p => p.CardCode.StartsWith("AMZ"))), 
            Times.Once);
    }
}
