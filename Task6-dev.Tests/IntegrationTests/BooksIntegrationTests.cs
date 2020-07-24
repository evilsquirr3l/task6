using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Business.Models;
using Data;
using Data.Entities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Task6.IntegrationTests
{
    [TestFixture]
    public class BooksIntegrationTests
    {
        private HttpClient _client;
        private CustomWebApplicationFactory _factory;
        private const string RequestUri = "api/books/";
        
        [OneTimeSetUp]
        public void Init()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Test, Order(0)]
        public async Task BooksController_GetByFilter_ReturnsAllWithNullFilter()
        {
            var httpResponse = await _client.GetAsync(RequestUri);
            
            httpResponse.EnsureSuccessStatusCode();
            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
            var books = JsonConvert.DeserializeObject<IEnumerable<Book>>(stringResponse);
            
            Assert.AreEqual(2, books.Count());
        }
        
        [Test, Order(0)]
        public async Task BooksController_GetByFilter_ReturnsBooksThatApplyFilter()
        {
            var httpResponse = await _client.GetAsync($"{RequestUri}?Author=Jon%20Snow&Year=1996");

            httpResponse.EnsureSuccessStatusCode();
            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
            var books = JsonConvert.DeserializeObject<IEnumerable<Book>>(stringResponse);

            foreach (var book in books)
            {
                Assert.AreEqual("Jon Snow", book.Author);
                Assert.AreEqual(1996, book.Year);
            }
        }

        [Test, Order(1)]
        public async Task BooksController_Add_AddsBookToDatabase()
        {
            var book = new BookModel{Author = "Charles Dickens", Title = "A Tale of Two Cities", Year = 1859};
            var content = new StringContent(JsonConvert.SerializeObject(book), Encoding.UTF8, "application/json");
            var httpResponse = await _client.PostAsync(RequestUri, content);

            httpResponse.EnsureSuccessStatusCode();
            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
            var bookInResponse = JsonConvert.DeserializeObject<BookModel>(stringResponse);

            using (var test = _factory.Services.CreateScope())
            {
                var context = test.ServiceProvider.GetService<LibraryDbContext>();
                var databaseBook = await context.Books.FindAsync(bookInResponse.Id);
                Assert.AreEqual(bookInResponse.Id, databaseBook.Id);
                Assert.AreEqual(bookInResponse.Author, databaseBook.Author);
                Assert.AreEqual(bookInResponse.Title, databaseBook.Title);
                Assert.AreEqual(3, context.Books.Count());
            }
        }

        [Test, Order(0)]
        public async Task BooksController_Update_UpdatesBookInDatabase()
        {
            var book = new BookModel{Id = 2, Author = "Honore de Balzac", Title = "Lost Illusions", Year = 1843};
            var content = new StringContent(JsonConvert.SerializeObject(book), Encoding.UTF8, "application/json");
            var httpResponse = await _client.PutAsync(RequestUri, content);

            httpResponse.EnsureSuccessStatusCode();
            
            using (var test = _factory.Services.CreateScope())
            {
                var context = test.ServiceProvider.GetService<LibraryDbContext>();
                var databaseBook = await context.Books.FindAsync(book.Id);
                Assert.AreEqual(book.Id, databaseBook.Id);
                Assert.AreEqual(book.Author, databaseBook.Author);
                Assert.AreEqual(book.Title, databaseBook.Title);
                Assert.AreEqual(2, context.Books.Count());
            }
        }

        [Test, Order(2)]
        public async Task BooksController_DeleteById_DeletesBookFromDatabase()
        {
            var bookId = 1;
            var httpResponse = await _client.DeleteAsync(RequestUri + bookId);

            httpResponse.EnsureSuccessStatusCode();
            
            using (var test = _factory.Services.CreateScope())
            {
                var context = test.ServiceProvider.GetService<LibraryDbContext>();
                
                Assert.AreEqual(2, context.Books.Count());
            }
        }
        
        [Test, Order(0)]
        public void ReaderController_Add_ThrowsExceptionIfModelIsIncorrect()
        {
            // Author is empty
            var book = new BookModel{Author = "", Title = "Lost Illusions", Year = 1843};
            CheckExceptionWhileAddNewModel(book);
        
            // Title is empty
            book.Author = "Honore de Balzac";
            book.Title = "";
            CheckExceptionWhileAddNewModel(book);
        
            // Year is invalid
            book.Title = "Lost Illusions";
            book.Year = 9999;
            CheckExceptionWhileAddNewModel(book);
        }
        
        [Test, Order(0)]
        public void ReaderController_Update_ThrowsExceptionIfModelIsIncorrect()
        {
            // Author is empty
            var book = new BookModel{Author = "", Title = "Lost Illusions", Year = 1843};
            CheckExceptionWhileUpdateModel(book);
        
            // Title is empty
            book.Author = "Honore de Balzac";
            book.Title = "";
            CheckExceptionWhileUpdateModel(book);
        
            // Year is invalid
            book.Year = 9999;
            CheckExceptionWhileUpdateModel(book);
        }

        private async void CheckExceptionWhileAddNewModel(BookModel bookModel)
        {
            var content = new StringContent(JsonConvert.SerializeObject(bookModel), Encoding.UTF8, "application/json");
            var httpResponse = await _client.PostAsync(RequestUri, content);

            Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        
        private async void CheckExceptionWhileUpdateModel(BookModel bookModel)
        {
            var content = new StringContent(JsonConvert.SerializeObject(bookModel), Encoding.UTF8, "application/json");
            var httpResponse = await _client.PutAsync(RequestUri, content);

            Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _factory.Dispose();
            _client.Dispose();
        }
    }
}