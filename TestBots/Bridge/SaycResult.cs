namespace TestBots
{
    public class SaycResult
    {
        public SaycResult(bool passed, int suggested)
        {
            this.passed = passed;
            this.suggested = suggested;
        }

        public bool passed { get; set; }
        public int suggested { get; set; }
    }
}