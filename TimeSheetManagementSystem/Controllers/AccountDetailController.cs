﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TimeSheetManagementSystem.Controllers
{
    public class AccountDetailController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult CreateOneAccountDetailForCustomerAccount()
        {
            return View();
        }
        public IActionResult ManageAccountDetailsForOneCustomerAccount()
        {
            return View();
        }
        public IActionResult UpdateOneAccountDetail()
        {
            return View();
        }

    }
}
