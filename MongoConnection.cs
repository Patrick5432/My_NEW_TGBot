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
            Console.WriteLine("Проверка класса");
            db = client.GetDatabase("telegram_bot");
            await db.CreateCollectionAsync("users");
            await db.CreateCollectionAsync("admins");
        }

        public async Task GetUser(string userName, string firstName, string lastName, long idUser)
        {
            
            Users user = new Users(userName: userName, firstName: firstName, lastName: lastName, idUser: idUser);
            var users = db.GetCollection<Users>("users");
            users.InsertOne(user);
        }

        public async Task GetAdmin(string userName, long userId)
        {
            Admins admin = new Admins(userName: userName, userId: userId);
            var admins = db.GetCollection<Admins>("admins");
            admins.InsertOne(admin);
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
            public string UserName { get; set; }
            public long UserId { get; set; }

            public Admins(string userName, long userId)
            {
                UserName = userName;
                UserId = userId;
            }
        }
    }
}
