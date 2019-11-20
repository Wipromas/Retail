using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DevNxt_1.Models
{
    public class FormModel
    {
        public string TagName { get; set; }
        public double Probability { get; set; }
        public Guid TagId { get; set; }
        public Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.BoundingBox BoundingBox { get; set; }
    }
}