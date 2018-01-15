using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeSheetManagementSystem.Data;
using TimeSheetManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace TimeSheetManagementSystem.APIs
{
    [Authorize("RequireAdminRole")]
    [Produces("application/json")]
    [Route("api/AccountRate/")]
    public class AccountRateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountRateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AccountRate
        [HttpGet]
        public IActionResult GetAccountRates()
        {
            var accountRates = from accountRate in _context.AccountRates
                                   select new
                                   {
                                       AccountRateId = accountRate.AccountRateId,
                                       RatePerHour = accountRate.RatePerHour,
                                       EffectiveStartDate= accountRate.EffectiveStartDate,
                                       EffectiveEndDate = accountRate.EffectiveEndDate,
                                       CustomerAccountId = accountRate.CustomerAccountId
                                   };
            return new JsonResult(accountRates);
        }

        // GET: api/AccountRate/5
        [HttpGet("GetAccountRateByAccoundRateId/{id}")]
        public IActionResult GetAccountRateByAccoundRateId([FromRoute] int id)
        {
            try
            {
                AccountRate oneAccountRate = _context.AccountRates
                     .Where(item => item.AccountRateId == id).Include(item => item.CustomerAccount).FirstOrDefault();

                var response = new
                {
                    AccountRateId = oneAccountRate.AccountRateId,
                    RatePerHour = oneAccountRate.RatePerHour,
                    EffectiveStartDate = oneAccountRate.EffectiveStartDate,
                    EffectiveEndDate = oneAccountRate.EffectiveEndDate,
                    CustomerAccountId = oneAccountRate.CustomerAccountId
                };//end of creation of the response object
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                object httpFailRequestResultMessage = new { message = "Unable to retrive account rate record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
        }
        // GET: api/AccountRate/GetAccountRateByCustomerAccountId/5
        [HttpGet("GetAccountRateByCustomerAccountId/{id}")]
        public IActionResult GetAccountRateByCustomerAccountId([FromRoute] int id)
        {//https://msdn.microsoft.com/en-us/library/cc668201.aspx
            try
            {
                //var oneAccountRate = _context.AccountRates
                //     .Where(item => item.CustomerAccountId == id).Include(item => item.CustomerAccount);

                //var response = new
                //{
                //    AccountRateId = oneAccountRate.AccountRateId,
                //    RatePerHour = oneAccountRate.RatePerHour,
                //    EffectiveStartDate = oneAccountRate.EffectiveStartDate,
                //    EffectiveEndDate = oneAccountRate.EffectiveEndDate,
                //    CustomerAccountId = oneAccountRate.CustomerAccountId

                //};//end of creation of the response object
                //return new JsonResult(response);
                //https://stackoverflow.com/questions/6359980/proper-linq-where-clauses
                var accountRates = from accountRate in _context.AccountRates
                                   where accountRate.CustomerAccountId == id
                                   select new
                                   {
                                       AccountRateId = accountRate.AccountRateId,
                                       RatePerHour = accountRate.RatePerHour,
                                       EffectiveStartDate = accountRate.EffectiveStartDate,
                                       EffectiveEndDate = accountRate.EffectiveEndDate,
                                       CustomerAccountId = accountRate.CustomerAccountId
                                   };
                return new JsonResult(accountRates);
            }
            catch (Exception ex)
            {
                object httpFailRequestResultMessage = new { message = "Unable to retrive account rate record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
        }

        // PUT: api/AccountRate/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody]string value)
        {
            int accountRateId = Int32.Parse(id);
            string databaseInnerExceptionMessage = "";
            var accountRateChangeInput = JsonConvert.DeserializeObject<dynamic>(value);
            List<object> messages = new List<object>();
            bool status = true; //This variable is used to track the overall success of all the database operations
            object response;
            
            var oneAccountRate = _context.AccountRates
                .Where(item => item.AccountRateId == accountRateId).FirstOrDefault();

            var effectiveEndDate = accountRateChangeInput.EffectiveEndDate;
            var effectiveStartDateChecking = accountRateChangeInput.EffectiveStartDate;
            if (accountRateChangeInput.EffectiveEndDate == "")
            {
                effectiveEndDate = null;
            }
            else if (effectiveEndDate < effectiveStartDateChecking)
            {
                response = new { status = "fail", message = "End date must not be earlier than start date" };
                return new JsonResult(response);
            }
            DateTime effectiveStartDate = accountRateChangeInput.EffectiveStartDate;
            if (effectiveStartDate < DateTime.Now.Date)
            {
                response = new { status = "fail", message = "Start date must not be in the past" };
                return new JsonResult(response);
            }

            oneAccountRate.RatePerHour = accountRateChangeInput.RatePerHour;
            oneAccountRate.EffectiveStartDate = accountRateChangeInput.EffectiveStartDate;
            oneAccountRate.EffectiveEndDate = effectiveEndDate;

            try
            {
                try
                {
                    //Changes are not persisted in the database until
                    //I use the following command.
                    _context.AccountRates.Update(oneAccountRate);
                    _context.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    databaseInnerExceptionMessage = ex.InnerException.Message;
                    status = false;
                    messages.Add(databaseInnerExceptionMessage);
                }
                if (status == true)
                {
                    response = new { status = "success", message = "Saved account rate record." };
                }
                else
                {
                    response = new { status = "fail", message = messages };
                }
            }
            catch (Exception outerException)
            {
                response = new { status = "fail", message = outerException.InnerException.Message };
            }
            return new JsonResult(response);
        }//End of Put()

        // POST: api/AccountRate
        [HttpPost]
        public async Task<IActionResult> PostAccountRate([FromBody] string value)
        {
            var accountRateNewInput = JsonConvert.DeserializeObject<dynamic>(value);
            object response = null;

            var newAccountRate = new AccountRate();

            int customerAccountId = accountRateNewInput.CustomerAccountId;
            CustomerAccount currentCustomerAccount = _context.CustomerAccounts
                .Where(item => item.CustomerAccountId == customerAccountId).FirstOrDefault();

            var effectiveEndDate = accountRateNewInput.EffectiveEndDate;
            var effectiveStartDateChecking = accountRateNewInput.EffectiveStartDate;
            if (accountRateNewInput.EffectiveEndDate == "")
            {
                effectiveEndDate = null;
            } else if (effectiveEndDate < effectiveStartDateChecking)
            {
                response = new { status = "fail", message = "End date must not be earlier than start date" };
                return new JsonResult(response);
            }
            DateTime effectiveStartDate = accountRateNewInput.EffectiveStartDate;
            if (effectiveStartDate < DateTime.Now.Date)
            {
                response = new { status = "fail", message = "Start date must not be in the past" };
                return new JsonResult(response);
            } 

            newAccountRate.CustomerAccountId = accountRateNewInput.CustomerAccountId;
            newAccountRate.CustomerAccount = currentCustomerAccount;
            newAccountRate.RatePerHour = accountRateNewInput.RatePerHour;
            newAccountRate.EffectiveStartDate = accountRateNewInput.EffectiveStartDate;
            newAccountRate.EffectiveEndDate = effectiveEndDate;


            try
            {
                try
                {
                    _context.AccountRates.Add(newAccountRate);
                    _context.SaveChanges();

                    response = new { status = "success", message = "Saved new account rate record." };
                }
                catch (DbUpdateException ex)
                {
                    response = new { status = "fail", message = ex.InnerException.Message + "Please enter a unique value" };
                }
            }
            catch (Exception outerException)
            {
                response = new { status = "fail", message = outerException.InnerException.Message };
            }
            return new JsonResult(response);
        }

        // DELETE: api/AccountRate/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccountRate([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var accountRate = await _context.AccountRates.SingleOrDefaultAsync(m => m.AccountRateId == id);
            if (accountRate == null)
            {
                return NotFound();
            }

            _context.AccountRates.Remove(accountRate);
            await _context.SaveChangesAsync();

            return Ok(accountRate);
        }

        private bool AccountRateExists(int id)
        {
            return _context.AccountRates.Any(e => e.AccountRateId == id);
        }
    }
}