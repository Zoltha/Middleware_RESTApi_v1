using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiddlewareApp.Models;
using MiddlewareApp.Services;

namespace MiddlewareApp.Tests
{
    /// <summary>
    /// Unit tests for DynamicsCrmService
    /// Note: These tests require actual CRM configuration or mocking of HTTP client
    /// For demonstration, we're testing the service instantiation and basic logic
    /// </summary>
    [TestClass]
    public class DynamicsCrmServiceTests
    {
        private DynamicsCrmService _crmService;

        [TestInitialize]
        public void Setup()
        {
            // Initialize the service (will use config from appsettings.json)
            _crmService = new DynamicsCrmService();
        }

        [TestMethod]
        public void DynamicsCrmService_Constructor_ShouldCreateInstance()
        {
            // Act & Assert
            _crmService.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetAccountsAsync_WhenCrmUnavailable_ShouldThrowException()
        {
            // Arrange
            var service = new DynamicsCrmService();

            // Act & Assert
            // This will fail with CRM connectivity issues if CRM is not configured
            // In a real test, we would mock the HttpClient
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await service.GetAccountsAsync();
            });
        }

        [TestMethod]
        public async Task GetAccountByIdAsync_WithInvalidGuid_ShouldHandleGracefully()
        {
            // Arrange
            var service = new DynamicsCrmService();
            var invalidId = Guid.Empty;

            // Act & Assert
            // This will fail with CRM connectivity issues if CRM is not configured
            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await service.GetAccountByIdAsync(invalidId);
            });
        }

        [TestMethod]
        public void AccountViewModel_Validation_ShouldRequireName()
        {
            // Arrange
            var account = new AccountViewModel
            {
                Name = null,
                Email = "test@example.com"
            };

            // Act
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(account);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                account, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        [TestMethod]
        public void AccountViewModel_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var account = new AccountViewModel
            {
                Name = "Test Account",
                Email = "test@example.com",
                Phone = "123-456-7890"
            };

            // Act
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(account);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                account, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeTrue();
            validationResults.Should().BeEmpty();
        }

        [TestMethod]
        public void AccountViewModel_WithInvalidEmail_ShouldFailValidation()
        {
            // Arrange
            var account = new AccountViewModel
            {
                Name = "Test Account",
                Email = "invalid-email"
            };

            // Act
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(account);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                account, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(r => r.MemberNames.Contains("Email"));
        }

        [TestMethod]
        public void AccountViewModel_WithTooLongName_ShouldFailValidation()
        {
            // Arrange
            var account = new AccountViewModel
            {
                Name = new string('A', 161), // Max length is 160
                Email = "test@example.com"
            };

            // Act
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(account);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                account, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        [TestMethod]
        public async Task GetAccountsByIdsAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var service = new DynamicsCrmService();
            var emptyList = new List<Guid>();

            // Act
            var result = await service.GetAccountsByIdsAsync(emptyList);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void AccountViewModel_Properties_ShouldBeSettable()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new AccountViewModel();

            // Act
            account.AccountId = accountId;
            account.Name = "Test Company";
            account.AccountNumber = "ACC-001";
            account.Email = "info@testcompany.com";
            account.Phone = "555-1234";
            account.WebsiteUrl = "https://testcompany.com";
            account.Address1_Line1 = "123 Main St";
            account.Address1_City = "Seattle";
            account.Address1_StateOrProvince = "WA";
            account.Address1_PostalCode = "98101";
            account.Address1_Country = "USA";
            account.Industry = "Technology";
            account.Revenue = 1000000.50m;
            account.NumberOfEmployees = 50;
            account.Description = "Test description";

            // Assert
            account.AccountId.Should().Be(accountId);
            account.Name.Should().Be("Test Company");
            account.AccountNumber.Should().Be("ACC-001");
            account.Email.Should().Be("info@testcompany.com");
            account.Phone.Should().Be("555-1234");
            account.WebsiteUrl.Should().Be("https://testcompany.com");
            account.Address1_Line1.Should().Be("123 Main St");
            account.Address1_City.Should().Be("Seattle");
            account.Address1_StateOrProvince.Should().Be("WA");
            account.Address1_PostalCode.Should().Be("98101");
            account.Address1_Country.Should().Be("USA");
            account.Industry.Should().Be("Technology");
            account.Revenue.Should().Be(1000000.50m);
            account.NumberOfEmployees.Should().Be(50);
            account.Description.Should().Be("Test description");
        }
    }
}
