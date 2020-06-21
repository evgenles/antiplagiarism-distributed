namespace AdvegoPlagiatusWorker
{
    public class AdvegoResult
    {
        public double Processed { get; set; } = 0;
        public double UniqueWords { get; set; } = 0;
        public double UniquePhrases { get; set; } = 0;
        public double DocumentChecked { get; set; } = 0;
        public double SimilarDocument { get; set; } = 0;
        public double Errors { get; set; } = 0;
    }
}