using Amazon.Runtime.Internal.Auth;
using Microsoft.VisualBasic;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NewTGBot;
using Newtonsoft.Json.Linq;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    private static string token = "7784841400:AAFjbVgO1xB9Vv3kN5ZSDMF6pPlJVP_Wjw0";
    private static ITelegramBotClient ? bot;
    private static ReceiverOptions ? receiverOptions;
    private static InlineKeyboardMarkup ? question;
    private static InlineKeyboardMarkup ? adminPanel;
    private static InlineKeyboardMarkup ? rafflePanel;
    private static InlineKeyboardMarkup? adminRafflePanel;
    private static int intRaffleId;
    private static byte[] imageDataArr;
    private static DateTime dateEvent;
    private static bool waitingAnswerUser = false;
    private static bool getAdmin = false;
    private static bool deleteAdmin = false;
    private static bool createRaffle = false;
    private static bool editRaffle = false;
    private static bool joinRaffle = false;
    private static int count = 0;
    private static string[] raffleNames = new string[3];
    private static long longRaffleId = 0;
    private static MongoConnection ? connection = new MongoConnection();
    public static async Task Main(string[] args)
    {
        connection.MongoUpdate();
        bot = new TelegramBotClient(token);
        receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery,
            },
            ThrowPendingUpdates = true,
        };

        question = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Да", "yes"),
                InlineKeyboardButton.WithCallbackData("Нет", "no"),
            });
        adminPanel = new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Создать розыгрыш", "create_raffle"),
                InlineKeyboardButton.WithCallbackData("Проверить время проведения розыгрышей", "check_time"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Добавить админа", "add_admin"),
                InlineKeyboardButton.WithCallbackData("Удалить админа", "delete_admin"),
            }
        });

        rafflePanel = new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Участвовать", "join_raffle"),
            },
        });

        adminRafflePanel = new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Редактировать розыгрыш", "edit_raffle"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Провести", "start_raffle"),
                InlineKeyboardButton.WithCallbackData("Удалить розыгрыш", "delete_raffle"),
            }
        });

        using var cts = new CancellationTokenSource();
        bot.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cts.Token);
        var me = await bot.GetMeAsync();
        Console.WriteLine($"Бот: {me.FirstName} запущен");
        await Task.Delay(-1);
    }

    private static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        var message = update.Message;
                        var text = message.Text;
                        var chat = message.Chat;
                        //Console.WriteLine(text);

                        var user = message.From;
                        Console.WriteLine($"Пришло сообщение от пользователя {user.Username} с Id: {user.Id}");
                        /*var testPhoto = await bot.GetFileAsync(photo);
                        using (var fileStream = new MemoryStream())
                        {
                            await bot.DownloadFileAsync(testPhoto.FilePath, fileStream);
                            fileStream.Position = 0;

                            byte[] imageData = fileStream.ToArray();
                            Console.WriteLine(imageData);
                        }*/
                        if (waitingAnswerUser)
                        {
                            bool checkAdmin = await connection.CheckListAdmin(user.Id);
                            if (checkAdmin)
                            {
                                if (getAdmin)
                                {
                                    long answerUser = await GetAnswerUser(update);
                                    Console.WriteLine(answerUser);
                                    if (answerUser == 2)
                                    {
                                        await bot.SendTextMessageAsync(chat.Id, "Этот пользователь уже администратор!");
                                        waitingAnswerUser = false;
                                    }
                                    else if (answerUser != 0)
                                    {
                                        await connection.GetAdmin(answerUser);
                                        await bot.SendTextMessageAsync(chat.Id, "Добавлен новый админ.");
                                        waitingAnswerUser = false;
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chat.Id, "Id не было введено!");
                                        waitingAnswerUser = false;
                                    }
                                }
                                if (deleteAdmin)
                                {
                                    long answerUser = await DeleteAdminAnswerUser(update);
                                    if (answerUser != 0)
                                    {
                                        await connection.DeleteAdmin(answerUser);
                                        await bot.SendTextMessageAsync(chat.Id, $"Пользователь с Id {answerUser} удалён из базы данных");
                                    }
                                    else
                                    {
                                        await bot.SendTextMessageAsync(chat.Id, "Id не было введено!");
                                        waitingAnswerUser = false;
                                    }
                                }
                                getAdmin = false;
                                deleteAdmin = false;
                            }
                        }
                        if (createRaffle)
                        {
                            bool checkAdmin = await connection.CheckListAdmin(user.Id);
                            if (checkAdmin)
                            {
                                Console.WriteLine("Проверка работы условия");
                                switch (count)
                                {
                                    case 0:
                                        await bot.SendTextMessageAsync(chat.Id, "Введите описание розыгрыша:");
                                        raffleNames[count] = text;
                                        break;
                                    case 1:
                                        await bot.SendTextMessageAsync(chat.Id, "Введите время проведения розыгрыша:\n" +
                                            "Пример: 01.01.2024 - день, месяц, год.");
                                        raffleNames[count] = text;
                                        break;
                                    case 2:
                                        raffleNames[count] = text.Trim();
                                        Console.WriteLine($"Проверка даты:{raffleNames[2]}");

                                        try
                                        {
                                            DateTime data = DateTime.ParseExact(raffleNames[2], "dd.MM.yyyy", null);
                                            Console.WriteLine(data);
                                            Console.WriteLine($"Проверка массива raffleNames: {raffleNames[2]}");
                                            dateEvent = data.Date;
                                        }
                                        catch (FormatException)
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "Некорректный формат даты, создание розыгрыша отменено");
                                            count = 0;
                                            createRaffle = false;
                                            break;
                                        }
                                        await bot.SendTextMessageAsync(chat.Id, "Установите картинку(необязательно, просто отправьте любой текст):");
                                        break;
                                    case 3:
                                        if (message.Type == MessageType.Photo)
                                        {
                                            var photo = message.Photo[^1].FileId;
                                            var imageData = await bot.GetFileAsync(photo);
                                            using (var fileStream = new MemoryStream())
                                            {
                                                await bot.DownloadFileAsync(imageData.FilePath, fileStream);
                                                fileStream.Position = 0;

                                                imageDataArr = fileStream.ToArray();
                                            }
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "Вы отказались от картинки");
                                        }
                                        long id = await connection.SetIdRaffle();
                                        await connection.GetRaffle(id, "Проводится", imageDataArr, raffleNames[0], raffleNames[1], dateEvent.AddDays(1));
                                        await bot.SendTextMessageAsync(chat.Id, "Розыгрыш создан!");
                                        Console.WriteLine($"{raffleNames[0]}\n{raffleNames[1]}");
                                        count = 0;
                                        createRaffle = false;
                                        break;
                                }
                                await connection.CheckDataRaffle();
                                count++;
                            }
                        }
                        if (editRaffle)
                        {
                            bool checkAdmin = await connection.CheckListAdmin(user.Id);
                            if (checkAdmin)
                            {
                                Console.WriteLine("Проверка работы условия");
                                switch (count)
                                {
                                    case 0:
                                        await bot.SendTextMessageAsync(chat.Id, "Введите описание розыгрыша:");
                                        raffleNames[count] = text;
                                        break;
                                    case 1:
                                        await bot.SendTextMessageAsync(chat.Id, "Введите время проведения розыгрыша:\n" +
                                            "Пример: 01.01.2024 - день, месяц, год.");
                                        raffleNames[count] = text;
                                        break;
                                    case 2:
                                        raffleNames[count] = text.Trim();
                                        Console.WriteLine($"Проверка даты:{raffleNames[2]}");

                                        try
                                        {
                                            DateTime data = DateTime.ParseExact(raffleNames[2], "dd.MM.yyyy", null);
                                            Console.WriteLine(data);
                                            Console.WriteLine($"Проверка массива raffleNames: {raffleNames[2]}");
                                            dateEvent = data.Date;
                                            dateEvent.AddDays(1);
                                        }
                                        catch (FormatException)
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "Некорректный формат даты, обновление розыгрыша отменено");
                                            count = 0;
                                            createRaffle = false;
                                            break;
                                        }
                                        await bot.SendTextMessageAsync(chat.Id, "Установите картинку(необязательно, просто отправьте любой текст):");
                                        break;
                                    case 3:
                                        if (message.Type == MessageType.Photo)
                                        {
                                            var photo = message.Photo[^1].FileId;
                                            var imageData = await bot.GetFileAsync(photo);
                                            using (var fileStream = new MemoryStream())
                                            {
                                                await bot.DownloadFileAsync(imageData.FilePath, fileStream);
                                                fileStream.Position = 0;

                                                imageDataArr = fileStream.ToArray();
                                            }
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "Вы отказались от картинки");
                                        }
                                        long id = await connection.SetIdRaffle();
                                        await connection.EditRaffle(imageDataArr, raffleNames[0], raffleNames[1], dateEvent, intRaffleId);
                                        await bot.SendTextMessageAsync(chat.Id, "Розыгрыш обновлён!");
                                        Console.WriteLine($"{raffleNames[0]}\n{raffleNames[1]}");
                                        count = 0;
                                        createRaffle = false;
                                        break;
                                }
                                await connection.CheckDataRaffle();
                                count++;
                            }
                        }
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                {
                                    if (message.Text == "/start")
                                    {
                                        Message msg = await bot.SendTextMessageAsync(chat.Id, "Привет");
                                        InlineKeyboardMarkup buttonsRaffles = await connection.GetButtonsRaffle();
                                        bool check = await connection.CheckAdmins();
                                        bool isAdmin = await connection.CheckListAdmin(user.Id);
                                        long checkRaffle = await connection.SetIdRaffle();
                                        if (checkRaffle == 0)
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "На данный момент нету проводимых розыгрышей.");
                                        }
                                        else
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "Выберите розыгрыш", replyMarkup: buttonsRaffles);
                                        }
                                        if (check != true)
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "На данный момент у меня нет администратора");
                                            msg = await bot.SendTextMessageAsync(chat.Id, "Хотите стать администратором?", replyMarkup: question);
                                        }
                                        else if (isAdmin == true)
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "Меню администратора:", replyMarkup: adminPanel);
                                        }
                                        
                                        return;
                                    }
                                    return;
                                }
                        }
                        return;
                    }
                case UpdateType.CallbackQuery:
                    {
                        var callbackQuery = update.CallbackQuery;
                        var user = callbackQuery.From;
                        var chat = callbackQuery.Message.Chat;
                        await bot.AnswerCallbackQueryAsync(callbackQuery.Id);

                        bool checkAdmin = await connection.CheckAdmins();
                        string checkTypeCallback = callbackQuery.Data;
                        if (int.TryParse(checkTypeCallback, out int intCallbackQuery))
                        {
                            intRaffleId = intCallbackQuery;
                        }
                        string raffleId = callbackQuery.Data;
                        if (long.TryParse(raffleId, out var result))
                        {
                            bool checkRaffle = await connection.CheckIdRaffle(raffleId);
                            if (checkRaffle)
                            {
                                string statusRaffle = await connection.FindStatusRaffle(raffleId);
                                string nameRaffle = await connection.FindNameRaffle(raffleId);
                                string description = await connection.FindDescriptionRaffle(raffleId);
                                bool checkAdminRaffle = await connection.CheckListAdmin(user.Id);
                                string winnerRaffle = await connection.FindWinnerInRaffle(raffleId);
                                string dateEventRaffle = await connection.FindDateRaffle(raffleId);
                                byte[] imageData = await connection.FindImageDataRaffle(raffleId);
                                
                                string strWinner = "";
                                if (winnerRaffle != "")
                                {
                                    strWinner = $"Победитель: {winnerRaffle}";
                                }
                                string caption = $"<b>Статус: {statusRaffle}</b>\n<b>{nameRaffle}</b>\n{description}\n{strWinner}\n<b>Дата проведения: {dateEventRaffle}</b>";
                                if (imageData != null)
                                {
                                    await SendImageUser(chat.Id, imageData, caption, checkAdminRaffle);
                                }
                                else
                                {
                                    switch (checkAdminRaffle)
                                    {
                                        case true:
                                            await bot.SendTextMessageAsync(chat.Id, caption,
                                                replyMarkup: adminRafflePanel,
                                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                            break;
                                        case false:
                                            longRaffleId = result;
                                            await bot.SendTextMessageAsync(chat.Id, caption,
                                                replyMarkup: rafflePanel,
                                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                            break;
                                    }
                                }
                            }
                        }
                        //Console.WriteLine(raffleId);
                        
                        switch (callbackQuery.Data)
                        {
                            case "yes":
                                bool check = await connection.CheckAdmins();
                                if (check != true)
                                {
                                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                    await connection.GetAdmin(user.Id);
                                    await bot.SendTextMessageAsync(chat.Id, "Вы стали администратором");
                                }
                                else
                                {
                                    await bot.SendTextMessageAsync(chat.Id, "Администратор уже есть");
                                }
                                
                                break;
                            case "no":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                await bot.SendTextMessageAsync(chat.Id, "Ожидайте когда появиться администратор");
                                break;
                            case "create_raffle":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                //====================================ПИСАТЬ ЗДЕСЬ======================================
                                Console.WriteLine($"{user.Username} создаёт розыгрыш");
                                await bot.SendTextMessageAsync(chat.Id, "Введите название розыгрыша:");
                                count = 0;
                                createRaffle = true;
                                break;
                            case "check_time":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                bool checkDateRaffle = await connection.CheckDataRaffle();
                                if (checkDateRaffle)
                                {
                                    await bot.SendTextMessageAsync(chat.Id, "Обновлён статус найденных розыгрышей.");
                                }
                                else
                                {
                                    await bot.SendTextMessageAsync(chat.Id, "Проводимые розыгрыши не найдены.");
                                }
                                break;
                            case "edit_raffle":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                Console.WriteLine($"{user.Username} Редактирует розыгрыш");
                                await bot.SendTextMessageAsync(chat.Id, "Введите название розыгрыша:");
                                count = 0;
                                editRaffle = true;
                                break;
                            case "add_admin":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                await bot.SendTextMessageAsync(chat.Id, "Введите Id пользователя");
                                waitingAnswerUser = true;
                                getAdmin = true;
                                break;
                            case "delete_admin":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                string listAdmin = await connection.ToListAdmins();
                                await bot.SendTextMessageAsync(chat.Id, $"Введите Id пользователя, которого хотите удалить:\n{listAdmin}" );
                                waitingAnswerUser = true;
                                deleteAdmin = true;
                                break;
                            case "join_raffle":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                Console.WriteLine(longRaffleId);
                                string strId = intRaffleId.ToString();
                                string raffleStatus = await connection.FindStatusRaffle(strId);
                                if (raffleStatus != "Завершено")
                                {
                                    bool checkJoinRaffle = await connection.CheckUserInRaffle(longRaffleId, user.Id);
                                    if (checkJoinRaffle)
                                    {
                                        await bot.SendTextMessageAsync(chat.Id, "Только один раз можно участвовать!");
                                    }
                                    else
                                    {
                                        await connection.GetUserInRaffle(longRaffleId, user.Id);
                                        await bot.SendTextMessageAsync(chat.Id, "Вы участвуйте в розыгрыше!");
                                    }
                                }
                                else
                                {
                                    await bot.SendTextMessageAsync(chat.Id, "Розыгрыш уже завершен");
                                }
                                break;
                            case "start_raffle":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                Console.WriteLine($"Сейчас проводиться розыгрыш с id: {intRaffleId}");
                                int ranUser = await connection.GetRandomNumberInUsers(intRaffleId);
                                Console.WriteLine("Проверка на изменение айди: " + intRaffleId);
                                var strRanUser = await connection.GetRandomUser(ranUser, intRaffleId);
                                if (strRanUser != "")
                                {
                                    await connection.UpdateRaffleWinnerAndStatus(strRanUser, intRaffleId);
                                    await bot.SendTextMessageAsync(chat.Id, $"Добавлен победитель в розыгрыш!\nПобедитель: {strRanUser}");
                                }
                                else
                                {
                                    await bot.SendTextMessageAsync(chat.Id, "Сейчас никто не участвует в розыгрыше");
                                }
                                break;
                            case "delete_raffle":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                await connection.DeleteRaffleAndReassignIds(intRaffleId);
                                await bot.SendTextMessageAsync(chat.Id, "Розыгрыш удалён");
                                break;
                        }
                        return;
                    }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public static async Task SendImageUser(long chatId, byte[] imageData, string caption, bool checkAdminRaffle)
    {
        InlineKeyboardMarkup checkRafflePanel = rafflePanel;
        if (checkAdminRaffle)
        {
            checkRafflePanel = adminRafflePanel;
        }
        using (var stream = new MemoryStream(imageData))
        {
            stream.Position = 0;
            await bot.SendPhotoAsync(chatId: chatId,
                photo: new InputFileStream(stream, "fawef.jpg"),
                caption: caption, replyMarkup: checkRafflePanel, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }
    }


    public static async Task<long> GetAnswerUser(Update update)
    {
        var message = update.Message;
        var user = message.From;
        var chat = message.Chat;
        var text = message.Text;
        long answer;
        long.TryParse(text, out answer);
        bool check = await connection.CheckListAdmin(answer);
        if (check)
        {
            return 2;
        }
        else if (long.TryParse(text, out answer))
        {
            return answer;
        }
        return 0;
    }

    public static async Task<long> DeleteAdminAnswerUser(Update update)
    {
        var message = update.Message;
        var user = message.From;
        var chat = message.Chat;
        var text = message.Text;
        long answer;
        long.TryParse(text, out answer);
        bool check = await connection.CheckListAdmin(answer);
        if (long.TryParse(text, out answer))
        {
            return answer;
        }
        return 0;
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}