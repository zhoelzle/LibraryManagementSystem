namespace LibraryManagementSystem.Models
{
    public class AddReviewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string UserId { get; set; } // Assuming UserId is not null in database
        public int Rating { get; set; }
        public string ReviewText { get; set; }
        public DateTime ReviewDate { get; set; } // Assuming ReviewDate is set in backend
    }
}
