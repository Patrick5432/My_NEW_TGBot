﻿using Amazon.Runtime.Internal.Auth;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

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

        public async Task<List<string>> GetArrayIdRaffles()
        {
            List<string> stringId = new List<string>();
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            List<Raffle> raffles = await collection.Find(new BsonDocument()).ToListAsync();

            foreach (var raffle in raffles)
            {
                string strId = raffle.Id.ToString();
                stringId.Add(strId);
            }
            return stringId;
        }

        public async Task<List<string>> GetArrayNamesRaffles()
        {
            List<string> stringNames = new List<string>();
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            List<Raffle> raffles = await collection.Find(new BsonDocument()).ToListAsync();

            foreach (var raffle in raffles)
            {
                string strName = raffle.Name.ToString();
                stringNames.Add(strName);
            }
            return stringNames;
        }

        public async Task<bool> CheckIdRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();
            string strRaffle = raffle.Id.ToString();
            Console.WriteLine(strRaffle);
            if (strRaffle != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<long> FindIdRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();

            return raffle.Id;
        }

        public async Task<string> FindNameRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();
            string strRaffle = raffle.Name.ToString();

            return strRaffle;
        }

        public async Task<string> FindDescriptionRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();
            string strRaffle = raffle.Description.ToString();

            return strRaffle;
        }

        public async Task<bool> CheckUserInRaffle(long raffleId, long userId)
        {
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<UsersInRaffle>("users_in_raffle");

            // Преобразуем идентификаторы в строки
            string strUser = userId.ToString();
            string strId = raffleId.ToString();

            // Проверяем, существует ли запись с указанным RaffleId и UserId
            var userInRaffle = await collection.Find(new BsonDocument { { "RaffleId", strId }, { "UserId", strUser } }).FirstOrDefaultAsync();

            // Если запись найдена, возвращаем true, иначе false
            return userInRaffle != null;
        }



        public async Task<InlineKeyboardMarkup> GetButtonsRaffle()
        {
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            List<Raffle> raffles = await collection.Find(new BsonDocument()).ToListAsync();
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();
            List<string> strId = await GetArrayIdRaffles();
            List<string> strNames = await GetArrayNamesRaffles();
            int count = Math.Min(raffles.Count, Math.Min(strId.Count, strNames.Count));

            for (int i = 0; i < count; i++)
            {
                var button = InlineKeyboardButton.WithCallbackData($"Розыгрыш: {strNames[i]}", strId[i]);
                inlineKeyboard.Add(new List<InlineKeyboardButton> { button });
            }

            return new InlineKeyboardMarkup(inlineKeyboard);
        }

        public async Task GetUserInRaffle(long raffleId, long userId)
        {
            UsersInRaffle usersInRaffle = new UsersInRaffle(raffleId, userId);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<UsersInRaffle>("users_in_raffle");
            collection.InsertOne(usersInRaffle);
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
            public long RaffleId { get; set; }
            public long UserId { get; set; }

            public UsersInRaffle(long raffleId, long userId)
            {
                RaffleId = raffleId;
                UserId = userId;
            }
        }
    }
}
