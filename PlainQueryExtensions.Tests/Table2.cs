namespace PlainQueryExtensions.Tests
{
    public class Table2
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        [ReadOnly]
        public int ReadOnlyColumn1 { get; set; }        
    }
}