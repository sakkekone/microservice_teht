using microservice_teht.DTO;
using microservice_teht.Extensions;
using microservice_teht.models;
using Microsoft.AspNetCore.Mvc;
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
            return data2.Prices.Count + " entries fetched, not adding duplicates: " + counter.ToString() + " new entries added";
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
        public async Task<IActionResult> get_prices([FromQuery] DateTime? start,[FromQuery] DateTime? end)
        {
            if(start == null || end == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest,"start and end parameters must be in DateTime format");
            }
            string log = "entries between " + start + " and " + end + "\n";
            try
            {
                var result = _electricityDbContext.ElectricityPriceDatas
                    .Where(e => (e.StartDate > start && e.StartDate < end))
                    .OrderBy(e => e.StartDate).ToList();
                foreach(var a in result)
                {
                    log += a.StartDate + ":\t" + a.Price + "\n";
                }
                _logger.LogInformation(log);
                _logger.LogInformation("total entries: " + result.Count.ToString());
                return Ok(result);
            } catch(Exception e)
            {
                _logger.LogError(e,"error!");
                return StatusCode(StatusCodes.Status500InternalServerError,"error");
            }
        }

        [Route("getprices_page")]
        [HttpGet]
        public async Task<IActionResult> get_prices_page([FromQuery] DateTime? start,[FromQuery] DateTime? end,
            [FromQuery] int page,[FromQuery] int pageSize)
        {
            if(page < 1) { page = 1; }
            if(pageSize < 1) { pageSize = 1; }
            if(pageSize > 20) { pageSize = 20; }//max pagesize 20
            if(start == null || end == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest,"start and end parameters must be in DateTime format");
            }
            string log = "entries between " + start + " and " + end + "\n";
            try
            {
                var result = _electricityDbContext.ElectricityPriceDatas
                    .Where(e => (e.StartDate > start && e.StartDate < end))
                    .OrderBy(e => e.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize).ToList();

                foreach(var a in result)
                {
                    log += a.StartDate + ":\t" + a.Price + "\n";
                }
                log += "page: " + page + " pageSize: " + pageSize;
                _logger.LogInformation(log);
                return Ok(result);
            } catch(Exception e)
            {
                _logger.LogError(e,"error!");
                return StatusCode(StatusCodes.Status500InternalServerError,"error");
            }
        }
        [Route("GetPriceDifference")]//returns total priceDifference between start and end compared to fixedPrice, negative number means fixedPrice is cheaper
        [HttpGet]
        public async Task<IActionResult> get_prices_dif([FromQuery] DateTime start,[FromQuery] DateTime end,
            [FromQuery] decimal fixedPrice)
        {
            if(start == null || end == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest,"start and end parameters must be in DateTime format");
            }
            string log = "price difference between " + start + " and " + end + "\tfixedPrice:" + fixedPrice + "\n";
            try
            {
                var result = _electricityDbContext.ElectricityPriceDatas
                    .Where(e => (e.StartDate > start && e.StartDate < end))
                    .OrderBy(e => e.StartDate).ToList();
                decimal totalprice = 0;
                foreach(var a in result)
                {
                    totalprice += a.Price;
                }
                totalprice /= result.Count;
                log += "totalprice: " + totalprice + "\n";
                totalprice -= fixedPrice;
                _logger.LogInformation(log);
                return Ok(new PriceDifference(start,end,totalprice));
            } catch(Exception e)
            {
                _logger.LogError(e,"error!");
                return StatusCode(StatusCodes.Status500InternalServerError,"error");
            }
        }
        [Route("GetPriceDifferenceList")]//returns list of priceDifference for each hour between start and end, negative number means 
        [HttpGet]
        public async Task<IActionResult> get_prices_dif_list([FromQuery] DateTime? start,[FromQuery] DateTime? end,
            [FromQuery] decimal fixedPrice)
        {
            if(start == null || end == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest,"start and end parameters must be in DateTime format");
            }
            string log = "price difference between " + start + " and " + end + "\tfixedPrice:" + fixedPrice + "\n";
            try
            {
                var result = _electricityDbContext.ElectricityPriceDatas
                    .Where(e => (e.StartDate > start && e.StartDate < end))
                    .OrderBy(e => e.StartDate).ToList();
                List<PriceDifference> pd_list = new List<PriceDifference>();
                foreach(var a in result)
                {
                    pd_list.Add(new PriceDifference(a.StartDate,a.EndDate,a.Price - fixedPrice));
                    log += a.StartDate + ":\t" + pd_list[pd_list.Count - 1].PriceDifferenceValue + "\n";
                }
                _logger.LogInformation(log);
                return Ok(pd_list);
            } catch(Exception e)
            {
                _logger.LogError(e,"error!");
                return StatusCode(StatusCodes.Status500InternalServerError,"error");
            }
        }
    }
}
