using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kroniiapi.DB;
using kroniiapi.DB.Models;
using kroniiapi.DTO.CompanyDTO;
using kroniiapi.DTO.PaginationDTO;
using kroniiapi.Helper;
using Microsoft.EntityFrameworkCore;

namespace kroniiapi.Services
{
    public class CompanyService : ICompanyService
    {
        private DataContext _dataContext;

        public CompanyService(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        /// Get a company by its ID
        /// </summary>
        /// <param name="id">ID of company</param>
        /// <returns>Company data</returns>
        public async Task<Company> GetCompanyById(int id)
        {
            return await _dataContext.Companies.Where(c => c.CompanyId == id && c.IsDeactivated == false).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get a company by its username
        /// </summary>
        /// <param name="username">Username of company</param>
        /// <returns>Company data</returns>
        public async Task<Company> GetCompanyByUsername(string username)
        {
            return await _dataContext.Companies.Where(c => c.Username.Equals(username) && c.IsDeactivated == false).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get a company by its email
        /// </summary>
        /// <param name="username">Email of company</param>
        /// <returns>Company data</returns>
        public async Task<Company> GetCompanyByEmail(string email)
        {
            return await _dataContext.Companies.Where(c => c.Email.ToLower().Equals(email.ToLower()) && c.IsDeactivated == false).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get a report list of companies
        /// </summary>
        /// <param name="paginationParameter"></param>
        /// <returns>Total record, report list</returns>
        public async Task<Tuple<int, IEnumerable<CompanyReport>>> GetCompanyReportList(PaginationParameter paginationParameter)
        {
            IQueryable<CompanyRequest> companyRequests = _dataContext.CompanyRequests;
            if (paginationParameter.SearchName != "")
            {
                companyRequests = companyRequests.Where(c => EF.Functions.ToTsVector("simple", EF.Functions.Unaccent(c.Company.Fullname.ToLower() + " " + c.Company.Username.ToLower() + " " + c.Company.Email.ToLower()))
                                                      .Matches(EF.Functions.ToTsQuery("simple", EF.Functions.Unaccent(paginationParameter.SearchName.ToLower()))));
            }

            IEnumerable<CompanyRequest> listRequestAccepted = await companyRequests
                .Select(c => new CompanyRequest
                {
                    CompanyRequestId = c.CompanyRequestId,
                    Company = new Company
                    {
                        Fullname = c.Company.Fullname
                    },
                    CompanyRequestDetails = c.CompanyRequestDetails.ToList(),
                    AcceptedAt = c.AcceptedAt,
                    ReportURL = c.ReportURL
                })
                .OrderByDescending(c => c.AcceptedAt)
                .ToListAsync();

            List<CompanyReport> listCompanyReport = new List<CompanyReport>();
            foreach (var request in listRequestAccepted)
            {
                var report = new CompanyReport
                {
                    CompanyRequestId = request.CompanyRequestId,
                    CompanyName = request.Company.Fullname,
                    NumberOfTrainee = request.CompanyRequestDetails.Count(),
                    AcceptedAt = (DateTime)request.AcceptedAt,
                    ReportURL = request.ReportURL
                };
                listCompanyReport.Add(report);
            }

            return Tuple.Create(listCompanyReport.Count(),
                                PaginationHelper.GetPage(listCompanyReport, paginationParameter.PageSize, paginationParameter.PageNumber));
        }

        /// <summary>
        /// Insert new company to database
        /// </summary>
        /// <param name="company">Company data</param>
        /// <returns>-1: existed / 0: fail / 1: done</returns>
        public async Task<int> InsertNewCompany(Company company)
        {
            if (_dataContext.Companies.Any(c => c.Username == company.Username || c.Email == company.Email))
            {
                return -1;
            }

            int rowInserted = 0;

            _dataContext.Companies.Add(company);
            rowInserted = await _dataContext.SaveChangesAsync();

            return rowInserted;
        }

        /// <summary>
        /// Insert new (company) account to DbContext without save change to DB
        /// </summary>
        /// <param name="company">Company data</param>
        /// <returns>true: insert done / false: dupplicate data</returns>
        public bool InsertNewCompanyNoSaveChange(Company company)
        {
            if (_dataContext.Companies.Any(c => c.Username == company.Username || c.Email == company.Email))
            {
                return false;
            }

            _dataContext.Companies.Add(company);

            return true;
        }

        /// <summary>
        /// Update a company with new input data
        /// </summary>
        /// <param name="id">ID of company to update</param>
        /// <param name="company">New company data</param>
        /// <returns>-1: not found / 0: fail / 1: done</returns>
        public async Task<int> UpdateCompany(int id, Company company)
        {
            var existedCompany = await _dataContext.Companies.Where(c => c.CompanyId == id && c.IsDeactivated == false).FirstOrDefaultAsync();

            if (existedCompany == null)
            {
                return -1;
            }

            existedCompany.Fullname = company.Fullname;
            existedCompany.AvatarURL = company.AvatarURL;
            existedCompany.Phone = company.Phone;
            existedCompany.Address = company.Address;
            existedCompany.Facebook = company.Facebook;

            int rowUpdated = 0;
            rowUpdated = await _dataContext.SaveChangesAsync();
            return rowUpdated;
        }

        /// <summary>
        /// Delete a company in database
        /// </summary>
        /// <param name="id">ID of company to delete</param>
        /// <returns>-1: not found / 0: fail / 1: done</returns>
        public async Task<int> DeleteCompany(int id)
        {
            var existedCompany = await _dataContext.Companies.Where(c => c.CompanyId == id && c.IsDeactivated == false).FirstOrDefaultAsync();

            if (existedCompany == null)
            {
                return -1;
            }

            existedCompany.IsDeactivated = true;
            existedCompany.DeactivatedAt = DateTime.Now;

            int rowDeleted = 0;
            rowDeleted = await _dataContext.SaveChangesAsync();
            return rowDeleted;
        }
        public async Task<CompanyRequest> GetCompanyRequestById(int id)
        {
            var cr = await _dataContext.CompanyRequests.Where(c => c.CompanyRequestId == id).FirstOrDefaultAsync();
            return cr;
        }
        /// <summary>
        /// Accept or reject company request
        /// </summary>
        /// <param name="id"> company request id </param>
        /// <param name="isAccepted"> accept or reject</param>
        /// <returns></returns>
        public async Task<(int, List<string>)> ConfirmCompanyRequest(int id, bool isAccepted)
        {
            var cr = await _dataContext.CompanyRequests.Where(c => c.CompanyRequestId == id).FirstOrDefaultAsync();
            if (cr == null)
            {
                return (-1, null);
            }
            if (cr.IsAccepted is not null)
            {
                return (-2, null);
            }

            // if request is accepted, update trainee wage and onboard
            if (isAccepted == true)
            {
                var traineeOnBoard = new List<string>();
                var crdList = await _dataContext.CompanyRequestDetails.Where(c => c.CompanyRequestId == id).ToListAsync();
                foreach (var item in crdList)
                {
                    var trainee = await _dataContext.Trainees.Where(t => t.TraineeId == item.TraineeId).FirstOrDefaultAsync();
                    if (trainee.OnBoard != true)
                    {
                        trainee.Wage = item.Wage;
                        trainee.OnBoard = true;
                    }
                    else
                    {
                        //if trainees are already in onboard, add them to a list and inform these trainees to user
                        traineeOnBoard.Add(trainee.Username);
                    }
                }
                if (traineeOnBoard.Count() != 0)
                {
                    return (-3, traineeOnBoard);
                }
            }
            cr.IsAccepted = isAccepted;
            cr.AcceptedAt = DateTime.Now;
            int rowUpdated = 0;
            rowUpdated = await _dataContext.SaveChangesAsync();
            return (rowUpdated, null);
        }
        public async Task<Tuple<int, IEnumerable<CompanyRequestResponse>>> GetCompanyRequestList(PaginationParameter paginationParameter)
        {
            IQueryable<CompanyRequest> listRequest = _dataContext.CompanyRequests.Where(c => c.IsAccepted == null);
            if (paginationParameter.SearchName != "")
            {
                listRequest = listRequest.Where(c => EF.Functions.ToTsVector("simple", EF.Functions.Unaccent(c.Company.Fullname.ToLower()))
                    .Matches(EF.Functions.ToTsQuery("simple", EF.Functions.Unaccent(paginationParameter.SearchName.ToLower()))));
            }
            List<CompanyRequest> rs = await listRequest
                .GetCount(out var totalRecords)
                .OrderByDescending(e => e.CreatedAt)
                .Select(c => new CompanyRequest
                {
                    CompanyRequestId = c.CompanyRequestId,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    ReportURL = c.ReportURL,
                    IsAccepted = c.IsAccepted,
                    AcceptedAt = c.AcceptedAt,
                    Company = c.Company,
                    CompanyRequestDetails = c.CompanyRequestDetails
                                            .Where(comreq => comreq.CompanyRequestId == c.CompanyRequestId && comreq.Trainee.IsDeactivated == false)
                                            .ToList()
                }
                                    )
                    .ToListAsync();
            List<CompanyRequestResponse> companyRequestResponses = new List<CompanyRequestResponse>();

            foreach (var item in rs)
            {
                CompanyRequestResponse itemToResponse = new CompanyRequestResponse()
                {
                    CompanyRequestId = item.CompanyRequestId,
                    CompanyName = item.Company.Fullname,
                    NumberOfTrainee = item.CompanyRequestDetails.Count(),
                    Content = item.Content,
                    CreatedAt = item.CreatedAt
                };
                companyRequestResponses.Add(itemToResponse);

            }
            return Tuple.Create(totalRecords, companyRequestResponses.GetPage(paginationParameter));
        }
        /// <summary>
        /// Get Company Request Detail
        /// </summary>
        /// <param name="requestId">ID of Request</param>
        /// <returns>Company request</returns>
        public async Task<CompanyRequest> GetCompanyRequestDetail(int requestId)
        {
            var CompanyRequest = await _dataContext.CompanyRequests.Where(comreq => comreq.CompanyRequestId == requestId)
            .Select(comreq => new CompanyRequest
            {
                CompanyRequestId = comreq.CompanyRequestId,
                Company = new Company
                {
                    Fullname = comreq.Company.Fullname
                },
                Content = comreq.Content,
                CreatedAt = comreq.CreatedAt
            }).FirstOrDefaultAsync();
            return CompanyRequest;
        }
        /// <summary>
        /// Get Trainees By Company Request Id
        /// </summary>
        /// <param name="requestId">ID of Request</param>
        /// <param name="paginationParameter">Pagination Parameter</param>
        /// <returns>Trainee list</returns>
        public async Task<Tuple<int, IEnumerable<Trainee>>> GetTraineesByCompanyRequestId(int requestId, PaginationParameter paginationParameter)
        {
            var traineeIds = await _dataContext.CompanyRequestDetails.Where(comreq => comreq.CompanyRequestId == requestId)
            .Select(comreq => comreq.TraineeId).ToListAsync();
            var traineeLists = new List<Trainee>();
            foreach (var item in traineeIds)
            {
                traineeLists.Add(await _dataContext.Trainees.Where(t => t.TraineeId == item).FirstOrDefaultAsync());
            }
            var result = traineeLists.Where(t => t.IsDeactivated == false && (t.Fullname.ToUpper().Contains(paginationParameter.SearchName.ToUpper()) || t.Email.ToUpper().Contains(paginationParameter.SearchName.ToUpper()) || t.Username.ToUpper().Contains(paginationParameter.SearchName.ToUpper())));
            int totalRecords = result.Count();
            var rs = result.OrderBy(c => c.TraineeId)
                     .Skip((paginationParameter.PageNumber - 1) * paginationParameter.PageSize)
                     .Take(paginationParameter.PageSize);
            return Tuple.Create(totalRecords, rs);
        }

    }
}