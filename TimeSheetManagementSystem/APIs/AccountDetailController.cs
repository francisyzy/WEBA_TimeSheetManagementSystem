using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeSheetManagementSystem.Data;
using TimeSheetManagementSystem.Models;
using Newtonsoft.Json;

namespace TimeSheetManagementSystem.APIs
{
    [Produces("application/json")]
    [Route("api/AccountDetail")]
    public class AccountDetailController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountDetailController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AccountDetail
        [HttpGet]
        public IEnumerable<AccountDetail> GetAccountDetails()
        {
            return _context.AccountDetails;
        }

        // GET: api/AccountDetail/5
        [HttpGet("GetAccountDetailByAccountDetailId/{id}")]
        public IActionResult GetAccountDetailByAccountDetailId([FromRoute] int id)
        {
            try
            {
                AccountDetail oneAccountDetail = _context.AccountDetails
                     .Where(item => item.AccountDetailId == id).Include(item => item.CustomerAccount).FirstOrDefault();

                var response = new
                {
                    day = oneAccountDetail.DayOfWeekNumber,
                    AccountDetailId = oneAccountDetail.AccountDetailId,
                    StartTimeInMinutes = oneAccountDetail.StartTimeInMinutes,
                    EndTimeInMinutes = oneAccountDetail.EndTimeInMinutes,
                    EffectiveStartDate = oneAccountDetail.EffectiveStartDate,
                    EffectiveEndDate = oneAccountDetail.EffectiveEndDate,
                    IsVisible = oneAccountDetail.IsVisible,
                    CustomerAccountId = oneAccountDetail.CustomerAccountId
                };//end of creation of the response object
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                object httpFailRequestResultMessage = new { message = "Unable to retrive account detail record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
        }

        // GET: api/AccountDetail/GetAccountDetailByCustomerAccountId/5
        [HttpGet("GetAccountDetailByCustomerAccountId/{id}")]
        public IActionResult GetAccountDetailByCustomerAccountId([FromRoute] int id)
        {//https://msdn.microsoft.com/en-us/library/cc668201.aspx
            try
            {
                //https://stackoverflow.com/questions/6359980/proper-linq-where-clauses
                var accountDetails = from accountDetail in _context.AccountDetails
                                     where accountDetail.CustomerAccountId == id
                                     select new
                                     {
                                         AccountDetailId = accountDetail.AccountDetailId,
                                         DayOfWeekNumber = accountDetail.DayOfWeekNumber,
                                         StartTimeInMinutes = accountDetail.StartTimeInMinutes,
                                         EndTimeInMinutes = accountDetail.EndTimeInMinutes,
                                         EffectiveStartDate = accountDetail.EffectiveStartDate,
                                         EffectiveEndDate = accountDetail.EffectiveEndDate,
                                         IsVisible = accountDetail.IsVisible,
                                         CustomerAccountId = accountDetail.CustomerAccountId
                                     };
                return new JsonResult(accountDetails);
            }
            catch (Exception ex)
            {
                object httpFailRequestResultMessage = new { message = "Unable to retrive account details record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
        }

        // PUT: api/AccountDetail/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody]string value)
        {
            int AccountDetailId = Int32.Parse(id);
            string databaseInnerExceptionMessage = "";
            var accountDetailChangeInput = JsonConvert.DeserializeObject<dynamic>(value);
            List<object> messages = new List<object>();
            bool status = true; //This variable is used to track the overall success of all the database operations
            object response;

            var oneAccountDetail = _context.AccountDetails
                .Where(item => item.AccountDetailId == AccountDetailId).FirstOrDefault();


            var effectiveEndDate = accountDetailChangeInput.EffectiveEndDate;
            var effectiveStartDateChecking = accountDetailChangeInput.EffectiveStartDate;
            if (accountDetailChangeInput.EffectiveEndDate == "")
            {
                effectiveEndDate = null;
            }
            else if (effectiveEndDate < effectiveStartDateChecking)
            {
                response = new { status = "fail", message = "End date must not be earlier than start date" };
                return new JsonResult(response);
            }
            DateTime effectiveStartDate = accountDetailChangeInput.EffectiveStartDate;

            if (oneAccountDetail != null && oneAccountDetail.EffectiveEndDate != null && oneAccountDetail.EffectiveEndDate > effectiveStartDate)
            {
                response = new { status = "fail", message = "Start date must be earlier than existing end date" };
                return new JsonResult(response);
            }

            string lessonStartTimeString = accountDetailChangeInput.lessonStartTime;
            int startTimeInt = (int.Parse(lessonStartTimeString.Substring(0, lessonStartTimeString.IndexOf(":"))) * 60) +
                int.Parse(lessonStartTimeString.Substring(lessonStartTimeString.IndexOf(":") + 1));


            string lessonEndTimeString = accountDetailChangeInput.lessonEndTime;
            int endTimeInt = int.Parse(lessonEndTimeString.Substring(0, lessonEndTimeString.IndexOf(":"))) * 60 +
                int.Parse(lessonEndTimeString.Substring(lessonEndTimeString.IndexOf(":") + 1));

            if (endTimeInt < startTimeInt)
            {
                response = new { status = "fail", message = "Start time must not be earlier than end time" };
                return new JsonResult(response);
            }

            string dayOfWeekNumberString = accountDetailChangeInput.day;
            int dayOfWeekNumber = int.Parse(dayOfWeekNumberString);
            //if (oneAccountDetail != null && oneAccountDetail.DayOfWeekNumber == dayOfWeekNumber && endTimeInt > oneAccountDetail.StartTimeInMinutes && oneAccountDetail.EndTimeInMinutes > startTimeInt)
            //{
            //    response = new { status = "fail", message = "Time Overlap" };
            //    return new JsonResult(response);
            //}

            var accountDetailCheck = from AccountDetails in _context.AccountDetails
                                     where AccountDetails.CustomerAccountId == AccountDetailId
                                     select new
                                     {
                                         StartTimeInMinutes = AccountDetails.StartTimeInMinutes,
                                         EndTimeInMinutes = AccountDetails.EndTimeInMinutes,
                                         DayOfWeekNumber = AccountDetails.DayOfWeekNumber
                                     };

            foreach (var oneAccountDetailCheck in accountDetailCheck)
            {
                if (oneAccountDetailCheck != null && oneAccountDetailCheck.DayOfWeekNumber == dayOfWeekNumber && endTimeInt > oneAccountDetailCheck.StartTimeInMinutes && oneAccountDetailCheck.EndTimeInMinutes > startTimeInt)
                {
                    response = new { status = "fail", message = "Time Overlap" };
                    return new JsonResult(response);
                }
            }

            oneAccountDetail.DayOfWeekNumber = accountDetailChangeInput.day;
            oneAccountDetail.StartTimeInMinutes = startTimeInt;
            oneAccountDetail.EndTimeInMinutes = endTimeInt;
            oneAccountDetail.EffectiveStartDate = accountDetailChangeInput.EffectiveStartDate;
            oneAccountDetail.EffectiveEndDate = effectiveEndDate;
            oneAccountDetail.IsVisible = accountDetailChangeInput.IsVisible;


            try
            {
                try
                {
                    //Changes are not persisted in the database until
                    //I use the following command.
                    _context.AccountDetails.Update(oneAccountDetail);
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
                    response = new { status = "success", message = "Saved account detail record." };
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

        // POST: api/AccountDetail
        [HttpPost]
        public async Task<IActionResult> PostAccountDetail([FromBody] string value)
        {
            var accountDetailNewInput = JsonConvert.DeserializeObject<dynamic>(value);
            object response = null;

            var newAccountDetail = new AccountDetail();

            int customerAccountId = accountDetailNewInput.CustomerAccountId;
            CustomerAccount currentCustomerAccount = _context.CustomerAccounts
                .Where(item => item.CustomerAccountId == customerAccountId).FirstOrDefault();

            var effectiveEndDate = accountDetailNewInput.EffectiveEndDate;
            var effectiveStartDateChecking = accountDetailNewInput.EffectiveStartDate;
            if (accountDetailNewInput.EffectiveEndDate == "")
            {
                effectiveEndDate = null;
            }
            else if (effectiveEndDate < effectiveStartDateChecking)
            {
                response = new { status = "fail", message = "End date must not be earlier than start date" };
                return new JsonResult(response);
            }
            DateTime effectiveStartDate = accountDetailNewInput.EffectiveStartDate;
            if (effectiveStartDate < DateTime.Now.Date)
            {
                response = new { status = "fail", message = "Start date must not be in the past" };
                return new JsonResult(response);
            }

            AccountDetail oneAccountDetail = (AccountDetail)_context.AccountDetails
                 .Where(accountDetailItem => accountDetailItem.CustomerAccountId == customerAccountId).FirstOrDefault();

            if (oneAccountDetail != null && oneAccountDetail.EffectiveEndDate != null && oneAccountDetail.EffectiveEndDate > effectiveStartDate)
            {
                response = new { status = "fail", message = "Start date must be earlier than existing end date" };
                return new JsonResult(response);
            }

            string lessonStartTimeString = accountDetailNewInput.lessonStartTime;
            int startTimeInt = (int.Parse(lessonStartTimeString.Substring(0, lessonStartTimeString.IndexOf(":"))) * 60) + 
                int.Parse(lessonStartTimeString.Substring(lessonStartTimeString.IndexOf(":") + 1));


            string lessonEndTimeString = accountDetailNewInput.lessonEndTime;
            int endTimeInt = int.Parse(lessonEndTimeString.Substring(0, lessonEndTimeString.IndexOf(":"))) * 60 +
                int.Parse(lessonEndTimeString.Substring(lessonEndTimeString.IndexOf(":") + 1));

            if (endTimeInt < startTimeInt)
            {
                response = new { status = "fail", message = "End time must not be earlier than start time" };
                return new JsonResult(response);
            }

            string dayOfWeekNumberString = accountDetailNewInput.day;
            int dayOfWeekNumber = int.Parse(dayOfWeekNumberString);
            //if (oneAccountDetail != null && oneAccountDetail.DayOfWeekNumber == dayOfWeekNumber && endTimeInt > oneAccountDetail.StartTimeInMinutes && oneAccountDetail.EndTimeInMinutes > startTimeInt)
            //{
            //    response = new { status = "fail", message = "Time Overlap" };
            //    return new JsonResult(response);
            //}

            var accountDetailCheck = from AccountDetails in _context.AccountDetails
                                     where AccountDetails.CustomerAccountId == customerAccountId
                                     select new
                                     {
                                         StartTimeInMinutes = AccountDetails.StartTimeInMinutes,
                                         EndTimeInMinutes = AccountDetails.EndTimeInMinutes,
                                         DayOfWeekNumber = AccountDetails.DayOfWeekNumber
                                     };

            foreach(var oneAccountDetailCheck in accountDetailCheck)
            {
                if (oneAccountDetailCheck != null && oneAccountDetailCheck.DayOfWeekNumber == dayOfWeekNumber && endTimeInt > oneAccountDetailCheck.StartTimeInMinutes && oneAccountDetailCheck.EndTimeInMinutes > startTimeInt)
                {
                    response = new { status = "fail", message = "Time Overlap" };
                    return new JsonResult(response);
                }
            }
            
            newAccountDetail.CustomerAccountId = accountDetailNewInput.CustomerAccountId;
            newAccountDetail.CustomerAccount = currentCustomerAccount;
            newAccountDetail.DayOfWeekNumber = accountDetailNewInput.day;
            newAccountDetail.StartTimeInMinutes = startTimeInt;
            newAccountDetail.EndTimeInMinutes = endTimeInt;
            newAccountDetail.EffectiveStartDate = accountDetailNewInput.EffectiveStartDate;
            newAccountDetail.EffectiveEndDate = effectiveEndDate;
            newAccountDetail.IsVisible = accountDetailNewInput.IsVisible;

            try
            {
                try
                {
                    _context.AccountDetails.Add(newAccountDetail);
                    _context.SaveChanges();

                    response = new { status = "success", message = "Saved new account detail record." };
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

        // DELETE: api/AccountDetail/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccountDetail([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                object httpFailRequestResultMessage = new { message = "Unable to delete account rate record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }

            var accountDetail = await _context.AccountDetails.SingleOrDefaultAsync(m => m.AccountDetailId == id);
            if (accountDetail == null)
            {
                return NotFound();
            }

            _context.AccountDetails.Remove(accountDetail);
            await _context.SaveChangesAsync();
            object response = new { status = "success", message = "Deleted account rate record." };

            return Ok(response);
        }

        // DELETE: api/AccountDetail/DeleteByCustomerAccountId/5
        //[HttpDelete("DeleteByCustomerAccountId/{id}")]
        //public IActionResult DeleteByCustomerAccountId(int id)
        //{
        //    string databaseInnerExceptionMessage = "";
        //    List<object> messages = new List<object>();
        //    bool status = true; //This variable is used to track the overall success of all the database operations
        //    object response = null;

        //    int count = (int)_context.AccountDetails
        //         .Where(accountDetailItem => accountDetailItem.CustomerAccountId == id).Count();

        //    while (count > 0)
        //    {
        //        AccountDetail oneAccountDetail = (AccountDetail)_context.AccountDetails
        //         .Where(accountDetailItem => accountDetailItem.CustomerAccountId == id).FirstOrDefault();
        //        try
        //        {
        //            try
        //            {
        //                _context.AccountDetails.Remove(oneAccountDetail);
        //                _context.SaveChanges();

        //            }
        //            catch (DbUpdateException ex)
        //            {
        //                databaseInnerExceptionMessage = ex.InnerException.Message;
        //                status = false;
        //                messages.Add(databaseInnerExceptionMessage);
        //                return new JsonResult(response);
        //            }

        //        }
        //        catch (Exception outerException)
        //        {
        //            //object httpFailRequestResultMessage = new { message = "Unable to delete account details record." +outerException };
        //            ////Return a bad http request message to the client
        //            //return BadRequest(httpFailRequestResultMessage);
        //        }
        //        count--;
        //    }

        //    response = new { status = "success", message = "Deleted account details record." };
        //    return new JsonResult(response);
        //}//End of DeleteAccountDetailByCustomerId()

        private bool AccountDetailExists(int id)
        {
            return _context.AccountDetails.Any(e => e.AccountDetailId == id);
        }
    }
}