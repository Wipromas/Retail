using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
namespace DemoClassification
{
    class Program
    {
        public static string SouthCentralUsEndpoint = "https://southcentralus.api.cognitive.microsoft.com";
        private static List<string> tags;
        public static string trainingKey = "7dee396051ee4245a41edc3450d3c093";// "aabe2a09f2084b00b9f0cea8b66c2d84";
        public static string predictionKey = "53eee070b185488bbe3aff6915c3cfbb"; //"9074dc3cd5864d39838ee0b415c6848a";

        static Guid projectID;
        //return Images fetched through Bing Image Search API for a particular query string
        public static Images ImageSearch(string queryString)
        {
            string subscriptionKey = "5bca1b71485849c99c362c4c7299102e";
            Images imageResult = null;
            // initializing search client.
            var searchClient = new ImageSearchClient(new ApiKeyServiceClientCredentials(subscriptionKey));
            try
            {
                imageResult = searchClient.Images.SearchAsync(query: queryString).Result;
                return imageResult;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }


        }

        static void Main(string[] args)
        {
            tags = new List<string>();
         

           // Console.WriteLine("Enter the tags of the image to be uploaded");
            for (int i = 0; i < 2; i++)
            {
                tags.Add(Console.ReadLine());
            }
            // Add your training & prediction key from the settings page of the portal


            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = SouthCentralUsEndpoint
            };
            uploadImages(trainingApi);
            // Now there is a trained endpoint, it can be used to make a prediction

            // Create a prediction endpoint, passing in obtained prediction key
           

           // predictingImages(endpoint);
            Console.ReadKey();

        }

        public static void predictingImages(String imgUrl)
        {
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = SouthCentralUsEndpoint
            };
            // Make a prediction against the new project
            //Console.WriteLine("Predicting:");
            //Test Image to be predicted after training of classifier
            projectID = new Guid("83867a3b-64ae-4c1d-994c-1fc75b7f0e5a");
            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl testImage = new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl(imgUrl);
            var result = endpoint.PredictImageUrl(projectID, testImage);

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            }
        }

        public static void uploadImages(CustomVisionTrainingClient trainingApi)
        {
            //getting the project details via projectId
            Guid projectId = new Guid("83867a3b-64ae-4c1d-994c-1fc75b7f0e5a");
            var project = trainingApi.GetProject(projectId);
            projectID = project.Id;
            foreach (String t in tags)
            {
                Images resultImages = ImageSearch(t);
                List<Guid> tagList = new List<Guid>();
                //Spliting the query string for getting the tags for the  images
                string[] splitString = t.Split(new Char[] { ' ', ',', '.', '-', '\n', '\t' });
                foreach (string y in splitString)
                {

                    if (y.Trim() != "")
                    {
                        try
                        {
                            Tag tg = trainingApi.CreateTag(project.Id, y.Trim());
                            tagList.Add(tg.Id);
                        }
                        catch (Exception e)
                        {
                            //Tag is already stored in the model, so we are fetching its TagID
                            List<Tag> oldTagList = new List<Tag>(trainingApi.GetTags(project.Id));
                            foreach (Tag sampleTag in oldTagList)
                            {
                                if (sampleTag.Name.Equals(y.Trim()))
                                {
                                    tagList.Add(sampleTag.Id);
                                    break;
                                }
                            }
                        }
                    }
                }
                List<ImageUrlCreateEntry> imageUrlCreateEntries = new List<ImageUrlCreateEntry>();
                foreach (var img in resultImages.Value)
                {
                    //ImageUrl imageUrl = new ImageUrl(img.ContentUrl);
                    ImageUrlCreateEntry imageUrlCreateEntry = new ImageUrlCreateEntry(img.ContentUrl, tagList);
                    imageUrlCreateEntries.Add(imageUrlCreateEntry);
                }
                //Creating the images batch with their tag ids
                ImageUrlCreateBatch batchImages = new ImageUrlCreateBatch(imageUrlCreateEntries);
                trainingApi.CreateImagesFromUrls(project.Id, batchImages);
                //training the classifier
                var iteration = trainingApi.TrainProject(project.Id);
                // The returned iteration will be in progress, and can be queried periodically to see when it has completed
                while (iteration.Status == "Training")
                {
                    Thread.Sleep(1000);

                    // Re-query the iteration to get it's updated status
                    iteration = trainingApi.GetIteration(project.Id, iteration.Id);
                }
                // The iteration is now trained. Make it the default project endpoint
                iteration.IsDefault = true;
                trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);
            }
        }
    }
}
