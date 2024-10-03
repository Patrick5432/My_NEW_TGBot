using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewTGBot
{
    internal class MongoConnection
    {
        private static MongoClient client = new MongoClient("mongodb://localhost:27017");
        private static IMongoDatabase? db;
        public async Task MongoUpdate()
        {
            Console.WriteLine("База данных работает");
            db = client.GetDatabase("telegram_bot");
            await db.CreateCollectionAsync("users");
            await db.CreateCollectionAsync("admins");
            await db.CreateCollectionAsync("raffles");
            await db.CreateCollectionAsync("users_in_raffle");
        }

        public async Task GetUser(string userName, string firstName, string lastName, long idUser)
        {

            Users user = new Users(userName: userName, firstName: firstName, lastName: lastName, idUser: idUser);
            var users = db.GetCollection<Users>("users");
            users.InsertOne(user);
        }

        public async Task GetAdmin(long userId)
        {
            Admins admin = new Admins(userId: userId);
            var admins = db.GetCollection<Admins>("admins");
            admins.InsertOne(admin);
        }

        public async Task<bool> CheckListAdmin(long userId)
        {
            //Console.WriteLine(userId);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Admins>("admins");
            using var cursor = await collection.FindAsync(new BsonDocument());
            List<Admins> users = await cursor.ToListAsync();
            foreach (var user in users)
            {
                if (userId == user.UserId)
                {
                    return true;
                }
            }
            return false;

        }

        public async Task<bool> CheckAdmins()
        {
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<BsonDocument>("admins");
            using var cursor = await collection.FindAsync(new BsonDocument());
            List<BsonDocument> users = cursor.ToList();
            foreach (var user in users)
            {
                if (user == null)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public async Task GetRaffle(string status, string name, string description, string userRaffle)
        {
            Raffle raffle = new Raffle(status: status, name: name, description: description, userRaffle: userRaffle);
            var raffles = db.GetCollection<Raffle>("raffles");
            raffles.InsertOne(raffle);
        }

        public class Users
        {
            public ObjectId Id { get; set; }
            public string UserName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public long IdUser { get; set; }

            public Users(string userName, string firstName, string lastName, long idUser)
            {
                UserName = userName;
                FirstName = firstName;
                LastName = lastName;
                IdUser = idUser;
            }
        }

        public class Admins
        {
            public ObjectId Id { get; set; }
            public long UserId { get; set; }

            public Admins(long userId)
            {
                UserId = userId;
            }
        }

        public class Raffle
        {
            public ObjectId Id { get; set; }
            public string Status { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string UsersInRaffle { get; set; }

            public Raffle(string status, string name, string description, string userRaffle)
            {
                Status = status;
                Name = name;
                Description = description;
                UsersInRaffle = userRaffle;
            }
        }


        public class UsersInRaffle
        {
            public ObjectId Id { get; set; }
            public int RaffleId { get; set; }
            public int UserId { get; set; }

            public UsersInRaffle(int raffleId, int userId)
            {
                RaffleId = raffleId;
                UserId = userId;
            }
        }
    }
}
