using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types.ReplyMarkups;

namespace NewTGBot
{
    internal class MongoConnection
    {
        private static MongoClient client = new MongoClient("mongodb://localhost:27017");
        private static IMongoDatabase? db;
        public async Task MongoUpdate()
        {
            try
            {
                Console.WriteLine("База данных работает");
                db = client.GetDatabase("telegram_bot");
                await db.CreateCollectionAsync("users");
                await db.CreateCollectionAsync("admins");
                await db.CreateCollectionAsync("raffles");
                await db.CreateCollectionAsync("users_in_raffle");
                await CheckDataRaffle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
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

        public async Task GetRaffle(long id, string status, byte[] image, string name, string description, DateTime dateTime)
        {
            Raffle raffle = new Raffle(id: id, status: status, image: image, name: name, description: description, dataEvent: dateTime);
            var raffles = db.GetCollection<Raffle>("raffles");
            raffles.InsertOne(raffle);
        }

        public async Task<long> SetIdRaffle()
        {
            var db = client.GetDatabase("telegram_bot");
            var collections = db.GetCollection<Raffle>("raffles");

            // Находим максимальный ID в коллекции
            var maxIdDocument = await collections.Find(new BsonDocument())
                .Sort(Builders<Raffle>.Sort.Descending("_id"))
                .Limit(1)
                .FirstOrDefaultAsync();

            // Если коллекция пуста, возвращаем 0
            if (maxIdDocument == null)
            {
                return 0;
            }

            // Возвращаем ID на один больше максимального
            long maxId = maxIdDocument.Id; // Предполагается, что у вас есть свойство Id в классе Raffle
            return maxId + 1;
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

        public async Task<string> FindStatusRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            
            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();
            string strRaffle = raffle.Status.ToString();

            return strRaffle;
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

        public async Task<string> FindDateRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();
            string strRaffle = raffle.DataEvent.ToShortDateString();

            return strRaffle;
        }

        public async Task<byte[]> FindImageDataRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();

            return raffle.Image;
        }

        public async Task<string> FindWinnerInRaffle(string id)
        {
            long.TryParse(id, out var result);
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            var raffle = await collection.Find(new BsonDocument("_id", result)).FirstAsync();
            string strRaffle = raffle.Winner.ToString();

            return strRaffle;
        }

        public async Task<bool> CheckUserInRaffle(long raffleId, long userId)
        {
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<UsersInRaffle>("users_in_raffle");

            // Создаем фильтр для поиска пользователя в конкретном конкурсе
            var filter = Builders<UsersInRaffle>.Filter.And(
                Builders<UsersInRaffle>.Filter.Eq(u => u.RaffleId, raffleId),
                Builders<UsersInRaffle>.Filter.Eq(u => u.UserId, userId)
            );

            // Ищем пользователя в коллекции
            var userInRaffle = await collection.Find(filter).FirstOrDefaultAsync();

            // Если пользователь найден, значит он участвует в конкурсе
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

        public async Task<int> GetRandomNumberInUsers(long raffleId)
        {
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<UsersInRaffle>("users_in_raffle");
            var filter = new BsonDocument { { "RaffleId", raffleId } };
            List<UsersInRaffle> raffles = await collection.Find(filter).ToListAsync();
            int count = 0;
            List<long> userInRaffle;
            foreach (var raffle in raffles)
            {
                count++;
            }
            Random ran = new Random();
            int ranUser = ran.Next(0, count);
            return ranUser;
        }

        public async Task<string> GetRandomUser(long ranUser, long raffleId)
        {
            db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<UsersInRaffle>("users_in_raffle");
            var filter = new BsonDocument { { "RaffleId", raffleId } };
            List<string> userInRaffle = new List<string>();
            List<UsersInRaffle> raffles = await collection.Find(filter).ToListAsync();
            
            foreach (var raffle in raffles)
            {
                userInRaffle.Add(raffle.UserId.ToString());
            }
            Console.WriteLine($"Проверка: {userInRaffle}");
            if (userInRaffle.Count > 0)
            {
                int intRanUser = (int)ranUser;
                Console.WriteLine($"Победитель {userInRaffle[intRanUser]}");
                return userInRaffle[intRanUser];
            }
            else
            {
                Console.WriteLine("Сейчас никто не участвует!");
                return "";
            }
            
        }

        public async Task UpdateRaffleWinnerAndStatus(string ranUser, long raffleId)
        {
            Console.WriteLine("Айди розыгрыша" + raffleId);
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");

            // Создаем фильтр для поиска нужного розыгрыша
            var filter = Builders<Raffle>.Filter.Eq("_id", raffleId);

            // Создаем обновление с использованием одного $set
            var update = Builders<Raffle>.Update
                .Set("Winner", ranUser)
                .Set("Status", "Завершено");

            // Выполняем обновление
            await collection.UpdateOneAsync(filter, update);
            Console.WriteLine("Розыгрыш обновлён");
        }

        public async Task EditRaffle(byte[] image, string name, string description, DateTime dateEvent,long raffleId)
        {
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            var filter = Builders<Raffle>.Filter.Eq("_id", raffleId);
            var update = Builders<Raffle>.Update
                .Set ("Status", "Проводится")
                .Set ("Image", image)
                .Set("Name", name)
                .Set("Description", description)
                .Set("DataEvent", dateEvent);
            await collection.UpdateOneAsync(filter, update);
            Console.WriteLine("Имя и описание розыгрыша обновлены");
        }

        public async Task DeleteRaffleAndReassignIds(long raffleId)
        {
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            await collection.DeleteOneAsync(new BsonDocument("_id", raffleId));
            var collection_userInRaffle = db.GetCollection<UsersInRaffle>("users_in_raffle");
            await collection_userInRaffle.DeleteManyAsync(new BsonDocument("RaffleId", raffleId));
            Console.WriteLine($"Розыгрыш с ID {raffleId} удалён.");
        }

        public async Task<bool> CheckDataRaffle()
        {
            var db = client.GetDatabase("telegram_bot");
            var collection = db.GetCollection<Raffle>("raffles");
            var raffles = await collection.Find("{}").ToListAsync();
            bool checkDateRaffle = false;
            foreach (var raffle in raffles)
            {
                if (raffle != null)
                {
                    Console.WriteLine($"Розыгрыш с айди: {raffle.Id}, со статусом: {raffle.Status} и временем {raffle.DataEvent} Текущее время: {DateTime.Now}");
                    if (raffle.DataEvent < DateTime.Now && raffle.Status == "Проводится")
                    {
                        int ranCount = await GetRandomNumberInUsers(raffle.Id);
                        string strUser = await GetRandomUser(ranCount, raffle.Id);
                        await UpdateRaffleWinnerAndStatus(strUser, raffle.Id);
                        checkDateRaffle = true;
                        Console.WriteLine($"Розыгрыш под id: {raffle.Id} автоматически проведён!");
                    }
                    else
                    {
                        Console.WriteLine("Сейчас не время автоматически проводить розыгрыш");
                    }
                }
            }
            return checkDateRaffle;
        }




        public class Raffle
        {
            public long Id { get; set; }
            public string Status { get; set; }
            public byte[] Image { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime DataEvent { get; set; }
            public string Winner { get; set; }

            public Raffle(long id, string status, byte[] image, string name, string description, DateTime dataEvent)
            {
                Id = id;
                Status = status;
                Image = image;
                Name = name;
                Description = description;
                DataEvent = dataEvent;
                Winner = "";
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
