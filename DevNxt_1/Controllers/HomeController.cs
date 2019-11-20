using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System.IO;
using System.Configuration;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Web.UI;
using DevNxt_1.Models;
using Newtonsoft.Json;

namespace DevNxt_1.Controllers
{
    public class HomeController : Controller
    {
        public static string SouthCentralUsEndpoint = ConfigurationManager.AppSettings["CustomVisionApiEndPoint"];
        private static List<string> tags;
        public static string trainingKey = ConfigurationManager.AppSettings["CustomVisionTrainingKey"];
        public static string predictionKey = ConfigurationManager.AppSettings["CustomVisionPredictionKey"];
        public static string filePath;
        public static string filePath1;
        public string responsetext;
        IList<Result> result;

        static Guid projectID;
        public ActionResult Index()

        {
            ViewBag.Title = "Home Page";
            return View();
        }



        [HttpPost]
        public ActionResult TrainingForm(string tagDataForm)
        {
            tags = tagDataForm.Split(new Char[] { ';', ',', '.', '-', '\n' }).ToList();
            uploadImages();
            TempData["Message"] = "Training Completed";
            return new RedirectResult(@"~\Home\");

        }

        [HttpPost]
        public ActionResult PredictingForm(string urlFormData)
        {
            //IList<Result> predictionModel = new List<Result>();
            var predictionModel = predictingImages(urlFormData);
            TempData["prediction"] = predictionModel;
            return new RedirectResult(@"~\PredictionResult\");
        }

        [HttpPost]
        public JsonResult Upload(string baseData)
        {
            if (HttpContext.Request.Files.AllKeys.Any())
            {
                for (int i = 0; i <= HttpContext.Request.Files.Count; i++)
                {
                    var file = HttpContext.Request.Files["files" + i];
                    if (file != null)
                    {
                        var fileSavePath = Path.Combine(Server.MapPath("/Files"), file.FileName);
                        var fileSavePath1 = Path.Combine(Server.MapPath("/Images"), file.FileName);
                        filePath = fileSavePath;
                        filePath1 = fileSavePath1;
                        //Session["filePath"] = fileSavePath;

                        file.SaveAs(fileSavePath);
                        file.SaveAs(fileSavePath1);
                        
                    }
                }
            }
            return Json("Image Uploaded :" + Session["filePath"]);
        }
        [HttpPost]
        public async Task<JsonResult> PredictingForm1(string imagePath)
        {
            var img=Upload("");
            imagePath = filePath1;
            IList<Result> predictionModel = await predictingImages1("" + imagePath);
            //string predictionModel = predictingImages1("" + imagePath);
            TempData["prediction"] = predictionModel;
            Session["prediction"] = predictionModel;
            TempData["image"] = filePath1;
            // return new RedirectResult(@"~\PredictionResult\");
            return Json("",JsonRequestBehavior.AllowGet);// RedirectToAction("Index", "PredictionResult");
        }
        static byte[] GetByteArray(string LocalimageFilePath)
        {
            FileStream ImagefileStream = new FileStream(LocalimageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader ImagebinaryReader = new BinaryReader(ImagefileStream);
            byte[] bytedata = ImagebinaryReader.ReadBytes((int)ImagefileStream.Length);
            ImagefileStream.Close();
            return bytedata;
        }
        public async Task<IList<Result>> predictingImages1(String filepath)
        {
            var APIclient = new HttpClient();

            try
            {
                //ComputerVisionAPIclient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Subscriptionkey());
                //string requestParameters = RequestParameters();
                string APIuri = "http://localhost:62566/api/classifierPrediction?imgpath=" + filePath1;
                HttpResponseMessage myresponse;
                MultipartFormDataContent form = new MultipartFormDataContent();
                //byte[] byteData = GetByteArray(filePath);
                //var content = new ByteArrayContent(byteData);
                var fileStream = new FileStream(filePath, FileMode.Open);
                form.Add(new StreamContent(fileStream));
                //content.Headers.ContentType = new MediaTypeHeaderValue(Contenttypes());
                myresponse = await APIclient.PostAsync(APIuri, form);
                myresponse.EnsureSuccessStatusCode();
                responsetext = await myresponse.Content.ReadAsStringAsync();

                result = JsonConvert.DeserializeObject<IList<Result>>(responsetext);
                //foreach(var result in ras)
                //{

                //}


            }
            catch (Exception e)
            {
            }
            return result;
        }

        public static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream1 = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream1);
            return binaryReader.ReadBytes((int)fileStream1.Length);
        }

        public IList<Result> predictingImages(String imgUrl)
        {
            
            IList<Result> predictionResult = new List<Result>();
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = SouthCentralUsEndpoint
            };
            projectID = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);
            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl testImage = new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl(imgUrl);
            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction result1 = endpoint.ClassifyImageUrl(projectID, "Veg Classifier", testImage);
            foreach(var output in result1.Predictions)
            {
                Result finalresult = new Result();
                finalresult.probability = output.Probability*100;
                finalresult.tagName = output.TagName;
                predictionResult.Add(finalresult);
            }
            return predictionResult;
        }
        public static Images ImageSearch(string queryString)
        {
            string subscriptionKey = ConfigurationManager.AppSettings["BingImageSearchApiSubscriptionKey"];
            Images imageResult = null;

            var searchClient = new ImageSearchClient(new ApiKeyServiceClientCredentials(subscriptionKey));
            try
            {

                int count = 60;
                int offset = count;
                imageResult = searchClient.Images.SearchAsync(query: queryString, count: count, offset: offset).Result;
                return imageResult;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }


        }
        public static void uploadImages()
        {
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = SouthCentralUsEndpoint
            };

            Guid projectId = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);
            var project = trainingApi.GetProject(projectId);
            projectID = project.Id;
            foreach (String t in tags)
            {
                Images resultImages = ImageSearch(t);
                List<Guid> tagList = new List<Guid>();

                if (t != "")
                {
                    try
                    {
                        Tag tg = trainingApi.CreateTag(project.Id, t);
                        tagList.Add(tg.Id);
                    }
                    catch (Exception e)
                    {

                        List<Tag> oldTagList = new List<Tag>(trainingApi.GetTags(project.Id));
                        foreach (Tag sampleTag in oldTagList)
                        {
                            if (sampleTag.Name.Equals(t))
                            {
                                tagList.Add(sampleTag.Id);
                                break;
                            }
                        }
                    }
                }

                List<ImageUrlCreateEntry> imageUrlCreateEntries = new List<ImageUrlCreateEntry>();
                foreach (var img in resultImages.Value)
                {
                    ImageUrlCreateEntry imageUrlCreateEntry = new ImageUrlCreateEntry(img.ContentUrl, tagList);
                    imageUrlCreateEntries.Add(imageUrlCreateEntry);
                }

                ImageUrlCreateBatch batchImages = new ImageUrlCreateBatch(imageUrlCreateEntries);
                trainingApi.CreateImagesFromUrls(project.Id, batchImages);

                var iteration = trainingApi.TrainProject(project.Id);

                while (iteration.Status == "Training")
                {
                    Thread.Sleep(1000);


                    iteration = trainingApi.GetIteration(project.Id, iteration.Id);
                }

                iteration.IsDefault = true;
                trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);
            }
        }

    }
}