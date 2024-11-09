namespace ST_Production.Models
{


    public class UserWiseSeries
    {
        public int User_ID { get; set; }
        public List<subSeriesClass> Series_Details { get; set; }
    }


    public class ShiftSeries
    {
        public List<subSeriesClass> SeriesList { get; set; }
    }


    public class subSeriesClass
    {
        public string text { get; set; }
        public List<string> seriesList { get; set; }
    }


    public class seriesMasterDetailClass
    {
        public List<subSeriesClass> Series_Details { get; set; }
    }


    public class getUserSeries
    {
        public int User_ID { get; set; }
    }
}
