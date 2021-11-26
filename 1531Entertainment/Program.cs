using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sendy.Client;
using MySql.Data.MySqlClient;
using Sendy.Client.Model;
using System.Text.RegularExpressions;

namespace SendyAPI
{
    class Program
    {

        // HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        static readonly HttpClient client = new HttpClient();

        static void Main()
        {
            List<string> removedLists = ListRemoval().GetAwaiter().GetResult();
            // Get the books posted today
            List<string> todaysBooks = GetBooks().GetAwaiter().GetResult();
            // Get the genres of the books posted today
            List<string> todaysGenres = GetGenres().GetAwaiter().GetResult();
            // Build the newsletter body
            List<string> htmlBody = BuildNewsletterBody().GetAwaiter().GetResult();

            List<SendyResponse> results = CreateNewsletter().GetAwaiter().GetResult();



        }

        public static async Task<List<string>> GetBooks()
        {
            List<string> todaysBooks = new List<string>();
            // Gathering the HTML from the home page of 1531entertainment
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.website.com/");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();


            StreamReader reader = new StreamReader(response.GetResponseStream());

            string stringOfReader = reader.ReadToEnd().ToString();

            reader.Close();

            // Field that each book sits in
            string[] bookDelimiter = { "<article id=" };
            string appendMe = "<article id=";

            string[] allGenres = { "Audiobooks", "Movies", "Childrens", "Cozy Mysteries", "Fantasy / Sci-Fi / Spec.", "Historical Fiction", "Mystery / Thriller / Action Adventure", "Romance - Amish", "Romance - Contemporary", "Romance - Historical", "Romance - Suspense / Mystery", "Womens Fiction", "Young Adult / New Adult", "Christian Living", "Memoir", "Self-Help" };
            string[] allBooks = stringOfReader.Split(bookDelimiter, StringSplitOptions.None);
            // Only return the books that were added today
            string[] newBooks = Array.FindAll(allBooks, element => element.Contains(DateTime.Today.ToString("yyyy-MM-dd")));

            // Repairing HTML broken by the split and adding them to a list to return
            foreach (string brokenBook in newBooks)
            {
                string fixedBook = brokenBook.Insert(0, appendMe);
                todaysBooks.Add(fixedBook);
            }

            return (todaysBooks);



        }
        public static async Task<List<string>> GetGenres()
        {
            List<string> todaysBooks = GetBooks().GetAwaiter().GetResult();
            string[] todaysBooksAsArray = todaysBooks.ToArray();
            List<string> todaysGenres = new List<string>();

            string[] allGenres = { "Audiobooks", "Movies", "Childrens", "Cozy Mysteries", "Fantasy / Sci-Fi / Spec.", "Historical Fiction", "Mystery / Thriller / Action Adventure", "Romance - Amish", "Romance - Contemporary", "Romance - Historical", "Romance - Suspense / Mystery", "Womens Fiction", "Young Adult / New Adult", "Christian Living", "Memoir", "Self Help" };


            foreach (string genre in allGenres)
            {
                string[] containsGenre = Array.FindAll(todaysBooksAsArray, element => element.Contains(genre));

                if (containsGenre.Length != 0)
                {
                    todaysGenres.Add(genre);
                }
            }
            return (todaysGenres);
        }
        private static async Task<List<string>> DBQuery(string query)
        {
            string connectionString = "connection_string;";
            MySqlConnection db = new MySqlConnection(connectionString);

            // Open connection
            db.Open();



            // Create a list to store the result
            List<string> subscribers = new List<string>();

            // Create Command
            MySqlCommand cmd = new MySqlCommand(query, db);
            // Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();

            //Read the data and store them in the list
            while (dataReader.Read())
            {
                subscribers.Add(dataReader["email"].ToString());
            }

            // close Data Reader
            dataReader.Close();

            // close Connection
            db.Close();

            return subscribers;
        }
        private static long DBListCreate(string query)
        {
            string connectionString = "connection_string;";
            MySqlConnection db = new MySqlConnection(connectionString);

            // Open connection
            db.Open();



            // Create Command
            MySqlCommand cmd = new MySqlCommand(query, db);
            // Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();

            //Read the ID of the last record created
            long result = cmd.LastInsertedId;

            // close Data Reader
            dataReader.Close();

            // close Connection
            db.Close();

            return result;
        }
        private static void DBSubsToLists(string query)
        {
            string connectionString = "connection_string;";
            MySqlConnection db = new MySqlConnection(connectionString);

            // Open connection
            db.Open();



            // Create Command
            MySqlCommand cmd = new MySqlCommand(query, db);
            // Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();


            // close Data Reader
            dataReader.Close();

            // close Connection
            db.Close();

        }
        public static async Task<List<string>> BuildNewsletterBody()
        {
            List<string> todaysBooks = GetBooks().GetAwaiter().GetResult();
            List<string> htmlBody = new List<string>();
            List<string> htmlBooks = new List<string>();
            string[] bookDelimiter = { "<div class=\"news-block-content\">" };
            string appendMe = "<div class=\"news-block-content\">";
            foreach (string book in todaysBooks)
            {
                string pattern = @"href=""(?:(?:https?|ftp|file):\/\/|www\.|ftp\.)(?:\([-A-Z0-9+&@#\/%=~_|$?!:,.]*\)|[-A-Z0-9+&@#\/%=~_|$?!:,.])*(?:\([-A-Z0-9+&@#\/%=~_|$?!:,.]*\)|[A-Z0-9+&@#\/%=~_|$])";
                RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline;
                string result = Regex.Match(book, pattern, options).ToString();
                string bookWithoutAmazonLinks = Regex.Replace(book, pattern, result, options).ToString();
                string[] allBooks = bookWithoutAmazonLinks.Split(bookDelimiter, StringSplitOptions.None);

                foreach (string brokenBook in allBooks)
                {
                    string lessBrokenBook = brokenBook.Replace("\n\t\t\t\n", "");
                    string fixedBook = lessBrokenBook.Insert(0, appendMe);
                    htmlBooks.Add(fixedBook);
                }
            }
            string[] afterBook = { "<img class=\"wp-image-885\"", "***Please note:" };

            foreach (string bookHtml in htmlBooks)
            {
                string[] removeEnd = bookHtml.Split(afterBook, StringSplitOptions.None);
                foreach (string brokenHtml in removeEnd)
                {
                    if (brokenHtml.StartsWith("<div class=\"news-block-content\"><div class"))
                    {
                        htmlBody.Add(brokenHtml);
                    }
                }
            }
            Console.WriteLine(htmlBody[1]);
            return htmlBody;
        }

        public static async Task<List<SendyResponse>> CreateNewsletter()
        {

            // A place to hold the results
            List<SendyResponse> results = new List<SendyResponse>();

            // Get the Genres of the books posted today
            List<string> todaysGenres = GetGenres().GetAwaiter().GetResult();


            List<KeyValuePair<string[], long>> genreAndListId = await GenerateLists(todaysGenres);
            foreach (KeyValuePair<string[], long> kvp in genreAndListId)
            {
                SendyResponse result = await EmailCreation(kvp.Key, kvp.Value);
                results.Add(result);
            }


            return results;
        }

        private static async Task<SendyResponse> EmailCreation(string[] genres, long listId)
        {
            List<int> htmlIntList = new List<int> { };

            // Get the Books posted today
            List<string> todaysBooks = GetBooks().GetAwaiter().GetResult();
            string[] todaysBooksAsArray = todaysBooks.ToArray();


            List<string> htmlBody = BuildNewsletterBody().GetAwaiter().GetResult();
            foreach (string book in todaysBooksAsArray)
            {
                foreach (string genre in genres)
                {
                    if (book.Contains(genre)) { htmlIntList.Add(todaysBooks.IndexOf(book)); };
                }
            }
            string genresTostring = String.Join(" ", genres);



            int brandId = 3;
            string subject = "Today's Uplifting Offerings from {domain}! - " + System.DateTime.Now.ToString("MMMM dd");
            string email = "admin@{domain}.com";
            string fromName = "{domain}";
            string plainText = "";



            List<string> htmlTextList = new List<string>();
            foreach (int index in htmlIntList) { htmlTextList.Add(htmlBody[index]); }
            string htmlText = string.Join("", htmlTextList);
            string headerFile = ".\\Static Newsletter Pieces\\Header.html";
            string footerFile = ".\\Static Newsletter Pieces\\Footer.html";
            string htmlHeader = File.ReadAllText(headerFile);
            string htmlFooter = File.ReadAllText(footerFile);
            string htmlHeaderFixed = htmlHeader.Replace("\r\n", "");
            string htmlFooterFixed = htmlFooter.Replace("\r\n", "");


            string fullHtmlBody = string.Concat(htmlHeader, htmlText, htmlFooter);


            var sendyClient = new SendyClient(new Uri("https://www.{domain}.com/sendy/"), "{sendy_secret}");
            var campaign = new Campaign
            {
                BrandId = brandId,
                FromEmail = email,
                FromName = fromName,
                HtmlText = fullHtmlBody,
                PlainText = plainText,
                Querystring = "",
                ReplyTo = email,
                Subject = subject,
                Title = System.DateTime.Now.ToString("MMMM dd") + " " + genresTostring
            };
            // Create the lists and campaigns but don't send
            var result = await sendyClient.CreateCampaignAsync(campaign, false, null);
            
            // Create the lists and campaigns AND send the campaigns out doesn't work assuming that the list ID needs to be encrypted?
            //IEnumerable<string> idOfList = new string[] { listId.ToString() };
            //var result = await sendyClient.CreateCampaignAsync(campaign, true, new Groups(idOfList));
            return result;
        }

        private static async Task<List<KeyValuePair<string[], long>>> GenerateLists(List<string> todaysGenres)
        {

            List<KeyValuePair<string[], long>> genreAndListId = new List<KeyValuePair<string[], long>> { };

            // Match genres to ListId
            Dictionary<string, long> listId = new Dictionary<string, long>();
            listId.Add("Audiobooks", 21);
            listId.Add("Movies", 22);
            listId.Add("Childrens", 23);
            listId.Add("Cozy Mysteries", 24);
            listId.Add("Fantasy / Sci-Fi / Spec.", 25);
            listId.Add("Historical Fiction", 26);
            listId.Add("Mystery / Thriller / Action Adventure", 27);
            listId.Add("Romance - Amish", 28);
            listId.Add("Romance - Contemporary", 29);
            listId.Add("Romance - Historical", 30);
            listId.Add("Romance - Suspense / Mystery", 31);
            listId.Add("Womens Fiction", 32);
            listId.Add("Young Adult / New Adult", 33);
            listId.Add("Christian Living", 34);
            listId.Add("Memoir", 35);
            listId.Add("Self Help", 36);

            if (todaysGenres.Count == 1)
            {
                string[] genres = new string[] { };
                foreach (string genre in todaysGenres)
                {
                    genres = todaysGenres.ToArray();
                    genreAndListId.Add(new KeyValuePair<string[], long>(genres, listId[genre]));
                }
            }
            if (todaysGenres.Count == 2)
            {
                List<long> genreListIds = new List<long> { };
                foreach (string genre in todaysGenres)
                {
                    genreListIds.Add(listId[genre]);
                }
                long g1 = genreListIds[0];
                long g2 = genreListIds[1];
                List<string> genreOne = new List<string>();
                List<string> genreTwo = new List<string>();
                List<string> bothGenreOneandGenreTwo = new List<string>();
                List<string> justGenreOne = new List<string>();
                List<string> justGenreTwo = new List<string>();
                string queryOne = "SELECT * FROM subscribers WHERE list = '" + g1 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryTwo = "SELECT * FROM subscribers WHERE list = '" + g2 + "' AND unsubscribed = '0' AND bounced = '0'";
                genreOne = await DBQuery(queryOne);
                genreTwo = await DBQuery(queryTwo);
                foreach (string subg1 in genreOne)
                {
                    if (genreTwo.Contains(subg1))
                        {
                            bothGenreOneandGenreTwo.Add(subg1);
                        }
                        else
                        {
                            justGenreOne.Add(subg1);
                        }
                    
                }
                foreach (string subg2 in genreTwo)
                {
                    if (genreOne.Contains(subg2)) { }
                    else 
                    { 
                        justGenreTwo.Add(subg2); 
                    }
                }

                string g1OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";

                long g1OnlyListId = DBListCreate(g1OnlyList);
                long g2OnlyListId = DBListCreate(g2OnlyList);
                long g1AndG2ListId = DBListCreate(g1AndG2List);

                // Adding subs to newly created lists
                foreach (string g1Sub in justGenreOne)
                {
                    string g1SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1Sub + "', '" + g1OnlyListId + "');";
                    DBSubsToLists(g1SubQuery);
                }
                foreach (string g2Sub in justGenreTwo)
                {
                    string g2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2Sub + "', '" + g2OnlyListId + "');";
                    DBSubsToLists(g2SubQuery);
                }
                foreach (string g1AndG2Sub in bothGenreOneandGenreTwo)
                {
                    string g1AndG2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2Sub + "', '" + g1AndG2ListId + "');";
                    DBSubsToLists(g1AndG2SubQuery);
                }

                string[] g1Only = new string[] { todaysGenres[0] };
                string[] g2Only = new string[] { todaysGenres[1] };
                string[] g1AndG2 = new string[] { todaysGenres[0], todaysGenres[1] };
                genreAndListId.Add(new KeyValuePair<string[], long>(g1Only, g1OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2Only, g2OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2, g1AndG2ListId));
            }
            if (todaysGenres.Count == 3)
            {
                List<long> genreListIds = new List<long> { };
                foreach (string genre in todaysGenres)
                {
                    genreListIds.Add(listId[genre]);
                }
                long g1 = genreListIds[0];
                long g2 = genreListIds[1];
                long g3 = genreListIds[2];
                List<string> genreOne = new List<string>();
                List<string> genreTwo = new List<string>();
                List<string> genreThree = new List<string>();
                List<string> justGenreOneandGenreTwo = new List<string>();
                List<string> justGenreOneandGenreThree = new List<string>();
                List<string> justGenreTwoandGenreThree = new List<string>();
                List<string> justGenreOne = new List<string>();
                List<string> justGenreTwo = new List<string>();
                List<string> justGenreThree = new List<string>();
                List<string> genreOneTwoAndThree = new List<string>();
                string queryOne = "SELECT * FROM subscribers WHERE list = '" + g1 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryTwo = "SELECT * FROM subscribers WHERE list = '" + g2 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryThree = "SELECT * FROM subscribers WHERE list = '" + g3 + "' AND unsubscribed = '0' AND bounced = '0'";
                genreOne = await DBQuery(queryOne);
                genreTwo = await DBQuery(queryTwo);
                genreThree = await DBQuery(queryThree);
                foreach (string subg1 in genreOne)
                {
                    if (genreTwo.Contains(subg1))
                    {
                        if (genreThree.Contains(subg1)) { genreOneTwoAndThree.Add(subg1); }
                        else justGenreOneandGenreTwo.Add(subg1);
                    } 
                    
                    else
                        {
                           if (genreThree.Contains(subg1)) { justGenreOneandGenreThree.Add(subg1); }

                                else { justGenreOne.Add(subg1); }

                            }
                        }
                    
                
                foreach (string subg2 in genreTwo)
                {
                    if (genreOne.Contains(subg2)) { }
                        else
                        {
                            
                                if (genreThree.Contains(subg2)) { justGenreTwoandGenreThree.Add(subg2); }
                                else { justGenreTwo.Add(subg2); }

                            
                        }
                    }
                
                foreach (string subg3 in genreThree)
                {
                    
                        if (genreOne.Contains(subg3)) { }
                        else
                        {
                            if (genreThree.Contains(subg3)) { }
                                else { justGenreThree.Add(subg3); }
                            
                        }
                    
                }

                string g1OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";

                long g1OnlyListId = DBListCreate(g1OnlyList);
                long g2OnlyListId = DBListCreate(g2OnlyList);
                long g3OnlyListId = DBListCreate(g3OnlyList);
                long g1AndG2ListId = DBListCreate(g1AndG2List);
                long g1AndG3ListId = DBListCreate(g1AndG3List);
                long g2AndG3ListId = DBListCreate(g2AndG3List);
                long g1AndG2AndG3ListId = DBListCreate(g1AndG2AndG3List);

                string[] g1Only = new string[] { todaysGenres[0] };
                string[] g2Only = new string[] { todaysGenres[1] };
                string[] g3Only = new string[] { todaysGenres[2] };
                string[] g1AndG2 = new string[] { todaysGenres[0], todaysGenres[1] };
                string[] g1AndG3 = new string[] { todaysGenres[0], todaysGenres[2] };
                string[] g2AndG3 = new string[] { todaysGenres[1], todaysGenres[2] };
                string[] g1AndG2AndG3 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2] };

                genreAndListId.Add(new KeyValuePair<string[], long>(g1Only, g1OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2Only, g2OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3Only, g3OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2, g1AndG2ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3, g1AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3, g2AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3, g1AndG2AndG3ListId));

                foreach (string g1Sub in justGenreOne)
                {
                    string g1SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1Sub + "', '" + g1OnlyListId + "');";
                    DBSubsToLists(g1SubQuery);
                }
                foreach (string g2Sub in justGenreTwo)
                {
                    string g2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2Sub + "', '" + g2OnlyListId + "');";
                    DBSubsToLists(g2SubQuery);
                }
                foreach (string g3Sub in justGenreThree)
                {
                    string g3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3Sub + "', '" + g3OnlyListId + "');";
                    DBSubsToLists(g3SubQuery);
                }
                foreach (string g1AndG2Sub in justGenreOneandGenreTwo)
                {
                    string g1AndG2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2Sub + "', '" + g1AndG2ListId + "');";
                    DBSubsToLists(g1AndG2SubQuery);
                }
                foreach (string g1AndG3Sub in justGenreOneandGenreThree)
                {
                    string g1AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3Sub + "', '" + g1AndG3ListId + "');";
                    DBSubsToLists(g1AndG3SubQuery);
                }
                foreach (string g2AndG3Sub in justGenreTwoandGenreThree)
                {
                    string g2AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3Sub + "', '" + g2AndG3ListId + "');";
                    DBSubsToLists(g2AndG3SubQuery);
                }
                foreach (string g1AndG2AndG3Sub in genreOneTwoAndThree)
                {
                    string g1AndG2AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3Sub + "', '" + g1AndG2AndG3ListId + "');";
                    DBSubsToLists(g1AndG2AndG3SubQuery);
                }

            }
            if (todaysGenres.Count == 4)
            {
                List<long> genreListIds = new List<long> { };
                foreach (string genre in todaysGenres)
                {
                    genreListIds.Add(listId[genre]);
                }
                long g1 = genreListIds[0];
                long g2 = genreListIds[1];
                long g3 = genreListIds[2];
                long g4 = genreListIds[3];
                List<string> genreOne = new List<string>();
                List<string> genreTwo = new List<string>();
                List<string> genreThree = new List<string>();
                List<string> genreFour = new List<string>();
                List<string> justGenreOneandGenreTwo = new List<string>();
                List<string> justGenreOneandGenreThree = new List<string>();
                List<string> justGenreOneandGenreFour = new List<string>();
                List<string> justGenreTwoandGenreThree = new List<string>();
                List<string> justGenreTwoandGenreFour = new List<string>();
                List<string> justGenreThreeandGenreFour = new List<string>();
                List<string> justGenreOne = new List<string>();
                List<string> justGenreTwo = new List<string>();
                List<string> justGenreThree = new List<string>();
                List<string> justGenreFour = new List<string>();
                List<string> justGenreOneTwoAndThree = new List<string>();
                List<string> justGenreOneTwoAndFour = new List<string>();
                List<string> justGenreOneThreeAndFour = new List<string>();
                List<string> justGenreTwoThreeAndFour = new List<string>();
                List<string> genreOneTwoThreeAndFour = new List<string>();

                string queryOne = "SELECT * FROM subscribers WHERE list = '" + g1 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryTwo = "SELECT * FROM subscribers WHERE list = '" + g2 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryThree = "SELECT * FROM subscribers WHERE list = '" + g3 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryFour = "SELECT * FROM subscribers WHERE list = '" + g4 + "' AND unsubscribed = '0' AND bounced = '0'";

                genreOne = await DBQuery(queryOne);
                genreTwo = await DBQuery(queryTwo);
                genreThree = await DBQuery(queryThree);
                genreFour = await DBQuery(queryFour);
                foreach (string subg1 in genreOne)
                {

                    if (genreTwo.Contains(subg1))
                    {
                        if (genreThree.Contains(subg1))
                        {
                            if (genreFour.Contains(subg1)) { genreOneTwoThreeAndFour.Add(subg1); }

                            else { justGenreOneTwoAndThree.Add(subg1); }
                        }
                        else
                        {

                            if (genreFour.Contains(subg1)) { justGenreOneTwoAndFour.Add(subg1); }
                            else { justGenreOneandGenreTwo.Add(subg1); }

                        }
                    }


                    else
                    {
                        
                            if (genreThree.Contains(subg1))
                            {
                                
                                    if (genreFour.Contains(subg1)) { justGenreOneThreeAndFour.Add(subg1); }
                                    else { justGenreOneandGenreThree.Add(subg1); }
                                
                            }
                            else
                            {
                                
                                    if (genreFour.Contains(subg1)) { justGenreOneandGenreFour.Add(subg1); }
                                    else { justGenreOne.Add(subg1); }
                                
                            }
                        
                    }
                    }
                
                foreach (string subg2 in genreTwo)
                {
                   if (genreOne.Contains(subg2)) { }
                   else
                        {
                            
                                if (genreThree.Contains(subg2))
                                {
                                        if (genreFour.Contains(subg2)) { justGenreTwoThreeAndFour.Add(subg2); }
                                        else { justGenreTwoandGenreThree.Add(subg2); }
                                }
                                else
                                {
                                    if (genreFour.Contains(subg2)) { justGenreTwoandGenreFour.Add(subg2); }
                                    else { justGenreTwo.Add(subg2); }
                                    
                                }
                        }
                    }
                
                foreach (string subg3 in genreThree)
                {
                    if (genreOne.Contains(subg3)) { }
                        else
                        {
                            if (genreTwo.Contains(subg3)) { }
                                else
                                {
                                    if (genreFour.Contains(subg3)) { justGenreThreeandGenreFour.Add(subg3); }
                                        else
                                        {
                                            justGenreThree.Add(subg3);
                                        }
                                    }
                                }
                            }
                       
                foreach (string subg4 in genreFour)
                {
                    if (genreOne.Contains(subg4)) { }
                        else
                        {
                           if (genreTwo.Contains(subg4)) { }
                                else
                                {
                                        if (genreThree.Contains(subg4)) { }
                                        else
                                        {
                                            justGenreFour.Add(subg4);
                                        }
                                    }
                                }
                            }
                        

                string g1OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g4OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";

                long g1OnlyListId = DBListCreate(g1OnlyList);
                long g2OnlyListId = DBListCreate(g2OnlyList);
                long g3OnlyListId = DBListCreate(g3OnlyList);
                long g4OnlyListId = DBListCreate(g4OnlyList);
                long g1AndG2ListId = DBListCreate(g1AndG2List);
                long g1AndG3ListId = DBListCreate(g1AndG3List);
                long g1AndG4ListId = DBListCreate(g1AndG4List);
                long g2AndG3ListId = DBListCreate(g2AndG3List);
                long g2AndG4ListId = DBListCreate(g2AndG4List);
                long g3AndG4ListId = DBListCreate(g3AndG4List);
                long g1AndG2AndG3ListId = DBListCreate(g1AndG2AndG3List);
                long g1AndG2AndG4ListId = DBListCreate(g1AndG2AndG4List);
                long g1AndG3AndG4ListId = DBListCreate(g1AndG3AndG4List);
                long g2AndG3AndG4ListId = DBListCreate(g2AndG3AndG4List);
                long g1AndG2AndG3AndG4ListId = DBListCreate(g1AndG2AndG3AndG4List);

                string[] g1Only = new string[] { todaysGenres[0] };
                string[] g2Only = new string[] { todaysGenres[1] };
                string[] g3Only = new string[] { todaysGenres[2] };
                string[] g4Only = new string[] { todaysGenres[3] };
                string[] g1AndG2 = new string[] { todaysGenres[0], todaysGenres[1] };
                string[] g1AndG3 = new string[] { todaysGenres[0], todaysGenres[2] };
                string[] g1AndG4 = new string[] { todaysGenres[0], todaysGenres[3] };
                string[] g2AndG3 = new string[] { todaysGenres[1], todaysGenres[2] };
                string[] g2AndG4 = new string[] { todaysGenres[1], todaysGenres[3] };
                string[] g3AndG4 = new string[] { todaysGenres[2], todaysGenres[3] };
                string[] g1AndG2AndG3 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2] };
                string[] g1AndG2AndG4 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[3] };
                string[] g1AndG3AndG4 = new string[] { todaysGenres[0], todaysGenres[2], todaysGenres[3] };
                string[] g2AndG3AndG4 = new string[] { todaysGenres[1], todaysGenres[2], todaysGenres[3] };
                string[] g1AndG2AndG3AndG4 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2], todaysGenres[3] };

                genreAndListId.Add(new KeyValuePair<string[], long>(g1Only, g1OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2Only, g2OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3Only, g3OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g4Only, g4OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2, g1AndG2ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3, g1AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG4, g1AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3, g2AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG4, g2AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3AndG4, g3AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3, g1AndG2AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG4, g1AndG2AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3AndG4, g1AndG3AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3AndG4, g2AndG3AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3AndG4, g1AndG2AndG3AndG4ListId));

                foreach (string g1Sub in justGenreOne)
                {
                    string g1SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1Sub + "', '" + g1OnlyListId + "');";
                    DBSubsToLists(g1SubQuery);
                }
                foreach (string g2Sub in justGenreTwo)
                {
                    string g2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2Sub + "', '" + g2OnlyListId + "');";
                    DBSubsToLists(g2SubQuery);
                }
                foreach (string g3Sub in justGenreThree)
                {
                    string g3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3Sub + "', '" + g3OnlyListId + "');";
                    DBSubsToLists(g3SubQuery);
                }
                foreach (string g4Sub in justGenreFour)
                {
                    string g4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g4Sub + "', '" + g4OnlyListId + "');";
                    DBSubsToLists(g4SubQuery);
                }
                foreach (string g1AndG2Sub in justGenreOneandGenreTwo)
                {
                    string g1AndG2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2Sub + "', '" + g1AndG2ListId + "');";
                    DBSubsToLists(g1AndG2SubQuery);
                }
                foreach (string g1AndG3Sub in justGenreOneandGenreThree)
                {
                    string g1AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3Sub + "', '" + g1AndG3ListId + "');";
                    DBSubsToLists(g1AndG3SubQuery);
                }
                foreach (string g1AndG4Sub in justGenreOneandGenreFour)
                {
                    string g1AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG4Sub + "', '" + g1AndG4ListId + "');";
                    DBSubsToLists(g1AndG4SubQuery);
                }
                foreach (string g2AndG3Sub in justGenreTwoandGenreThree)
                {
                    string g2AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3Sub + "', '" + g2AndG3ListId + "');";
                    DBSubsToLists(g2AndG3SubQuery);
                }
                foreach (string g2AndG4Sub in justGenreTwoandGenreFour)
                {
                    string g2AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG4Sub + "', '" + g2AndG4ListId + "');";
                    DBSubsToLists(g2AndG4SubQuery);
                }
                foreach (string g3AndG4Sub in justGenreThreeandGenreFour)
                {
                    string g3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3AndG4Sub + "', '" + g3AndG4ListId + "');";
                    DBSubsToLists(g3AndG4SubQuery);
                }
                foreach (string g1AndG2AndG3Sub in justGenreOneTwoAndThree)
                {
                    string g1AndG2AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3Sub + "', '" + g1AndG2AndG3ListId + "');";
                    DBSubsToLists(g1AndG2AndG3SubQuery);
                }
                foreach (string g1AndG2AndG4Sub in justGenreOneTwoAndFour)
                {
                    string g1AndG2AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG4Sub + "', '" + g1AndG2AndG4ListId + "');";
                    DBSubsToLists(g1AndG2AndG4SubQuery);
                }
                foreach (string g1AndG3AndG4Sub in justGenreOneThreeAndFour)
                {
                    string g1AndG3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3AndG4Sub + "', '" + g1AndG3AndG4ListId + "');";
                    DBSubsToLists(g1AndG3AndG4SubQuery);
                }
                foreach (string g2AndG3AndG4Sub in justGenreTwoThreeAndFour)
                {
                    string g2AndG3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3AndG4Sub + "', '" + g2AndG3AndG4ListId + "');";
                    DBSubsToLists(g2AndG3AndG4SubQuery);
                }
                foreach (string g1AndG2AndG3AndG4Sub in genreOneTwoThreeAndFour)
                {
                    string g1AndG2AndG3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3AndG4Sub + "', '" + g1AndG2AndG3AndG4ListId + "');";
                    DBSubsToLists(g1AndG2AndG3AndG4SubQuery);
                }
            }
            if (todaysGenres.Count == 5)
            {
                List<long> genreListIds = new List<long> { };
                foreach (string genre in todaysGenres)
                {
                    genreListIds.Add(listId[genre]);
                }
                long g1 = genreListIds[0];
                long g2 = genreListIds[1];
                long g3 = genreListIds[2];
                long g4 = genreListIds[3];
                long g5 = genreListIds[4];

                List<string> genreOne = new List<string>();
                List<string> genreTwo = new List<string>();
                List<string> genreThree = new List<string>();
                List<string> genreFour = new List<string>();
                List<string> genreFive = new List<string>();
                List<string> justGenreOneandGenreTwo = new List<string>();
                List<string> justGenreOneandGenreThree = new List<string>();
                List<string> justGenreOneandGenreFour = new List<string>();
                List<string> justGenreOneandGenreFive = new List<string>();
                List<string> justGenreTwoandGenreThree = new List<string>();
                List<string> justGenreTwoandGenreFour = new List<string>();
                List<string> justGenreTwoandGenreFive = new List<string>();
                List<string> justGenreThreeandGenreFour = new List<string>();
                List<string> justGenreThreeandGenreFive = new List<string>();
                List<string> justGenreFourandGenreFive = new List<string>();
                List<string> justGenreOne = new List<string>();
                List<string> justGenreTwo = new List<string>();
                List<string> justGenreThree = new List<string>();
                List<string> justGenreFour = new List<string>();
                List<string> justGenreFive = new List<string>();
                List<string> justGenreOneTwoAndThree = new List<string>();
                List<string> justGenreOneTwoAndFour = new List<string>();
                List<string> justGenreOneTwoAndFive = new List<string>();
                List<string> justGenreOneThreeAndFour = new List<string>();
                List<string> justGenreOneThreeAndFive = new List<string>();
                List<string> justGenreOneFourAndFive = new List<string>();
                List<string> justGenreTwoThreeAndFour = new List<string>();
                List<string> justGenreTwoThreeAndFive = new List<string>();
                List<string> justGenreTwoFourAndFive = new List<string>();
                List<string> justGenreThreeFourAndFive = new List<string>();
                List<string> justGenreOneTwoThreeAndFour = new List<string>();
                List<string> justGenreOneTwoThreeAndFive = new List<string>();
                List<string> justGenreOneTwoFourAndFive = new List<string>();
                List<string> justGenreOneThreeFourAndFive = new List<string>();
                List<string> justGenreTwoThreeFourAndFive = new List<string>();
                List<string> genreOneTwoThreeFourAndFive = new List<string>();

                string queryOne = "SELECT * FROM subscribers WHERE list = '" + g1 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryTwo = "SELECT * FROM subscribers WHERE list = '" + g2 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryThree = "SELECT * FROM subscribers WHERE list = '" + g3 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryFour = "SELECT * FROM subscribers WHERE list = '" + g4 + "' AND unsubscribed = '0' AND bounced = '0'";
                string queryFive = "SELECT * FROM subscribers WHERE list = '" + g5 + "' AND unsubscribed = '0' AND bounced = '0'";

                genreOne = await DBQuery(queryOne);
                genreTwo = await DBQuery(queryTwo);
                genreThree = await DBQuery(queryThree);
                genreFour = await DBQuery(queryFour);
                genreFive = await DBQuery(queryFive);
                foreach (string subg1 in genreOne)
                {

                    if (genreTwo.Contains(subg1))
                    {
                        if (genreThree.Contains(subg1))
                        {
                            if (genreFour.Contains(subg1))
                            {

                                if (genreFive.Contains(subg1))
                                {
                                    genreOneTwoThreeFourAndFive.Add(subg1);
                                }

                                else { justGenreOneTwoThreeAndFour.Add(subg1); }
                            }
                            else
                            {
                                if (genreFive.Contains(subg1))
                                {
                                    justGenreOneTwoThreeAndFive.Add(subg1);
                                }
                                else { justGenreOneTwoAndThree.Add(subg1); }
                            }
                        }
                        else
                        {

                            if (genreFour.Contains(subg1))
                            {
                                if (genreFive.Contains(subg1)) { justGenreOneTwoFourAndFive.Add(subg1); }
                                else { justGenreOneTwoAndFour.Add(subg1); }
                            }
                            else
                            {
                                if (genreFive.Contains(subg1))
                                {
                                    justGenreOneTwoAndFive.Add(subg1);
                                }

                                else { justGenreOneandGenreTwo.Add(subg1); }

                            }
                        }

                    }


                    else
                    {

                        if (genreThree.Contains(subg1))
                        {

                            if (genreFour.Contains(subg1)) {
                                if (genreFive.Contains(subg1)) { justGenreOneThreeFourAndFive.Add(subg1); }
                                else { justGenreOneThreeAndFour.Add(subg1); } }
                            else 
                            {
                                if (genreFive.Contains(subg1)) { justGenreOneThreeAndFive.Add(subg1); }
                                else { justGenreOneandGenreThree.Add(subg1); }

                            } }
                        else
                        {

                            if (genreFour.Contains(subg1)) {
                                if (genreFive.Contains(subg1)) { justGenreOneFourAndFive.Add(subg1); }
                                else { justGenreOneandGenreFour.Add(subg1); } }
                            else {  
                                if (genreFive.Contains(subg1)) { justGenreOneandGenreFive.Add(subg1); } 
                                    
                                else { justGenreOne.Add(subg1); }

                        }

                    }
                        }
                    }
                foreach (string subg2 in genreTwo)
                {
                    if (genreOne.Contains(subg2)) { }
                    else
                    {

                        if (genreThree.Contains(subg2))
                        {
                            if (genreFour.Contains(subg2))
                            {
                                if (genreFive.Contains(subg2)) { justGenreTwoThreeFourAndFive.Add(subg2); }
                                else { justGenreTwoThreeAndFour.Add(subg2); }
                            }
                            else
                            {
                                if (genreFive.Contains(subg2))
                                {
                                    justGenreTwoThreeAndFive.Add(subg2);
                                }
                                else { justGenreTwoandGenreThree.Add(subg2); }
                            }
                        }
                        else
                        {
                            if (genreFour.Contains(subg2)) 
                            {
                                if (genreFive.Contains(subg2))
                                {
                                    justGenreTwoFourAndFive.Add(subg2);
                                }
                                else { justGenreTwoandGenreFour.Add(subg2); } 
                            }
                            else
                            {
                                if (genreFive.Contains(subg2)) { justGenreTwoandGenreFive.Add(subg2); }
                                else { justGenreTwo.Add(subg2); }
                            }
                            

                        }
                    }
                }
                foreach (string subg3 in genreThree)
                {
                    if (genreOne.Contains(subg3)) { }
                    else
                    {
                        if (genreTwo.Contains(subg3)) { }
                        else
                        {
                            if (genreFour.Contains(subg3)) 
                            {
                                if (genreFive.Contains(subg3)) { justGenreThreeFourAndFive.Add(subg3); }
                                else { justGenreThreeandGenreFour.Add(subg3); } 
                            }
                            else
                            {
                                if (genreFive.Contains(subg3)) { justGenreThreeandGenreFive.Add(subg3); }
                                else { justGenreThree.Add(subg3); }
                            }
                        }
                    }
                }
                foreach (string subg4 in genreFour)
                {
                    if (genreOne.Contains(subg4)) { }
                    else
                    {
                        if (genreTwo.Contains(subg4)) { }
                        else
                        {
                            if (genreThree.Contains(subg4)) { }
                            else
                            {
                                if (genreFive.Contains(subg4))
                                {
                                    justGenreFourandGenreFive.Add(subg4);
                                }
                                else { justGenreFour.Add(subg4); }
                            }
                        }
                    }
                }
                foreach (string subg5 in genreFive)
                {
                    if (genreOne.Contains(subg5)) { }
                    else
                    {
                        if (genreTwo.Contains(subg5)) { }
                        else
                        {
                            if (genreThree.Contains(subg5)) { }
                            else
                            {
                                if (genreFour.Contains(subg5)) { }
                                else { justGenreFive.Add(subg5); }
                            }
                        }
                    }
                }


                string g1OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g4OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g5OnlyList = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g3AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[2] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3AndG4List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[3] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG3AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[2] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g2AndG3AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";
                string g1AndG2AndG3AndG4AndG5List = "INSERT INTO `lists` (`app`, `userID`, `name`) VALUES('3', '1', '" + todaysGenres[0] + " " + todaysGenres[1] + " " + todaysGenres[2] + " " + todaysGenres[3] + " " + todaysGenres[4] + " for " + System.DateTime.Now.ToString("MMMM dd") + "');";

                long g1OnlyListId = DBListCreate(g1OnlyList);
                long g2OnlyListId = DBListCreate(g2OnlyList);
                long g3OnlyListId = DBListCreate(g3OnlyList);
                long g4OnlyListId = DBListCreate(g4OnlyList);
                long g5OnlyListId = DBListCreate(g5OnlyList);
                long g1AndG2ListId = DBListCreate(g1AndG2List);
                long g1AndG3ListId = DBListCreate(g1AndG3List);
                long g1AndG4ListId = DBListCreate(g1AndG4List);
                long g1AndG5ListId = DBListCreate(g1AndG5List);
                long g2AndG3ListId = DBListCreate(g2AndG3List);
                long g2AndG4ListId = DBListCreate(g2AndG4List);
                long g2AndG5ListId = DBListCreate(g2AndG5List);
                long g3AndG4ListId = DBListCreate(g3AndG4List);
                long g3AndG5ListId = DBListCreate(g3AndG5List);
                long g4AndG5ListId = DBListCreate(g4AndG5List);
                long g1AndG2AndG3ListId = DBListCreate(g1AndG2AndG3List);
                long g1AndG2AndG4ListId = DBListCreate(g1AndG2AndG4List);
                long g1AndG2AndG5ListId = DBListCreate(g1AndG2AndG5List);
                long g1AndG3AndG4ListId = DBListCreate(g1AndG3AndG4List);
                long g1AndG3AndG5ListId = DBListCreate(g1AndG3AndG5List);
                long g1AndG4AndG5ListId = DBListCreate(g1AndG4AndG5List);
                long g2AndG3AndG4ListId = DBListCreate(g2AndG3AndG4List);
                long g2AndG3AndG5ListId = DBListCreate(g2AndG3AndG5List);
                long g2AndG4AndG5ListId = DBListCreate(g2AndG4AndG5List);
                long g3AndG4AndG5ListId = DBListCreate(g3AndG4AndG5List);
                long g1AndG2AndG3AndG4ListId = DBListCreate(g1AndG2AndG3AndG4List);
                long g1AndG2AndG3AndG5ListId = DBListCreate(g1AndG2AndG3AndG5List);
                long g1AndG2AndG4AndG5ListId = DBListCreate(g1AndG2AndG4AndG5List);
                long g1AndG3AndG4AndG5ListId = DBListCreate(g1AndG3AndG4AndG5List);
                long g2AndG3AndG4AndG5ListId = DBListCreate(g2AndG3AndG4AndG5List);
                long g1AndG2AndG3AndG4AndG5ListId = DBListCreate(g1AndG2AndG3AndG4AndG5List);

                string[] g1Only = new string[] { todaysGenres[0] };
                string[] g2Only = new string[] { todaysGenres[1] };
                string[] g3Only = new string[] { todaysGenres[2] };
                string[] g4Only = new string[] { todaysGenres[3] };
                string[] g5Only = new string[] { todaysGenres[4] };
                string[] g1AndG2 = new string[] { todaysGenres[0], todaysGenres[1] };
                string[] g1AndG3 = new string[] { todaysGenres[0], todaysGenres[2] };
                string[] g1AndG4 = new string[] { todaysGenres[0], todaysGenres[3] };
                string[] g1AndG5 = new string[] { todaysGenres[0], todaysGenres[4] };
                string[] g2AndG3 = new string[] { todaysGenres[1], todaysGenres[2] };
                string[] g2AndG4 = new string[] { todaysGenres[1], todaysGenres[3] };
                string[] g2AndG5 = new string[] { todaysGenres[1], todaysGenres[4] };
                string[] g3AndG4 = new string[] { todaysGenres[2], todaysGenres[3] };
                string[] g3AndG5 = new string[] { todaysGenres[2], todaysGenres[4] };
                string[] g4AndG5 = new string[] { todaysGenres[3], todaysGenres[4] };
                string[] g1AndG2AndG3 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2] };
                string[] g1AndG2AndG4 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[3] };
                string[] g1AndG2AndG5 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[4] };
                string[] g1AndG3AndG4 = new string[] { todaysGenres[0], todaysGenres[2], todaysGenres[3] };
                string[] g1AndG3AndG5 = new string[] { todaysGenres[0], todaysGenres[2], todaysGenres[4] };
                string[] g1AndG4AndG5 = new string[] { todaysGenres[0], todaysGenres[3], todaysGenres[4] };
                string[] g2AndG3AndG4 = new string[] { todaysGenres[1], todaysGenres[2], todaysGenres[3] };
                string[] g2AndG3AndG5 = new string[] { todaysGenres[1], todaysGenres[2], todaysGenres[4] };
                string[] g2AndG4AndG5 = new string[] { todaysGenres[1], todaysGenres[3], todaysGenres[4] };
                string[] g3AndG4AndG5 = new string[] { todaysGenres[2], todaysGenres[3], todaysGenres[4] };
                string[] g1AndG2AndG3AndG4 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2], todaysGenres[3] };
                string[] g1AndG2AndG3AndG5 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2], todaysGenres[4] };
                string[] g1AndG2AndG4AndG5 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[3], todaysGenres[4] };
                string[] g1AndG3AndG4AndG5 = new string[] { todaysGenres[0], todaysGenres[2], todaysGenres[3], todaysGenres[4] };
                string[] g2AndG3AndG4AndG5 = new string[] { todaysGenres[1], todaysGenres[2], todaysGenres[3], todaysGenres[4] };
                string[] g1AndG2AndG3AndG4AndG5 = new string[] { todaysGenres[0], todaysGenres[1], todaysGenres[2], todaysGenres[3], todaysGenres[4] };

                genreAndListId.Add(new KeyValuePair<string[], long>(g1Only, g1OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2Only, g2OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3Only, g3OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g4Only, g4OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g5Only, g5OnlyListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2, g1AndG2ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3, g1AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG4, g1AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG5, g1AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3, g2AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG4, g2AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG5, g2AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3AndG4, g3AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3AndG5, g3AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g4AndG5, g4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3, g1AndG2AndG3ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG4, g1AndG2AndG4ListId)); 
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG5, g1AndG2AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3AndG4, g1AndG3AndG4ListId)); 
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3AndG5, g1AndG3AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG4AndG5, g1AndG4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3AndG4, g2AndG3AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3AndG5, g2AndG3AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG4AndG5, g2AndG4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g3AndG4AndG5, g3AndG4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3AndG4, g1AndG2AndG3AndG4ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3AndG5, g1AndG2AndG3AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG4AndG5, g1AndG2AndG4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG3AndG4AndG5, g1AndG3AndG4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g2AndG3AndG4AndG5, g2AndG3AndG4AndG5ListId));
                genreAndListId.Add(new KeyValuePair<string[], long>(g1AndG2AndG3AndG4AndG5, g1AndG2AndG3AndG4AndG5ListId));

                foreach (string g1Sub in justGenreOne)
                {
                    string g1SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1Sub + "', '" + g1OnlyListId + "');";
                    DBSubsToLists(g1SubQuery);
                }
                foreach (string g2Sub in justGenreTwo)
                {
                    string g2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2Sub + "', '" + g2OnlyListId + "');";
                    DBSubsToLists(g2SubQuery);
                }
                foreach (string g3Sub in justGenreThree)
                {
                    string g3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3Sub + "', '" + g3OnlyListId + "');";
                    DBSubsToLists(g3SubQuery);
                }
                foreach (string g4Sub in justGenreFour)
                {
                    string g4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g4Sub + "', '" + g4OnlyListId + "');";
                    DBSubsToLists(g4SubQuery);
                }
                foreach (string g5Sub in justGenreFive)
                {
                    string g5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g5Sub + "', '" + g5OnlyListId + "');";
                    DBSubsToLists(g5SubQuery);
                }
                foreach (string g1AndG2Sub in justGenreOneandGenreTwo)
                {
                    string g1AndG2SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2Sub + "', '" + g1AndG2ListId + "');";
                    DBSubsToLists(g1AndG2SubQuery);
                }
                foreach (string g1AndG3Sub in justGenreOneandGenreThree)
                {
                    string g1AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3Sub + "', '" + g1AndG3ListId + "');";
                    DBSubsToLists(g1AndG3SubQuery);
                }
                foreach (string g1AndG4Sub in justGenreOneandGenreFour)
                {
                    string g1AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG4Sub + "', '" + g1AndG4ListId + "');";
                    DBSubsToLists(g1AndG4SubQuery);
                }
                foreach (string g1AndG5Sub in justGenreOneandGenreFive)
                {
                    string g1AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG5Sub + "', '" + g1AndG5ListId + "');";
                    DBSubsToLists(g1AndG5SubQuery);
                }
                foreach (string g2AndG3Sub in justGenreTwoandGenreThree)
                {
                    string g2AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3Sub + "', '" + g2AndG3ListId + "');";
                    DBSubsToLists(g2AndG3SubQuery);
                }
                foreach (string g2AndG4Sub in justGenreTwoandGenreFour)
                {
                    string g2AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG4Sub + "', '" + g2AndG4ListId + "');";
                    DBSubsToLists(g2AndG4SubQuery);
                }
                foreach (string g2AndG5Sub in justGenreTwoandGenreFive)
                {
                    string g2AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG5Sub + "', '" + g2AndG5ListId + "');";
                    DBSubsToLists(g2AndG5SubQuery);
                }
                foreach (string g3AndG4Sub in justGenreThreeandGenreFour)
                {
                    string g3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3AndG4Sub + "', '" + g3AndG4ListId + "');";
                    DBSubsToLists(g3AndG4SubQuery);
                }
                foreach (string g3AndG5Sub in justGenreThreeandGenreFive)
                {
                    string g3AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3AndG5Sub + "', '" + g3AndG5ListId + "');";
                    DBSubsToLists(g3AndG5SubQuery);
                }
                foreach (string g4AndG5Sub in justGenreFourandGenreFive)
                {
                    string g4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g4AndG5Sub + "', '" + g4AndG5ListId + "');";
                    DBSubsToLists(g4AndG5SubQuery);
                }
                foreach (string g1AndG2AndG3Sub in justGenreOneTwoAndThree)
                {
                    string g1AndG2AndG3SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3Sub + "', '" + g1AndG2AndG3ListId + "');";
                    DBSubsToLists(g1AndG2AndG3SubQuery);
                }
                foreach (string g1AndG2AndG4Sub in justGenreOneTwoAndFour)
                {
                    string g1AndG2AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG4Sub + "', '" + g1AndG2AndG4ListId + "');";
                    DBSubsToLists(g1AndG2AndG4SubQuery);
                }
                foreach (string g1AndG2AndG5Sub in justGenreOneTwoAndFive)
                {
                    string g1AndG2AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG5Sub + "', '" + g1AndG2AndG5ListId + "');";
                    DBSubsToLists(g1AndG2AndG5SubQuery);
                }
                foreach (string g1AndG3AndG4Sub in justGenreOneThreeAndFour)
                {
                    string g1AndG3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3AndG4Sub + "', '" + g1AndG3AndG4ListId + "');";
                    DBSubsToLists(g1AndG3AndG4SubQuery);
                }
                foreach (string g1AndG3AndG5Sub in justGenreOneThreeAndFive)
                {
                    string g1AndG3AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3AndG5Sub + "', '" + g1AndG3AndG5ListId + "');";
                    DBSubsToLists(g1AndG3AndG5SubQuery);
                }
                foreach (string g1AndG4AndG5Sub in justGenreOneFourAndFive)
                {
                    string g1AndG4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG4AndG5Sub + "', '" + g1AndG4AndG5ListId + "');";
                    DBSubsToLists(g1AndG4AndG5SubQuery);
                }
                foreach (string g2AndG3AndG4Sub in justGenreTwoThreeAndFour)
                {
                    string g2AndG3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3AndG4Sub + "', '" + g2AndG3AndG4ListId + "');";
                    DBSubsToLists(g2AndG3AndG4SubQuery);
                }
                foreach (string g2AndG3AndG5Sub in justGenreTwoThreeAndFive)
                {
                    string g2AndG3AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3AndG5Sub + "', '" + g2AndG3AndG5ListId + "');";
                    DBSubsToLists(g2AndG3AndG5SubQuery);
                }
                foreach (string g2AndG4AndG5Sub in justGenreTwoFourAndFive)
                {
                    string g2AndG4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG4AndG5Sub + "', '" + g2AndG4AndG5ListId + "');";
                    DBSubsToLists(g2AndG4AndG5SubQuery);
                }
                foreach (string g3AndG4AndG5Sub in justGenreThreeFourAndFive)
                {
                    string g3AndG4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g3AndG4AndG5Sub + "', '" + g3AndG4AndG5ListId + "');";
                    DBSubsToLists(g3AndG4AndG5SubQuery);
                }
                foreach (string g1AndG2AndG3AndG4Sub in justGenreOneTwoThreeAndFour)
                {
                    string g1AndG2AndG3AndG4SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3AndG4Sub + "', '" + g1AndG2AndG3AndG4ListId + "');";
                    DBSubsToLists(g1AndG2AndG3AndG4SubQuery);
                }
                foreach (string g1AndG2AndG3AndG5Sub in justGenreOneTwoThreeAndFive)
                {
                    string g1AndG2AndG3AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3AndG5Sub + "', '" + g1AndG2AndG3AndG5ListId + "');";
                    DBSubsToLists(g1AndG2AndG3AndG5SubQuery);
                }
                foreach (string g1AndG3AndG4AndG5Sub in justGenreOneThreeFourAndFive)
                {
                    string g1AndG3AndG4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG3AndG4AndG5Sub + "', '" + g1AndG3AndG4AndG5ListId + "');";
                    DBSubsToLists(g1AndG3AndG4AndG5SubQuery);
                }
                foreach (string g2AndG3AndG4AndG5Sub in justGenreTwoThreeFourAndFive)
                {
                    string g2AndG3AndG4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g2AndG3AndG4AndG5Sub + "', '" + g2AndG3AndG4AndG5ListId + "');";
                    DBSubsToLists(g2AndG3AndG4AndG5SubQuery);
                }
                foreach (string g1AndG2AndG3AndG4AndG5Sub in genreOneTwoThreeFourAndFive)
                {
                    string g1AndG2AndG3AndG4AndG5SubQuery = "INSERT INTO `subscribers` (`email`, `list`) VALUES('" + g1AndG2AndG3AndG4AndG5Sub + "', '" + g1AndG2AndG3AndG4AndG5ListId + "');";
                    DBSubsToLists(g1AndG2AndG3AndG4AndG5SubQuery);
                }
            }
            return genreAndListId;
        }
        public static async Task<List<string>> ListRemoval()
        {
            List<string> removedLists = new List<string>();
            string connectionString = "connection_string;";
            MySqlConnection db = new MySqlConnection(connectionString);

            // Open connection
            db.Open();
            DateTime today = DateTime.Now;
            DateTime lastMonth = today.AddMonths(-1);
            string lastMonthString = lastMonth.ToString("MMMM");
            string query = "Delete FROM lists WHERE name LIKE '%" + lastMonthString + "%'";

            // Create Command
            MySqlCommand cmd = new MySqlCommand(query, db);
            // Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();

            // close Data Reader
            dataReader.Close();

            // close Connection
            db.Close();



            return removedLists;
        }
    }
}


