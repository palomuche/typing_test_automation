using Npgsql;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

class Program
{
    static void Main(string[] args)
    {
        // Configuração do WebDriver
        IWebDriver driver = new ChromeDriver();

        // Navegar para o site
        driver.Navigate().GoToUrl("https://10fastfingers.com/typing-test/portuguese");

        WaitForTimerToFinish(driver);

        Thread.Sleep(1000);

        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
        // Capturar os resultados
        IWebElement wpmElement = wait.Until(e => e.FindElement(By.CssSelector("#wpm strong")));
        IWebElement keystrokesElement = wait.Until(e => e.FindElement(By.CssSelector("#keystrokes .value")));
        IWebElement accuracyElement = wait.Until(e => e.FindElement(By.CssSelector("#accuracy strong")));
        IWebElement correctWordsElement = wait.Until(e => e.FindElement(By.CssSelector("#correct strong")));
        IWebElement wrongWordsElement = wait.Until(e => e.FindElement(By.CssSelector("#wrong strong")));

        // Extrair os textos dos elementos
        string wpm = wpmElement.Text.Split(" ")[0];
        string keystrokes = keystrokesElement.Text.Split('|')[0].Trim().Replace("(", "");
        string accuracy = accuracyElement.Text.Replace("%", "");
        string correctWords = correctWordsElement.Text;
        string wrongWords = wrongWordsElement.Text;

        // Salvar os resultados no banco de dados
        SaveToDatabase(wpm, keystrokes, accuracy, correctWords, wrongWords);

        // Fechar o navegador
        driver.Quit();
    }


    // Método para esperar até que o tempo chegue a 0:00
    static void WaitForTimerToFinish(IWebDriver driver)
    {
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        IWebElement timerElement = wait.Until(e => e.FindElement(By.Id("timer")));
        IWebElement inputField = wait.Until(e => e.FindElement(By.Id("inputfield")));

        while (driver.FindElement(By.Id("timer")).Text != "0:00")
        {
            TypeHighlightedWord(driver, inputField, 100);
        }
    }

    // Método para capturar e digitar a palavra destacada
    static void TypeHighlightedWord(IWebDriver driver, IWebElement inputField, int delayBetweenWordsInMillis)
    {
        try
        {
            // Encontrar a palavra destacada
            IWebElement highlightedWord = driver.FindElement(By.XPath("//span[@class='highlight']"));
            string word = highlightedWord.Text;

            // Digitar a palavra no campo de entrada
            inputField.SendKeys(word + " ");

            // Aguardar um curto intervalo entre as palavras
            Thread.Sleep(delayBetweenWordsInMillis);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ocorreu um erro: {e.Message}");
        }
    }
    
    // Método para salvar os valores no banco de dados
    static void SaveToDatabase(string wpm, string keystrokes, string accuracy, string correctWords, string wrongWords)
    {
        // Convertendo valores para os tipos apropriados
        int wpmValue = int.Parse(wpm);
        int keystrokesValue = int.Parse(keystrokes);
        double accuracyValue = double.Parse(accuracy)/100;
        int correctWordsValue = int.Parse(correctWords);
        int wrongWordsValue = int.Parse(wrongWords);

        // String de conexão
        string connectionString = "Host=localhost;Username=postgres;Password=Teste@123;Database=TypingDB";

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            using (NpgsqlCommand cmd = new NpgsqlCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = "INSERT INTO TypingScore (Date, WPM, Keystrokes, Accuracy, Correct_Words, Wrong_Words) VALUES (@date, @wpm, @keystrokes, @accuracy, @correctWords, @wrongWords)";
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                cmd.Parameters.AddWithValue("@wpm", wpmValue);
                cmd.Parameters.AddWithValue("@keystrokes", keystrokesValue);
                cmd.Parameters.AddWithValue("@accuracy", accuracyValue);
                cmd.Parameters.AddWithValue("@correctWords", correctWordsValue);
                cmd.Parameters.AddWithValue("@wrongWords", wrongWordsValue);
                cmd.ExecuteNonQuery();
            }
        }
    }

}
