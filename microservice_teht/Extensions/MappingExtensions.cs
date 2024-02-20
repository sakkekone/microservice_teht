using microservice_teht.DTO;
using microservice_teht.models;

namespace microservice_teht.Extensions
{
    public static class MappingExtensions
    {
        public static sahko_entity ToEntity(this PriceInfo priceInfo)
        {
            return new sahko_entity
            {
                StartDate = priceInfo.StartDate,
                EndDate = priceInfo.EndDate,
                Price = priceInfo.Price
            };
        }
    }
}
