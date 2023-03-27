
namespace BookingAPI
{

    public struct myDate
    {
        public myDate()
        {

        }

        /* BEWARE there is difference in DateTime and DateAndTime. This is DateTime https://learn.microsoft.com/en-us/dotnet/api/system.datetime?view=net-8.0
         var dateString = "5/1/2008 8:30:52 AM";
DateTime date1 = DateTime.Parse(dateString,
                          System.Globalization.CultureInfo.InvariantCulture); // String to date
         var iso8601String = "20080501T08:30:52Z";
DateTime dateISO8602 = DateTime.ParseExact(iso8601String, "yyyyMMddTHH:mm:ssZ",
                                System.Globalization.CultureInfo.InvariantCulture); // date to string
         */

        public DateTime X { get; }
        public double Y { get; }

        //public override string ToString() => $"({X}, {Y})";
    }
    public class Today
    {
        public System.DateTime dateToday()
        {
            System.DateTime today = DateTime.Today;
            return today;

        }
        
    }

}
