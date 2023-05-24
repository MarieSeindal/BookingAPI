namespace BookingAPI.Objects
{
    public class Person
    {
        public Person(int id, string lName, string fName)
        {
            Id = id;
            LName = lName;
            FName = fName;
        }

        public int Id { get; set; }
        public string LName { get; set; }
        public string FName { get; set; }
    }
}
