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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace TimeSheetManagementSystem.APIs
{
    [Authorize("RequireAdminRole")]
    [Produces("application/json")]
    [Route("api/CustomerAccount")]
    public class CustomerAccountController : Controller
    {
        public ApplicationDbContext Database { get; }
        public IConfigurationRoot Configuration { get; }

        public CustomerAccountController(ApplicationDbContext database)
        {
            Database = database;
        }

        // GET: api/CustomerAccount
        [HttpGet]
        public IActionResult Get()
        {
            ////https://stackoverflow.com/questions/3404975/left-outer-join-in-linq#3413732
            //var customerAccounts = from customerAccount in Database.CustomerAccounts
            //                       join accountRate in Database.AccountRates
            //                       on customerAccount.CustomerAccountId equals accountRate.CustomerAccountId into customerAccount_accountRate
            //                       from accountRate in customerAccount_accountRate.DefaultIfEmpty()
            //                       select new
            //                       {
            //                           CustomerAccountId = customerAccount.CustomerAccountId,
            //                           AccountName = customerAccount.AccountName,
            //                           IsVisible = customerAccount.IsVisible,
            //                           comments = customerAccount.Comments,
            //                           createdBy = customerAccount.CreatedBy,
            //                           updatedBy = customerAccount.UpdatedBy,
            //                           createdAt = customerAccount.CreatedAt,
            //                           updatedAt = customerAccount.UpdatedAt,
            //                           accountRateId = accountRate == null ? 0 : accountRate.AccountRateId,
            //                           //instructorsCount = Database.InstructorAccounts.Where(input => input.CustomerAccountId == customerAccount.CustomerAccountId).Count(),
            //                           //ratesCount = Database.AccountRates.Where(input => input.CustomerAccountId == customerAccount.CustomerAccountId).Count()
            //                       };
            //return new JsonResult(customerAccounts);
            List<object> customerAccountList = new List<object>();
            var customerAccounts = Database.CustomerAccounts
                .Include(i => i.UpdatedBy);
            foreach (var customerAccount in customerAccounts)
            {
                int rateCount = (from AccountRate in Database.AccountRates
                                 where AccountRate.CustomerAccountId == customerAccount.CustomerAccountId
                                 select AccountRate).Count();
                int instructorCount = (from InstructorAccount in Database.InstructorAccounts
                                       where InstructorAccount.CustomerAccountId == customerAccount.CustomerAccountId
                                       select InstructorAccount).Count();

                //var updatedBy = customerAccount.UpdatedBy;
                //string updatedByName;
                //if (updatedBy == null)
                //{
                //    updatedByName = null;
                //}
                //else
                //{
                //    updatedByName = updatedBy.FullName;
                //}

                customerAccountList.Add(new
                {
                    CustomerAccountId = customerAccount.CustomerAccountId,
                    AccountName = customerAccount.AccountName,
                    IsVisible = customerAccount.IsVisible,
                    comments = customerAccount.Comments,
                    //createdByName = customerAccount.CreatedBy.FullName,
                    createdById = customerAccount.CreatedById,
                    updatedByName = customerAccount.UpdatedBy.FullName,
                    updatedById = customerAccount.UpdatedById,
                    createdAt = customerAccount.CreatedAt,
                    updatedAt = customerAccount.UpdatedAt,
                    rateCount = rateCount,
                    instructorCount = instructorCount
                    //instructorsCount = Database.InstructorAccounts.Where(input => input.CustomerAccountId == customerAccount.CustomerAccountId).Count(),
                    //ratesCount = Database.AccountRates.Where(input => input.CustomerAccountId == customerAccount.CustomerAccountId).Count()
                });
            }
            return new JsonResult(customerAccountList);
            //var customerAccounts = from customerAccount in Database.CustomerAccounts
            //                       select new
            //                       {
            //                           CustomerAccountId = customerAccount.CustomerAccountId,
            //                           AccountName = customerAccount.AccountName,
            //                           IsVisible = customerAccount.IsVisible,
            //                           comments = customerAccount.Comments,
            //                           createdBy = customerAccount.CreatedBy,
            //                           updatedBy = customerAccount.UpdatedBy,
            //                           createdAt = customerAccount.CreatedAt,
            //                           updatedAt = customerAccount.UpdatedAt,
            //                       };
            //return new JsonResult(customerAccounts);
        }

        // GET: api/CustomerAccount/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                CustomerAccount oneCustomerAccount = Database.CustomerAccounts
                     .Where(item => item.CustomerAccountId == id).FirstOrDefault();

                var response = new
                {
                    CustomerAccountId = oneCustomerAccount.CustomerAccountId,
                    AccountName = oneCustomerAccount.AccountName,
                    IsVisible = oneCustomerAccount.IsVisible,
                    comments = oneCustomerAccount.Comments,
                    createdBy = oneCustomerAccount.CreatedBy,
                    updatedBy = oneCustomerAccount.UpdatedBy,
                    createdAt = oneCustomerAccount.CreatedAt,
                    updatedAt = oneCustomerAccount.UpdatedAt

                };//end of creation of the response object
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                object httpFailRequestResultMessage = new { message = "Unable to retrive customer account." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
        }//end of httpget by id

        // PUT: api/CustomerAccount/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody]string value)
        {
            int customerAccountId = Int32.Parse(id);
            string databaseInnerExceptionMessage = "";
            var customerAccountChangeInput = JsonConvert.DeserializeObject<dynamic>(value);
            List<object> messages = new List<object>();
            bool status = true; //This variable is used to track the overall success of all the database operations
            object response;
            //http://stackoverflow.com/questions/20444022/updating-user-data-asp-net-identity
            //Database is our database context set in this controller.
            //I used the following 2 lines of command to create a userStore which represents AspNetUser table in the DB.

            var oneCustomerAccount = Database.CustomerAccounts
                .Where(item => item.CustomerAccountId == customerAccountId).FirstOrDefault();

            string updatedById = customerAccountChangeInput.UpdatedById;
            UserInfo currentUser = Database.UserInfo
                .Where(item => item.LoginUserName == updatedById).FirstOrDefault();

            oneCustomerAccount.AccountName = customerAccountChangeInput.AccountName;
            oneCustomerAccount.IsVisible = customerAccountChangeInput.IsVisible;
            oneCustomerAccount.Comments = customerAccountChangeInput.Comments;
            oneCustomerAccount.UpdatedAt = DateTime.Now;
            oneCustomerAccount.UpdatedBy = currentUser;

            try
            {
                try
                {
                    //Changes are not persisted in the database until
                    //I use the following command.
                    Database.CustomerAccounts.Update(oneCustomerAccount);
                    Database.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    databaseInnerExceptionMessage = ex.InnerException.Message;
                    status = false;
                    messages.Add(databaseInnerExceptionMessage);
                }
                if (status == true)
                {
                    response = new { status = "success", message = "Saved customer account." };
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
        // PUT: api/CustomerAccount/5
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> PutUpdate(string id, [FromBody]string value)
        {
            int customerAccountId = Int32.Parse(id);
            string databaseInnerExceptionMessage = "";
            var customerAccountChangeInput = JsonConvert.DeserializeObject<dynamic>(value);
            List<object> messages = new List<object>();
            bool status = true; //This variable is used to track the overall success of all the database operations
            object response;
            //http://stackoverflow.com/questions/20444022/updating-user-data-asp-net-identity
            //Database is our database context set in this controller.
            //I used the following 2 lines of command to create a userStore which represents AspNetUser table in the DB.

            var oneCustomerAccount = Database.CustomerAccounts
                .Where(item => item.CustomerAccountId == customerAccountId).FirstOrDefault();

            string updatedById = customerAccountChangeInput.UpdatedById;
            UserInfo currentUser = Database.UserInfo
                .Where(item => item.LoginUserName == updatedById).FirstOrDefault();

            oneCustomerAccount.UpdatedAt = DateTime.Now;
            oneCustomerAccount.UpdatedBy = currentUser;

            try
            {
                try
                {
                    //Changes are not persisted in the database until
                    //I use the following command.
                    Database.CustomerAccounts.Update(oneCustomerAccount);
                    Database.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    databaseInnerExceptionMessage = ex.InnerException.Message;
                    status = false;
                    messages.Add(databaseInnerExceptionMessage);
                }
                if (status == true)
                {
                    response = new { status = "success", message = "updated updated by account." };
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

        // POST: api/CustomerAccount
        [HttpPost]
        public async Task<IActionResult> PostCustomerAccount([FromBody] string value)
        {
            var customerAccountNewInput = JsonConvert.DeserializeObject<dynamic>(value);
            object response = null;

            var newCustomerAccount = new CustomerAccount();

            string currentUserId = customerAccountNewInput.CreatedById;
            UserInfo currentUser = Database.UserInfo
                .Where(item => item.LoginUserName == currentUserId).FirstOrDefault();

            newCustomerAccount.AccountName = customerAccountNewInput.AccountName;
            newCustomerAccount.Comments = customerAccountNewInput.comments;
            newCustomerAccount.IsVisible = customerAccountNewInput.IsVisible;
            newCustomerAccount.CreatedById = customerAccountNewInput.CreatedById;
            newCustomerAccount.CreatedBy = currentUser;
            newCustomerAccount.CreatedAt = DateTime.Now;
            newCustomerAccount.UpdatedById = customerAccountNewInput.UpdatedById;
            newCustomerAccount.UpdatedBy = currentUser;  //await userManager.FindByIdAsync(currentUserId);
            newCustomerAccount.UpdatedAt = DateTime.Now;

            try
            {
                try
                {
                    Database.CustomerAccounts.Add(newCustomerAccount);
                    Database.SaveChanges();

                    CustomerAccount createdCustomerAccount = Database.CustomerAccounts
                        .Where(item => item.AccountName == newCustomerAccount.AccountName).FirstOrDefault();
                    int newCustomerAccountId = createdCustomerAccount.CustomerAccountId;

                    response = new { status = "success", message = "Saved new customer account record.", customerAccountId = newCustomerAccountId };
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

        // DELETE: api/CustomerAccount/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerAccount([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customerAccount = await Database.CustomerAccounts.SingleOrDefaultAsync(m => m.CustomerAccountId == id);
            if (customerAccount == null)
            {
                object httpFailRequestResultMessage = new { message = "Unable to delete customer account." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }

            Database.CustomerAccounts.Remove(customerAccount);
            await Database.SaveChangesAsync();

            object response = new { status = "success", message = "Deleted customer account." };

            return new JsonResult(response);
        }

        private bool CustomerAccountExists(int id)
        {
            return Database.CustomerAccounts.Any(e => e.CustomerAccountId == id);
        }
    }
}