using FFMpegCore;
using FFMpegCore.Enums;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(File.ReadAllText(@"..\..\..\TOKEN.txt"), cancellationToken: cts.Token);
var me = await bot.GetMe();

String? to_type = null;
Chat? chat = null;

bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;

Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
Console.ReadLine();
cts.Cancel(); // stop the bot



// method that handle messages received by the bot:
async Task OnMessage(Message msg, UpdateType type)
{
    chat = msg.Chat;
    Console.WriteLine($"Received {type} '{msg.Text}' in {msg.Chat}");
    if (msg.From.Username == "quops015" || msg.From.Username == "Alex352f")
    {
        await bot.SendMessage(msg.Chat, "otsoseee");
        return;
    }

    if(msg.Text == "/start")
    {
        await bot.SendMessage(msg.Chat, "Привет. Я бот для конвертации видео в gif и webm в mp4. Отправь mp4,mov или webm файл.");
        return;
    }

    if (msg.Video is not null)
    {
        if(msg.Video.FileSize > 10485761)
        {
            await bot.SendMessage(msg.Chat, "Только видео приколы до 10мб.");
            return;
        }
        var fileId = msg.Video.FileId;
        var tgFile = await bot.GetFile(fileId);

        await using var stream_ = File.Create("../downloaded.mp4");
        await bot.DownloadFile(tgFile, stream_);
        await bot.SendMessage(msg.Chat, "Конвертация...");
        stream_.Close();
        
        ConvertMp4ToGif("../downloaded.mp4", "../downloaded.gif");
        await using Stream stream = File.OpenRead("../downloaded.gif");

        await bot.SendAnimation(msg.Chat, stream);
        stream.Close();
        File.Delete("../downloaded.mp4");
        File.Delete("../downloaded.gif");
        return;
    }
    if (msg.Document is not null)
    {
        if( (msg.Document.FileName.EndsWith("webm")))
        {
            var fileId = msg.Document.FileId;
            var tgFile = await bot.GetFile(fileId);

            await using var stream_ = File.Create("../downloadedWEBM.webm");
            await bot.DownloadFile(tgFile, stream_);
            
            stream_.Close();

            await bot.SendMessage(msg.Chat, "В какой тип конвертировать?",
            replyMarkup: new InlineKeyboardButton[] { "В mp4", "В gif" });

            return;
        }
        
    }
    if (msg.Animation is not null)
    {
        var fileId = msg.Animation.FileId;
        var tgFile = await bot.GetFile(fileId);

        await using var stream_ = File.Create("../downloadedG.gif");
        await bot.DownloadFile(tgFile, stream_);
        await bot.SendMessage(msg.Chat, "...");
        stream_.Close();

        ReverseGif("../downloadedG.gif", "../downloadedI.gif");
        await using Stream stream = File.OpenRead("../downloadedI.gif");

        await bot.SendAnimation(msg.Chat, stream);
        stream.Close();
        File.Delete("../downloadedG.gif");
        File.Delete("../downloadedI.gif");
        return;
    }
    if (msg.Sticker is not null)
    {
        Console.WriteLine("sticker");
        await using Stream stream = File.OpenRead("bat.jpg");
        await bot.SendSticker(msg.Chat, stream);
        stream.Close();
        return;
    }

    await bot.SendMessage(msg.Chat, "Отправьте видео, либо webm файл");
}
async Task OnUpdate(Update update)
{   
    if(chat == null)
    {
        return;
    }
    if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
    {
        await bot.EditMessageReplyMarkup(
            chatId: query.Message.Chat.Id,
            messageId: query.Message.MessageId,
            replyMarkup: null
        );
        await bot.AnswerCallbackQuery(query.Id, $"Вы выбрали вариант \"{query.Data}\"");
        if(query.Data == "В mp4") {
            ConvertWebmToMp4("../downloadedWEBM.webm", "../downloadedWEBM.mp4");
            await using Stream stream = File.OpenRead("../downloadedWEBM.mp4");
            await bot.SendMessage(chat, "Конвертация...");
            await bot.SendVideo(chat, stream);
            stream.Close();
            File.Delete("../downloadedWEBM.mp4");
        }
        else if(query.Data == "В gif")
        {
            ConvertWebmToGif("../downloadedWEBM.webm", "../downloadedWEBM.gif");
            await using Stream stream = File.OpenRead("../downloadedWEBM.gif");
            await bot.SendMessage(chat, "Конвертация...");
            await bot.SendAnimation(chat, stream);
            stream.Close();
            File.Delete("../downloadedWEBM.gif");
        }
        File.Delete("../downloadedWEBM.webm");
    }
}

void ConvertMp4ToGif(string inputPath, string outputPath)
{
    try
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Входной файл не найден 0_0:", inputPath);
        }

        FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithVideoCodec("gif")
                .WithSpeedPreset(FFMpegCore.Enums.Speed.VeryFast)
                .WithVideoFilters(filterOptions => filterOptions
                    .Scale(320, -1)
                )
            )
            .ProcessSynchronously();

        Console.WriteLine($"Конвертация завершена: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при конвертации: {ex.Message}");
    }
}

void ConvertWebmToMp4(string inputPath, string outputPath)
{
    try
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Входной файл не найден 0_0:", inputPath);
        }

        FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithVideoCodec(VideoCodec.LibX264) 
                .WithAudioCodec(AudioCodec.Aac)
            )
            .ProcessSynchronously();

        Console.WriteLine($"Конвертация завершена: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при конвертации: {ex.Message}");
    }
}

void ConvertWebmToGif(string inputPath, string outputPath)
{
    try
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Входной файл не найден 0_0:", inputPath);
        }

        FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithVideoCodec("gif")
                .WithAudioCodec(AudioCodec.Aac)
            )
            .ProcessSynchronously();

        Console.WriteLine($"Конвертация завершена: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при конвертации: {ex.Message}");
    }
}

void ReverseGif(string inputPath, string outputPath)
{
    try
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Входной файл не найден 0_0:", inputPath);
        }

        FFMpegArguments
                   .FromFileInput(inputPath)
                   .OutputToFile(outputPath, overwrite: true, options => options
                       .WithCustomArgument("-vf reverse") 
                   )
                   .ProcessSynchronously();

        Console.WriteLine("REVERSE DONE.");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при конвертации: {ex.Message}");
    }
    
}
