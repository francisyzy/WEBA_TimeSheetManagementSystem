using TimeSheetManagementSystem.Models;
using TimeSheetManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TimeSheetManagementSystem.APIs
{
    [Authorize("RequireAdminRole")]
    [Produces("application/json")]//?
    [Route("api/SessionSynopsis")]
    public class SessionSynopsisController : Controller
    {
        //Create a property DbContext so that the code in the Action Methods
        //can use this property to communicate with the database. Note that, this DbContext
        //property is required in every controller. The property is initiatialized in the Controller's
        //Constructor. (In this case, the public UserManagerController() constructor has been created
        //so that the DbContext property is instantiated as an ApplicationDbContext instance.
        public ApplicationDbContext Database { get; }
        public IConfigurationRoot Configuration { get; }

        //Create a Constructor, so that the .NET engine can pass in the dbContext object
        //which represents the database session.
        public SessionSynopsisController(ApplicationDbContext database)
        {
            //The code below was useful to get my project working.
            //After that, I have referred to two online site content to improve the code
            //because having three lines of such code in every Web API controller class is definitely
            //a No No.
            /*
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseSqlServer(@"Server=IT-NB147067\SQLEXPRESS;database=InternshipManagementSystemWithSecurityDB_V1;Trusted_Connection=True;MultipleActiveResultSets=True");
            Database = new ApplicationDbContext(options.Options);
            */
            Database = database;

        }
        // GET: api/UserManager
        [HttpGet]
        public IActionResult Get()
        {
            var sessionSynopses = from sessionSynopsis in Database.SessionSynopses
                                  select new
                                  {
                                      sessionSynopsisId = sessionSynopsis.SessionSynopsisId,
                                      sessionSynopsisName = sessionSynopsis.SessionSynopsisName,
                                      visible = sessionSynopsis.IsVisible,
                                      createdBy = sessionSynopsis.CreatedBy,
                                      updatedBy = sessionSynopsis.UpdatedBy
                                  };

            return new JsonResult(sessionSynopses);
        }//end of Get()

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                SessionSynopsis oneSessionSynopsis = Database.SessionSynopses
                     .Where(sessionSynopsisItem => sessionSynopsisItem.SessionSynopsisId == id).FirstOrDefault();

                var response = new
                {
                    sessionSynopsisId = oneSessionSynopsis.SessionSynopsisId,
                    sessionSynopsisName = oneSessionSynopsis.SessionSynopsisName,
                    visible = oneSessionSynopsis.IsVisible,
                    createdBy = oneSessionSynopsis.CreatedBy,
                    updatedBy = oneSessionSynopsis.UpdatedBy,

                };//end of creation of the response object
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                object httpFailRequestResultMessage = new { message = "Unable to retrive session synopsis record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
        }//end of httpget by id



        // PUT api/values/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody]string value)
        {
            int sessionSynopsisId = Int32.Parse(id);

            string databaseInnerExceptionMessage = "";
            var sessionSynopsisChangeInput = JsonConvert.DeserializeObject<dynamic>(value);
            List<object> messages = new List<object>();
            bool status = true; //This variable is used to track the overall success of all the database operations
            object response;
            //http://stackoverflow.com/questions/20444022/updating-user-data-asp-net-identity
            //Database is our database context set in this controller.
            //I used the following 2 lines of command to create a userStore which represents AspNetUser table in the DB.

            var oneSessionSynopsis = Database.SessionSynopses
                .Where(item => item.SessionSynopsisId == sessionSynopsisId).FirstOrDefault();

            string updatedById = sessionSynopsisChangeInput.UpdatedById;
            UserInfo currentUser = Database.UserInfo
                .Where(item => item.LoginUserName == updatedById).FirstOrDefault();

            oneSessionSynopsis.SessionSynopsisName = sessionSynopsisChangeInput.SessionSynopsisName;
            oneSessionSynopsis.IsVisible = sessionSynopsisChangeInput.IsVisible;
            oneSessionSynopsis.UpdatedBy = currentUser;

            try
            {
                try
                {
                    //Changes are not persisted in the database until
                    //I use the following command.
                    Database.SessionSynopses.Update(oneSessionSynopsis);
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
                    response = new { status = "success", message = "Saved session synopsis record." };
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
         // POST api/Post/
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]string value)
        {
            var sessionSypnosisNewInput = JsonConvert.DeserializeObject<dynamic>(value);
            object response = null;

            var newSessionSypnosis = new SessionSynopsis();

            string currentUserId = sessionSypnosisNewInput.CreatedById;
            UserInfo currentUser = Database.UserInfo
                .Where(item => item.LoginUserName == currentUserId).FirstOrDefault();

            newSessionSypnosis.SessionSynopsisName = sessionSypnosisNewInput.SessionSynopsisName;
            newSessionSypnosis.IsVisible = sessionSypnosisNewInput.IsVisible;
            newSessionSypnosis.CreatedById = sessionSypnosisNewInput.CreatedById;
            newSessionSypnosis.CreatedBy = currentUser;
            newSessionSypnosis.UpdatedById = sessionSypnosisNewInput.UpdatedById;
            newSessionSypnosis.UpdatedBy = currentUser;  //await userManager.FindByIdAsync(currentUserId);

            try
            {
                try
                {
                    Database.SessionSynopses.Add(newSessionSypnosis);
                    Database.SaveChanges();

                    response = new { status = "success", message = "Saved new session synopsis record." };
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
        }//End of Post()

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            string databaseInnerExceptionMessage = "";
            List<object> messages = new List<object>();
            bool status = true; //This variable is used to track the overall success of all the database operations
            object response = null;

            SessionSynopsis oneSessionSynopsis = (SessionSynopsis)Database.SessionSynopses
                 .Where(sessionSynopsisItem => sessionSynopsisItem.SessionSynopsisId == id).FirstOrDefault();


            try
            {
                try
                {
                    Database.SessionSynopses.Remove(oneSessionSynopsis);
                    Database.SaveChanges();

                    response = new { status = "success", message = "Deleted session synopsis record." };
                }
                catch (DbUpdateException ex)
                {
                    databaseInnerExceptionMessage = ex.InnerException.Message;
                    status = false;
                    messages.Add(databaseInnerExceptionMessage);
                }

            }
            catch (Exception outerException)
            {
                object httpFailRequestResultMessage = new { message = "Unable to delete session synopsis record." };
                //Return a bad http request message to the client
                return BadRequest(httpFailRequestResultMessage);
            }
            return new JsonResult(response);
        }//End of Delete()
    }//End of UserManagerController class
}