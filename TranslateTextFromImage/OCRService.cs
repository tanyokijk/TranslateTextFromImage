using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Core;
using Newtonsoft.Json.Linq;

namespace TranslateTextFromImage;

public class OCRService
{
    string subscriptionKey = "c5d19c144d534d8ea53637dc519027d0";
    string endpoint = "https://computer-vision-dev-cus.cognitiveservices.azure.com/";

    public async Task<string> ExtractTextUsingAzure(string imagePath)
    {
        string ocrUri = $"{endpoint}/vision/v3.2/read/analyze";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            using (FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    byte[] byteData = binaryReader.ReadBytes((int)fileStream.Length);

                    using (ByteArrayContent content = new ByteArrayContent(byteData))
                    {
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                        HttpResponseMessage response = await client.PostAsync(ocrUri, content);
                        response.EnsureSuccessStatusCode();

                        string operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();

                        bool done = false;
                        while (!done)
                        {
                            HttpResponseMessage result = await client.GetAsync(operationLocation);
                            string resultContent = await result.Content.ReadAsStringAsync();
                            JObject jsonResult = JObject.Parse(resultContent);
                            string status = jsonResult["status"].ToString();

                            if (status == "succeeded")
                            {
                                done = true;
                                var lines = jsonResult["analyzeResult"]["readResults"].SelectMany(r => r["lines"]);
                                foreach (var line in lines)
                                {
                                    var text = line["text"].ToString();                                    
                                    return text;
                                }
                            }
                            else if (status == "failed")
                            {
                                Console.WriteLine("OCR operation failed.");
                                done = true;
                            }
                            else
                            {
                                await Task.Delay(1000);
                            }
                        }
                    }
                }
            }
        }
        return null;
    }
}
