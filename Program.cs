using Microsoft.VisualBasic;
using MongoDB.Driver;
using NewTGBot;
using Newtonsoft.Json.Linq;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static string token = "7784841400:AAFjbVgO1xB9Vv3kN5ZSDMF6pPlJVP_Wjw0";
    private static ITelegramBotClient ? bot;
    private static ReceiverOptions ? receiverOptions;
    private static InlineKeyboardMarkup ? question;
    private static InlineKeyboardMarkup ? adminPanel;
    private static bool waitingAnswerUser = false;
    private static bool getAdmin = false;
    private static bool deleteAdmin = false;
    private static bool createRaffle = false;
    private static int count = 0;
    private static string[] raffleNames = new string[2];
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
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Добавить админа", "add_admin"),
                InlineKeyboardButton.WithCallbackData("Удалить админа", "delete_admin"),
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
                            Console.WriteLine("Проверка работы условия");
                            switch (count)
                            {
                                case 0:
                                    await bot.SendTextMessageAsync(chat.Id, "Введите описание розыгрыша:");
                                    raffleNames[count] = text;
                                    break;
                                case 1:
                                    raffleNames[count] = text;
                                    long id = await connection.SetIdRaffle();
                                    await connection.GetRaffle(id, "Проводится", raffleNames[0], raffleNames[1]);
                                    await bot.SendTextMessageAsync(chat.Id, "Розыгрыш создан!");
                                    Console.WriteLine($"{raffleNames[0]}\n{raffleNames[1]}");
                                    count = 0;
                                    createRaffle = false;
                                    break;
                            }
                            count++;
                        }
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                {
                                    if (message.Text == "/start")
                                    {
                                        Message msg = await bot.SendTextMessageAsync(chat.Id, "Привет");
                                        InlineKeyboardMarkup buttonsRaffles = await connection.GetButtonsRaffle();
                                        await bot.SendTextMessageAsync(chat.Id, "Выберите розыгрыш", replyMarkup: buttonsRaffles);
                                        bool check = await connection.CheckAdmins();
                                        bool isAdmin = await connection.CheckListAdmin(user.Id);
                                        long checkRaffle = await connection.SetIdRaffle();
                                        if (checkRaffle == 0)
                                        {
                                            await bot.SendTextMessageAsync(chat.Id, "На данный момент нету проводимых розыгрышей.");
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
                        bool checkAdmin = await connection.CheckAdmins();
                        switch(callbackQuery.Data)
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