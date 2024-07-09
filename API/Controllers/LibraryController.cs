using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.AccessControl;

namespace LibraryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibraryController : ControllerBase
    {
        private IConfiguration _configuration;

        public LibraryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetBooks")]
        // Make results into a table, with a radio button at the end that enables you to check out the book and click submit
        // Check in radio button (greyed out for customer, functional for librarian) make it impossible to click both check 
        // out and check in
        public JsonResult GetBooks()
        {
            string query = "SELECT a.[BookId], a.[Title], a.[Author], a.[Description], a.[CoverImage], a.[Publisher], a.[PublicationDate], a.[Category], a.[ISBN], a.[PageCount], a.[AddedDate], a.[IsCheckedOut], a.[CheckedOutDate], a.[NextAvailableDate], COUNT(b.[ReviewId]) AS ReviewCount FROM [master].[dbo].[Books] a LEFT JOIN [master].[dbo].[Reviews] b ON a.[BookId] = b.[BookId] GROUP BY a.[BookId], a.[Title], a.[Author], a.[Description], a.[CoverImage], a.[Publisher], a.[PublicationDate], a.[Category], a.[ISBN], a.[PageCount], a.[AddedDate], a.[IsCheckedOut], a.[CheckedOutDate], a.[NextAvailableDate];";
            DataTable table = new DataTable();
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");
            SqlDataReader myReader;

            try
            { 
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                { 
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                        myCon.Close();
                    }
                }

                return new JsonResult(table);
            }
            catch (SqlException ex)
            {
                return new JsonResult($"Error: {ex.Message}");
            }
        }


        [HttpGet]
        [Route("GetReviews")]
        public JsonResult GetReviews(int? BookId = null)
        {
            if (BookId == null)
            {
                return new JsonResult("Error: BookId must be provided");
            }

            string query = @"SELECT [ReviewId], [BookId], [Title], [UserId], [Rating], [ReviewText], [ReviewDate] 
                     FROM [master].[dbo].[Reviews] 
                     WHERE [BookId] = @BookId";

            DataTable table = new DataTable();
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        // Add parameter for BookId
                        myCommand.Parameters.AddWithValue("@BookId", BookId);

                        // Execute query and load results into DataTable
                        SqlDataReader myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                    }
                    myCon.Close();
                }

                return new JsonResult(table);
            }
            catch (SqlException ex)
            {
                return new JsonResult($"Error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("AddBook")] // Ensure this matches your Axios request URL
        public JsonResult AddBook(Book book)
        {
            string queryCheck = "SELECT COUNT(*) FROM dbo.Books WHERE Title = @Title";

            string queryInsert = @"INSERT INTO [master].[dbo].[Books] 
                           ([Title], [Author], [Description], [CoverImage], [Publisher], 
                            [PublicationDate], [Category], [ISBN], [PageCount]) 
                           VALUES (@Title, @Author, @Description, @CoverImage, @Publisher, 
                                   @PublicationDate, @Category, @ISBN, @PageCount)";

            DataTable table = new DataTable();
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();

                    // Check if book with the same title already exists
                    using (SqlCommand checkCommand = new SqlCommand(queryCheck, myCon))
                    {
                        checkCommand.Parameters.AddWithValue("@Title", book.Title);
                        int existingCount = (int)checkCommand.ExecuteScalar();

                        if (existingCount > 0)
                        {
                            // Handle duplicate entry case
                            return new JsonResult("Book with the same title already exists.");
                        }
                    }

                    // Insert the new book
                    using (SqlCommand insertCommand = new SqlCommand(queryInsert, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@Title", book.Title);
                        insertCommand.Parameters.AddWithValue("@Author", book.Author);
                        insertCommand.Parameters.AddWithValue("@Description", book.Description ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@CoverImage", book.CoverImage ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@Publisher", book.Publisher ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@PublicationDate", book.PublicationDate != DateTime.MinValue ? (object)book.PublicationDate : DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@Category", book.Category ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@ISBN", book.ISBN ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@PageCount", book.PageCount != 0 ? (object)book.PageCount : DBNull.Value);

                        int rowsAffected = insertCommand.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new JsonResult("Book added successfully.");
                        }
                        else
                        {
                            return new JsonResult("Failed to add book.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return new JsonResult($"Error: {ex.Message}");
            }
        }



        /*[HttpPost]
        [Route("AddReview")]
        public async Task<IActionResult> AddReview([FromBody] ReviewModel review)
        {
            if (review == null || review.UserId == 0 || string.IsNullOrEmpty(review.ReviewText) || review.Rating == 0)
            {
                return BadRequest("Please fill in all review details.");
            }

            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    await myCon.OpenAsync();

                    string query = @"INSERT INTO [dbo].[Reviews]
                                     ([BookId]
                                     ,[Title]
                                     ,[UserId]
                                     ,[Rating]
                                     ,[ReviewText]
                                     ,[ReviewDate])
                                     VALUES
                                     (@BookId
                                     ,@Title
                                     ,@UserId
                                     ,@Rating
                                     ,@ReviewText
                                     ,@ReviewDate)";

                    using (SqlCommand insertCommand = new SqlCommand(query, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@BookId", review.BookId);
                        insertCommand.Parameters.AddWithValue("@Title", review.Title); // Assuming you want to store the review title as well
                        insertCommand.Parameters.AddWithValue("@UserId", review.UserId);
                        insertCommand.Parameters.AddWithValue("@Rating", review.Rating);
                        insertCommand.Parameters.AddWithValue("@ReviewText", review.ReviewText);
                        insertCommand.Parameters.AddWithValue("@ReviewDate", DateTime.UtcNow); // Using UTC time for consistency

                        int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Ok("Review added successfully.");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to add review.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }*/

        [HttpPost]
        [Route("AddReview")] // Ensure this matches your Axios request URL
        public JsonResult AddReview(ReviewModel review)
        {
            string queryCheck = "SELECT COUNT(*) FROM [master].[dbo].[AspNetUsers] WHERE Id = @UserId";

            string queryInsert = @"INSERT INTO [dbo].[Reviews]
                                     ([BookId]
                                     ,[Title]
                                     ,[UserId]
                                     ,[Rating]
                                     ,[ReviewText]
                                     ,[ReviewDate])
                                     VALUES
                                     (@BookId
                                     ,@Title
                                     ,@UserId
                                     ,@Rating
                                     ,@ReviewText
                                     ,@ReviewDate)";

            DataTable table = new DataTable();
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();

                    // Check if book with the same title already exists
                    using (SqlCommand checkCommand = new SqlCommand(queryCheck, myCon))
                    {
                        checkCommand.Parameters.AddWithValue("@UserId", review.Id);
                        int existingCount = (int)checkCommand.ExecuteScalar();

                        if (existingCount < 1)
                        {
                            // Handle duplicate entry case
                            return new JsonResult("UserId does not exist.");
                        }
                    }

                    // Insert the new review
                    using (SqlCommand insertCommand = new SqlCommand(queryInsert, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@BookId", review.BookId);
                        insertCommand.Parameters.AddWithValue("@Title", review.Title); // Assuming you want to store the review title as well
                        insertCommand.Parameters.AddWithValue("@UserId", review.Id);
                        insertCommand.Parameters.AddWithValue("@Rating", review.Rating);
                        insertCommand.Parameters.AddWithValue("@ReviewText", review.ReviewText);
                        insertCommand.Parameters.AddWithValue("@ReviewDate", DateTime.UtcNow); // Using UTC time for consistency

                        int rowsAffected = insertCommand.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new JsonResult("Review added successfully.");
                        }
                        else
                        {
                            return new JsonResult("Failed to add review.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return new JsonResult($"Error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("CheckOutBook/{BookId}")]
        public async Task<IActionResult> CheckOutBook(int BookId)
        {
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            string queryCheckOut = @"
            UPDATE dbo.Books 
            SET IsCheckedOut = 1, 
                CheckedOutDate = GETDATE(),
                NextAvailableDate = TRY_CONVERT(date, DATEADD(day, 5, GETDATE()))
            WHERE BookId = @BookId AND IsCheckedOut = 0";

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    await myCon.OpenAsync();

                    using (SqlCommand checkOutCommand = new SqlCommand(queryCheckOut, myCon))
                    {
                        checkOutCommand.Parameters.AddWithValue("@BookId", BookId);
                        int rowsAffected = await checkOutCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Book checked out successfully.");
                        }
                        else
                        {
                            return BadRequest("Book is already checked out or does not exist.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("CheckInBook/{BookId}")]
        public async Task<IActionResult> CheckInBook(int BookId)
        {
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            string queryCheckIn = @"
            UPDATE dbo.Books 
            SET IsCheckedOut = 0, 
                CheckedOutDate = NULL,
                NextAvailableDate = NULL
            WHERE BookId = @BookId AND IsCheckedOut = 1";

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    await myCon.OpenAsync();

                    using (SqlCommand checkInCommand = new SqlCommand(queryCheckIn, myCon))
                    {
                        checkInCommand.Parameters.AddWithValue("@BookId", BookId);
                        int rowsAffected = await checkInCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Book checked in successfully.");
                        }
                        else
                        {
                            return BadRequest("Book is not checked out or does not exist.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut]
        [Route("UpdateBooks")]
        public JsonResult UpdateBooks(Book book)
        {
            if (book.BookId == 0)
            {
                return new JsonResult("Error: BookId must be provided");
            }

            string query = @"UPDATE [master].[dbo].[Books] 
                     SET [Title] = @Title, 
                         [Author] = @Author, 
                         [Description] = @Description, 
                         [CoverImage] = @CoverImage, 
                         [Publisher] = @Publisher, 
                         [PublicationDate] = @PublicationDate, 
                         [Category] = @Category, 
                         [ISBN] = @ISBN, 
                         [PageCount] = @PageCount, 
                         [IsCheckedOut] = @IsCheckedOut                    
                     WHERE [BookId] = @BookId";

            DataTable table = new DataTable();
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");
            SqlDataReader myReader;

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@BookId", book.BookId);
                        myCommand.Parameters.AddWithValue("@Title", book.Title);
                        myCommand.Parameters.AddWithValue("@Author", book.Author);
                        myCommand.Parameters.AddWithValue("@Description", book.Description ?? (object)DBNull.Value);
                        myCommand.Parameters.AddWithValue("@CoverImage", book.CoverImage ?? (object)DBNull.Value);
                        myCommand.Parameters.AddWithValue("@Publisher", book.Publisher ?? (object)DBNull.Value);
                        myCommand.Parameters.AddWithValue("@PublicationDate", book.PublicationDate != DateTime.MinValue ? (object)book.PublicationDate : DBNull.Value);
                        myCommand.Parameters.AddWithValue("@Category", book.Category ?? (object)DBNull.Value);
                        myCommand.Parameters.AddWithValue("@ISBN", book.ISBN ?? (object)DBNull.Value);
                        myCommand.Parameters.AddWithValue("@PageCount", book.PageCount != 0 ? (object)book.PageCount : DBNull.Value);
                        myCommand.Parameters.AddWithValue("@IsCheckedOut", book.IsCheckedOut);
                        myCommand.Parameters.AddWithValue("@ReviewCount", book.ReviewCount);

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                        myCon.Close();
                    }
                }
                return new JsonResult("Updated Successfully");
            }
            catch (SqlException ex)
            {
                return new JsonResult($"Error: {ex.Message}");
            }
        }

        [HttpPut]
        [Route("UpdateReview/{ReviewId}")]
        public IActionResult UpdateReview(int ReviewId, [FromBody] ReviewModel review)
        {
            if (review == null)
            {
                return BadRequest("Review object is null");
            }

            // SQL query to update review in the database
            string query = @"
                UPDATE [dbo].[Reviews] 
                SET 
                    [BookId] = @BookId,
                    [Title] = @Title,
                    [UserId] = @UserId,
                    [Rating] = @Rating,
                    [ReviewText] = @ReviewText,
                    [ReviewDate] = @ReviewDate
                WHERE [ReviewId] = @ReviewId";

            try
            {
                using (SqlConnection myCon = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        // Add parameters
                        myCommand.Parameters.AddWithValue("@ReviewId", ReviewId);
                        myCommand.Parameters.AddWithValue("@BookId", review.BookId);
                        myCommand.Parameters.AddWithValue("@Title", review.Title);
                        myCommand.Parameters.AddWithValue("@UserId", 1); // Replace with actual user ID
                        myCommand.Parameters.AddWithValue("@Rating", review.Rating);
                        myCommand.Parameters.AddWithValue("@ReviewText", review.ReviewText);
                        myCommand.Parameters.AddWithValue("@ReviewDate", DateTime.Now.Date); // Today's date

                        myCon.Open();
                        int rowsAffected = myCommand.ExecuteNonQuery();
                        myCon.Close();

                        if (rowsAffected > 0)
                        {
                            return Ok("Review updated successfully");
                        }
                        else
                        {
                            return BadRequest("Failed to update review");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }



        [HttpDelete]
        [Route("DeleteBooks/{BookId}")] // Changed {id} to {BookId}
        public async Task<IActionResult> DeleteBooks(int BookId)
        {
            string queryDelete = "DELETE FROM dbo.Books WHERE BookId = @BookId";
            string sqlDatasource = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    await myCon.OpenAsync();

                    using (SqlCommand deleteCommand = new SqlCommand(queryDelete, myCon))
                    {
                        deleteCommand.Parameters.AddWithValue("@BookId", BookId);
                        int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Book deleted successfully.");
                        }
                        else
                        {
                            return NotFound("Book not found.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete]
        [Route("DeleteReview/{ReviewId}")]
        public IActionResult DeleteReview(int ReviewId)
        {
            // SQL query to delete review from the database
            string query = @"DELETE FROM [dbo].[Reviews] WHERE [ReviewId] = @ReviewId";

            try
            {
                using (SqlConnection myCon = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        // Add parameter
                        myCommand.Parameters.AddWithValue("@ReviewId", ReviewId);

                        myCon.Open();
                        int rowsAffected = myCommand.ExecuteNonQuery();
                        myCon.Close();

                        if (rowsAffected > 0)
                        {
                            return Ok("Review deleted successfully");
                        }
                        else
                        {
                            return BadRequest("Failed to delete review");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }

    

    //Will the below need to evenutually go in it's own model file?
    public class Book
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

    // Model class for Review
    public class ReviewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Id { get; set; } // Assuming UserId is not null in database
        public int Rating { get; set; }
        public string ReviewText { get; set; }
        public DateTime ReviewDate { get; set; } // Assuming ReviewDate is set in backend
    }
}
