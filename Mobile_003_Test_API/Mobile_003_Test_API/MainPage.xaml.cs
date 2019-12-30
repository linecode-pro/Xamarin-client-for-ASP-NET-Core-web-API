using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Mobile_003_Test_API
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        // Свойства - ВНИМАНИЕ! ИЗМЕНИТЬ ПАРАМЕТРЫ НА СВОИ !!!
        private string userEmail = "123@mail.ru";
        private string userPassword = "Password@123";
        private string connectionAuthenticationString = "https://mysite.site/api/AuthJWT";  // путь к API для авторизации и получения токена
        private string connectionAPIString = "https://mysite.site/api/TodoItemsAPI";   // путь к API для работы с задачами
        private string tokenString;

        // Список задач
        ObservableCollection<TodoItem> TodoItemsCollection = new ObservableCollection<TodoItem>();

        public MainPage()
        {
            InitializeComponent();

            this.userEmailBox.Text = userEmail;
            this.userPasswordBox.Text = userPassword;

            this.connectionAuthenticationStringBox.Text = connectionAuthenticationString;
            this.connectionAPIStringBox.Text = connectionAPIString;
        }

        private async void Button_GetToken_Clicked(object sender, EventArgs e)
        {
            userEmail = userEmailBox.Text.Trim();
            userPassword = userPasswordBox.Text.Trim();
            connectionAuthenticationString = connectionAuthenticationStringBox.Text.Trim();

            // Авторизоваться и получить токен
            string token = await GetToken();
            tokenString = token;
            tokenStringBox.Text = tokenString;
        }

        public async Task<string> GetToken()
        {
            // Тело запроса
            Dictionary<string, string> userData = new Dictionary<string, string>
            {
                { "Username", userEmail },
                { "Password", userPassword },
            };

            var jsonData = JsonConvert.SerializeObject(userData, Formatting.Indented);

            // Создать Http-клиент для подключения
            HttpClient client = new HttpClient();

            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                // Выполнить подключение к API, используя метод POST
                HttpResponseMessage response = await client.PostAsync(connectionAuthenticationString, content);

                // Отобразить текст ответа на странице UWP-приложения
                responseStringBox.Text = CreateAnswerResponseText(response);

                // Проверить, что получен правильный ответ от сервера
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    // Преобразовать ответ и получить из него token
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Dictionary<string, string> theJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);

                    string tokenString = theJson["token"];
                    return tokenString;
                }
                else
                {
                    return response.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string CreateAnswerResponseText(HttpResponseMessage response)
        {
            string responseText = $"Status code: {response.StatusCode.ToString()} \n" +
                $"\n" +
                $"Content: {response.Content.ToString()}";

            return responseText;
        }


        private async void Button_GetTasksListUsingToken_Clicked(object sender, EventArgs e)
        {
            // Для учебных целей возьмём строку токена из поля ввода
            tokenString = tokenStringBox.Text.Trim();

            // Получить список задач, используя полученный токен
            if (!String.IsNullOrEmpty(tokenString))
            {
                string result = await GetTasksListUsingTokenAsync(tokenString);
            }
        }

        public async Task<string> GetTasksListUsingTokenAsync(string access_token)
        {
            // Для учебных целей возьмём строку подключения из поля ввода
            connectionAPIString = connectionAPIStringBox.Text.Trim();

            // Очистить коллекцию задач
            TodoItemsCollection.Clear();

            // Создать Http-клиент для подключения
            HttpClient client = new HttpClient();

            //Добавить заголовок в наш GET-запрос, в котором прописать полученный ранее токен 
            var headers = client.DefaultRequestHeaders;
            headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

            try
            {
                // Выполнить подключение к API, используя метод GET
                Uri requestUri = new Uri(connectionAPIString);
                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                // Отобразить текст ответа на странице UWP-приложения
                responseStringBox.Text = CreateAnswerResponseText(httpResponse);

                // Проверить, что получен правильнй ответ от сервера
                httpResponse.EnsureSuccessStatusCode();

                // Преобразовать ответ и получить из него массив json-объектов (задач)
                string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                List<TodoItem> todoItemList = JsonConvert.DeserializeObject<List<TodoItem>>(httpResponseBody);

                // Перебрать полученный массив и создать из его элементов задачи - объекты класса TodoItem
                // Созданные объекты - поместить в коллекцию (для отображения на странице нашего UWP-приложения)
                foreach (var todoItem in todoItemList)
                {  
                    TodoItemsCollection.Add(todoItem);
                }

                //BindingContext = this;
                todoListView.ItemsSource = TodoItemsCollection;
                return "Ok!";
            }
            catch (Exception ex)
            {
                responseStringBox.Text = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                return "Error!";
            }
        }

        private async void Button_AddNewTodoItem_Clicked(object sender, EventArgs e)
        {
            string newTodoName = newTodoNameBox.Text.Trim();
            if (String.IsNullOrEmpty(newTodoName))
            {
                return;
            }
            string newTodoDescription = newTodoDescriptionBox.Text.Trim();

            // Для учебных целей возьмём строку токена из поля ввода
            tokenString = tokenStringBox.Text.Trim();

            // Получить список задач, используя полученный токен
            if (!String.IsNullOrEmpty(tokenString))
            {
                string result = await AddNewTodoItemUsingTokenAsync(tokenString, newTodoName, newTodoDescription);
            }
        }

        public async Task<string> AddNewTodoItemUsingTokenAsync(string access_token, string newTodoName, string newTodoDescription)
        {
            // Очистить коллекцию задач
            TodoItemsCollection.Clear();

            HttpClient client = new HttpClient();

            //Добавить заголовок в наш GET-запрос, в котором прописать полученный ранее токен 
            var headers = client.DefaultRequestHeaders;
            headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

            // Тело запроса
            Dictionary<string, string> newToDoData = new Dictionary<string, string>
            {
                { "Name", newTodoName },
                { "Description", newTodoDescription },
            };

            var jsonData = JsonConvert.SerializeObject(newToDoData, Formatting.Indented);

            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                // Выполнить подключение к API, используя метод POST
                Uri requestUri = new Uri(connectionAPIString);
                HttpResponseMessage httpResponse = await client.PostAsync(requestUri, content);

                // Отобразить текст ответа на странице UWP-приложения
                responseStringBox.Text = CreateAnswerResponseText(httpResponse);

                // Проверить, что получен правильнй ответ от сервера
                httpResponse.EnsureSuccessStatusCode();

                // Преобразовать ответ и получить из него json-объект (новую, только что созданную на веб-сервере задачу)
                string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                //JsonObject jsonObject = JsonObject.Parse(httpResponseBody);
                TodoItem todoItem = JsonConvert.DeserializeObject<TodoItem>(httpResponseBody);

                TodoItemsCollection.Add(todoItem);

                return "Ok!";
            }
            catch (Exception ex)
            {
                responseStringBox.Text = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                return "Error!";
            }
        }
    }

    public class TodoItem
    {
        public int Id { get; set; } // идентификатор задачи
        public string Name { get; set; } // название задачи
        public string Description { get; set; } // описание задачи
        public bool IsComplete { get; set; } // признак выполнена задача или нет
        public string UserId { get; set; } // идентификатор пользователя
        public DateTime CreationDate { get; set; } // дата создания задачи
    }
}
