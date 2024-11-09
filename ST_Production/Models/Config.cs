namespace ST_Production.Models
{
    public class Config
    {
    }

    public class GetConfig
    {
        public int Id { get; set; }
        public int PriceListNum { get; set; }
        public string PriceListName { get; set; }

    }

    public class SaveConfig
    {
        public int PriceListNum { get; set; }

    }

}
