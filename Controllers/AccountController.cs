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
using kroniiapi.DTO.ExcelDTO;
using kroniiapi.DTO.PaginationDTO;
using kroniiapi.Helper;
using kroniiapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace kroniiapi.Controllers
{
    [ApiController]
    [Authorize(Policy = "Account")]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        private readonly IEmailService _emailService;

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
            (int totalRecord, IEnumerable<AccountResponse> listAccount) = await _accountService.GetAccountList(paginationParameter);

            if (totalRecord == 0)
            {
                return NotFound(new ResponseDTO(404, "Search email not found"));
            }

            return Ok(new PaginationResponse<IEnumerable<AccountResponse>>(totalRecord, listAccount));
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
                return NotFound(new ResponseDTO(404, "Id not found!"));
            }
            if (result == 0)
            {
                return BadRequest(new ResponseDTO(409, "Can't deactivate administrator"));
            }
            return Ok(new ResponseDTO(200, "Deleted!"));
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
            if (isDuplicated == -1)
            {
                return Conflict(new ResponseDTO(409, "The account is existed"));
            }
            int result = await _accountService.SaveChange();
            if (result == 0)
            {
                return BadRequest(new ResponseDTO(400, "Error when saving the insertion"));
            }
            return CreatedAtAction(nameof(GetAccountList), new ResponseDTO(201, "Successfully inserted"));
        }

        /// <summary>
        /// Insert a list accounts to db using excel file, send email with password generated by system to that user
        /// </summary>
        /// <param name="file">Excel file to store account data</param>
        /// <returns>201: Created / 400: File content inapproriate</returns>
        [HttpPost("excel")]
        public async Task<ActionResult<ExcelResponseDTO>> CreateNewAccountByExcel([FromForm] IFormFile file)
        {
            List<ExcelErrorDTO> excelErrorList = new();

            // Check the file extension
            var (success, message) = FileHelper.CheckExcelExtension(file);
            if (!success)
            {
                return BadRequest(new ExcelResponseDTO(400, message)
                {
                    Errors = excelErrorList
                });
            }

            // Arrange the checker and the converter
            static bool Checker(List<string> list) => list.Contains("username") && list.Contains("fullname") && list.Contains("email") && list.Contains("role");
            static AccountInput Converter(Dictionary<string, object> dict) => new()
            {
                Username = dict["username"]?.ToString()?.Trim(),
                Fullname = dict["fullname"]?.ToString()?.Trim(),
                Email = dict["email"]?.ToString()?.Trim(),
                Role = dict["role"]?.ToString()?.Trim()
            };

            // Try to export data from the file
            List<AccountInput> list;
            await using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                list = stream.ExportDataFromExcel(Converter, Checker, out success, out message);
            }

            // Return if the attempt is failed
            if (!success)
            {
                return BadRequest(new ExcelResponseDTO(400, message)
                {
                    Errors = excelErrorList
                });
            }


            // Loop through each data and validate the model
            for (var i = 0; i < list.Count; i++)
            {
                var row = i + 2; // row starts from 1, but we skip the first row as it's the column name
                var accountInput = list[i];

                // Validate the account
                if (!accountInput.Validate(out var validationResults))
                {
                    excelErrorList.Add(new ExcelErrorDTO()
                    {
                        Row = row,
                        Value = accountInput,
                        Errors = validationResults.Select(x => x.ErrorMessage).ToList()
                    });
                }
            }

            // Return if there is validation error
            if (excelErrorList.Count > 0)
            {
                return BadRequest(new ExcelResponseDTO(400, "Failed to validate the accounts. Check Errors")
                {
                    Errors = excelErrorList
                });
            }

            // Insert the accounts
            try
            {
                var failIndexes = await _accountService.InsertNewAccount(list);
                excelErrorList.AddRange(from errorItem in failIndexes
                    let row = errorItem.Key + 2
                    select new ExcelErrorDTO()
                    {
                        Row = row,
                        Value = list[errorItem.Key],
                        Error = errorItem.Value switch
                        {
                            0 => "Invalid Email",
                            -1 => "Account Existed",
                            1 => "Invalid Role",
                            _ => "Unknown Error"
                        }
                    });
                return CreatedAtAction(nameof(GetAccountList), new ExcelResponseDTO(201, "Successfully inserted. Check Errors for details to failed accounts")
                {
                    Errors = excelErrorList
                });
            }
            catch (Exception)
            {
                _accountService.DiscardChanges();
                throw;
            }
        }

        /// <summary>
        /// Get list of deactivated account with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters from client</param>
        /// <returns>200: List of account with pagination / 404: search username not found</returns>
        [HttpGet("deleted")]
        public async Task<ActionResult> GetDeactivatedAccountList([FromQuery] PaginationParameter paginationParameter)
        {
            (int totalRecord, IEnumerable<DeletedAccountResponse> deletedAccount) = await _accountService.GetDeactivatedAccountList(paginationParameter);
            if (totalRecord == 0)
            {
                return NotFound(new ResponseDTO(404));
            }
            return Ok(new PaginationResponse<IEnumerable<DeletedAccountResponse>>(totalRecord, deletedAccount));
        }

        /// <summary>
        /// Send new password to user with inputed email
        /// </summary>
        /// <param name="emailInput">Email for sending</param>
        /// <returns>200: Sent / 404: Email not found</returns>
        [HttpPost("forgot")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] EmailInput emailInput)
        {
            if (emailInput == null || emailInput.Email == "")
            {
                return NotFound(new ResponseDTO(404, "Email not found"));
            }
            string password = AutoGeneratorPassword.passwordGenerator(15, 5, 5, 5);


            if (await _accountService.UpdateAccountPassword(emailInput.Email, password) == 1)
            {
                return Ok(new ResponseDTO(200, "Sent"));
            }
            else
            {
                return NotFound(new ResponseDTO(404, "Email not found"));
            }
        }
    }
}