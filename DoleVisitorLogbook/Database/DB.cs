using MySql.Data.MySqlClient;
using System.Configuration;

namespace DoleVisitorLogbook.Database
{
    public class DB
    {
        public static MySqlConnection GetConnection()
        {
            string connStr = ConfigurationManager
                .ConnectionStrings["DoleDB"]
                .ConnectionString;

            return new MySqlConnection(connStr);
        }
    }
}
