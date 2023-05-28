namespace BookingAPI.Model
{
    public class Booking
    {
        public Booking(string id, string userId, string title, DateTime startDate, DateTime endDate,bool allDay, int roomId, string description)
        {
            Id = id;
            UserId = userId;
            Title = title;
            StartDate = startDate;
            EndDate = endDate;
            AllDay = allDay;
            RoomId = roomId;
            Description = description;

        }

        public string Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AllDay { get; set; }
        public int RoomId { get; set; }
        public string Description { get; set; }

    }
}
