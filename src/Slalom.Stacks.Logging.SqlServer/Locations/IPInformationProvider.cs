using MaxMind.GeoIP2;

namespace Slalom.Stacks.Logging.SqlServer.Locations
{
    public class IPInformation
    {
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string Isp { get; set; }

        public string IPAddress { get; set; }
    }

    public class IPInformationProvider
    {
        public IPInformation Get(string address)
        {
            using (var client = new WebServiceClient(119543, "1Ksa6iuvfOJu"))
            {
                try
                {
                    var response = client.CityAsync(address).Result;

                    return new IPInformation
                    {
                        Latitude = response.Location.Latitude,
                        Longitude = response.Location.Longitude,
                        Isp = response.Traits.Isp,
                        IPAddress = response.Traits.IPAddress
                    };
                }
                catch
                {
                    return new IPInformation
                    {
                        IPAddress = address
                    };
                }
            }
        }
    }
}
