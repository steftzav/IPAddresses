using IPAddresses.Models;
using Microsoft.AspNetCore.Mvc;

namespace IPAddresses.Services
{
    public interface IDBService
    {
        Task<Country> GetCountry(string twoLetterCode);

        Task<Ipaddress> GetIpWithCountry(string ip);

        Task<List<Ipaddress>> GetIpListWithCountry();

        void AddCountry(Country country);

        void AddIpAddress(Ipaddress ipaddress);

        void UpdateIpAddress(Ipaddress ipaddress);

        void DeleteIp(Ipaddress ipaddress);

        IQueryable<string> ExecuteSQLQuery(string query);
    }
}
