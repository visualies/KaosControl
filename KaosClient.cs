using Dapper;
using KaosControl.Entities;
using MySql.Data.MySqlClient;
using PermissionRanks.Exceptions;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace KaosControl
{
    public class KaosClient
    {
        public string ConnectionString { get; set; }

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
                    throw new EntityNotFoundException("User could not be found in database");
                }

                user.Client = this;

                return user;
            }
        }
        public async Task RankUpAsync(KaosUser user)
        {
            try
            {
                var rank = await GetRankAsync(user.Rank + 1);
                await user.AddPointsAsync(rank.PointsReward);
                await user.SetRankAsync(rank.Id);

            }
            catch (EntityNotFoundException)
            {
                return;
            }
        }
        public async Task SetRankAsync(KaosUser user, int id)
        {
            var sql = $"UPDATE players SET Rank = {id} WHERE SteamId = {user.SteamId}";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }
        }
        public async Task AddPointsAsync(KaosUser user, int amount)
        {
            var sql = $"UPDATE arkshopplayers SET Points = Points + {amount} WHERE SteamId = {user.SteamId}";

            using (IDbConnection connection = new MySqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql);
            }
        }
        public async Task AddExperienceAsync(KaosUser user, int amount)
        {
            var xp = (int)(user.Experience + amount);
            var rank = await GetRankAsync(user.Rank + 1);

            if (xp < rank.RequiredExperience)
            {
                await SetExperienceAsync(user, xp);
                return;
            }
            if (xp >= rank.RequiredExperience)
            {
                await SetExperienceAsync(user, xp - rank.RequiredExperience);
                await RankUpAsync(user);
                await AddExperienceAsync(user, 0);
                return;
            }
        }

        private async Task SetExperienceAsync(KaosUser user, int amount)
        {  
            var sql = $"UPDATE players SET Experience = {amount} WHERE SteamId = {user.SteamId}";

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
