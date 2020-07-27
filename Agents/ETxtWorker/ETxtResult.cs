using System.Collections.Generic;

namespace ETxtWorker
{
    public class ETxtResult
    {
        public double Processed { get; set; } = 0;
        public double UniquePhrases { get; set; } = 0;
        public double Errors { get; set; } = 0;
        
        public List<ETxtDetailed> Detailed { get; set; } = new List<ETxtDetailed>();
    }
}