namespace LibraryManagementSystem.Models
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        //add in role, going to need add role in controller, re-watch video
    }

}
