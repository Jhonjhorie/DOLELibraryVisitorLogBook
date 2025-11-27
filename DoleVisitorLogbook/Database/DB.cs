using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoleVisitorLogbook.Database
{
    public class DB
    {
        public static MySqlConnection GetConnection()
        {
            string connStr = "Server=localhost;Port=3307;Database=dole_logbook;Uid=root;Pwd=Information@1;";
            return new MySqlConnection(connStr);
        }
    }
}
