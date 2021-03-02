using Dapper;
using KaosControl.Entities;
using KaosControl.Events;
using KaosControl.Exceptions;
using KaosControl.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace KaosControl
{
    public class KaosClient
    {
        public string ConnectionString { get; set; }
        private KaosClientConfiguration Config;

        public KaosClient(KaosClientConfiguration kaosClientConfiguration)
        {
            Config = kaosClientConfiguration;
        }
        public KaosClient()
        {

        }

        public delegate void UserRankedUpEventHandler(object source, UserRankUpEventArgs args);
        public event UserRankedUpEventHandler UserRankedUp;
        protected virtual void OnUserRankUp(KaosUser user, KaosPlayerRank rank)
        {
            if (UserRankedUp == null) return;
            UserRankedUp(this, new UserRankUpEventArgs()
            {
                Rank = rank,
                User = user
            });
        }

        #region Public
        public void AddConnection(string host, string user, string password, string database)
        {
            ConnectionString = $"Server={host}; Database={database}; User Id={user}; Password={password};";
        }
        public void AddConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public void ReloadConfig(KaosClientConfiguration config)
        {
            Config = config;
        }
        public async Task<KaosUser> GetUserAsync(long steamId)
        {
            var sql = $"SELECT * FROM players WHERE SteamId = '{steamId}'";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var user = await connection.QueryFirstOrDefaultAsync<KaosUser>(sql);
                if (user == null)
                {
                    throw new EntityNotFoundException("[KaosUser] returned null from database");
                }

                user.Client = this;
                return user;
            }
        }
        public async Task<KaosUser> GetUserAsync(ulong discordId)
        {
            var sql = $"SELECT * FROM discordaddonplayers WHERE discid = @discid";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var discordAddonPlayer = await connection.QueryFirstOrDefaultAsync<DiscordAddonPlayer>(sql, new { discid = discordId });
                if (discordAddonPlayer == null)
                {
                    throw new EntityNotFoundException("User with specified Discord ID not found in database: [DiscordAddonPlayers]");
                }
                try
                {
                    return await GetUserAsync(discordAddonPlayer.SteamId);
                }
                catch (EntityNotFoundException)
                {
                    throw new EntityNotFoundException("User could not be found");
                }
            }
        }
        public async Task<List<KaosUser>> GetAllUsersAsync()
        {
            var sql = $"SELECT * FROM players";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var users = await connection.QueryAsync<KaosUser>(sql);

                foreach (KaosUser user in users)
                {
                    user.Client = this;

                }

                return users.ToList();
            }
        }
        public async Task<KaosPlayerRank> GetRankAsync(int id)
        {
            var rank = Config.PlayerRanks.FirstOrDefault(a => a.Id == id);
            if (rank == null)
            {
                throw new EntityNotFoundException("Rank could not be found in Config");
            }
            await Task.CompletedTask;
            return rank;
        }
        public async Task RemoveExpiredExperienceMultipliersAsync()
        {
            var sql = $"DELETE FROM experienceboosts WHERE ExpiryDate < @Date";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { Date = DateTime.Now });
            }
        }
        #endregion

        #region KaosUser
        internal async Task RankUpAsync(KaosUser user)
        {
            try
            {
                var oldRank = await GetRankAsync(user.Rank);
                var newRank = await GetRankAsync(user.Rank + 1);

                await SetRankAsync(user, newRank);
                await RemovePermissionGroupAsync(user, oldRank.PermissionGroup);
                await AddPermissionGroupAsync(user, newRank.PermissionGroup);

                OnUserRankUp(user, newRank);
            }
            catch (EntityNotFoundException)
            {
                return;
            }
        }
        internal async Task DeRankAsync(KaosUser user)
        {
            try
            {
                var oldRank = await GetRankAsync(user.Rank);
                var newRank = await GetRankAsync(user.Rank - 1);

                await SetRankAsync(user, newRank);
                await RemovePermissionGroupAsync(user, oldRank.PermissionGroup);
                await AddPermissionGroupAsync(user, newRank.PermissionGroup);
            }
            catch (EntityNotFoundException)
            {
                return;
            }
        }
        internal async Task SetRankAsync(KaosUser user, KaosPlayerRank rank)
        {
            var oldRank = await GetRankAsync(user.Rank);
            await RemovePermissionGroupAsync(user, oldRank.PermissionGroup);
            await AddPermissionGroupAsync(user, rank.PermissionGroup);
            await SetExperienceAsync(user, 0);

            var sql = $"UPDATE players SET Rank = @Rank WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { Rank = rank.Id, SteamId = user.SteamId });
            }
        }

        internal async Task AddPointsAsync(KaosUser user, int amount)
        {
            var sql = $"UPDATE arkshopplayers SET Points = Points + @Amount WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new {Amount = amount, SteamId = user.SteamId });
            }
        }
        internal async Task RemovePointsAsync(KaosUser user, int amount)
        {
            var points = await GetPointsAsync(user);

            if (points < amount)
            {
                await SetPointsAsync(user, 0);
            }
            else
            {
                await SetPointsAsync(user, points - amount);
            }
        }
        internal async Task<int> GetPointsAsync(KaosUser user)
        {
            var sql = $"SELECT Points FROM arkshopplayers WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var points = await connection.QueryFirstOrDefaultAsync<int>(sql, new {SteamId = user.SteamId});
                return points;
            }
        }

        internal async Task AddExperienceAsync(KaosUser user, int amount)
        {
            var multiplier = await GetExperienceMultiplierAsync(user);

            var newXp = (int)(user.Experience + (amount * multiplier));
            try
            {
                var newRank = await GetRankAsync(user.Rank + 1);

                if (newXp < newRank.RequiredExperience)
                {
                    await SetExperienceAsync(user, newXp);
                    return;
                }
                if (newXp >= newRank.RequiredExperience)
                {
                    await SetExperienceAsync(user, newXp - newRank.RequiredExperience);
                    await RankUpAsync(user);
                    await AddExperienceAsync(user, 0);
                    return;
                }
            }
            catch (EntityNotFoundException)
            {
                return;
            }
        }
        internal async Task RemoveExperienceAsync(KaosUser user, int amount)
        {
            if (user.Experience < amount)
            {
                await SetExperienceAsync(user, 0);
            }
            else
            {
                await SetExperienceAsync(user, user.Experience - amount);
            }
        }

        internal async Task AddExperienceMultiplierAsync(KaosUser user, double multiplier, string type, int duration)
        {

            var date = DateTime.Now.AddMinutes(duration);
            var query = $"SELECT * FROM experienceboosts WHERE SteamId = @SteamId AND Type = @Type";
            var insert = $"INSERT INTO experienceboosts (SteamId, Multiplier, Type, ExpiryDate) Values (@SteamId, @Multiplier, @Type, @ExpiryDate)";
            var update = $"UPDATE experienceboosts SET ExpiryDate = @ExpiryDate, Multiplier = @Multiplier WHERE SteamId = @SteamId AND Type = @Type";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var boost = await connection.QueryFirstOrDefaultAsync(query, new { SteamId = user.SteamId, Type = type });
                if (boost != null)
                {
                    //If boost type already present, refresh expiry date
                    await connection.ExecuteAsync(update, new { ExpiryDate = date, Multiplier = multiplier, SteamId = user.SteamId, Type = type });
                    return;
                }
                else
                {
                    await connection.ExecuteAsync(insert, new { SteamId = user.SteamId, Multiplier = multiplier, Type = type, ExpiryDate = date });
                }
            }
        }
        internal async Task RemoveExperienceMultiplierAsync(KaosUser user, string type)
        {
            var sql = $"DELETE FROM experienceboosts WHERE SteamId = @SteamId AND Type = @Type";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var query = await connection.QueryAsync<ExperienceBoost>(sql, new {SteamId = user.SteamId, Type = type });
            }
        }
        internal async Task<double> GetExperienceMultiplierAsync(KaosUser user)
        {
            double multiplier = 1;

            var sql = $"SELECT * FROM experienceboosts WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var query = await connection.QueryAsync<ExperienceBoost>(sql, new {SteamId = user.SteamId });


                foreach (ExperienceBoost boost in query)
                {
                    multiplier = multiplier * boost.Multiplier;
                }
            }

            return multiplier;
        }

        internal async Task AddPermissionGroupAsync(KaosUser user, string group)
        {
            var perms = await GetPermissionGroupsAsync(user);
            if (perms.Contains(group)) return;
            if (string.IsNullOrWhiteSpace(group)) return;

            perms.Add(group);
            await SetPermissionGroupsAsync(user, perms);
        }
        internal async Task RemovePermissionGroupAsync(KaosUser user, string group)
        {
            var perms = await GetPermissionGroupsAsync(user);
            if (perms.Contains(group) == false) return;

            perms.Remove(group);
            await SetPermissionGroupsAsync(user, perms);
        }
        internal async Task<List<string>> GetPermissionGroupsAsync(KaosUser user)
        {
            var sql = $"SELECT PermissionGroups FROM players WHERE SteamId = @SteamID";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var query = await connection.QueryFirstOrDefaultAsync<string>(sql, new { SteamId = user.SteamId });
                var perms = query.Split(',').Where(s => !string.IsNullOrWhiteSpace(s));
                return perms.ToList();
            }

        }

        internal async Task<List<KaosTribe>> GetTribesAsync(KaosUser user)
        {
            var tribes = new List<KaosTribe>();
            var sql = $"SELECT TribeList FROM pvpve_players WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var tribeString = await connection.QueryFirstOrDefaultAsync<string>(sql, new { SteamId = user.SteamId });
                if (tribeString == null)
                {
                    return tribes;
                }

                var tribeIdList = tribeString.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                foreach (string tribeId in tribeIdList)
                {
                    var sql2 = $"SELECT * FROM pvpve_tribes WHERE ID = @Id";
                    var tribe = await connection.QueryFirstOrDefaultAsync<KaosTribe>(sql2, new { Id = tribeId });
                    tribe.Client = this;
                    tribes.Add(tribe);
                }

                return tribes;
            }
        }
        internal async Task<ulong> GetDiscordIdAsync(KaosUser user)
        {
            var sql = $"SELECT * FROM discordaddonplayers WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var discordAddonPlayer = await connection.QueryFirstOrDefaultAsync<DiscordAddonPlayer>(sql, new { SteamId = user.SteamId });
                if (discordAddonPlayer == null)
                {
                    throw new EntityNotFoundException("User with specified Steam ID not found in database");
                }
                if (discordAddonPlayer.discid == null)
                {
                    throw new UserNotVerifiedException("User with specified Steam ID not verified with Discord");
                }
                else
                {
                    return (ulong)discordAddonPlayer.discid;
                }
            }
        }
        internal async Task<int> GetMaxTribeSizeAsync(KaosUser user)
        {
            var sizes = new List<int>();
            var tribes = await GetTribesAsync(user);

            foreach (KaosTribe tribe in tribes)
            {
                var size = await tribe.GetTribeSizeAsync();
                sizes.Add(size);
            }

            return sizes.Max();
        }
        #endregion

        #region KaosTribe
        internal async Task<List<KaosUser>> GetMembersAsync(KaosTribe tribe)
        {
            var members = new List<KaosUser>();
            var sql = $"SELECT SteamId FROM pvpve_playerss WHERE TribeList LIKE @Id";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var steamIds = await connection.QueryAsync<long>(sql, new { Id = $"%{tribe.Id}" });

                foreach (long steamId in steamIds)
                {
                    var user = await GetUserAsync(steamId);
                    members.Add(user);
                }
            }

            return members;
        }
        internal async Task<int> GetTribeSizeAsync(KaosTribe tribe)
        {
            var sql = $"SELECT COUNT(SteamId) FROM pvpve_players WHERE TribeList LIKE @Id";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var size = await connection.QueryFirstOrDefaultAsync<int>(sql, new { Id = $"%{tribe.Id}" });
                return size;
            }
        }
        internal async Task AddBubbleExperienceAsync(KaosTribe tribe, double amount)
        {
            await Task.CompletedTask;
        }
        #endregion

        #region Private
        private async Task SetPointsAsync(KaosUser user, int amount)
        {
            var sql = $"UPDATE arkshopplayers SET Points = @Amount WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { Amount = amount, SteamId = user.SteamId} );
            }
        }
        private async Task SetPermissionGroupsAsync(KaosUser user, List<string> groups)
        {
            var perms = $"{string.Join(',', groups)},";

            var sql = $"UPDATE players SET PermissionGroups = @Groups WHERE SteamId = @SteamID";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { Groups = perms, SteamId = user.SteamId });
            }
        }
        private async Task SetExperienceAsync(KaosUser user, int value)
        {
            var sql = $"UPDATE players SET Experience = @Value WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new {Value = value, SteamId = user.SteamId });
            }
        }
        private async Task SetBubbleExperienceAsync(KaosTribe tribe, int value)
        {
            var sql = $"UPDATE pvpve_tribes SET PveBubble = {value} WHERE TribeID = {tribe.TribeId}";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }
        }
        #endregion
    }
}
