namespace BookingAPI.Objects
{
    public class User
    {
        public User(string userId, string lastName, string firstName, string password, bool isAdmin)
        {
            UserId = userId;
            LastName = lastName;
            FirstName = firstName;
            Password = password;
            IsAdmin = isAdmin;
        }

        public string UserId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
    }
}
