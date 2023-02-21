using Microsoft.Extensions.Hosting;
using IPAddresses.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using static IPAddresses.Services.EnumerableExtension;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace IPAddresses.Services
{
    public class PeriodicUpdateService : BackgroundService
    {

        private static HttpClient _client;

        private readonly IMemoryCache _memoryCache;

        private readonly IServiceScopeFactory _scopeFactory;

        public PeriodicUpdateService(IMemoryCache memoryCache, IServiceScopeFactory scope)
        {
            _memoryCache = memoryCache;
            _scopeFactory = scope;
            _client = new()
            {
                BaseAddress = new Uri("https://ip2c.org/"),
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            var response = new HttpResponseMessage();

            var batchSize = 100;

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                // Access scoped services like this:
                var _dBService = scope.ServiceProvider.GetRequiredService<IDBService>();

                var ipAddressesInDB = await _dBService.GetIpListWithCountry();

                var batches = ipAddressesInDB.Batch(batchSize);

                var outdatedIPs = new List<String>();

                foreach (var batch in batches)
                {
                    foreach (var item in batch)
                    {
                        response = await _client.GetAsync(item.Ip);

                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var countryInfo = jsonResponse.Split(";");

                        if (countryInfo[0] == "1")
                        {
                            if (countryInfo[1] != item.Country.TwoLetterCode)
                            {
                                outdatedIPs.Add(item.Ip);
                                var ipCountry = await _dBService.GetCountry(countryInfo[1]);

                                if (ipCountry == null)
                                {
                                    _dBService.AddCountry(new Country()
                                    {
                                        Name = countryInfo[3],
                                        TwoLetterCode = countryInfo[1],
                                        ThreeLetterCode = countryInfo[2]
                                    });

                                    ipCountry = await _dBService.GetCountry(countryInfo[1]);
                                }
                                item.Country = ipCountry;
                                _dBService.UpdateIpAddress(item);
                            }
                        }
                        else
                        {
                            outdatedIPs.Add(item.Ip);
                            _dBService.DeleteIp(item);
                        }
                    }
                }

                if (outdatedIPs.Any())
                {
                    foreach (var outdatedIp in outdatedIPs)
                    {
                        _memoryCache.Remove(outdatedIp);
                    }
                }
            }
        }
    }
}
