using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;

namespace ConagraFcnOCR
{
    //Documentaiton: https://westus.dev.cognitive.microsoft.com/docs/services/56f91f2d778daf23d8ec6739/operations/56f91f2e778daf14a499e1fa

    public static class Function2
    {
        const string endpointUrl = "https://eastus2.api.cognitive.microsoft.com/vision/v2.0/analyze";
        const string subscriptionKey = "da093e8ba0d34b10849224c3858f4088";

        const string cosmosSubscriptionKey = "3Y0MTtygYvQw2kTSO1NpHrxYohj92qQqmtR3nLtVqjZnZwE4kmBOLLUM5dFQU43bZRV4FxqzYWEjATBFWJyEBQ==";
        const string cosmosUrl = "https://conagraimgdb.documents.azure.com:443/";


        [FunctionName("Function2")]
        public static void Run([BlobTrigger("photos/{name}", Connection = "")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info("Insert a picture for analysis.");
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            log.Info($"Name: {name}");

            AnalyzeImage(myBlob, log, name).Wait();
        }

        public static async Task AnalyzeImage(Stream myBlob, TraceWriter log, string name) {

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            string requestParameters = "visualFeatures=Tags";
            string uri = endpointUrl + "?" + requestParameters;

            HttpResponseMessage response;

            
            byte[] byteData = ConvertStreamToByteArray(myBlob);


            string contentString;
            JObject json;
            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);

                contentString = await response.Content.ReadAsStringAsync();
                log.Info("Image details: ");
                log.Info(contentString);

                json = JObject.Parse(contentString);
                json.Add("id", name);
            }

            //sends JSON of image tags to cosmos DB 
            DocumentClient clientDB = new DocumentClient(new Uri(cosmosUrl), cosmosSubscriptionKey);
           // await clientDB.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("VisionAPIDB", "ImgRespCollection"), json, null, true);
            await clientDB.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("VisionAPIDB", "ImgRespCollection"), json, null, true);
        }


        
        private static byte[] ConvertStreamToByteArray(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

    }
}
