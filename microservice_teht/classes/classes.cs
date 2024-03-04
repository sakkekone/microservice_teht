namespace microservice_teht
{
    public class PriceDifference
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PriceDifferenceValue { get; set; }

        public bool fixedIsCheaper { get; set; }

        public PriceDifference(DateTime startDate,DateTime endDate,decimal priceDifference)
        {
            StartDate = startDate;
            EndDate = endDate;
            PriceDifferenceValue = priceDifference;
            fixedIsCheaper = (PriceDifferenceValue < 0);
        }
    }
}
