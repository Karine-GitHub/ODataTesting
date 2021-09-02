namespace TestConnectedService
{
    public static class ODataServiceExtension
    {
        public static void AuthenticatedDataServiceContext(this Default.Container container)
        {
            container.Configurations.RequestPipeline.OnMessageCreating = (args) => 
            {
                var message = new HttpClientRequestMessage(args.ActualMethod) { Url = args.RequestUri, Method = args.Method, };
                foreach (var header in args.Headers)
                {
                    message.SetHeader(header.Key, header.Value);
                }

                return message;
            };
        }
    }
}
