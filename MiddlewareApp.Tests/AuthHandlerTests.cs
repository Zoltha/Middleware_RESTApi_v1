using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiddlewareApp.App_Start;

namespace MiddlewareApp.Tests
{
    [TestClass]
    public class AuthHandlerTests
    {
        private AuthHandler _authHandler;
        private HttpMessageInvoker _invoker;

        [TestInitialize]
        public void Setup()
        {
            _authHandler = new AuthHandler
            {
                InnerHandler = new TestHttpMessageHandler()
            };
            _invoker = new HttpMessageInvoker(_authHandler);
        }

        [TestMethod]
        public async Task AuthHandler_WithValidBasicAuth_ShouldAllowRequest()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:password123"));
            request.Headers.Add("Authorization", $"Basic {credentials}");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task AuthHandler_WithoutAuthHeader_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Headers.Should().Contain(h => h.Key == "WWW-Authenticate");
        }

        [TestMethod]
        public async Task AuthHandler_WithInvalidScheme_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            request.Headers.Add("Authorization", "Bearer some-token");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task AuthHandler_WithShortUsername_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("ab:password123"));
            request.Headers.Add("Authorization", $"Basic {credentials}");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task AuthHandler_WithShortPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:12345"));
            request.Headers.Add("Authorization", $"Basic {credentials}");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task AuthHandler_WithInvalidBase64_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            request.Headers.Add("Authorization", "Basic invalid-base64!!!");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task AuthHandler_WithHealthEndpoint_ShouldAllowWithoutAuth()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/health");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task AuthHandler_WithSwaggerEndpoint_ShouldAllowWithoutAuth()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/swagger");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task AuthHandler_WithMissingColonInCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("testuserpassword"));
            request.Headers.Add("Authorization", $"Basic {credentials}");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task AuthHandler_WithEmptyCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/accounts");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":"));
            request.Headers.Add("Authorization", $"Basic {credentials}");

            // Act
            var response = await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Test handler that returns OK for authenticated requests
        /// </summary>
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
