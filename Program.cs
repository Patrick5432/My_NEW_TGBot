using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    private static string token = "7784841400:AAFjbVgO1xB9Vv3kN5ZSDMF6pPlJVP_Wjw0";
    private static ITelegramBotClient ? bot;
    private static ReceiverOptions? receiverOptions;

    public static async Task Main(string[] args)
    {
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
                                        //======================= ПРИ НАПИСАНИИ КОМАНДЫ ДОБАВИТЬ ПРОВЕРКУ НА АДМИНА =======================
                                        Message msg = await bot.SendTextMessageAsync(chat.Id, "Привет");
                                        bot.EditMessageTextAsync(chat.Id, msg.MessageId,"Пока");
                                        return;
                                    }
                                    return;
                                }
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