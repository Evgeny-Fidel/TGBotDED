using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

var version = "0.3.5";
var autor = "";
string TokenTelegramAPI = "";
string TokenWeather = "";
string connStr = "";

bool Doki = false; // Включение/отключение функции сохранения файлов пользователя
bool AutoUpdate = true; // Включение/отключение функции автообновления бота
int AutoUpdateMinete = 30; // Частота проверки обновлений
bool AutoValRUB = true; // Включение/отключение функции автоперевода лир в рубли
bool Logs = true; // Включение/отключение логирования приватных сообщений в консоль
bool WeatherLoc = true; // Включение/отключение отправка погоды по геолокации

string DirectoryProg = Environment.CurrentDirectory;
string DirectorySettings = $"{DirectoryProg}/Settings";
Directory.CreateDirectory(DirectorySettings);
if (System.IO.File.Exists($"{DirectorySettings}/Authentication.txt"))
{
    using (StreamReader reader = new StreamReader($"{DirectorySettings}/Authentication.txt"))
    {
        string server = "";
        string database = "";
        string uid = "";
        string pwd = "";

        string? line;
        try
        {
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("Token.Telegram.API ="))
                {
                    line = line.Replace("Token.Telegram.API =", "");
                    TokenTelegramAPI = line.Replace(" ", "");
                }
                if (line.StartsWith("Token.Weather ="))
                {
                    line = line.Replace("Token.Weather =", "");
                    TokenWeather = line.Replace(" ", "");
                }
                if (line.StartsWith("Server ="))
                {
                    line = line.Replace("Server =", "");
                    server = line.Replace(" ", "");
                }
                if (line.StartsWith("Database ="))
                {
                    line = line.Replace("Database =", "");
                    database = line.Replace(" ", "");
                }
                if (line.StartsWith("Uid ="))
                {
                    line = line.Replace("Uid =", "");
                    uid = line.Replace(" ", "");
                }
                if (line.StartsWith("Pwd ="))
                {
                    line = line.Replace("Pwd =", "");
                    pwd = line.Replace(" ", "");
                }
                if (line.StartsWith("Autor ="))
                {
                    line = line.Replace("Autor =", "");
                    autor = line.Replace(" ", "");
                }
                if (line.StartsWith("Save_Document ="))
                {
                    line = line.Replace("Save_Document =", "");
                    Doki = Convert.ToBoolean(line.Replace(" ", ""));
                }
                if (line.StartsWith("Auto_Update ="))
                {
                    line = line.Replace("Auto_Update =", "");
                    AutoUpdate = Convert.ToBoolean(line.Replace(" ", ""));
                }
                if (line.StartsWith("Auto_Update_Minute ="))
                {
                    line = line.Replace("Auto_Update_Minute =", "");
                    AutoUpdateMinete = Convert.ToInt32(line.Replace(" ", ""));
                }
                if (line.StartsWith("Auto_ValtoRUB ="))
                {
                    line = line.Replace("Auto_ValtoRUB =", "");
                    AutoValRUB = Convert.ToBoolean(line.Replace(" ", ""));
                }
                if (line.StartsWith("Weather_Location ="))
                {
                    line = line.Replace("Weather_Location =", "");
                    WeatherLoc = Convert.ToBoolean(line.Replace(" ", ""));
                }
            }
            connStr = $@"Server={server};Database={database};Uid={uid};Pwd={pwd};";
        }
        catch
        {
            Console.WriteLine($"Возникла ошибка при счении настроек - {DirectorySettings}/Authentication.txt\n" +
                $"Для того чтобы сбросить файл найстроек, удалите его, запустите бота снова, он сгенирирует правильный файл, после этого, Вам нужно его заполнить.\n");
        }
    }
}
else
{
    Console.WriteLine($"Отсутствуют файлы аутентификации! Заполните значения по этому пути - {DirectorySettings}/Authentication.txt");
    System.IO.File.WriteAllText($"{DirectorySettings}/Authentication.txt", "" +
        "———————————————————————————Telegram API————————————————————————————\n" +
        "Token.Telegram.API = ЗАМЕНИТЕ_ЭТОТ_ТЕКСТ_НА_СВОЙ_ТОКЕН_TELEGRAM\n" +
        "Token.Weather = ЗАМЕНИТЕ_ЭТОТ_ТЕКСТ_НА_СВОЙ_ТОКЕН_OPENWEATHERMAP\n\n" +
        "——————————————————————————————MySQL————————————————————————————————\n" +
        "Server = IP_ВАШЕЙ_БД\n" +
        "Database = ИМЯ_ВАШЕЙ_БД\n" +
        "Uid = ЛОГИН\n" +
        "Pwd = ПАРОЛЬ\n\n" +
        "———————————————————————————Telegram BOT————————————————————————————\n" +
        "Autor = @evgeny_fidel\n" +
        "Save_Document = false\n" +
        "Auto_Update = true\n" +
        "Auto_Update_Minute = 30\n" +
        "Auto_ValtoRUB = true\n" +
        "Weather_Location = true" +
        "");
}

var botClient = new TelegramBotClient(TokenTelegramAPI);
using var cts = new CancellationTokenSource();
Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};

MySqlConnection MySqlBase = new MySqlConnection(connStr);
try
{
    MySqlBase.Open();
    Console.WriteLine($"Успешное подключение к БД");
    MySqlBase.Close();
}
catch
{
    MySqlBase.Close();
    Console.WriteLine($"Не удалось подключиться к БД");
}

botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, cts.Token);
var me = await botClient.GetMeAsync();
Console.WriteLine($"Вышел на смену: \"{botClient.GetMeAsync().Result.FirstName}\" @{botClient.GetMeAsync().Result.Username} | Версия бота: {version} | {DateTime.Now.ToString("dd.MM.yy | HH:mm:ss")}");

if (AutoUpdate == true)
{
    Timer timer = new Timer(showTime, null, 0, AutoUpdateMinete * 60 * 1000);
}
if (System.IO.File.Exists($"{DirectoryProg}/Update TGBotDED.zip"))
{
    System.IO.File.Delete($"{DirectoryProg}/Update TGBotDED.zip");
}
if (System.IO.File.Exists($"{DirectoryProg}/UpdaterProg.exe"))
{
    System.IO.File.Delete($"{DirectoryProg}/UpdaterProg.exe");
}

Console.ReadLine();
cts.Cancel();


async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var rrr = update.Message;
    if (update.Type == UpdateType.Message && update?.Message?.Text != null)
    {
        await HandleMessage(botClient, update, update.Message);
        return;
    }
    if (update.Type == UpdateType.Message && update?.Message?.Document != null)
    {
        await HandleDocument(botClient, update.Message);
        return;
    }
    if (update.Type == UpdateType.Message && update?.Message?.Photo != null)
    {
        await HandlePhoto(botClient, update.Message);
        return;
    }
    if (update.Type == UpdateType.CallbackQuery)
    {
        await HandleCallbackQuery(botClient, update.CallbackQuery);
        return;
    }
    if (update.MyChatMember != null)
    {
        if (update.MyChatMember.NewChatMember.Status == ChatMemberStatus.Administrator)
        {
            await HandleMember(botClient, update, update.Message);
            return;
        }
    }
    if (update.Message != null)
    {
        if (update.Message.Type == MessageType.ChatTitleChanged)
        {
            await HandleMember(botClient, update, update.Message);
            return;
        }
        if (update.Message.Type == MessageType.Location)
        {
            await HandleLocation(botClient, update.Message);
            return;
        }
    }
    return;
}


async Task HandleMessage(ITelegramBotClient botClient, Update update, Message message)
{
    string Hello = $"Привет! Я бот \"{botClient.GetMeAsync().Result.FirstName}\"\n\n" +
        $"☀️ Я умею показывать погоду.\n" +
        $"💰 Показывать актуальный курс валют.\n" +
        $"🎮 Со мной можно поиграть.\n" +
        $"📈 Могу проверить не упал ли твой сайт или любой IP.\n" +
        $"💾 Так же я немного умею работать с файлами.\n" +
        $"❌ Если добавишь меня в группу то, ты сможешь заблокировать пользователя на время благодаря мне.\n" +
        $"🔫 А так же вызвать кого-то на дуэль!\n" +
        $"\n/help - некоторые команды, с минимальным описанием.\n" +
        $"Ниже ссылочка, с полным и подробным всех команд!😉";

    string buttonRnd10 = "0 - 10 🔢";
    string buttonRnd100 = "0 - 100 🔢";
    string buttonRnd1000 = "0 - 1000 🔢";
    string buttonDeleteKeyboard = "Убрать клавиатуру ⬇️";
    string buttonStone = "✊ Камень";
    string buttonScissors = "✌️ Ножницы";
    string buttonPaper = "🖐 Бумага";

    string buttonRUBtoAUD = "Австралийский доллар 🇦🇺";
    string buttonRUBtoAZN = "Азербайджанский манат 🇦🇿";
    string buttonRUBtoGBP = "Фунт стерлингов Соединенного королевства 🇬🇧";
    string buttonRUBtoAMD = "Армянский драм 🇦🇲";
    string buttonRUBtoBYN = "Белорусский рубль 🇧🇾";
    string buttonRUBtoBGN = "Болгарский лев 🇧🇬";
    string buttonRUBtoBRL = "Бразильский реал 🇧🇷";
    string buttonRUBtoHUF = "Венгерский форинт 🇭🇺";
    string buttonRUBtoHKD = "Гонконгский доллар 🇭🇰";
    string buttonRUBtoDKK = "Датская крона 🇩🇰";
    string buttonRUBtoUSD = "Доллар США 🇺🇸";
    string buttonRUBtoEUR = "Евро 🇪🇺";
    string buttonRUBtoINR = "Индийская рупия 🇮🇳";
    string buttonRUBtoKZT = "Казахстанская тенге 🇰🇿";
    string buttonRUBtoCAD = "Канадский доллар 🇨🇦";
    string buttonRUBtoKGS = "Киргизский сом 🇰🇬";
    string buttonRUBtoCNY = "Китайская юань 🇨🇳";
    string buttonRUBtoMDL = "Молдавский лей 🇲🇩";
    string buttonRUBtoNOK = "Норвежская крона 🇳🇴";
    string buttonRUBtoPLN = "Польский злотый 🇵🇱";
    string buttonRUBtoRON = "Румынский лей 🇷🇴";
    string buttonRUBtoXDR = "СДР (специальные права заимствования) 🏴";
    string buttonRUBtoSGD = "Сингапурский доллар 🇸🇬";
    string buttonRUBtoTJS = "Таджикских сомони 🇹🇯";
    string buttonRUBtoTRY = "Турецкая лира 🇹🇷";
    string buttonRUBtoTMT = "Новый туркменский манат 🇹🇲";
    string buttonRUBtoUZS = "Узбекская сума 🇺🇿";
    string buttonRUBtoUAH = "Украинская гривна 🇺🇦";
    string buttonRUBtoCZK = "Чешская крона 🇨🇿";
    string buttonRUBtoSEK = "Шведская крона 🇸🇪";
    string buttonRUBtoCHF = "Швейцарский франк 🇨🇭";
    string buttonRUBtoZAR = "Южноафриканская рэнда 🇿🇦";
    string buttonRUBtoKRW = "Вона Республики Корея 🇰🇷";
    string buttonRUBtoJPY = "Японская иена 🇯🇵";

    // Ниже только приват
    if (message.Chat.Type == ChatType.Private)
    {
        if (Logs == true)
        {
            string TextMes = message.Text;
            Console.WriteLine($"{message.From.Id} - @{message.From.Username} | Сообщение | {DateTime.Now.ToString("dd.MM.yy | HH:mm:ss")} | {TextMes.Replace("\n", " ")}");
        }
        if (message.Text.StartsWith("/say_all_users_test "))
        {
            var ID = "";
            var Text = message.Text.Replace("/say_all_users_test ", "");
            if (Text == "" || Text == " ")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы не написали текст сообщения!", disableNotification: true);
                return;
            }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDUser;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                //string name = command.ExecuteScalar().ToString();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var TestMes = reader.GetString("TestMes");
                    if (TestMes == "True")
                    {
                        try { await botClient.SendTextMessageAsync(reader.GetString("id"), $"{Text}\n\nДля отключения тестовых сообщений, нажмите /test_mes_off"); ID += $"{reader.GetString("id")}\n"; } catch { }
                    }
                }
                await botClient.SendTextMessageAsync(message.Chat, $"Отправил:\n{ID}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ошибка", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/say_all_users "))
        {
            var ID = "";
            var Text = message.Text.Replace("/say_all_users ", "");
            if (Text == "" || Text == " ")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы не написали текст сообщения!", disableNotification: true);
                return;
            }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDUser;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                //string name = command.ExecuteScalar().ToString();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try { await botClient.SendTextMessageAsync(reader.GetString("id"), $"{Text}"); ID += $"{reader.GetString("id")}\n"; } catch { }
                }
                await botClient.SendTextMessageAsync(message.Chat, $"Отправил:\n{ID}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ошибка", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/say_all_group"))
        {
            var ID = "";
            var Text = message.Text.Replace("/say_all_group ", "");
            Text = message.Text.Replace("/say_all_group", "");
            if (Text == "" || Text == " ")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы не написали текст сообщения!", disableNotification: true);
                return;
            }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDGroup;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                //string name = command.ExecuteScalar().ToString();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var Market = reader.GetString("market");
                    if (Market == "True")
                    {
                        try { await botClient.SendTextMessageAsync(reader.GetString("id"), $"{Text}"); ID += $"{reader.GetString("id")}\n"; } catch { }
                    }
                }
                await botClient.SendTextMessageAsync(message.Chat, $"Отправил:\n{ID}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ошибка", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }

        message.Text = message.Text.ToLower();
        if (message.Text.StartsWith("/start"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            InlineKeyboardMarkup inlineKeyboard = new(new[] { InlineKeyboardButton.WithUrl(text: "Описание всех команд ➡️", url: "https://evgeny-fidel.ru/cmdtgbotded/") });
            Message sentMessage = await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: Hello, replyMarkup: inlineKeyboard, disableNotification: true);

            try
            {
                MySqlBase.Open();
                try
                {
                    string cmdsql = $"INSERT INTO BDUser (id, username, firstname, lastname) VALUES ('{message.From.Id}', '{message.From.Username}', '{message.From.FirstName}', '{message.From.LastName}');";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                }
                catch
                {
                    string cmdsql = $"UPDATE BDUser SET username = '{message.From.Username}', firstname = '{message.From.FirstName}', lastname = '{message.From.LastName}' WHERE id = '{message.From.Id}';";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                }
                MySqlBase.Close();
            }
            catch
            {
                Console.WriteLine("При добавлении в БД нового пользователя, произошла ошибка.");
            }

            return;
        }
        if (message.Text.StartsWith("/my_disk"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            string directoryDesctop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string directoryDesctopTGBot = $@"{directoryDesctop}/TGBotDED/BDUserFile";
            Directory.CreateDirectory(directoryDesctopTGBot);
            string directoryUsername = $@"{directoryDesctopTGBot}/{message.From.Id}";
            if (Directory.Exists(directoryUsername) == false)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Вы мне не присылали никаких файлов ☹️", disableNotification: true);
                Console.WriteLine($"{message.From.Id} | Сбор файлов | Файлов нету");
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(directoryUsername);
            foreach (var item in dir.GetFiles())
            {
                Console.WriteLine($"Отправлен файл: {item.Name}");
                //await botClient.SendTextMessageAsync(message.Chat, item.Name);
                await using Stream stream = System.IO.File.OpenRead(@$"{directoryUsername}/{item.Name.ToString()}");
                await botClient.SendDocumentAsync(message.Chat.Id, new InputOnlineFile(stream, @$"{directoryUsername}/{item.Name.ToString()}"), disableNotification: true);
            }
            await botClient.SendTextMessageAsync(message.Chat, "Это все, что у меня было 😊");
            return;
        }
        if (message.Text.StartsWith("/delete_disk"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            string directoryDesctop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string directoryDesctopTGBot = $@"{directoryDesctop}/TGBotDED/BDUserFile";
            Directory.CreateDirectory(directoryDesctopTGBot);
            string directoryUsername = $@"{directoryDesctopTGBot}/{message.From.Id}";
            if (Directory.Exists(directoryUsername))
            {
                Directory.Delete(directoryUsername, true);
                await botClient.SendTextMessageAsync(message.Chat, $"Все Ваши файлы удалены! 😊", disableNotification: true);
            }
            Directory.CreateDirectory(directoryUsername);
            return;
        }
        if (message.Text.StartsWith("/delete_file"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            if (message.ReplyToMessage == null) // Проверка об ответном сообщении
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Я не вижу файла, отпрвьте команду в ответ на сообщение с файлом..", disableNotification: true);
                return;
            }
            if (message.ReplyToMessage.Document.FileName == null) // Проверка на наличие файла в сообщении
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Я не вижу файла, отпрвьте команду в ответ на сообщение с файлом..", disableNotification: true);
                return;
            }
            var mes = await botClient.SendTextMessageAsync(message.ReplyToMessage.Chat.Id, $"Секундочку..", disableNotification: true);
            string DirectoryFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TGBotDED/BDUserFile/{message.From.Id}/{message.ReplyToMessage.Document.FileName}";
            if (System.IO.File.Exists(DirectoryFile))
            {
                System.IO.File.Delete(DirectoryFile);
                try { await botClient.DeleteMessageAsync(message.Chat.Id, message.ReplyToMessage.MessageId); } catch { }
                await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"Файл успешно удален");
                return;
            }
            else
            {
                await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"Я не могу найти файл у себя(");
                return;
            }
        }

        if (message.Text.StartsWith("/test_mes_off"))
        {
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDUser SET TestMes = '0' WHERE id = '{message.From.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                await botClient.SendTextMessageAsync(message.Chat, $"Успешное отключение Вас от тестовых сообщений.\nЕсли передумаете то, введите команду /test_mes_on", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }

            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/test_mes_on"))
        {
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDUser SET TestMes = '1' WHERE id = '{message.From.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                await botClient.SendTextMessageAsync(message.Chat, $"Успешное подключение Вас к тестовым сообщениям.\nЕсли передумаете то, введите команду /test_mes_off", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }

            MySqlBase.Close();
            return;
        }
    }
    // Ниже команды для всех
    if (message.Chat.Type != null)
    {
        message.Text = message.Text.ToLower();
        if (message.Text.StartsWith("/delete_keyboard") || message.Text == buttonDeleteKeyboard.ToLower())
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, "✅ Клавиатура заблокирована!", disableNotification: true, replyMarkup: new ReplyKeyboardRemove());
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            return;
        }
        if (message.Text.StartsWith("/info"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            string TextMes = "";
            string Username = "";
            string FirstName = "";
            string LastName = "";
            string ID = "";
            ChatMember chatMember;
            if (message.ReplyToMessage != null) // Проверка об ответном сообщении
            {
                Username = message.ReplyToMessage.From.Username;
                FirstName = message.ReplyToMessage.From.FirstName;
                LastName = message.ReplyToMessage.From.LastName;
                ID = $"{message.ReplyToMessage.From.Id}";
                chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
            }
            else
            {
                Username = message.From.Username;
                FirstName = message.From.FirstName;
                LastName = message.From.LastName;
                ID = $"{message.From.Id}";
                chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
            }
            TextMes = $"" +
                $"Chat Title: {message.Chat.Title}\n" +
                $"Chat ID: {message.Chat.Id}\n" +
                $"\n" +
                $"From Username: @{Username}\n" +
                $"From Name: {FirstName} {LastName}\n" +
                $"From ID: {ID}\n" +
                $"From Member: {chatMember.Status}";
            try
            {
                try // Пользователь
                {
                    MySqlBase.Open();

                    var IDUser = message.From.Id;
                    if (message.ReplyToMessage != null)
                    {
                        IDUser = message.ReplyToMessage.From.Id;
                    }
                    string cmdsql = $"SELECT * FROM BDUser WHERE id = '{IDUser}';";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    MySqlDataReader reader = command.ExecuteReader();
                    string saveurl = "";
                    string saveurlval = "";
                    string TestMes = "";
                    while (reader.Read())
                    {
                        saveurl = reader.GetString("saveurl");
                        saveurlval = reader.GetString("saveurlval");
                        TestMes = reader.GetString("TestMes");
                    }
                    MySqlBase.Close();
                    TextMes = $"{TextMes}\n\n" +
                        $"Информация из БД по пользователю:\n" +
                        $"SaveUrl: {saveurl}\n" +
                        $"SaveURLVal: {saveurlval}\n" +
                        $"TestMes: {TestMes}";
                }
                catch { MySqlBase.Close(); }
                try // Группа
                {
                    MySqlBase.Open();
                    string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    MySqlDataReader reader = command.ExecuteReader();
                    string Market = "";
                    string AutoCurrency = "";
                    string AutoWeatherLoc = "";
                    while (reader.Read())
                    {
                        Market = reader.GetString("market");
                        AutoCurrency = reader.GetString("auto_currency");
                        AutoWeatherLoc = reader.GetString("auto_weather_loc");
                    }
                    MySqlBase.Close();
                    TextMes = $"{TextMes}\n\n" +
                        $"Информация из БД по группе:\n" +
                        $"Market: {Market}\n" +
                        $"AutoCurrency: {AutoCurrency}\n" +
                        $"AutoWeatherLoc: {AutoWeatherLoc}";
                }
                catch { MySqlBase.Close(); }
            }
            catch { }
            TextMes = $"{TextMes}\n\n" +
                $"Разработчик: {autor}\n" +
                $"Версия бота: {version}\n";
            await botClient.SendTextMessageAsync(message.Chat, TextMes, disableNotification: true);
            return;
        }
        if (message.Text.StartsWith("/weather_"))
        {
            var mes = await botClient.SendTextMessageAsync(message.Chat, "Секунду, сейчас слетаю и посмотрю! 🛩", disableNotification: true);
            try
            {
                try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
                string Smiley = "";
                string SmileyWeather = "";
                string Country = "";
                string City = "";
                string Lat = "";
                string Lon = "";
                int ChekPog = 0;
                int WeatherAll = 1;
                string Text = "";
                int ChekAll = 0;
                if (message.Text.StartsWith("/weather_all"))
                {
                    WeatherAll = 3;
                    ChekAll = 2;
                }
                for (int i = 0; i < WeatherAll; i++)
                {
                    if (message.Text.StartsWith("/weather_moscow") || ChekAll == 4)
                    {
                        Country = "🇷🇺";
                        City = "Москва";
                        Lat = "55.75";
                        Lon = "37.61";
                        ChekPog++;
                        ChekAll++;
                    }
                    if (message.Text.StartsWith("/weather_im") || ChekAll == 3)
                    {
                        Country = "🇷🇺";
                        City = "Императорские Мытищи";
                        Lat = "55.95";
                        Lon = "37.68";
                        ChekPog++;
                        ChekAll++;
                    }
                    if (message.Text.StartsWith("/weather_antalya") || ChekAll == 2)
                    {
                        Country = "🇹🇷";
                        City = "Анталия";
                        Lat = "36.9293";
                        Lon = "30.7019";
                        ChekPog++;
                        ChekAll++;
                    }
                    if (ChekPog > 0)
                    {
                        string url = $"https://api.openweathermap.org/data/2.5/weather?lat={Lat}&lon={Lon}&units=metric&mode=xml&appid={TokenWeather}&lang=ru";

                        WebClient client = new WebClient();
                        var xml = client.DownloadString(url);
                        XDocument xdoc = XDocument.Parse(xml);
                        XElement? Temperature = xdoc.Element("current").Element("temperature");
                        XAttribute? TemperatureVal = Temperature.Attribute("value");

                        XElement? Weather = xdoc.Element("current").Element("weather");
                        XAttribute? WeatherVal = Weather.Attribute("value");

                        var WeatherValue = WeatherVal.Value;
                        double Temp = 0;
                        try
                        {
                            Temp = Convert.ToDouble(TemperatureVal.Value);
                        }
                        catch { }

                        Temp = Math.Round(Temp, 0);
                        if (Temp == -0)
                        {
                            Temp = 0;
                        }

                        if (Temp <= -15) { Smiley = "🥶"; }
                        if (Temp > -15 && Temp <= -10) { Smiley = "😖"; }
                        if (Temp > -10 && Temp <= -5) { Smiley = "😣"; }
                        if (Temp > -5 && Temp <= 0) { Smiley = "😬"; }
                        if (Temp > 0 && Temp <= 5) { Smiley = "😕"; }
                        if (Temp > 5 && Temp <= 10) { Smiley = "😏"; }
                        if (Temp > 10 && Temp <= 20) { Smiley = "😌"; }
                        if (Temp > 20 && Temp <= 25) { Smiley = "☺️"; }
                        if (Temp > 25) { Smiley = "🥵"; }

                        if (WeatherValue == "ясно") { SmileyWeather = "☀️"; }
                        if (WeatherValue == "небольшая облачность" || WeatherValue == "переменная облачность") { SmileyWeather = "🌤"; }
                        if (WeatherValue == "облачно с прояснениями") { SmileyWeather = "🌥"; }
                        if (WeatherValue == "пасмурно") { SmileyWeather = "☁️"; }
                        if (WeatherValue == "небольшой дождь") { SmileyWeather = "🌦"; }
                        if (WeatherValue == "небольшой проливной дождь") { SmileyWeather = "🌧"; }
                        if (WeatherValue == "гроза" || WeatherValue == "гроза с дождём" || WeatherValue == "гроза с небольшим дождём" || WeatherValue == "гроза с сильным дождём") { SmileyWeather = "⛈"; }
                        if (WeatherValue == "небольшой снег" || WeatherValue == "небольшой снегопад") { SmileyWeather = "🌨"; }
                        if (WeatherValue == "сильный снег" || WeatherValue == "снегопад" || WeatherValue == "снег") { SmileyWeather = "❄️"; }
                        if (WeatherValue == "туман" || WeatherValue == "плотный туман") { SmileyWeather = "🌫"; }

                        if (SmileyWeather == "") { SmileyWeather = "❔"; }

                        try { WeatherValue = WeatherValue.Substring(0, 1).ToUpper() + WeatherValue.Substring(1); } catch { }
                        Text = $"{Text}\n\n{Country} {City}: {Temp}°C {Smiley}\n{SmileyWeather} {WeatherValue}";

                    }
                    else
                    {
                        Text = $"Что-то я не понял какой город вы хотите. Я умею только эти:\n" +
                            $"/weather_moscow - Москва;\n" +
                            $"/weather_im - Императорские Мытищи;\n" +
                            $"/weather_antalya - Анталия;\n";
                    }
                }
                await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"{Text}");
            }
            catch
            {
                await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"К сожалению произошла ошибка, попробуйте чуточку позже 😔");
            }
            return;
        }
        if (message.Text != null)
        {
            if (message.Text.StartsWith("/val_")
                || message.Text == buttonRUBtoAUD.ToLower()
                || message.Text == buttonRUBtoAZN.ToLower()
                || message.Text == buttonRUBtoGBP.ToLower()
                || message.Text == buttonRUBtoAMD.ToLower()
                || message.Text == buttonRUBtoBYN.ToLower()
                || message.Text == buttonRUBtoBGN.ToLower()
                || message.Text == buttonRUBtoBRL.ToLower()
                || message.Text == buttonRUBtoHUF.ToLower()
                || message.Text == buttonRUBtoHKD.ToLower()
                || message.Text == buttonRUBtoDKK.ToLower()
                || message.Text == buttonRUBtoUSD.ToLower()
                || message.Text == buttonRUBtoEUR.ToLower()
                || message.Text == buttonRUBtoINR.ToLower()
                || message.Text == buttonRUBtoKZT.ToLower()
                || message.Text == buttonRUBtoCAD.ToLower()
                || message.Text == buttonRUBtoKGS.ToLower()
                || message.Text == buttonRUBtoCNY.ToLower()
                || message.Text == buttonRUBtoMDL.ToLower()
                || message.Text == buttonRUBtoNOK.ToLower()
                || message.Text == buttonRUBtoPLN.ToLower()
                || message.Text == buttonRUBtoRON.ToLower()
                || message.Text == buttonRUBtoXDR.ToLower()
                || message.Text == buttonRUBtoSGD.ToLower()
                || message.Text == buttonRUBtoTJS.ToLower()
                || message.Text == buttonRUBtoTRY.ToLower()
                || message.Text == buttonRUBtoTMT.ToLower()
                || message.Text == buttonRUBtoUZS.ToLower()
                || message.Text == buttonRUBtoUAH.ToLower()
                || message.Text == buttonRUBtoCZK.ToLower()
                || message.Text == buttonRUBtoSEK.ToLower()
                || message.Text == buttonRUBtoCHF.ToLower()
                || message.Text == buttonRUBtoZAR.ToLower()
                || message.Text == buttonRUBtoKRW.ToLower()
                || message.Text == buttonRUBtoJPY.ToLower())
            {
                var mes = await botClient.SendTextMessageAsync(message.Chat, "Секунду, взламываю сайта ЦБ, чтобы узнать для Вас курс! 📊", disableNotification: true);
                try
                {
                    try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
                    string Value = "";
                    string Icon = "";
                    string IDCirrency = "";
                    string Nominal = "";
                    string Name = "";

                    int ChekMes = 0;

                    if (message.Text.StartsWith("/val_aud") || message.Text == buttonRUBtoAUD.ToLower())
                    {
                        Icon = "$";
                        IDCirrency = "R01010";
                        Name = buttonRUBtoAUD;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_azn") || message.Text == buttonRUBtoAZN.ToLower())
                    {
                        Icon = "₼";
                        IDCirrency = "R01020A";
                        Name = buttonRUBtoAZN;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_gbp") || message.Text == buttonRUBtoGBP.ToLower())
                    {
                        Icon = "£";
                        IDCirrency = "R01035";
                        Name = buttonRUBtoGBP;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_amd") || message.Text == buttonRUBtoAMD.ToLower())
                    {
                        Icon = "֏";
                        IDCirrency = "R01060";
                        Name = buttonRUBtoAMD;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_byn") || message.Text == buttonRUBtoBYN.ToLower())
                    {
                        Icon = "Br";
                        IDCirrency = "R01090B";
                        Name = buttonRUBtoBYN;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_bgn") || message.Text == buttonRUBtoBGN.ToLower())
                    {
                        Icon = "BGN";
                        IDCirrency = "R01100";
                        Name = buttonRUBtoBGN;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_brl") || message.Text == buttonRUBtoBRL.ToLower())
                    {
                        Icon = "R$";
                        IDCirrency = "R01115";
                        Name = buttonRUBtoBRL;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_huf") || message.Text == buttonRUBtoHUF.ToLower())
                    {
                        Icon = "F";
                        IDCirrency = "R01135";
                        Name = buttonRUBtoHUF;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_hkd") || message.Text == buttonRUBtoHKD.ToLower())
                    {
                        Icon = "HK$";
                        IDCirrency = "R01200";
                        Name = buttonRUBtoHKD;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_dkk") || message.Text == buttonRUBtoDKK.ToLower())
                    {
                        Icon = "Kr";
                        IDCirrency = "R01215";
                        Name = buttonRUBtoDKK;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_usd") || message.Text == buttonRUBtoUSD.ToLower())
                    {
                        Icon = "$";
                        IDCirrency = "R01235";
                        Name = buttonRUBtoUSD;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_eur") || message.Text == buttonRUBtoEUR.ToLower())
                    {
                        Icon = "€";
                        IDCirrency = "R01239";
                        Name = buttonRUBtoEUR;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_inr") || message.Text == buttonRUBtoINR.ToLower())
                    {
                        Icon = "₹";
                        IDCirrency = "R01270";
                        Name = buttonRUBtoINR;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_kzt") || message.Text == buttonRUBtoKZT.ToLower())
                    {
                        Icon = "₸";
                        IDCirrency = "R01335";
                        Name = buttonRUBtoKZT;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_cad") || message.Text == buttonRUBtoCAD.ToLower())
                    {
                        Icon = "$";
                        IDCirrency = "R01350";
                        Name = buttonRUBtoCAD;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_kgs") || message.Text == buttonRUBtoKGS.ToLower())
                    {
                        Icon = "с";
                        IDCirrency = "R01370";
                        Name = buttonRUBtoKGS;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_cny") || message.Text == buttonRUBtoCNY.ToLower())
                    {
                        Icon = "Y";
                        IDCirrency = "R01375";
                        Name = buttonRUBtoCNY;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_mdl") || message.Text == buttonRUBtoMDL.ToLower())
                    {
                        Icon = "L";
                        IDCirrency = "R01500";
                        Name = buttonRUBtoMDL;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_nok") || message.Text == buttonRUBtoNOK.ToLower())
                    {
                        Icon = "NKr";
                        IDCirrency = "R01535";
                        Name = buttonRUBtoNOK;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_pln") || message.Text == buttonRUBtoPLN.ToLower())
                    {
                        Icon = "zł";
                        IDCirrency = "R01565";
                        Name = buttonRUBtoPLN;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_ron") || message.Text == buttonRUBtoRON.ToLower())
                    {
                        Icon = "L";
                        IDCirrency = "R01585F";
                        Name = buttonRUBtoRON;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_xdr") || message.Text == buttonRUBtoXDR.ToLower())
                    {
                        Icon = "XDR";
                        IDCirrency = "R01589";
                        Name = buttonRUBtoXDR;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_sgd") || message.Text == buttonRUBtoSGD.ToLower())
                    {
                        Icon = "S$";
                        IDCirrency = "R01625";
                        Name = buttonRUBtoSGD;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_tjs") || message.Text == buttonRUBtoTJS.ToLower())
                    {
                        Icon = "с";
                        IDCirrency = "R01670";
                        Name = buttonRUBtoTJS;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_try") || message.Text == buttonRUBtoTRY.ToLower())
                    {
                        Icon = "₺";
                        IDCirrency = "R01700J";
                        Name = buttonRUBtoTRY;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_tmt") || message.Text == buttonRUBtoTMT.ToLower())
                    {
                        Icon = "T";
                        IDCirrency = "R01710A";
                        Name = buttonRUBtoTMT;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_uzs") || message.Text == buttonRUBtoUZS.ToLower())
                    {
                        Icon = "UZS";
                        IDCirrency = "R01717";
                        Name = buttonRUBtoUZS;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_uah") || message.Text == buttonRUBtoUAH.ToLower())
                    {
                        Icon = "₴";
                        IDCirrency = "R01720";
                        Name = buttonRUBtoUAH;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_czk") || message.Text == buttonRUBtoCZK.ToLower())
                    {
                        Icon = "Kč";
                        IDCirrency = "R01760";
                        Name = buttonRUBtoCZK;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_sek") || message.Text == buttonRUBtoSEK.ToLower())
                    {
                        Icon = "kr";
                        IDCirrency = "R01770";
                        Name = buttonRUBtoSEK;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_chk") || message.Text == buttonRUBtoCHF.ToLower())
                    {
                        Icon = "₣";
                        IDCirrency = "R01775";
                        Name = buttonRUBtoCHF;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_zar") || message.Text == buttonRUBtoZAR.ToLower())
                    {
                        Icon = "R";
                        IDCirrency = "R01810";
                        Name = buttonRUBtoZAR;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_krw") || message.Text == buttonRUBtoKRW.ToLower())
                    {
                        Icon = "₩";
                        IDCirrency = "R01815";
                        Name = buttonRUBtoKRW;
                        ChekMes++;
                    }
                    if (message.Text.StartsWith("/val_jpy") || message.Text == buttonRUBtoJPY.ToLower())
                    {
                        Icon = "¥";
                        IDCirrency = "R01820";
                        Name = buttonRUBtoJPY;
                        ChekMes++;
                    }

                    if (ChekMes > 0)
                    {
                        var SplitVal = message.Text.Split(' ').Last();
                        float Mng = 1;
                        try
                        {
                            Mng = Convert.ToSingle(SplitVal.Replace(",", "."));
                        }
                        catch { }

                        WebClient client = new WebClient();
                        var xml = client.DownloadString("https://www.cbr-xml-daily.ru/daily.xml");
                        XDocument xdoc = XDocument.Parse(xml);
                        var el = xdoc.Element("ValCurs").Elements("Valute");
                        Value = el.Where(x => x.Attribute("ID").Value == IDCirrency).Select(x => x.Element("Value").Value).FirstOrDefault();
                        Nominal = el.Where(x => x.Attribute("ID").Value == IDCirrency).Select(x => x.Element("Nominal").Value).FirstOrDefault();
                        Value = Value.Substring(0, Value.Length - 2);

                        double ValueCor = Convert.ToDouble(Value.Replace(",", "."));
                        int NominalCor = Convert.ToInt32(Nominal);
                        if (NominalCor > 1)
                        {
                            ValueCor = ValueCor / NominalCor;
                            ValueCor = Math.Round(ValueCor, 2);
                        }
                        ValueCor = ValueCor * Mng;
                        ValueCor = Math.Round(ValueCor, 2);
                        Value = Convert.ToString(ValueCor).Replace(".", ",");
                        string CorMng = Convert.ToString(Mng).Replace(".", ",");
                        await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"{CorMng}{Icon} = {Value}₽\n{Name}");
                    }
                    else
                    {
                        await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"Такую валюту я не умею обрабатывать 😔");
                    }
                }
                catch
                {
                    await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"К сожалению произошла ошибка, попробуйте чуточку позже 😔");
                }
                return;
            }
        }
        if (message.Text.StartsWith("/keyboard_valut"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }

            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                    {
                            new KeyboardButton[] { buttonDeleteKeyboard },
                            new KeyboardButton[] { buttonRUBtoAUD },
                            new KeyboardButton[] { buttonRUBtoAZN },
                            new KeyboardButton[] { buttonRUBtoGBP },
                            new KeyboardButton[] { buttonRUBtoAMD },
                            new KeyboardButton[] { buttonRUBtoBYN },
                            new KeyboardButton[] { buttonRUBtoBGN },
                            new KeyboardButton[] { buttonRUBtoBRL },
                            new KeyboardButton[] { buttonRUBtoHUF },
                            new KeyboardButton[] { buttonRUBtoHKD },
                            new KeyboardButton[] { buttonRUBtoDKK },
                            new KeyboardButton[] { buttonRUBtoUSD },
                            new KeyboardButton[] { buttonRUBtoEUR },
                            new KeyboardButton[] { buttonRUBtoINR },
                            new KeyboardButton[] { buttonRUBtoKZT },
                            new KeyboardButton[] { buttonRUBtoCAD },
                            new KeyboardButton[] { buttonRUBtoKGS },
                            new KeyboardButton[] { buttonRUBtoCNY },
                            new KeyboardButton[] { buttonRUBtoMDL },
                            new KeyboardButton[] { buttonRUBtoNOK },
                            new KeyboardButton[] { buttonRUBtoPLN },
                            new KeyboardButton[] { buttonRUBtoRON },
                            new KeyboardButton[] { buttonRUBtoXDR },
                            new KeyboardButton[] { buttonRUBtoSGD },
                            new KeyboardButton[] { buttonRUBtoTJS },
                            new KeyboardButton[] { buttonRUBtoTRY },
                            new KeyboardButton[] { buttonRUBtoTMT },
                            new KeyboardButton[] { buttonRUBtoUZS },
                            new KeyboardButton[] { buttonRUBtoUAH },
                            new KeyboardButton[] { buttonRUBtoCZK },
                            new KeyboardButton[] { buttonRUBtoSEK },
                            new KeyboardButton[] { buttonRUBtoCHF },
                            new KeyboardButton[] { buttonRUBtoZAR },
                            new KeyboardButton[] { buttonRUBtoKRW },
                            new KeyboardButton[] { buttonRUBtoJPY },
                            new KeyboardButton[] { buttonDeleteKeyboard },
                        })
            {
                ResizeKeyboard = true
            };
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, "Клавиатура валют разблокирована", disableNotification: true, replyMarkup: replyKeyboardMarkup);
            return;
        }
        if (message.Text.StartsWith("/bidon") || message.Text.StartsWith("ботя, где байден"))
        {
            await botClient.SendLocationAsync(message.Chat, latitude: 38.8976763f, longitude: -77.0365298f, disableNotification: true);
            Random rnd = new Random();
            var Value = rnd.Next(1, 10);
            switch (Value)
            {
                case 1:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден в своем white house, на чиле, на расслабоне...", disableNotification: true); break; }
                case 2:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден опять приветствует пустоту на выступлении в white house...", disableNotification: true); break; }
                case 3:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден пошел покакать...", disableNotification: true); break; }
                case 4:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден на совещании...", disableNotification: true); break; }
                case 5:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден сидит в кресле и думает о тебе...", disableNotification: true); break; }
                case 6:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден сидит и играет в игрушки...", disableNotification: true); break; }
                case 7:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден потерялся в white house...", disableNotification: true); break; }
                case 8:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден все там же, сидит и делает вид, что работает...", disableNotification: true); break; }
                case 9:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден у себя дома, пытается понять, что такое ЕМИАС...", disableNotification: true); break; }
                case 10:
                    { await botClient.SendTextMessageAsync(message.Chat, $"Байден дома, выбирает, что заказать на алике...", disableNotification: true); break; }
            }

            return;
        }
        if (message.Text.StartsWith("/what_katya"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            await botClient.SendTextMessageAsync(message.Chat, "Параметры гнома:\n" +
               "• Имя - Екатеринка Тороторкина;\n" +
               "• Рост - 150 см;\n" +
               "• Восраст - 8 годиков;\n" +
               "• Увлечения - нюхать кошечек (ей кажется, что они пахнут печеньками);\n" +
               "• Место работы - там где есть помещение для челяди;\n" +
               "• Прозвища - Котенок, Солнышко, Туполобик, Любимая, Оладушек;\n" +
               "• Непонятное свойство - почему-то до сих пор не ищет работу на hh.ru;\n" +
               "\nP.S. Чуть не забыл о самом главном.. Катя маленькая!😘");
            return;
        }
        if (message.Text.StartsWith("/stop_bot"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
            if (message.Chat.Type != ChatType.Private)
            {
                if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Эта команда только для администрации чата..", disableNotification: true);
                    return;
                }
            }
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, "Я на паузе на 5 секунд...", disableNotification: true);
            for (int i = 4; i > 0; i--)
            {
                await Task.Delay(1000);
                await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"Я на паузе на {i} секунд...");
            }
            await Task.Delay(1000);
            await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"Оп! Я Снова в строю, сейчас посмотрю, что вы мне писали 🙃");
            await Task.Delay(2000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            return;
        }
        if (message.Text.StartsWith("/ping"))
        {
            //try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }

            try
            {
                var SplitMes = message.Text.Split(' ');
                string ValueRepeat = message.Text.Split(' ').Last();
                var IPAdr = "";
                if (SplitMes[0] == "/ping_my")
                {
                    try
                    {
                        MySqlBase.Open();
                        string cmdsql = $"SELECT saveurl FROM BDUser WHERE id = '{message.From.Id}';";
                        MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                        IPAdr = command.ExecuteScalar().ToString();
                        MySqlBase.Close();
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка, попробуйте чуточку позже..", disableNotification: true);
                        MySqlBase.Close();
                        return;
                    }

                    try
                    {
                        MySqlBase.Open();
                        string cmdsql = $"SELECT saveurlval FROM BDUser WHERE id = '{message.From.Id}';";
                        MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                        ValueRepeat = command.ExecuteScalar().ToString();
                        MySqlBase.Close();
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка, попробуйте чуточку позже..", disableNotification: true);
                        MySqlBase.Close();
                        return;
                    }


                }
                else
                {
                    IPAdr = SplitMes[1];
                }
                if (IPAdr == "" || IPAdr == "/ping")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вы не указали IP адресс!", disableNotification: true);
                    return;
                }
                if (ValueRepeat == "" || ValueRepeat == IPAdr || ValueRepeat == "/ping_my")
                {
                    ValueRepeat = "1";
                }
                int ValueRepeatInt = Convert.ToInt32(ValueRepeat);
                var MessegeText = $"Проверяем: {IPAdr}";
                var MessegeBot = await botClient.SendTextMessageAsync(message.Chat.Id, $"Проверяем: {IPAdr}\nПожалуйста подождите...", disableNotification: true);
                var Status = "";
                if (ValueRepeatInt > 30)
                {
                    ValueRepeatInt = 30;
                }
                if (ValueRepeatInt < 1)
                {
                    ValueRepeatInt = 1;
                }
                try
                {
                    for (int i = 1; i <= ValueRepeatInt; i++)
                    {

                        Ping ping = new Ping();
                        PingReply pingReply = ping.Send(IPAdr);
                        if (pingReply.Status.ToString() == "Success")
                        {
                            Status = "Успех! ✅";
                        }
                        else
                        {
                            if (pingReply.Status.ToString() == "TimedOut")
                            {
                                Status = "Таймаут ⌛";
                            }
                            else
                            {
                                Status = pingReply.Status.ToString();
                            }
                        }
                        MessegeText = $"{MessegeText}\n\n" +
                            $"Попытка: {i} из {ValueRepeatInt}\n" +
                            $"Время ответа: {pingReply.RoundtripTime}\n" +
                            $"Статус: {Status}\n" +
                            $"IP: {pingReply.Address}";
                    }
                    await botClient.EditMessageTextAsync(message.Chat, MessegeBot.MessageId, $"{MessegeText}");
                }
                catch { await botClient.EditMessageTextAsync(message.Chat, MessegeBot.MessageId, $"{MessegeText}\nПроизошла ошибка, проверьте правильность введеных Вами данных..."); }


            }
            catch { await botClient.SendTextMessageAsync(message.Chat.Id, $"{message.Text}\nПроизошла ошибка, проверьте правильность введеных Вами данных..."); }
            return;
        }
        if (message.Text.StartsWith("/ticket_svo_try"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            string HereAir = "SVO";
            string ThereAir = "AYT";
            var Date = DateTime.Now;
            Date = Date.AddDays(1);
            string DateUser = Date.ToString("dd.MM.yyyy");
            string Tomorrow = Date.ToString("ddMM");
            string Url = $"https://www.aviasales.ru/search/{HereAir}{Tomorrow}{ThereAir}1?origin_airports=0";
            InlineKeyboardMarkup inlineKeyboard = new(new[] { InlineKeyboardButton.WithUrl(text: "Посмотреть ➡️", url: Url) });
            Message sentMessage = await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Смотри какие рейсы на {DateUser}\nв город Анталья 🇹🇷 из Шереметьево 🇷🇺", replyMarkup: inlineKeyboard, disableNotification: true);

            return;
        }
        if (message.Text.StartsWith("/game_one"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }

            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "game_one_stone"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "game_one_scissors"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "game_one_paper"),
                },
            });
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, "Игра \"Камень, ножницы, бумага\". Выберите действие на клавиатуре!", disableNotification: true, replyMarkup: inlineKeyboard);
            return;
        }
        if (message.Text.StartsWith("/game_two"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }

            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Подкинуть монетку 🪙", callbackData: "game_two_play"),
                },
            });
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, "Игра \"Орел или решка\". Просто подкинь менетку ⬇️", disableNotification: true, replyMarkup: inlineKeyboard);
            return;
        }
        if (message.Text.StartsWith("/random"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonRnd10, callbackData: "random_10"),
                    InlineKeyboardButton.WithCallbackData(text: buttonRnd100, callbackData: "random_100"),
                    InlineKeyboardButton.WithCallbackData(text: buttonRnd1000, callbackData: "random_1000"),
                },
            });
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, "Случайное число. Выберите диапозон.", disableNotification: true, replyMarkup: inlineKeyboard);
            return;
        }

        if (message.Text.StartsWith("/update_user"))
        {
            try
            {
                MySqlBase.Open();
                try
                {
                    string cmdsql = $"INSERT INTO BDUser (id, username, firstname, lastname) VALUES ('{message.From.Id}', '{message.From.Username}', '{message.From.FirstName}', '{message.From.LastName}');";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                    await botClient.SendTextMessageAsync(message.Chat, $"Данные по пользователю добавлены!", disableNotification: true);
                }
                catch
                {
                    string cmdsql = $"UPDATE BDUser SET username = '{message.From.Username}', firstname = '{message.From.FirstName}', lastname = '{message.From.LastName}' WHERE id = '{message.From.Id}';";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                    await botClient.SendTextMessageAsync(message.Chat, $"Данные по пользователю актуализированы!", disableNotification: true);
                }
            }
            catch { }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/bd_show"))
        {
            //try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            string Text = "";
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDUser;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                MySqlDataReader reader = command.ExecuteReader();
                Text = $"БД Пользователей:\n" +
                    $"ID|Usename|FirstName|LastName|SaveURL|SaveURLVal|TestMes\n";
                while (reader.Read())
                {
                    string id = reader.GetString("id");
                    string username = reader.GetString("username");
                    string firstname = reader.GetString("firstname");
                    string lastname = reader.GetString("lastname");
                    string saveurl = reader.GetString("saveurl");
                    string saveurlval = reader.GetString("saveurlval");
                    string TestMes = reader.GetString("TestMes");

                    if (id == "") { id = "-"; }
                    if (username == "") { username = "-"; } else { username = $"@{username}"; }
                    if (firstname == "") { firstname = "-"; }
                    if (lastname == "") { lastname = "-"; }
                    if (saveurl == "") { saveurl = "-"; }
                    if (saveurlval == "") { saveurlval = "-"; }

                    Text = $"{Text}{id}|{username}|{firstname}|{lastname}|{saveurl}|{saveurlval}|{TestMes}\n";
                }
                MySqlBase.Close();
                MySqlBase.Open();
                cmdsql = $"SELECT * FROM BDGroup;";
                command = new MySqlCommand(cmdsql, MySqlBase);
                reader = command.ExecuteReader();
                Text = $"{Text}\nБД Групп:\n" +
                    $"ID|Title|Type|Market|AutoCurrency|AutoWeatherLoc\n";
                while (reader.Read())
                {
                    string id = reader.GetString("id");
                    string title = reader.GetString("title");
                    string type = reader.GetString("type");
                    string market = reader.GetString("market");
                    string auto_currency = reader.GetString("auto_currency");
                    string auto_weather_loc = reader.GetString("auto_weather_loc");

                    if (id == "") { id = "-"; }
                    if (title == "") { title = "-"; }
                    if (type == "") { type = "-"; }
                    if (market == "") { market = "-"; }

                    Text = $"{Text}{id}|{title}|{type}|{market}|{auto_currency}|{auto_weather_loc}\n";
                }
                await botClient.SendTextMessageAsync(message.Chat, $"{Text}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Произошла ошибка..", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/save_url"))
        {
            try
            {
                string value = message.Text.Split(' ').Last();
                string[] SplitUrl = message.Text.Split(' ');
                string url = SplitUrl[1];
                if (value == "" || value == url)
                {
                    value = "1";
                }
                try
                {
                    MySqlBase.Open();
                    string cmdsql = $"UPDATE BDUser SET saveurl = '{url}', saveurlval = '{value}' WHERE id = '{message.From.Id}';";
                    MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                    await botClient.SendTextMessageAsync(message.Chat, $"URL \"{url}\" - сохранен!\nКоличество проверок - {value}\nЧтобы быстро пингануть, введите /ping_my", disableNotification: true);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
                }
            }
            catch { await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Проверьте правильность введенных вами данных..", disableNotification: true); }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/help"))
        {
            string Text = $"Список основных команд:\n" +
                $"/start - перезапуск бота;\n" +
                $"\n" +
                $"/my_disk - показывает все Ваши файлы;\n" +
                $"/delete_disk - удаляет все Ваши файлы;\n" +
                $"/delete_file - удаляет один файл;\n" +
                $"\n" +
                $"/mute - заблокировать пользователя;\n" +
                $"/rmute - разблокировать пользователя;\n" +
                $"/status_user - информация по пользователю;\n" +
                $"\n" +
                $"/weather_all - погода везде;\n" +
                $"/weather_moscow - погода в Москве;\n" +
                $"/weather_im - погода в ИМ;\n" +
                $"/weather_antalya - погода в Анталии;\n" +
                $"\n" +
                $"/keyboard_valut - клавиатура валют;\n" +
                $"/delete_keyboard - заблокировать клавиатуру;\n" +
                $"\n" +
                $"/game_one - игра с ботом №1;\n" +
                $"/game_two - игра с ботом №2;\n" +
                $"/duel_one - игра с пользователем;\n" +
                $"\n" +
                $"/ping - пинг сайта/IP;\n" +
                $"/save_url - сохранение сайта/IP;\n" +
                $"/ping_my - пинг сохраненного сайта/IP;\n" +
                $"\n" +
                $"/ticket_svo_try - авиабилеты в Анталию;\n" +
                $"/bidon - где Байден!?;\n" +
                $"/random - случайные числа;\n" +
                $"/info - полная информация по всему;\n" +
                $"/update_user - обновить данные пользователя в БД;\n" +
                $"/update_group - обновить данные группы в БД;\n" +
                $"/val_usd - курс валюты;\n" +
                $"/auto_val_on - включить автоперевод валют;\n" +
                $"/auto_val_off - отключить автоперевод валют;\n" +
                $"/auto_weather_loc_on - включить погоду по геопозиции;\n" +
                $"/auto_weather_loc_off - отключить погоду по геопозиции;\n";

            InlineKeyboardMarkup inlineKeyboard = new(new[] { InlineKeyboardButton.WithUrl(text: "Подробное описание команд ➡️", url: "https://evgeny-fidel.ru/cmdtgbotded/") });
            Message sentMessage = await botClient.SendTextMessageAsync(message.Chat.Id, Text, replyMarkup: inlineKeyboard, disableNotification: true);
            return;
        }


        await MessageParsing(message);
        return;
    }
    // Ниже только для групп и супергрупп
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        message.Text = message.Text.ToLower();
        if (message.Text.StartsWith("/start"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            InlineKeyboardMarkup inlineKeyboard = new(new[] { InlineKeyboardButton.WithUrl(text: "Описание всех команд ➡️", url: "https://evgeny-fidel.ru/cmdtgbotded/") });
            Message sentMessage = await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: Hello, replyMarkup: inlineKeyboard, disableNotification: true);

            return;
        }
        if (message.Text.StartsWith("/mute"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            if (message.ReplyToMessage == null) // Проверка об ответном сообщении
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Вы не указали пользователя!", disableNotification: true);
                return;
            }
            ChatMember chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
            ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
            if (message.ReplyToMessage.From.Username.ToLower() == "evgeny_fidel_bot") // Проверка не бот ли это
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"}" +
                    $" Вы меня пытаетесь заблокировать? Ха-ха, очень смешно...", disableNotification: true);
                return;
            }
            if (chatMember.Status == ChatMemberStatus.Creator) // Проверка на создателя
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} он создатель группы!{"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"} Ты действительно думаешь, что у меня хватит смелости его замутить? Ну уж нет.. Я умываю руки..", disableNotification: true);
                return;
            }
            if (chatMember.Status == ChatMemberStatus.Administrator) // Проверка на админа
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} явлеется администратором данной группы, замутить его невозможно (а жаль..)", disableNotification: true);
                return;
            }

            var SplitMes = message.Text.Split(' ').Last();
            SplitMes = Regex.Replace(SplitMes, @"\D+", "");

            var MesWar = "";
            int MaxMinuteMember = 1;
            int MuteMinute;
            if (chatMemberYou.Status == ChatMemberStatus.Administrator || chatMemberYou.Status == ChatMemberStatus.Creator)
            {
                if (SplitMes == "")
                {
                    MuteMinute = 1;
                }
                else
                {
                    MuteMinute = Convert.ToInt32(SplitMes);
                }
            }
            else
            {
                if (SplitMes == "")
                {
                    MuteMinute = 1;
                }
                else
                {
                    MuteMinute = Convert.ToInt32(SplitMes);
                    if (MuteMinute > MaxMinuteMember)
                    {
                        MuteMinute = MaxMinuteMember;
                        int Opachki = Convert.ToInt32(SplitMes);
                        MesWar = $"Только администрация чата может заблокировать больше чем на {MaxMinuteMember} минут, а Вы хотели на {Opachki} минут.";
                    }
                }
            }
            await botClient.RestrictChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id, permissions: new ChatPermissions
            {
                CanSendMessages = false,
                CanSendMediaMessages = false,
                CanSendOtherMessages = false
            }, DateTime.UtcNow.AddMinutes(MuteMinute));
            await botClient.SendTextMessageAsync(message.Chat, $"У {"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} " +
                $"заблокированы пальцы в чате на {MuteMinute} минут\n" +
                $"Скажем спасибо {"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"}\n" +
                $"{MesWar}",
                disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId);
            return;
        }
        if (message.Text.StartsWith("/rmute"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
            if (message.ReplyToMessage == null) // Проверка об ответном сообщении
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Вы не указали пользователя!", disableNotification: true);
                return;
            }
            ChatMember chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
            if (message.ReplyToMessage.From.Username.ToLower() == "evgeny_fidel_bot") // Проверка не бот ли это
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"}" +
                    $" Вы меня пытаетесь разблокировать? Ха-ха, очень смешно...", disableNotification: true);
                return;
            }
            if (chatMember.Status == ChatMemberStatus.Creator) // Проверка на создателя
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} он создатель группы! Ало..", disableNotification: true);
                return;
            }
            if (chatMember.Status == ChatMemberStatus.Administrator) // Проверка на админа
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} явлеется администратором данной группы", disableNotification: true);
                return;
            }
            if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"} я рад за Вашу доблесть и отвагу, но {"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} может разблокировать только администрация чата!", disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId);
                return;
            }
            await botClient.RestrictChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id, permissions: new ChatPermissions
            {
                CanSendMessages = true,
                CanSendMediaMessages = true,
                CanSendOtherMessages = true
            });
            await botClient.SendTextMessageAsync(message.Chat, $"У {"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"} разблокированы пальцы в чате", disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId);
            return;
        }
        if (message.Text.StartsWith("/status_user"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            if (message.ReplyToMessage == null)
            {
                await botClient.SendTextMessageAsync(message.Chat, $"{"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"} Вы не указали пользователя!", disableNotification: true);
                return;
            }
            ChatMember chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
            await botClient.SendTextMessageAsync(message.Chat, $"" +
                $"Информация по пользователю\n" +
                $"Username: {"@" + message.ReplyToMessage.From.Username ?? "Неизвестно"}\n" +
                $"Имя: {message.ReplyToMessage.From.FirstName ?? "Неизвестно"} {message.ReplyToMessage.From.LastName}\n" +
                $"Роль в чате: {chatMember.Status}\n" +
                $"ID: {message.ReplyToMessage.From.Id}\n",
                disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId);
        }
        if (message.Text.StartsWith("/market_off"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDGroup SET market = '0' WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                await Task.Delay(1000);
                await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/market_on"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDGroup SET market = '1' WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                await Task.Delay(1000);
                await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/update_group"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            await HandleMember(botClient, update, update.Message);
            return;
        }
        if (message.Text.StartsWith("/duel_one"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            if (message.ReplyToMessage == null) // Проверка об ответном сообщении
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Вы не указали пользователя!", disableNotification: true);
                return;
            }
            if (message.ReplyToMessage.From.Username.ToLower() == "evgeny_fidel_bot") // Проверка не бот ли это
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Чтобы поиграть со мной, введите /game_one", disableNotification: true);
                return;
            }
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "duel_one_player_one_stone"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "duel_one_player_one_scissors"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "duel_one_player_one_paper"),
                },
            });
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"Дамы и Господа! У нас тут дуэль!\n" +
                $"Играем в \"Камень, ножницы, бумага\".\n\n" +
                $"Игрок №1 - \"{"@" + message.From.Username ?? $"{message.From.FirstName} {message.From.LastName}"}\"\n" +
                $"Игрок №2 - \"{"@" + message.ReplyToMessage.From.Username ?? $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName}"}\"\n" +
                $"\nИ так, начнем!" +
                $"\nИгрок №1, выберите действие ⬇️", disableNotification: true, replyMarkup: inlineKeyboard);
            return;
        }

        if (message.Text.StartsWith("/auto_val_off"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDGroup SET auto_currency = '0' WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                await Task.Delay(1000);
                await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/auto_val_on"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDGroup SET auto_currency = '1' WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                await Task.Delay(1000);
                await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/auto_weather_loc_off"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDGroup SET auto_weather_loc = '0' WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                await Task.Delay(1000);
                await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Text.StartsWith("/auto_weather_loc_on"))
        {
            try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
            try
            {
                MySqlBase.Open();
                string cmdsql = $"UPDATE BDGroup SET auto_weather_loc = '1' WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                command.ExecuteNonQuery();
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                await Task.Delay(1000);
                await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
    }

    MySqlBase.Close();
    return;
}

async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    string buttonStone = "✊ Камень";
    string buttonScissors = "✌️ Ножницы";
    string buttonPaper = "🖐 Бумага";
    string buttonRnd10 = "0 - 10 🔢";
    string buttonRnd100 = "0 - 100 🔢";
    string buttonRnd1000 = "0 - 1000 🔢";

    switch (callbackQuery.Data)
    {
        case "game_one_stone":
        case "game_one_scissors":
        case "game_one_paper":
            {
                var TextMes = "Игра \"Камень, ножницы, бумага\". Выберите действие на клавиатуре!\n\n";
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "game_one_stone"),
                        InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "game_one_scissors"),
                        InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "game_one_paper"),
                    },
                });
                string ChoiceBot = "";
                Random rnd = new Random();
                var Value = rnd.Next(1, 99);

                if (Value <= 33) { ChoiceBot = buttonStone; }
                if (Value > 33 && Value <= 66) { ChoiceBot = buttonScissors; }
                if (Value > 66) { ChoiceBot = buttonPaper; }


                int ValueWin = 0;
                int ValueDraw = 0;
                int ValueLoser = 0;
                int ValueAll = 0;
                string MesSplit = callbackQuery.Message.Text.Split('-').Last();

                if (MesSplit != "Игра \"Камень, ножницы, бумага\". Выберите действие на клавиатуре!")
                {
                    MesSplit = MesSplit.Replace("Победы: ", "");
                    MesSplit = MesSplit.Replace("Ничья: ", "");
                    MesSplit = MesSplit.Replace("Проигрыши: ", "");
                    string[] V = MesSplit.Split("\n");
                    ValueWin = Convert.ToInt32(V[1]);
                    ValueDraw = Convert.ToInt32(V[2]);
                    ValueLoser = Convert.ToInt32(V[3]);
                }
                TextMes = $"{TextMes}Я выбираю \"{ChoiceBot}\"\n\n";
                if (ChoiceBot == buttonStone && callbackQuery.Data == "game_one_stone")
                {
                    TextMes = $"{TextMes}⚪ О! Ты тоже выбрал камень! У нас ничья!";
                    ValueDraw++;
                }
                if (ChoiceBot == buttonScissors && callbackQuery.Data == "game_one_stone")
                {
                    TextMes = $"{TextMes}🟢 Ай-ай-ай! Ты у ничтожил мои ножницы.. Ты выиграл!";
                    ValueWin++;
                }
                if (ChoiceBot == buttonPaper && callbackQuery.Data == "game_one_stone")
                {
                    TextMes = $"{TextMes}🔴 Все! Теперь ты мой! Я выиграл!";
                    ValueLoser++;
                }
                if (ChoiceBot == buttonStone && callbackQuery.Data == "game_one_scissors")
                {
                    TextMes = $"{TextMes}🔴 Мой камень бьет твои ножницы! Я выиграл!";
                    ValueLoser++;
                }
                if (ChoiceBot == buttonScissors && callbackQuery.Data == "game_one_scissors")
                {
                    TextMes = $"{TextMes}⚪ О! У тебя тоже ножницы! Хороший выбор! У нас ничья!";
                    ValueDraw++;
                }
                if (ChoiceBot == buttonPaper && callbackQuery.Data == "game_one_scissors")
                {
                    TextMes = $"{TextMes}🟢 О нет! Твои ножницы сделали во мне столько дыр.. Ты выиграл!";
                    ValueWin++;
                }
                if (ChoiceBot == buttonStone && callbackQuery.Data == "game_one_paper")
                {
                    TextMes = $"{TextMes}🟢 Ты меня завернул, мне не пошевелиться! Ты выиграл!";
                    ValueWin++;
                }
                if (ChoiceBot == buttonScissors && callbackQuery.Data == "game_one_paper")
                {
                    TextMes = $"{TextMes}🔴 Извини конечно, но я порвал тебя в клочья! Я выиграл!";
                    ValueLoser++;
                }
                if (ChoiceBot == buttonPaper && callbackQuery.Data == "game_one_paper")
                {
                    TextMes = $"{TextMes}⚪ Давай обнимемся, у нас ничья!";
                    ValueDraw++;
                }
                ValueAll = ValueWin + ValueDraw + ValueLoser;
                TextMes = $"{TextMes}\n\n-----------Счет-----------\nПобеды: {ValueWin}\nНичья: {ValueDraw}\nПроигрыши: {ValueLoser}\nВсего: {ValueAll}";

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "game_two_play":
        case "game_two_x10":
        case "game_two_x50":
        case "game_two_x100":
            {
                var TextMes = "Игра \"Орел или решка\". Просто подкинь менетку ⬇️\n\n";
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Подкинуть монетку 🪙", callbackData: "game_two_play"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "x10", callbackData: "game_two_x10"),
                        InlineKeyboardButton.WithCallbackData(text: "x50", callbackData: "game_two_x50"),
                        InlineKeyboardButton.WithCallbackData(text: "x100", callbackData: "game_two_x100"),
                    },
                });
                string ChoiceBot = "";
                int ValueWin = 0;
                int ValueLoser = 0;
                int ValueAll = 0;
                int Repeat = 1;
                if (callbackQuery.Data == "game_two_x10") { Repeat = 10; }
                if (callbackQuery.Data == "game_two_x50") { Repeat = 50; }
                if (callbackQuery.Data == "game_two_x100") { Repeat = 100; }

                for (int i = 0; i < Repeat; i++)
                {
                    Random rnd = new Random();
                    var Value = rnd.Next(0, 100);

                    if (Value % 2 == 0)
                    {
                        ChoiceBot = "Орел 🦅";
                    }
                    else
                    {
                        ChoiceBot = "Решка 🌰";
                    }
                    if (ChoiceBot == "Орел 🦅")
                    {
                        ValueWin++;
                    }
                    if (ChoiceBot == "Решка 🌰")
                    {
                        ValueLoser++;
                    }
                }

                string MesSplit = callbackQuery.Message.Text.Split('-').Last();

                if (MesSplit != "Игра \"Орел или решка\". Просто подкинь менетку ⬇️")
                {
                    MesSplit = MesSplit.Replace("Орел: ", "");
                    MesSplit = MesSplit.Replace("Решка: ", "");
                    string[] V = MesSplit.Split("\n");
                    ValueWin = ValueWin + Convert.ToInt32(V[1]);
                    ValueLoser = ValueLoser + Convert.ToInt32(V[2]);
                }
                TextMes = $"{TextMes}Тебе выпадает \"{ChoiceBot}\"\n\n";

                ValueAll = ValueWin + ValueLoser;
                TextMes = $"{TextMes}-----------Счет-----------\nОрел: {ValueWin}\nРешка: {ValueLoser}\nВсего: {ValueAll}";

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_stone":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerOne)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text.Replace("1,", "2,");
                TextMes = TextMes.Replace("И так, начнем!", "Отлично! Продалжаем!");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "duel_one_player_one_stone_player_two_stone"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "duel_one_player_one_stone_player_two_scissors"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "duel_one_player_one_stone_player_two_paper"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_scissors":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerOne)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text.Replace("1,", "2,");
                TextMes = TextMes.Replace("И так, начнем!", "Отлично! Продалжаем!");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "duel_one_player_one_scissors_player_two_stone"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "duel_one_player_one_scissors_player_two_scissors"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "duel_one_player_one_scissors_player_two_paper"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_paper":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerOne)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text.Replace("1,", "2,");
                TextMes = TextMes.Replace("И так, начнем!", "Отлично! Продалжаем!");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "duel_one_player_one_paper_player_two_stone"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "duel_one_player_one_paper_player_two_scissors"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "duel_one_player_one_paper_player_two_paper"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_stone_player_two_stone":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }

                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", "У Вас ничья! Победила дружба, так сказать 😁");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_stone_player_two_scissors":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"Под громкие авации, выигрывает игрок №1 🏆\n\nПочему?\nИгрок №1 - выбрал {buttonStone}\nИгрок №2 - выбрал {buttonScissors}");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_stone_player_two_paper":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"Под громкие авации, выигрывает игрок №2 🏆\n\nПочему?\nИгрок №1 - выбрал {buttonStone}\nИгрок №2 - выбрал {buttonPaper}");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_scissors_player_two_stone":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"Под громкие авации, выигрывает игрок №2 🏆\n\nПочему?\nИгрок №1 - выбрал {buttonScissors}\nИгрок №2 - выбрал {buttonStone}");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_scissors_player_two_scissors":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"У Вас ничья! Победила дружба, так сказать 😁");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_scissors_player_two_paper":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"Под громкие авации, выигрывает игрок №1 🏆\n\nПочему?\nИгрок №1 - выбрал {buttonScissors}\nИгрок №2 - выбрал {buttonPaper}");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_paper_player_two_stone":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"Под громкие авации, выигрывает игрок №1 🏆\n\nПочему?\nИгрок №1 - выбрал {buttonPaper}\nИгрок №2 - выбрал {buttonStone}");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_paper_player_two_scissors":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"Под громкие авации, выигрывает игрок №2 🏆\n\nПочему?\nИгрок №1 - выбрал {buttonPaper}\nИгрок №2 - выбрал {buttonScissors}");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_player_one_paper_player_two_paper":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = callbackQuery.Message.Text;
                TextMes = TextMes.Replace("Отлично! Продалжаем!", "Ах, вот и результаты!");
                TextMes = TextMes.Replace("Игрок №2, выберите действие ⬇️", $"У Вас ничья! Победила дружба, так сказать 😁");
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Играть еще!", callbackData: "duel_one_reset"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "duel_one_reset":
            {
                string[] NamePlayerSplit = callbackQuery.Message.Text.Split('"');
                string NamePlayerOne = NamePlayerSplit[3];
                string NamePlayerTwo = NamePlayerSplit[5];
                string ChekSelectPlayer = $"{"@" + callbackQuery.From.Username ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}"}";
                if (ChekSelectPlayer != NamePlayerOne && ChekSelectPlayer != NamePlayerTwo)
                {
                    break;
                }
                var TextMes = $"Дамы и Господа! У нас тут дуэль!\n" +
                    $"Играем в \"Камень, ножницы, бумага\".\n\n" +
                    $"Игрок №1 - \"{NamePlayerOne}\"\n" +
                    $"Игрок №2 - \"{NamePlayerTwo}\"\n" +
                    $"\nИ так, начнем!" +
                    $"\nИгрок №1, выберите действие ⬇️";
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonStone, callbackData: "duel_one_player_one_stone"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonScissors, callbackData: "duel_one_player_one_scissors"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonPaper, callbackData: "duel_one_player_one_paper"),
                },
            });

                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
        case "random_10":
        case "random_100":
        case "random_1000":
            {
                var TextMes = $"Случайное число. Выберите диапозон.\n\n";
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: buttonRnd10, callbackData: "random_10"),
                    InlineKeyboardButton.WithCallbackData(text: buttonRnd100, callbackData: "random_100"),
                    InlineKeyboardButton.WithCallbackData(text: buttonRnd1000, callbackData: "random_1000"),
                },
            });
                Random rnd = new Random();
                int ValueB = 10;
                if (callbackQuery.Data == "random_100") { ValueB = 100; }
                if (callbackQuery.Data == "random_1000") { ValueB = 1000; }
                var Value = rnd.Next(0, ValueB);
                try { await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, $"{TextMes}Вам выпало: {Value}", replyMarkup: inlineKeyboard); } catch { }
                break;
            }
    }

    return;
}

async Task HandleDocument(ITelegramBotClient botClient, Message message)
{
    if (Doki == true)
    {
        if (message.Chat.Type == ChatType.Private)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "Спасибо! Сохраню у себя на сервере 😋", disableNotification: true);
        }
        Console.WriteLine($"{message.From.Id} - @{message.From.Username} | Файл |{message.Document.FileName}");
        var fileInfo = await botClient.GetFileAsync(message.Document.FileId);
        var filePath = fileInfo.FilePath;
        string directoryDesctop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string directoryDesctopTGBot = $@"{directoryDesctop}/TGBotDED/BDUserFile";
        Directory.CreateDirectory(directoryDesctopTGBot);
        string directoryUsername = $@"{directoryDesctopTGBot}/{message.From.Id}";

        Directory.CreateDirectory(directoryUsername);

        message.Document.FileName = message.Document.FileName.Replace(" ", "_");
        message.Document.FileName = message.Document.FileName.Replace("(", "");
        message.Document.FileName = message.Document.FileName.Replace(")", "");
        message.Document.FileName = message.Document.FileName.Replace("й", "и");

        string destinationFilePath = $@"{directoryUsername}/{message.Document.FileName}";
        if (System.IO.File.Exists(destinationFilePath))
        {
            //System.IO.File.Delete(destinationFilePath);
            var TypeFile = message.Document.FileName.Split('.').Last();
            var NewNameFile = message.Document.FileName.Replace($".{TypeFile}", $"_1.{TypeFile}");
            for (int i = 1; i < 10000; i++)
            {
                if (System.IO.File.Exists($@"{directoryUsername}/{NewNameFile}"))
                {
                    int y = i;
                    NewNameFile = NewNameFile.Replace($" ({y}).{TypeFile}", $"_{y + 1}.{TypeFile}");
                }
                else { break; }
            }

            destinationFilePath = $@"{directoryUsername}/{NewNameFile}";
        }
        await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
        await botClient.DownloadFileAsync(filePath, fileStream);
        fileStream.Close();
        Console.WriteLine($"Файл сохранен | {destinationFilePath}");
    }
    else
    {
        if (message.Chat.Type == ChatType.Private)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"На данный момент отключена возможность сохранения Ваших файлов..\n" +
                $"{autor} - этой мой разработчик, можете у него уточнить почему так..", disableNotification: true);
        }
    }
    await MessageParsing(message);
    return;
}

async Task HandlePhoto(ITelegramBotClient botClient, Message message)
{
    if (message.Chat.Type == ChatType.Private)
    {
        if (message.Caption.StartsWith("/say_all_users_test"))
        {
            var ID = "";
            var Text = message.Caption.Replace("/say_all_users_test ", "");
            Text = message.Caption.Replace("/say_all_users_test", "");
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDUser;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                //string name = command.ExecuteScalar().ToString();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var TestMes = reader.GetString("TestMes");
                    if (TestMes == "True")
                    {
                        try { await botClient.SendPhotoAsync(reader.GetString("id"), message.Photo[0].FileId, $"{Text}\n\nДля отключения тестовых сообщений, нажмите /test_mes_off"); ID += $"{reader.GetString("id")}\n"; } catch { }
                    }
                }
                await botClient.SendTextMessageAsync(message.Chat, $"Отправил:\n{ID}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ошибка", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Caption.StartsWith("/say_all_users"))
        {
            var ID = "";
            var Text = message.Caption.Replace("/say_all_users ", "");
            Text = message.Caption.Replace("/say_all_users", "");
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDUser;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                //string name = command.ExecuteScalar().ToString();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try { await botClient.SendPhotoAsync(reader.GetString("id"), message.Photo[0].FileId, $"{Text}"); ID += $"{reader.GetString("id")}\n"; } catch { }
                }
                await botClient.SendTextMessageAsync(message.Chat, $"Отправил:\n{ID}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ошибка", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }
        if (message.Caption.StartsWith("/say_all_group"))
        {
            var ID = "";
            var Text = message.Caption.Replace("/say_all_group ", "");
            Text = message.Caption.Replace("/say_all_group", "");
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDGroup;";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                //string name = command.ExecuteScalar().ToString();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var Market = reader.GetString("market");
                    if (Market == "True")
                    {
                        try { await botClient.SendPhotoAsync(reader.GetString("id"), message.Photo[0].FileId, $"{Text}"); ID += $"{reader.GetString("id")}\n"; } catch { }
                    }
                }
                await botClient.SendTextMessageAsync(message.Chat, $"Отправил:\n{ID}", disableNotification: true);
            }
            catch
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ошибка", disableNotification: true);
            }
            MySqlBase.Close();
            return;
        }

        await MessageParsing(message);
        return;
    }
}

async Task HandleMember(ITelegramBotClient botClient, Update update, Message message)
{
    try
    {
        string ChatID = "";
        string ChatTitle = "";
        string ChatTypeS = "";
        string Mes = "";

        int chek = 0;

        if (update.MyChatMember == null && message.Text != null || message.Type == MessageType.ChatTitleChanged)
        {
            ChatID = $"{message.Chat.Id}";
            ChatTitle = $"{message.Chat.Title}";
            ChatTypeS = $"{message.Chat.Type}";
            Mes = "✅ Настройки группы обновленны!";
            chek++;
        }
        else
        {
            if (update.MyChatMember.Chat.Type == ChatType.Group || update.MyChatMember.Chat.Type == ChatType.Supergroup)
            {
                ChatID = $"{update.MyChatMember.Chat.Id}";
                ChatTitle = $"{update.MyChatMember.Chat.Title}";
                ChatTypeS = $"{update.MyChatMember.Chat.Type}";
                Mes = "✅ Бот подключен!";
            }
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"INSERT INTO BDGroup (id, title, type) VALUES ('{ChatID}', '{ChatTitle}', '{ChatTypeS}');";
            MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
        }
        catch
        {
            string cmdsql = $"UPDATE BDGroup SET title = '{ChatTitle}', type = '{ChatTypeS}' WHERE id = '{ChatID}';";
            MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
        }
        MySqlBase.Close();

        var mes = await botClient.SendTextMessageAsync(ChatID, Mes, disableNotification: true);
        if (chek > 0)
        {
            await Task.Delay(1500);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
    }
    catch { }
    return;
}

async Task HandleLocation(ITelegramBotClient botClient, Message message)
{
    if (WeatherLoc == true)
    {
        bool chek = false;
        if (message.Chat.Type == ChatType.Private) { chek = true; }
        else
        {
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var Auto_currency = reader.GetString("auto_weather_loc");
                    if (Auto_currency == "True")
                    {
                        chek = true;
                    }
                }
            }
            catch { }
        }
        if (chek == true)
        {
            try
            {
                var Lat = message.Location.Latitude;
                var Lon = message.Location.Longitude;
                string url = $"https://api.openweathermap.org/data/2.5/weather?lat={Lat}&lon={Lon}&units=metric&mode=xml&appid={TokenWeather}&lang=ru";
                //https://api.openweathermap.org/data/2.5/weather?lat=36.92928&lon=30.701937&units=metric&mode=xml&appid=66c03fe8ef1ef87c9a5fb4104d848418&lang=ru
                string Smiley = "";
                string SmileyWeather = "";

                WebClient client = new WebClient();
                var xml = client.DownloadString(url);
                XDocument xdoc = XDocument.Parse(xml);
                XElement? Temperature = xdoc.Element("current").Element("temperature");
                XAttribute? TemperatureVal = Temperature.Attribute("value");

                XElement? Weather = xdoc.Element("current").Element("weather");
                XAttribute? WeatherVal = Weather.Attribute("value");

                XElement? Humidity = xdoc.Element("current").Element("humidity");
                XAttribute? HumidityVal = Humidity.Attribute("value");

                XElement? Pressure = xdoc.Element("current").Element("pressure");
                XAttribute? PressureVal = Pressure.Attribute("value");
                double PressureValue = Convert.ToDouble(PressureVal.Value) * 0.750064;
                PressureValue = Math.Round(PressureValue, 0);

                XElement? Wind = xdoc.Element("current").Element("wind").Element("speed");
                XAttribute? WindVal = Wind.Attribute("value");

                var WeatherValue = WeatherVal.Value;
                double Temp = 0;
                try
                {
                    Temp = Convert.ToDouble(TemperatureVal.Value);
                }
                catch { }

                Temp = Math.Round(Temp, 0);
                if (Temp == -0)
                {
                    Temp = 0;
                }

                if (Temp <= -15) { Smiley = "🥶"; }
                if (Temp > -15 && Temp <= -10) { Smiley = "😖"; }
                if (Temp > -10 && Temp <= -5) { Smiley = "😣"; }
                if (Temp > -5 && Temp <= 0) { Smiley = "😬"; }
                if (Temp > 0 && Temp <= 5) { Smiley = "😕"; }
                if (Temp > 5 && Temp <= 10) { Smiley = "😏"; }
                if (Temp > 10 && Temp <= 20) { Smiley = "😌"; }
                if (Temp > 20 && Temp <= 25) { Smiley = "☺️"; }
                if (Temp > 25) { Smiley = "🥵"; }

                if (WeatherValue == "ясно") { SmileyWeather = "☀️"; }
                if (WeatherValue == "небольшая облачность" || WeatherValue == "переменная облачность") { SmileyWeather = "🌤"; }
                if (WeatherValue == "облачно с прояснениями") { SmileyWeather = "🌥"; }
                if (WeatherValue == "пасмурно") { SmileyWeather = "☁️"; }
                if (WeatherValue == "небольшой дождь") { SmileyWeather = "🌦"; }
                if (WeatherValue == "небольшой проливной дождь") { SmileyWeather = "🌧"; }
                if (WeatherValue == "гроза" || WeatherValue == "гроза с дождём" || WeatherValue == "гроза с небольшим дождём" || WeatherValue == "гроза с сильным дождём") { SmileyWeather = "⛈"; }
                if (WeatherValue == "небольшой снег" || WeatherValue == "небольшой снегопад") { SmileyWeather = "🌨"; }
                if (WeatherValue == "сильный снег" || WeatherValue == "снегопад" || WeatherValue == "снег") { SmileyWeather = "❄️"; }
                if (WeatherValue == "туман" || WeatherValue == "плотный туман") { SmileyWeather = "🌫"; }

                if (SmileyWeather == "") { SmileyWeather = "❔"; }

                try { WeatherValue = WeatherValue.Substring(0, 1).ToUpper() + WeatherValue.Substring(1); } catch { }
                string Text = $"{Smiley} В данном районе: {Temp}°C\n💦 Влажность: {HumidityVal.Value}%\n🧭 Давление: {PressureValue} мм рт. ст.\n💨 Скорость ветра: {WindVal.Value} м/с\n{SmileyWeather} {WeatherValue}";

                await botClient.SendTextMessageAsync(message.Chat, Text, disableNotification: true, replyToMessageId: message.MessageId);
            }
            catch { }
        }
    }
    MySqlBase.Close();
    return;
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Ошибка:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;

}

void showTime(Object obj)
{
    //Console.WriteLine($"Проверка обновлений | Auto_Update_Minute = {AutoUpdateMinete} | {DateTime.Now.ToString("dd.MM.yy | HH:mm:ss")}");
    try
    {
        //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        using (var client = new WebClient())
        using (var stream = client.OpenRead("http://www.google.com"))
            if (client.DownloadString("https://gaffer-prog.evgeny-fidel.ru/tgbotded/").Contains(version) != true)
            {
                try
                {
                    Console.WriteLine("Вышла новая версия бота! Начинаем обновление..");
                    client.DownloadFile("https://gaffer-prog.evgeny-fidel.ru/download/386/", DirectoryProg + @"/Update TGBotDED.zip");
                    client.DownloadFile("https://gaffer-prog.evgeny-fidel.ru/download/110/", DirectoryProg + @"/UpdaterProg.exe");
                    Process.Start(DirectoryProg + @"/UpdaterProg.exe");
                    Environment.Exit(0);
                    //Console.WriteLine($"Версия не актуальная! | {DateTime.Now.ToString("dd.MM.yy | HH:mm:ss")}");
                }
                catch { }
            }
            else
            {
                //Console.WriteLine($"Версия актуальная! | {DateTime.Now.ToString("dd.MM.yy | HH:mm:ss")}");
            }
    }
    catch { }
}

async Task MessageParsing(Message message)
{
    bool chek = false;
    string DoubleText = $"";
    try
    {
        if (message.Text != null) { DoubleText = DoubleText + message.Text; }
        if (message.Caption != null) { DoubleText = DoubleText + message.Caption; }
        DoubleText = DoubleText.Replace("\n", " ");
        string[] Text = DoubleText.Split(' ');
        string FinalMessage = "";

        for (int i = 0; i < Text.Length; i++)
        {
            if (AutoValRUB == true)
            {
                try
                {
                    string Icon = "";
                    string IDCirrency = "";
                    string Flag = "";
                    float Mng = 1;
                    int chekVal = 0;
                    if (Text[i].StartsWith("лир"))
                    {
                        Mng = Convert.ToSingle(Text[i - 1].Replace(",", "."));
                        Icon = "₺";
                        IDCirrency = "R01700J";
                        Flag = "🇹🇷";
                        chekVal++;
                    }
                    if (Text[i].StartsWith("доллар") || Text[i].StartsWith("бачей") || Text[i].StartsWith("бакс"))
                    {
                        Mng = Convert.ToSingle(Text[i - 1].Replace(",", "."));
                        Icon = "$";
                        IDCirrency = "R01235";
                        Flag = "🇺🇸";
                        chekVal++;
                    }
                    if (Text[i] == "евро" || Text[i].StartsWith("еврик"))
                    {
                        Mng = Convert.ToSingle(Text[i - 1].Replace(",", "."));
                        Icon = "€";
                        IDCirrency = "R01239";
                        Flag = "🇪🇺";
                        chekVal++;
                    }
                    if (message.Chat.Type == ChatType.Private) { chek = true; } else { ChekBDAutoCurrency(); }
                    if (chekVal > 0 && chek == true)
                    {
                        WebClient client = new WebClient();
                        var xml = client.DownloadString("https://www.cbr-xml-daily.ru/daily.xml");
                        XDocument xdoc = XDocument.Parse(xml);
                        var el = xdoc.Element("ValCurs").Elements("Valute");
                        string Value = el.Where(x => x.Attribute("ID").Value == IDCirrency).Select(x => x.Element("Value").Value).FirstOrDefault();
                        string Nominal = el.Where(x => x.Attribute("ID").Value == IDCirrency).Select(x => x.Element("Nominal").Value).FirstOrDefault();
                        Value = Value.Substring(0, Value.Length - 2);

                        double ValueCor = Convert.ToDouble(Value.Replace(",", "."));
                        int NominalCor = Convert.ToInt32(Nominal);
                        if (NominalCor > 1)
                        {
                            ValueCor = ValueCor / NominalCor;
                            ValueCor = Math.Round(ValueCor, 2);
                        }
                        ValueCor = ValueCor * Mng;
                        ValueCor = Math.Round(ValueCor, 2);
                        string CorValue = Convert.ToString(ValueCor).Replace(".", ",");
                        string CorMng = Convert.ToString(Mng).Replace(".", ",");
                        FinalMessage = $"{FinalMessage}\n{Flag} {CorMng}{Icon} = {CorValue}₽";
                    }
                }
                catch { }
            }
        }
        if (FinalMessage != "")
        {
            await botClient.SendTextMessageAsync(message.Chat, FinalMessage, disableNotification: true, replyToMessageId: message.MessageId);
        }
        else
        {
            if (message.Chat.Type == ChatType.Private && message.Document == null)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Я не понял, что ты хочешь =(\nПопробуй написать иначе!\n/start - команда для перезапуска;", disableNotification: true);
            }
        }
    }
    catch { }

    void ChekBDAutoCurrency()
    {
        try
        {
            MySqlBase.Open();
            string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new MySqlCommand(cmdsql, MySqlBase);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                var Auto_currency = reader.GetString("auto_currency");
                if (Auto_currency == "True")
                {
                    chek = true;
                }
            }
        }
        catch { }
    }
    MySqlBase.Close();
    return;
}