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

        public delegate void UserRankedUpEventHandler(object source, UserRankUpEventArgs args);
        public event UserRankedUpEventHandler UserRankedUp;
        protected virtual void OnUserRankUp(KaosUser user, KaosRank rank)
        {
            if (UserRankedUp == null) return;
            UserRankedUp(this, new UserRankUpEventArgs()
            {
                Rank = rank,
                User = user
            });
        }

        public async Task AddConnection(string host, string user, string password, string database)
        {
            ConnectionString = $"Server={host}; Database={database}; User Id={user}; Password={password};";
            await Task.CompletedTask;
        }
        public async Task AddConnection(string connectionString)
        {
            ConnectionString = connectionString;
            await Task.CompletedTask;
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
        public async Task RemoveExpiredExperienceMultipliersAsync()
        {
            var sql = $"DELETE FROM experienceboosts WHERE ExpiryDate < @Date";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { Date = DateTime.Now });
            }
        }

        internal async Task RankUpAsync(KaosUser user)
        {
            try
            {
                var rank = await GetRankAsync(user.Rank + 1);
                await user.SetRankAsync(rank.Id);
                OnUserRankUp(user, rank);
            }
            catch (EntityNotFoundException)
            {
                return;
            }
        }
        internal async Task SetRankAsync(KaosUser user, int id)
        {
            var sql = $"UPDATE players SET Rank = @Rank WHERE SteamId = @SteamId";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, new { Rank = id, SteamId = user.SteamId });
            }
        }
        internal async Task AddPointsAsync(KaosUser user, int amount)
        {
            var sql = $"UPDATE arkshopplayers SET Points = Points + {amount} WHERE SteamId = {user.SteamId}";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql);
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
        internal async Task<List<KaosUser>> GetMembersAsync(KaosTribe tribe)
        {

        }
        internal async Task<List<KaosTribe>> GetTribesAsync(KaosUser user)
        {
            
        }
        internal async Task<int> GetTribeSizeAsync(KaosTribe user)
        {
            
        }

        internal async Task<int> GetMaxTribeSizeAsync(KaosUser user)
        {
            
        }

        internal async Task AddBubbleExperienceAsync(KaosTribe tribe, double amount)
        {

        }

        private async Task<double> GetExperienceMultiplierAsync(KaosUser user)
        {
            double multiplier = 1;

            var sql = $"SELECT * FROM experienceboosts WHERE SteamId = '{user.SteamId}'";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var query = await connection.QueryAsync<ExperienceBoost>(sql);


                foreach (ExperienceBoost boost in query)
                {
                    multiplier = multiplier * boost.Multiplier;
                }
            }

            return multiplier;
        }
        private async Task SetExperienceAsync(KaosUser user, int value)
        {
            var sql = $"UPDATE players SET Experience = {value} WHERE SteamId = {user.SteamId}";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql);
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
        private async Task<KaosRank> GetRankAsync(int id)
        {
            var sql = $"SELECT * FROM ranks WHERE Id = '{id}'";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                var rank = await connection.QueryFirstOrDefaultAsync<KaosRank>(sql);

                if (rank == null)
                {
                    throw new EntityNotFoundException("Rank could not be found in database");
                }

                return rank;
            }
        }
    }
}
