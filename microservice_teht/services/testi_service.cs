namespace microservice_teht.services
{
    public class ElectricityPriceFetchingService : BackgroundService
    {
        private readonly ILogger<ElectricityPriceFetchingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ElectricityPriceFetchingService(ILogger<ElectricityPriceFetchingService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await FetchElectricityPricesAsync(stoppingToken);
                //Wait 1 hour to fetch again
                await Task.Delay(TimeSpan.FromDays(5), stoppingToken);
            }
        }

        private async Task FetchElectricityPricesAsync(CancellationToken stoppingToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetAsync(Constants.Constants.ElectrictyDataUrl, stoppingToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Hinnat haettu: {content}");
								_logger.LogInformation("\n---\n");


                // Tässä kohtaa voitaisiin välittää data toiselle palvelulle tai tallentaa se
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Virhe sähkön hintatietojen haussa");
            }
        }

      
    }
}
