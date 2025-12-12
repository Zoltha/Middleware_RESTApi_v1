using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using MiddlewareApp.Models;

namespace MiddlewareApp.Services
{
    /// <summary>
    /// Service for interacting with Dynamics 365 CRM using REST API with OAuth 2.0 authentication
    /// </summary>
    public class DynamicsCrmService
    {
        private readonly string _resource;
        private readonly string _authority;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _apiVersion;
        private readonly HttpClient _httpClient;
        private readonly MemoryCache _cache;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        private const string TokenCacheKey = "CrmAccessToken";
        private const string EntityName = "accounts";

        /// <summary>
        /// Initialize the CRM service with configuration
        /// </summary>
        public DynamicsCrmService()
        {
            var config = LoadConfiguration();
            _resource = config.Resource;
            _authority = config.Authority;
            _clientId = config.ClientId;
            _clientSecret = config.ClientSecret;
            _apiVersion = config.ApiVersion;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _cache = MemoryCache.Default;

            // Configure Polly retry policy for transient faults
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => 
                    (int)r.StatusCode >= 500 || 
                    r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        LoggingService.LogWarning(
                            $"Retry {retryCount} after {timespan.TotalSeconds}s. Status: {outcome.Result?.StatusCode}");
                    });
        }

        /// <summary>
        /// Get an access token using OAuth 2.0 client credentials flow with caching
        /// </summary>
        private async Task<string> GetAccessTokenAsync()
        {
            // Check cache first
            var cachedToken = _cache.Get(TokenCacheKey) as string;
            if (!string.IsNullOrEmpty(cachedToken))
            {
                LoggingService.LogDebug("Using cached access token");
                return cachedToken;
            }

            LoggingService.LogInformation("Requesting new access token from Azure AD");

            try
            {
                var tokenEndpoint = $"{_authority}/oauth2/v2.0/token";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("scope", $"{_resource}/.default"),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<JObject>(responseBody);
                var accessToken = tokenResponse["access_token"].ToString();
                var expiresIn = int.Parse(tokenResponse["expires_in"].ToString());

                // Cache token with 5 minute buffer before expiry
                var expirationTime = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 300);
                _cache.Set(TokenCacheKey, accessToken, expirationTime);

                LoggingService.LogInformation($"Access token obtained and cached until {expirationTime}");
                return accessToken;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to obtain access token", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all accounts from CRM with parallel batch retrieval
        /// </summary>
        public async Task<List<AccountViewModel>> GetAccountsAsync()
        {
            try
            {
                LoggingService.LogInformation("Fetching accounts from CRM");
                var token = await GetAccessTokenAsync();
                var url = $"{_resource}/api/data/v{_apiVersion}/{EntityName}?$select=accountid,name,accountnumber,emailaddress1,telephone1,websiteurl,address1_line1,address1_city,address1_stateorprovince,address1_postalcode,address1_country,industrycode,revenue,numberofemployees,description,createdon,modifiedon&$top=100";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("OData-MaxVersion", "4.0");
                request.Headers.Add("OData-Version", "4.0");
                request.Headers.Add("Prefer", "odata.include-annotations=*");

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(content);
                var accounts = result["value"].ToObject<List<JObject>>();

                var accountList = accounts.Select(MapToAccountViewModel).ToList();
                LoggingService.LogInformation($"Retrieved {accountList.Count} accounts from CRM");

                return accountList;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to fetch accounts from CRM", ex);
                throw;
            }
        }

        /// <summary>
        /// Get a single account by ID with caching
        /// </summary>
        public async Task<AccountViewModel> GetAccountByIdAsync(Guid accountId)
        {
            var cacheKey = $"Account_{accountId}";
            var cached = _cache.Get(cacheKey) as AccountViewModel;
            if (cached != null)
            {
                LoggingService.LogDebug($"Retrieved account {accountId} from cache");
                return cached;
            }

            try
            {
                LoggingService.LogInformation($"Fetching account {accountId} from CRM");
                var token = await GetAccessTokenAsync();
                var url = $"{_resource}/api/data/v{_apiVersion}/{EntityName}({accountId})?$select=accountid,name,accountnumber,emailaddress1,telephone1,websiteurl,address1_line1,address1_city,address1_stateorprovince,address1_postalcode,address1_country,industrycode,revenue,numberofemployees,description,createdon,modifiedon";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("OData-MaxVersion", "4.0");
                request.Headers.Add("OData-Version", "4.0");

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var accountData = JsonConvert.DeserializeObject<JObject>(content);
                var account = MapToAccountViewModel(accountData);

                // Cache for 30 minutes
                _cache.Set(cacheKey, account, DateTimeOffset.UtcNow.AddMinutes(30));

                LoggingService.LogInformation($"Retrieved account {accountId} from CRM");
                return account;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to fetch account {accountId} from CRM", ex);
                throw;
            }
        }

        /// <summary>
        /// Create a new account in CRM
        /// </summary>
        public async Task<AccountViewModel> CreateAccountAsync(AccountViewModel account)
        {
            try
            {
                LoggingService.LogInformation($"Creating account '{account.Name}' in CRM");
                var token = await GetAccessTokenAsync();
                var url = $"{_resource}/api/data/v{_apiVersion}/{EntityName}";

                var accountData = MapFromAccountViewModel(account);
                var json = JsonConvert.SerializeObject(accountData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("OData-MaxVersion", "4.0");
                request.Headers.Add("OData-Version", "4.0");
                request.Headers.Add("Prefer", "return=representation");
                request.Content = content;

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var createdAccount = JsonConvert.DeserializeObject<JObject>(responseContent);
                var result = MapToAccountViewModel(createdAccount);

                LoggingService.LogInformation($"Created account {result.AccountId} in CRM");
                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to create account in CRM", ex);
                throw;
            }
        }

        /// <summary>
        /// Update an existing account in CRM
        /// </summary>
        public async Task<AccountViewModel> UpdateAccountAsync(Guid accountId, AccountViewModel account)
        {
            try
            {
                LoggingService.LogInformation($"Updating account {accountId} in CRM");
                var token = await GetAccessTokenAsync();
                var url = $"{_resource}/api/data/v{_apiVersion}/{EntityName}({accountId})";

                var accountData = MapFromAccountViewModel(account);
                var json = JsonConvert.SerializeObject(accountData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("OData-MaxVersion", "4.0");
                request.Headers.Add("OData-Version", "4.0");
                request.Headers.Add("Prefer", "return=representation");
                request.Content = content;

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));
                response.EnsureSuccessStatusCode();

                // Invalidate cache
                var cacheKey = $"Account_{accountId}";
                _cache.Remove(cacheKey);

                // Fetch updated account
                var updatedAccount = await GetAccountByIdAsync(accountId);
                LoggingService.LogInformation($"Updated account {accountId} in CRM");

                return updatedAccount;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to update account {accountId} in CRM", ex);
                throw;
            }
        }

        /// <summary>
        /// Delete an account from CRM
        /// </summary>
        public async Task<bool> DeleteAccountAsync(Guid accountId)
        {
            try
            {
                LoggingService.LogInformation($"Deleting account {accountId} from CRM");
                var token = await GetAccessTokenAsync();
                var url = $"{_resource}/api/data/v{_apiVersion}/{EntityName}({accountId})";

                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("OData-MaxVersion", "4.0");
                request.Headers.Add("OData-Version", "4.0");

                var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                response.EnsureSuccessStatusCode();

                // Invalidate cache
                var cacheKey = $"Account_{accountId}";
                _cache.Remove(cacheKey);

                LoggingService.LogInformation($"Deleted account {accountId} from CRM");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to delete account {accountId} from CRM", ex);
                throw;
            }
        }

        /// <summary>
        /// Batch retrieve multiple accounts in parallel
        /// </summary>
        public async Task<List<AccountViewModel>> GetAccountsByIdsAsync(List<Guid> accountIds)
        {
            try
            {
                LoggingService.LogInformation($"Fetching {accountIds.Count} accounts in parallel");
                var tasks = accountIds.Select(id => GetAccountByIdAsync(id));
                var accounts = await Task.WhenAll(tasks);
                return accounts.Where(a => a != null).ToList();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to fetch accounts in parallel", ex);
                throw;
            }
        }

        /// <summary>
        /// Map CRM data to AccountViewModel
        /// </summary>
        private AccountViewModel MapToAccountViewModel(JObject data)
        {
            return new AccountViewModel
            {
                AccountId = data["accountid"]?.ToObject<Guid?>(),
                Name = data["name"]?.ToString(),
                AccountNumber = data["accountnumber"]?.ToString(),
                Email = data["emailaddress1"]?.ToString(),
                Phone = data["telephone1"]?.ToString(),
                WebsiteUrl = data["websiteurl"]?.ToString(),
                Address1_Line1 = data["address1_line1"]?.ToString(),
                Address1_City = data["address1_city"]?.ToString(),
                Address1_StateOrProvince = data["address1_stateorprovince"]?.ToString(),
                Address1_PostalCode = data["address1_postalcode"]?.ToString(),
                Address1_Country = data["address1_country"]?.ToString(),
                Industry = data["industrycode"]?.ToString(),
                Revenue = data["revenue"]?.ToObject<decimal?>(),
                NumberOfEmployees = data["numberofemployees"]?.ToObject<int?>(),
                Description = data["description"]?.ToString(),
                CreatedOn = data["createdon"]?.ToObject<DateTime?>(),
                ModifiedOn = data["modifiedon"]?.ToObject<DateTime?>()
            };
        }

        /// <summary>
        /// Map AccountViewModel to CRM data format
        /// </summary>
        private JObject MapFromAccountViewModel(AccountViewModel account)
        {
            var data = new JObject();

            if (!string.IsNullOrEmpty(account.Name))
                data["name"] = account.Name;
            if (!string.IsNullOrEmpty(account.AccountNumber))
                data["accountnumber"] = account.AccountNumber;
            if (!string.IsNullOrEmpty(account.Email))
                data["emailaddress1"] = account.Email;
            if (!string.IsNullOrEmpty(account.Phone))
                data["telephone1"] = account.Phone;
            if (!string.IsNullOrEmpty(account.WebsiteUrl))
                data["websiteurl"] = account.WebsiteUrl;
            if (!string.IsNullOrEmpty(account.Address1_Line1))
                data["address1_line1"] = account.Address1_Line1;
            if (!string.IsNullOrEmpty(account.Address1_City))
                data["address1_city"] = account.Address1_City;
            if (!string.IsNullOrEmpty(account.Address1_StateOrProvince))
                data["address1_stateorprovince"] = account.Address1_StateOrProvince;
            if (!string.IsNullOrEmpty(account.Address1_PostalCode))
                data["address1_postalcode"] = account.Address1_PostalCode;
            if (!string.IsNullOrEmpty(account.Address1_Country))
                data["address1_country"] = account.Address1_Country;
            if (!string.IsNullOrEmpty(account.Industry))
                data["industrycode"] = account.Industry;
            if (account.Revenue.HasValue)
                data["revenue"] = account.Revenue.Value;
            if (account.NumberOfEmployees.HasValue)
                data["numberofemployees"] = account.NumberOfEmployees.Value;
            if (!string.IsNullOrEmpty(account.Description))
                data["description"] = account.Description;

            return data;
        }

        /// <summary>
        /// Load configuration from appsettings.json
        /// </summary>
        private CrmConfiguration LoadConfiguration()
        {
            try
            {
                var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    var json = File.ReadAllText(appSettingsPath);
                    var settings = JsonConvert.DeserializeObject<JObject>(json);
                    var crmConfig = settings["DynamicsCrm"];

                    return new CrmConfiguration
                    {
                        Resource = crmConfig["Resource"]?.ToString(),
                        Authority = crmConfig["Authority"]?.ToString(),
                        ClientId = crmConfig["ClientId"]?.ToString(),
                        ClientSecret = crmConfig["ClientSecret"]?.ToString(),
                        ApiVersion = crmConfig["ApiVersion"]?.ToString() ?? "9.2"
                    };
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to load CRM configuration", ex);
            }

            // Return default configuration
            return new CrmConfiguration
            {
                Resource = "https://yourorg.crm.dynamics.com",
                Authority = "https://login.microsoftonline.com/your-tenant-id",
                ClientId = "your-client-id",
                ClientSecret = "your-client-secret",
                ApiVersion = "9.2"
            };
        }

        private class CrmConfiguration
        {
            public string Resource { get; set; }
            public string Authority { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string ApiVersion { get; set; }
        }
    }
}
