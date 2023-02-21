using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using IPAddresses.Models;
using Microsoft.IdentityModel.Tokens;
using IPAddresses.Services;

namespace IPAddresses.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IpaddressesController : ControllerBase
    {

        private static HttpClient _client;

        private readonly IMemoryCache _memoryCache;

        private static IDBService _dBService;

        public IpaddressesController(IpaddressesContext context, IMemoryCache memoryCache, IDBService dBService)
        {
            _dBService = dBService;
            _memoryCache = memoryCache;
            _client = new()
            {
                BaseAddress = new Uri("https://ip2c.org/"),
            };
        }

        // GET: api/Ipaddresses/ΧΧΧ.ΧΧΧ.ΧΧΧ.ΧΧΧ
        [HttpGet("{ipAddress}")]
        public async Task<ActionResult<List<String>>> GetIpaddressInfo(string ipAddress)
        {
            if (ipAddress.IsNullOrEmpty() || ipAddress.Length > 15)
            {
                return new List<String>() { "WRONG INPUT" };
            }

            var info = new List<String>();

            if (_memoryCache.TryGetValue(ipAddress, out List<String>? cacheOutput))
            {
                return cacheOutput;
            }
            else
            {
                var ipaddressObj = await _dBService.GetIpWithCountry(ipAddress);

                if (ipaddressObj != null)
                {
                    var country = ipaddressObj.Country;
                    info.AddRange(new List<String>() { country.Name, country.TwoLetterCode, country.ThreeLetterCode });
                    var cacheEntryOptions = new MemoryCacheEntryOptions();
                    cacheEntryOptions.SlidingExpiration = TimeSpan.FromHours(5);
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                    _memoryCache.Set(ipAddress, info, cacheEntryOptions);
                }
                else
                {
                    using HttpResponseMessage response = await _client.GetAsync(ipAddress);

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var countryInfo = jsonResponse.Split(";");

                    if (countryInfo[0] == "1")
                    {
                        info.AddRange(new List<String>() { countryInfo[3], countryInfo[1], countryInfo[2] });
                        var countryInDB = await _dBService.GetCountry(countryInfo[1]);

                        if (countryInDB == null)
                        {
                            _dBService.AddCountry(new Country()
                            {
                                Name = countryInfo[3],
                                TwoLetterCode = countryInfo[1],
                                ThreeLetterCode = countryInfo[2]
                            });

                            countryInDB = await _dBService.GetCountry(countryInfo[1]);
                        }

                        _dBService.AddIpAddress(new Ipaddress
                        {
                            Country = countryInDB,

                            Ip = ipAddress
                        });

                        var cacheEntryOptions = new MemoryCacheEntryOptions();
                        cacheEntryOptions.SlidingExpiration = TimeSpan.FromHours(5);
                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                        _memoryCache.Set(ipAddress, info, cacheEntryOptions);
                    }
                    else
                    {
                        info.Add(countryInfo[3]);
                    }
                }
            }

            return info;
        }

        [HttpGet("report/")]
        public async Task<ActionResult<List<string>>> GetReport (string? countryCodes=null)
        {
            string SQLQuery;
            if (countryCodes.IsNullOrEmpty())
            {
                SQLQuery = $"select concat('CountryName: ', c.name,', AddressesCount: ', count(ip.CountryId),', LastAddressUpdatedAt: ', max(ip.UpdatedAt))" +
                    "from IPAddresses ip " +
                    "inner join Countries c on ip.CountryId=c.Id " +
                    " group by c.name";
            }
            else
            {
                var codes = countryCodes.Split(",");
                var parameter = "";
                foreach (var code in codes)
                {
                    parameter += "'" + code.Trim() + "'" + ",";
                }
                parameter = parameter.Remove(parameter.Length - 1);
                SQLQuery = $"select concat('CountryName: ', c.name,', AddressesCount: ', count(ip.CountryId),', LastAddressUpdatedAt: ', max(ip.UpdatedAt))" +
                            "from IPAddresses ip " +
                            "inner join Countries c on ip.CountryId=c.Id " +
                            "where c.TwoLetterCode in ( " + parameter + ")" +
                            " group by c.name";
            }
            var report = await _dBService.ExecuteSQLQuery(SQLQuery).ToListAsync();

            return report;
        }
    }
}
