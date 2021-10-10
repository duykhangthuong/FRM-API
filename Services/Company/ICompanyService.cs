using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kroniiapi.DB.Models;

namespace kroniiapi.Services
{
    public interface ICompanyService
    {
        Task<Company> GetCompanyById(int id);
        Task<Company> GetCompanyByUsername(string username);
        Task<Company> GetCompanyByEmail(string email);
        Task<int> InsertNewCompany(Company company);
        Task<int> UpdateCompany(int id, Company company);
        Task<int> DeleteCompany(int id);
    }
}