namespace VerwijderWKUsers.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Naam { get; set; }
        public string Teamnaam { get; set; }
        public string Email { get; set; }
        public bool Actief { get; set; }
        public string Taal { get; set; }
        public bool Nieuwsbrief { get; set; }
        public byte WebsiteId { get; set; }
        public bool Reminder { get; set; }

        public virtual EmailLoginToken EmailLoginToken { get; set; }
    }
}
