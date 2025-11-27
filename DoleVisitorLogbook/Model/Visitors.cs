using System; 

namespace DoleVisitorLogbook.Model
{
    public class Visitor
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Gender { get; set; }
        public string? ClientType { get; set; }
        public string? Office { get; set; }
        public string? Purpose { get; set; }
        public required string TimeIn { get; set; }        
        public string? TimeOut { get; set; }            
    }
}
