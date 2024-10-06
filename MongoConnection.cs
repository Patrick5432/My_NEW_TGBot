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
            await SetIdRaffle();
            db = client.GetDatabase("telegram_bot");
            await db.CreateCollectionAsync("users");
            await db.CreateCollectionAsync("admins");
            await db.CreateCollectionAsync("raffles");
            await db.CreateCollectionAsync("users_in_raffle");
        }
        //======================АДМИН======================
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

        public async Task DeleteAdmin(long userId)
        {
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<BsonDocument>("admins");

            var filter = Builders<BsonDocument>.Filter.Eq("UserId", userId);
            var result = await collection.DeleteOneAsync(filter);

            Console.WriteLine($"Удалён админ с Id {userId}");
        }

        public async Task<string> ToListAdmins()
        {
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Admins>("admins");
            List<Admins> admins = await collection.Find(new BsonDocument()).ToListAsync();

            string listAdmins = "";
            foreach (var admin in admins)
            {
                string strAdming = admin.UserId.ToString();
                listAdmins = listAdmins + strAdming + "\n";
            }
            return listAdmins;
        }

        public async Task<bool> CheckAdmins()
        {
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Admins>("admins");
            using var cursor = await collection.FindAsync(new BsonDocument());
            List<Admins> users = cursor.ToList();
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

        public class Admins
        {
            public ObjectId Id { get; set; }
            public long UserId { get; set; }

            public Admins(long userId)
            {
                UserId = userId;
            }
        }

        //======================КОНЕЦ-АДМИН======================


        //======================Розыгрыши======================

        public async Task GetRaffle(long id, string status, string name, string description)
        {
            Raffle raffle = new Raffle(id: id, status: status, name: name, description: description);
            var raffles = db.GetCollection<Raffle>("raffles");
            raffles.InsertOne(raffle);
        }

        public async Task<long> SetIdRaffle()
        {
            db = client.GetDatabase("telegram_bot");
            var collections = db.GetCollection<Raffle>("raffles");
            long count = await collections.CountDocumentsAsync(new BsonDocument());
            if (count < 1)
            {
                return 0;
            }
            else if (count == 1)
            {
                return 1;
            }
            else
            {
                long id = count;
                return id;
            }
            
        }

        public class Raffle
        {
            public long Id { get; set; }
            public string Status { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

            public Raffle(long id, string status, string name, string description)
            {
                Id = id;
                Status = status;
                Name = name;
                Description = description;
            }
        }










        /*public async Task GetUser(string userName, string firstName, string lastName, long idUser)
        {

            Users user = new Users(userName: userName, firstName: firstName, lastName: lastName, idUser: idUser);
            var users = db.GetCollection<Users>("users");
            users.InsertOne(user);
        }*/

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
