namespace Supercell.Laser.Server.Database
{
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Club;
    using Supercell.Laser.Server.Database.Cache;
    using Supercell.Laser.Server.Settings;
    using Supercell.Laser.Logic.Util;

    public static class Alliances
    {
        private static long AllianceIdCounter;
        private static string ConnectionString;

        public static void Init()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = Configuration.Instance.MysqlHost;
            builder.UserID = Configuration.Instance.MysqlUsername;
            builder.Password = Configuration.Instance.MysqlPassword;
            builder.SslMode = MySqlSslMode.Disabled;
            builder.Database = Configuration.Instance.MysqlDatabase;
            builder.CharacterSet = "utf8mb4";

            JsonConvert.DefaultSettings = () =>
                new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

            ConnectionString = builder.ToString();

            AllianceCache.Init();

            AllianceIdCounter = GetMaxAllianceId();
        }
        public static List<Alliance> GetRandomAlliances(ClientAvatar avatar, int maxCount)
        {
            int found = 0;

            Random r = new Random();
            List<Alliance> list = new();
            while (found < maxCount)
            {
                try
                {
                    Alliance alliance = Load(r.NextInt64(1, AllianceIdCounter + 1));
                    if (alliance == null)
                        continue;
                    if (alliance.RequiredTrophies > avatar.Trophies)
                        continue;
                    if (alliance.Members.Count >= 100)
                        continue;
                    if (alliance.Type == 2)
                        continue;
                    found++;
                    list.Add(alliance);
                    if (found == maxCount)
                        break;
                }
                catch (Exception)
                {
                    // Logger.Error("ff Error" + e.ToString());
                }
            }
            return list;
        }

        public static long GetMaxAllianceId()
        {
            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand(
                "SELECT coalesce(MAX(Id), 0) FROM alliances",
                Connection
            );

            long result = Convert.ToInt64(command.ExecuteScalar());
            Connection.Close();
            return result;
        }

        public static void Create(Alliance alliance)
        {
            if (alliance == null)
                return;
            alliance.Id = ++AllianceIdCounter;
            string json = JsonConvert.SerializeObject(alliance);

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand(
                $"INSERT INTO alliances (`Id`, `Name`, `Trophies`, `Data`) VALUES ({(long)alliance.Id}, @name, {alliance.Trophies}, @data)",
                Connection
            );
            command.Parameters?.AddWithValue("@data", json);
            command.Parameters?.AddWithValue("@name", alliance.Name);
            command.ExecuteNonQuery();
            Connection.Close();

            AllianceCache.Cache(alliance);
        }

        public static void Delete(long id)
        {
            MySqlConnection Connection = new(ConnectionString);
            Connection.Open();
            MySqlCommand command = new($"DELETE FROM alliances WHERE id = @id", Connection);
            command.Parameters?.AddWithValue("@id", id);
            command.ExecuteNonQuery();
            Connection.Close();
            AllianceCache.RemoveAlliance(id);
        }

        public static void Save(Alliance alliance)
        {
            if (alliance == null)
                return;

            string json = JsonConvert.SerializeObject(alliance);

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand(
                $"UPDATE alliances SET `Trophies`='{alliance.Trophies}', `Data`=@data WHERE Id = '{(long)alliance.Id}'",
                Connection
            );
            command.Parameters?.AddWithValue("@data", json);
            command.ExecuteNonQuery();
            Connection.Close();
        }

        public static Alliance Load(long id)
        {
            if (AllianceCache.IsAllianceCached(id))
            {
                return AllianceCache.GetAlliance(id);
            }

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand(
                $"SELECT * FROM alliances WHERE Id = '{id}'",
                Connection
            );
            MySqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                Alliance alliance = JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]);
                AllianceCache.Cache(alliance);
                Connection.Close();
                return alliance;
            }
            Connection.Close();
            return null;
        }

        public static List<Alliance> GetRankingList()
        {
            #region GetGlobal

            var list = new List<Alliance>();

            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand($"SELECT * FROM alliances ORDER BY `Trophies` DESC LIMIT 200",
                        connection))
                    {
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                            list.Add(JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]));
                    }

                    connection.Close();
                }

                return list;
            }
            catch (Exception)
            {
                return list;
            }

            #endregion
        }

        public static List<Alliance> GetRandomAlliances(int maxCount)
        {
            long count = Math.Min(maxCount, AllianceIdCounter);

            #region GetGlobal

            var list = new List<Alliance>();

            try
            {
                Random rand = new Random();
                for (int i = 0; i < count; i++)
                {
                    var alliance = Load(rand.NextInt64(1, AllianceIdCounter + 1));
                    if (alliance != null)
                        list.Add(alliance);
                }

                return list;
            }
            catch (Exception)
            {
                return list;
            }
            #endregion
        }
        public static List<Alliance> GetRankingList(string searchValue, bool isHashtagSearch = false)
        {
            var list = new List<Alliance>();

            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();

                    string query = isHashtagSearch
                        ? "SELECT * FROM alliances WHERE `Id` = @searchValue ORDER BY `Trophies` DESC LIMIT 200"
                        : "SELECT * FROM alliances WHERE `Name` LIKE @searchValue ORDER BY `Trophies` DESC LIMIT 200";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        if (isHashtagSearch)
                        {
                            long id = LogicLongCodeGenerator.ToId(searchValue);
                            cmd.Parameters.AddWithValue("@searchValue", id);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@searchValue", "%" + searchValue + "%");
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return list;
            }
            return list;
        }
    }
}
