using Microsoft.Extensions.Hosting;
using IPAddresses.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using static IPAddresses.Services.EnumerableExtension;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IPAddresses.Services
{
    public class DBService : IDBService
    {
        private readonly IpaddressesContext _context;

        public DBService(IpaddressesContext context)
        {
            _context = context;
        }

        public async Task<Country> GetCountry(string twoLetterCode)
        {
            return await _context.Countries.FirstOrDefaultAsync(obj => obj.TwoLetterCode == twoLetterCode);
        }

        public async Task<Ipaddress> GetIpWithCountry(string ip)
        {
            return await _context.Ipaddresses.Include(obj => obj.Country).FirstOrDefaultAsync(obj => obj.Ip == ip);
        }

        public async Task<List<Ipaddress>> GetIpListWithCountry()
        {
            return await _context.Ipaddresses.Include(obj => obj.Country).ToListAsync();
        }

        public void AddCountry(Country country)
        {
            _context.Countries.Add(country);
            _context.SaveChanges();
            return;
        }

        public void AddIpAddress(Ipaddress ipaddress)
        {
            _context.Ipaddresses.Add(ipaddress);
            _context.SaveChanges();
            return;
        }

        public void UpdateIpAddress(Ipaddress ipaddress)
        {
            _context.Entry(ipaddress).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void DeleteIp(Ipaddress ipaddress)
        {
            _context.Ipaddresses.Remove(ipaddress);
            _context.SaveChanges();
        }

        public IQueryable<string> ExecuteSQLQuery(string query)
        {
            return _context.Database.SqlQueryRaw<string>(query);
        }
    }
}
