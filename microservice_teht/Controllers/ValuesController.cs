using microservice_teht.DTO;
using microservice_teht.Extensions;
using microservice_teht.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace microservice_teht.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController:ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private ElectricityDbContext _electricityDbContext;
        private ILogger<ValuesController> _logger;

        /*public ValuesController(IHttpClientFactory httpClientFactory)
            {
            _httpClient = httpClientFactory.CreateClient();
            }*/

        public ValuesController(ElectricityDbContext electricityDbContext,ILogger<ValuesController> logger,IHttpClientFactory httpClientFactory)
        {
            _electricityDbContext = electricityDbContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public string test_get()
        {
            return ("test return");
        }

        [Route("getdb")]
        [HttpGet]
        public async Task<IActionResult> FetchElectricityPricesAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            string content = null;

            try
            {
                var response = await httpClient.GetAsync(Constants.Constants.ElectrictyDataUrl);

                response.EnsureSuccessStatusCode();

                content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Hinnat haettu: {content}");
                _logger.LogInformation("\n---!\n");

            } catch(Exception ex)
            {
                _logger.LogError(ex,"Virhe sähkön hintatietojen haussa");
            }

            try
            {
                string ret = await save_to_db(content);
                return Ok(ret);

            } catch(Exception ex)
            {
                _logger.LogError(ex,"Error saving data to db");
                return StatusCode(StatusCodes.Status500InternalServerError,"Virhe tallennettaessa dataa tietokantaan.");

                throw;
            }
            return Ok("Data vastaanotettu ja käsitelty.");
        }

        private async Task<string> save_to_db(string data)
        {
            ElectricityPriceDataDtoIn data2 = JsonConvert.DeserializeObject<ElectricityPriceDataDtoIn>(data);
            _logger.LogInformation(data);
            var counter = 0;
            foreach(var hourPrice in data2.Prices)
            {
                bool exists = _electricityDbContext.ElectricityPriceDatas.Any(e => e.StartDate == hourPrice.StartDate);
                if(!exists)
                {
                    _electricityDbContext.ElectricityPriceDatas.Add(hourPrice.ToEntity());
                    counter++;
                }
            }
            _logger.LogInformation("added " + counter + " new entries");
            //_electricityDbContext.ElectricityPriceDatas.OrderBy(e => e.StartDate);
            await _electricityDbContext.SaveChangesAsync();
            return data2.Prices.Count+" entries fetched, not adding duplicates: "+counter.ToString()+" new entries added";
        }
        
        [Route("cleartable")]
        [HttpGet]
        public async Task<IActionResult> clear_table()
        {
            try
            {
                var linq = _electricityDbContext.ElectricityPriceDatas.ToList();
                _electricityDbContext.ElectricityPriceDatas.RemoveRange(linq);
                await _electricityDbContext.SaveChangesAsync();
            } catch(Exception e)
            {
                _logger.LogError(e,"error clearing table");
                return StatusCode(StatusCodes.Status500InternalServerError,"error clearing table");
            }
            _logger.LogInformation("table cleared");
            return Ok("table cleared");
        }
        [Route("getprices")]
        [HttpGet]
        public async Task<IActionResult> get_prices([FromQuery] DateTime start,[FromQuery] DateTime end)
        {
            if(start == null || end == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest,"start and end parameters must be in DateTime format");
            }
            string log = "entries between " + start + " and " + end + "\n";
            try
            {
                var linq = _electricityDbContext.ElectricityPriceDatas
                    .Where(e => (e.StartDate > start && e.StartDate < end))
                    .OrderBy(e => e.StartDate).ToList();
                foreach(var a in linq)
                {
                    log += a.StartDate + ":\t" + a.Price + "\n";
                }
                _logger.LogInformation(log);
                _logger.LogInformation("total entries: " + linq.Count.ToString());
                return Ok(log+"total entries: "+linq.Count.ToString());
            } catch(Exception e)
            {
                _logger.LogError(e,"error!");
                return StatusCode(StatusCodes.Status500InternalServerError,"error");
            }
        }
    }
}
