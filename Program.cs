using Microsoft.VisualBasic;
using MongoDB.Driver;
using NewTGBot;
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
    private static ReceiverOptions? receiverOptions;
    private static InlineKeyboardMarkup? question;
    private static InlineKeyboardMarkup? getAdmin;
    private static MongoConnection? connection = new MongoConnection();
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
                        var user = message.From;
                        Console.WriteLine($"Пришло сообщение от пользователя {user.Username} с Id: {user.Id}");
                        var chat = message.Chat;
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                {
                                    if (message.Text == "/start")
                                    {
                                        Message msg = await bot.SendTextMessageAsync(chat.Id, "Привет");
                                        bool check = await connection.CheckAdmins();
                                        if (check != true)
                                        {
                                           await bot.SendTextMessageAsync(chat.Id, "На данный момент у меня нет администратора");
                                           msg = await bot.SendTextMessageAsync(chat.Id, "Хотите стать администратором?", replyMarkup: question);
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
                        switch(callbackQuery.Data)
                        {
                            case "yes":
                                bool check = await connection.CheckAdmins();
                                if (check != true)
                                {
                                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                    await connection.GetAdmin(user.Username, user.Id);
                                    await bot.SendTextMessageAsync(chat.Id, "Вы стали администратором");
                                }
                                else
                                {
                                    await bot.SendTextMessageAsync(chat.Id, "Администратор уже есть.");
                                }
                                
                                break;
                            case "no":
                                await bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                                await bot.SendTextMessageAsync(chat.Id, "Ожидайте когда появиться администратор");
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