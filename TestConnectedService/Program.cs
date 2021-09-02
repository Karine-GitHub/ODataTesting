using System;
using System.Linq;

namespace TestConnectedService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var ctx = new Default.Container(new Uri("https://localhost:5001/odata"));
            ctx.AuthenticatedDataServiceContext();

            ctx.SendingRequest2 += Ctx_SendingRequest2;

            var countComm = ctx.WeatherForecast;
            var res = ctx.WeatherForecast.Execute();


            foreach (var item in countComm)
            {
                Console.WriteLine(item.TemperatureC);
            }       

            //Console.WriteLine(countComm);
            Console.ReadKey();
        }

        private static void Ctx_SendingRequest2(object sender, Microsoft.OData.Client.SendingRequest2EventArgs e)
        {
            
        }
    }
}
