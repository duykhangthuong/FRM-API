using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using kroniiapi.DTO;
using kroniiapi.DTO.AccountDTO;
using kroniiapi.DTO.Email;
using kroniiapi.DTO.PaginationDTO;
using kroniiapi.Helper;
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

        private IEmailService _emailService;

        public AccountController(IAccountService accountService, IMapper mapper, IEmailService emailService)
        {
            _accountService = accountService;
            _mapper = mapper;
            _emailService = emailService;
        }

        /// <summary>
        /// Get the list of account in the db with pagination config
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters from client</param>
        /// <returns>200: List of account with pagination / 404: search username not found</returns>
        [HttpGet("page")]
        public async Task<ActionResult<PaginationResponse<IEnumerable<AccountResponse>>>> GetAccountList([FromQuery] PaginationParameter paginationParameter)
        {
            (int totalRecord,IEnumerable<AccountResponse> listAccount) = await _accountService.GetAccountList(paginationParameter);

            if(totalRecord==0){
                return NotFound(new ResponseDTO(404,"Search username not found"));
            }

            return Ok(new PaginationResponse<IEnumerable<AccountResponse>>(totalRecord,listAccount));
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
            int result = await _accountService.DeactivateAccount(id, role);

            if (result == -1)
            {
                return NotFound(new ResponseDTO(404,"Id not found!"));
            }
            return Ok(new ResponseDTO(200,"Deleted!"));
        }

        /// <summary>
        /// Insert new account to db, send email with password generated by system to that user
        /// </summary>
        /// <param name="accountInput">Detail of account</param>
        /// <returns>201: Created / 409: Username || email || phone is existed</returns>
        [HttpPost]
        public async Task<ActionResult> CreateNewAccount([FromBody] AccountInput accountInput)
        {
            int isDuplicated = await _accountService.InsertNewAccount(accountInput);
            if (isDuplicated == -1) {
                return NotFound(new ResponseDTO(409,"User name or Email or Phone is existed!"));
            }
            int result = await _accountService.SaveChange();
            if (result == 0) {     
                return BadRequest(new ResponseDTO(400,"Insert failed!"));
            }
            return Ok(new ResponseDTO(201,"Created!"));
        }

        /// <summary>
        /// Insert a list accounts to db using excel file, send email with password generated by system to that user
        /// </summary>
        /// <param name="file">Excel file to store account data</param>
        /// <returns>201: Created / 400: File content inapproriate / 409: Username || email || phone is existed</returns>
        [HttpPost("excel")]
        public async Task<ActionResult> CreateNewAccountByExcel([FromForm] IFormFile file)
        {
            bool success;
            string message;

            // Check the file extension
            (success, message) = FileHelper.CheckExcelExtension(file);
            if (!success) {
                return BadRequest(new ResponseDTO(400, message));
            }

            // Arrange the checker and the converter
            Predicate<List<string>> checker = list => list.Contains("username") && list.Contains("fullname") && list.Contains("email") && list.Contains("role");
            Func<Dictionary<string, object>, AccountInput> converter = dict => new AccountInput() {
                Username = dict["username"]?.ToString().Trim(),
                Fullname = dict["fullname"]?.ToString().Trim(),
                Email = dict["email"]?.ToString().Trim(),
                Role = dict["role"]?.ToString().Trim()
            };

            // Try to export data from the file
            List<AccountInput> list;
            using (var stream = new MemoryStream()) {
                await file.CopyToAsync(stream);
                list = FileHelper.ExportDataFromExcel<AccountInput>(stream, converter, checker, out success, out message);
            }

            // Return if the attempt is failed
            if (!success) {
                return BadRequest(new ResponseDTO(400, message));
            }

            // Loop through each data
            for (int i = 0; i < list.Count; i++) {
                int row = i + 2; // row starts from 1, but we skip the first row as it's the column name
                AccountInput accountInput = list[i];

                // Validate the account
                List<ValidationResult> errors;
                if (!ValidationHelper.Validate(accountInput, out errors)) {
                    return BadRequest(new ResponseDTO(400, "Failed to validate the account on row " + row) {
                        Errors = errors
                    });
                }

                // Try to insert the account
                int result = await _accountService.InsertNewAccount(accountInput);

                // Return if existed
                if (result < 0) {
                    _accountService.DiscardChanges();
                    return Conflict(new ResponseDTO(409, "The account on row " + row + " existed"));
                }
            }

            // All successful
            int rows = await _accountService.SaveChange();
            return CreatedAtAction(nameof(GetAccountList), new ResponseDTO(201, rows + " accounts were inserted"));
        }

        /// <summary>
        /// Get list of deactivated account with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters from client</param>
        /// <returns>200: List of account with pagination / 404: search username not found</returns>
        [HttpGet("deleted")]
        public async Task<ActionResult> GetDeactivatedAccountList([FromQuery] PaginationParameter paginationParameter)
        {
            (int totalRecord, IEnumerable<AccountResponse> deletedAccount) = await _accountService.GetDeactivatedAccountList(paginationParameter);
            if (totalRecord == 0)
            {
                return NotFound(new ResponseDTO(404));
            }
            return Ok(new PaginationResponse<IEnumerable<AccountResponse>>(totalRecord,deletedAccount));
        }

        /// <summary>
        /// Send new password to user with inputed email
        /// </summary>
        /// <param name="emailInput">Email for sending</param>
        /// <returns>200: Sent / 404: Email not found</returns>
        [HttpPost("forgot")]
        public async Task<ActionResult> ForgotPassword([FromBody] EmailInput emailInput)
        {
            if(emailInput == null || emailInput.Email == "")
            {
                return NotFound(new ResponseDTO(404, "Email not found"));
            }
            string password = AutoGeneratorPassword.passwordGenerator(15, 5, 5, 5);


            if(await _accountService.UpdateAccountPassword(emailInput.Email,password ) == 1)
            {
                return Ok(new ResponseDTO(200,"Sent"));
            }
            else
            {
                return NotFound(new ResponseDTO(404, "Email not found"));
            }
        }
    }
}