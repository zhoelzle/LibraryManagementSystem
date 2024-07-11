namespace LibraryManagementSystem.Models
{
    public class BookModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }
        public string Publisher { get; set; }
        public DateTime PublicationDate { get; set; }
        public string Category { get; set; }
        public string ISBN { get; set; }
        public int PageCount { get; set; }
        public DateTime AddedDate { get; set; }
        public bool IsCheckedOut { get; set; }
        public int ReviewCount { get; set; }
    }
}
