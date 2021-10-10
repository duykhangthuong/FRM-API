using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using kroniiapi.DTO.AccountDTO;
using kroniiapi.DTO.PaginationDTO;
using kroniiapi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace kroniiapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        public AccountController(IAccountService accountService, IMapper mapper)
        {
            _accountService = accountService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get the list of account in the db with pagination config
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters from client</param>
        /// <returns>200: List of account with pagination / 404: search username not found</returns>
        [HttpGet("page")]
        public async Task<ActionResult> GetAccountList([FromQuery] PaginationParameter paginationParameter)
        {
            return Ok();
        }

        /// <summary>
        /// Deactivate an account in the db
        /// </summary>
        /// <param name="id">Id of that account</param>
        /// <param name="role">Role of that account</param>
        /// <returns>200: Deleted / 404: Id not found</returns>
        [HttpDelete("{id:int}/{role}")]
        public async Task<ActionResult> DeactivateAccount(int id, string role)
        {
            return Ok();
        }

        /// <summary>
        /// Insert new account to db, send email with password generated by system to that user
        /// </summary>
        /// <param name="accountInput">Detail of account</param>
        /// <returns>201: Created / 409: Username || email || phone is existed</returns>
        [HttpPost]
        public async Task<ActionResult> CreateNewAccount([FromBody] AccountInput accountInput)
        {
            return Ok();
        }

        /// <summary>
        /// Insert a list accounts to db using excel file, send email with password generated by system to that user
        /// </summary>
        /// <param name="file">Excel file to store account data</param>
        /// <returns>201: Created / 400: File content inapproriate / 409: Username || email || phone is existed</returns>
        [HttpPost("excel")]
        public async Task<ActionResult> CreateNewAccountByExcel([FromForm] IFormFile file)
        {
            return Ok();
        }

        /// <summary>
        /// Get list of deactivated account with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters from client</param>
        /// <returns>200: List of account with pagination / 404: search username not found</returns>
        [HttpGet("deleted")]
        public async Task<ActionResult> GetDeactivatedAccountList([FromQuery] PaginationParameter paginationParameter)
        {
            return Ok();
        }

        /// <summary>
        /// Send new password to user with inputed email
        /// </summary>
        /// <param name="emailInput">Email for sending</param>
        /// <returns>200: Sent / 404: Email not found</returns>
        [HttpPost("forgot")]
        public async Task<ActionResult> ForgotPassword([FromBody] EmailInput emailInput)
        {
            return Ok();
        }
    }
}