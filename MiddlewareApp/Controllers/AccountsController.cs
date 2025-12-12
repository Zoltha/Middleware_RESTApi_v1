using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using MiddlewareApp.Data;
using MiddlewareApp.Models;
using MiddlewareApp.Services;

namespace MiddlewareApp.Controllers
{
    /// <summary>
    /// Controller for managing Dynamics 365 CRM Account entities
    /// Implements async CRUD operations with idempotency support
    /// </summary>
    [RoutePrefix("api/accounts")]
    public class AccountsController : ApiController
    {
        private readonly DynamicsCrmService _crmService;
        private readonly SqlDbContext _dbContext;

        public AccountsController()
        {
            _crmService = new DynamicsCrmService();
            _dbContext = new SqlDbContext();
        }

        /// <summary>
        /// Get all accounts from Dynamics CRM
        /// </summary>
        /// <returns>List of account view models</returns>
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAccounts()
        {
            try
            {
                LoggingService.LogInformation("GET /api/accounts - Fetching all accounts");
                var accounts = await _crmService.GetAccountsAsync();
                LoggingService.LogInformation($"Successfully retrieved {accounts.Count} accounts");
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve accounts", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get a specific account by ID
        /// </summary>
        /// <param name="id">Account GUID</param>
        /// <returns>Account view model</returns>
        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IHttpActionResult> GetAccount(Guid id)
        {
            try
            {
                LoggingService.LogInformation($"GET /api/accounts/{id} - Fetching account");
                var account = await _crmService.GetAccountByIdAsync(id);
                
                if (account == null)
                {
                    LoggingService.LogWarning($"Account {id} not found");
                    return NotFound();
                }

                LoggingService.LogInformation($"Successfully retrieved account {id}");
                return Ok(account);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to retrieve account {id}", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Create a new account in Dynamics CRM with idempotency support
        /// </summary>
        /// <param name="account">Account view model</param>
        /// <returns>Created account</returns>
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateAccount([FromBody] AccountViewModel account)
        {
            if (account == null)
            {
                return BadRequest("Account data is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check for idempotency key
                var idempotencyKey = GetIdempotencyKey();
                
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    LoggingService.LogInformation($"POST /api/accounts with idempotency key: {idempotencyKey}");
                    
                    // Check if this request was already processed
                    var existingRecord = await _dbContext.IdempotencyRecords
                        .Where(r => r.IdempotencyKey == idempotencyKey && r.ExpiresAt > DateTime.UtcNow)
                        .FirstOrDefaultAsync();

                    if (existingRecord != null)
                    {
                        LoggingService.LogInformation($"Returning cached response for idempotency key: {idempotencyKey}");
                        
                        // Return the cached response
                        var cachedAccount = JsonConvert.DeserializeObject<AccountViewModel>(existingRecord.ResponseBody);
                        return Content((HttpStatusCode)existingRecord.StatusCode, cachedAccount);
                    }
                }
                else
                {
                    LoggingService.LogInformation("POST /api/accounts - Creating new account");
                }

                // Create the account in CRM
                var createdAccount = await _crmService.CreateAccountAsync(account);

                // Store idempotency record if key was provided
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    var idempotencyRecord = new IdempotencyRecord
                    {
                        IdempotencyKey = idempotencyKey,
                        ResourceId = createdAccount.AccountId.ToString(),
                        StatusCode = (int)HttpStatusCode.Created,
                        ResponseBody = JsonConvert.SerializeObject(createdAccount),
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    };

                    _dbContext.IdempotencyRecords.Add(idempotencyRecord);
                    await _dbContext.SaveChangesAsync();
                    
                    LoggingService.LogInformation($"Stored idempotency record for key: {idempotencyKey}");
                }

                LoggingService.LogInformation($"Successfully created account {createdAccount.AccountId}");
                return Created($"api/accounts/{createdAccount.AccountId}", createdAccount);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to create account", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Update an existing account in Dynamics CRM
        /// </summary>
        /// <param name="id">Account GUID</param>
        /// <param name="account">Updated account data</param>
        /// <returns>Updated account</returns>
        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IHttpActionResult> UpdateAccount(Guid id, [FromBody] AccountViewModel account)
        {
            if (account == null)
            {
                return BadRequest("Account data is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                LoggingService.LogInformation($"PUT /api/accounts/{id} - Updating account");

                // Check if account exists
                var existingAccount = await _crmService.GetAccountByIdAsync(id);
                if (existingAccount == null)
                {
                    LoggingService.LogWarning($"Account {id} not found for update");
                    return NotFound();
                }

                // Update the account
                var updatedAccount = await _crmService.UpdateAccountAsync(id, account);
                
                LoggingService.LogInformation($"Successfully updated account {id}");
                return Ok(updatedAccount);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to update account {id}", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Partially update an account using PATCH
        /// </summary>
        /// <param name="id">Account GUID</param>
        /// <param name="updates">Partial account data</param>
        /// <returns>Updated account</returns>
        [HttpPatch]
        [Route("{id:guid}")]
        public async Task<IHttpActionResult> PatchAccount(Guid id, [FromBody] AccountViewModel updates)
        {
            if (updates == null)
            {
                return BadRequest("Update data is required");
            }

            try
            {
                LoggingService.LogInformation($"PATCH /api/accounts/{id} - Partially updating account");

                // Get existing account
                var existingAccount = await _crmService.GetAccountByIdAsync(id);
                if (existingAccount == null)
                {
                    LoggingService.LogWarning($"Account {id} not found for patch");
                    return NotFound();
                }

                // Merge updates with existing data (only non-null values)
                MergeAccountUpdates(existingAccount, updates);

                // Update the account
                var updatedAccount = await _crmService.UpdateAccountAsync(id, existingAccount);
                
                LoggingService.LogInformation($"Successfully patched account {id}");
                return Ok(updatedAccount);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to patch account {id}", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Delete an account from Dynamics CRM
        /// </summary>
        /// <param name="id">Account GUID</param>
        /// <returns>No content on success</returns>
        [HttpDelete]
        [Route("{id:guid}")]
        public async Task<IHttpActionResult> DeleteAccount(Guid id)
        {
            try
            {
                LoggingService.LogInformation($"DELETE /api/accounts/{id} - Deleting account");

                var deleted = await _crmService.DeleteAccountAsync(id);
                
                if (!deleted)
                {
                    LoggingService.LogWarning($"Account {id} not found for deletion");
                    return NotFound();
                }

                LoggingService.LogInformation($"Successfully deleted account {id}");
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to delete account {id}", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Batch retrieve multiple accounts by IDs
        /// </summary>
        /// <param name="ids">Comma-separated list of GUIDs</param>
        /// <returns>List of accounts</returns>
        [HttpGet]
        [Route("batch")]
        public async Task<IHttpActionResult> GetAccountsBatch([FromUri] string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return BadRequest("IDs parameter is required");
            }

            try
            {
                // Parse GUIDs from comma-separated string
                var guidList = ids.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => Guid.TryParse(s, out _))
                    .Select(Guid.Parse)
                    .ToList();

                if (guidList.Count == 0)
                {
                    return BadRequest("No valid GUIDs provided");
                }

                LoggingService.LogInformation($"GET /api/accounts/batch - Fetching {guidList.Count} accounts in parallel");

                var accounts = await _crmService.GetAccountsByIdsAsync(guidList);
                
                LoggingService.LogInformation($"Successfully retrieved {accounts.Count} accounts in batch");
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve accounts in batch", ex);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get the idempotency key from request headers
        /// </summary>
        private string GetIdempotencyKey()
        {
            if (Request.Headers.Contains("Idempotency-Key"))
            {
                return Request.Headers.GetValues("Idempotency-Key").FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Merge non-null updates into existing account
        /// </summary>
        private void MergeAccountUpdates(AccountViewModel existing, AccountViewModel updates)
        {
            if (!string.IsNullOrEmpty(updates.Name))
                existing.Name = updates.Name;
            if (!string.IsNullOrEmpty(updates.AccountNumber))
                existing.AccountNumber = updates.AccountNumber;
            if (!string.IsNullOrEmpty(updates.Email))
                existing.Email = updates.Email;
            if (!string.IsNullOrEmpty(updates.Phone))
                existing.Phone = updates.Phone;
            if (!string.IsNullOrEmpty(updates.WebsiteUrl))
                existing.WebsiteUrl = updates.WebsiteUrl;
            if (!string.IsNullOrEmpty(updates.Address1_Line1))
                existing.Address1_Line1 = updates.Address1_Line1;
            if (!string.IsNullOrEmpty(updates.Address1_City))
                existing.Address1_City = updates.Address1_City;
            if (!string.IsNullOrEmpty(updates.Address1_StateOrProvince))
                existing.Address1_StateOrProvince = updates.Address1_StateOrProvince;
            if (!string.IsNullOrEmpty(updates.Address1_PostalCode))
                existing.Address1_PostalCode = updates.Address1_PostalCode;
            if (!string.IsNullOrEmpty(updates.Address1_Country))
                existing.Address1_Country = updates.Address1_Country;
            if (!string.IsNullOrEmpty(updates.Industry))
                existing.Industry = updates.Industry;
            if (updates.Revenue.HasValue)
                existing.Revenue = updates.Revenue;
            if (updates.NumberOfEmployees.HasValue)
                existing.NumberOfEmployees = updates.NumberOfEmployees;
            if (!string.IsNullOrEmpty(updates.Description))
                existing.Description = updates.Description;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
