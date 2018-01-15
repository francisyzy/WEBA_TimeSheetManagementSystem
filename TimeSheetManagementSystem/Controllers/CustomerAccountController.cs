using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TimeSheetManagementSystem.Controllers
{
    public class CustomerAccountController : Controller
    {
        public IActionResult ManageCustomerAccounts()
        {
            return View();
        }
        public IActionResult CreateCustomerAccount()
        {
            return View();
        }
        public IActionResult UpdateCustomerAccount()
        {
            return View();
        }
        public IActionResult ManageAccountRate()
        {
            return View();
        }
        public IActionResult UpdateAccountRate()
        {
            return View();
        }
        public IActionResult CreateAccountRate()
        {
            return View();
        }
    }
}