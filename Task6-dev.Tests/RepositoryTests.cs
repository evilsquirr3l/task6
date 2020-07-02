using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Business;
using Business.Interfaces;
using Business.Models;
using Business.Services;
using Data;
using Data.Entities;
using Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using WebApi.Controllers;

namespace Task6
{
    public class Tests
    {
        private DbContextOptions<LibraryDbContext> _options;
        
        [SetUp]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using (var context = new LibraryDbContext(_options))
            {
                context.Books.Add(new Book(){ Id = 1, Author = "Jon Snow", Title = "A song of ice and fire", Year = 1996});
                context.Cards.Add(new Card(){ Id = 1, ReaderId = 1, Created = DateTime.Now});
                context.Readers.Add(new Reader(){Id = 1, Email = "jon_snow@epam.com", Name = "Jon Snow"});
                context.ReaderProfiles.Add(new ReaderProfile(){ Id = 1, ReaderId = 1, Address = "The night's watch", Phone = "golub"});
                context.Histories.Add(new History(){BookId = 1, CardId = 1, Id = 1, TakeDate = DateTime.Now.AddDays(-2), ReturnDate = DateTime.Now.AddDays(-1)});
                context.SaveChanges();
            }
        }

        [Test]
        public void BookRepository_FindAll_ReturnsAllValues()
        {
            using (var context = new LibraryDbContext(_options))
            {
                var booksRepository = new Repository<Book>(context);

                var books = booksRepository.FindAll();

                Assert.AreEqual(1, books.Count());
            }
        }

        [Test]
        public void BookRepository_FindByCondition_ReturnsSingleValue()
        {
            using (var context = new LibraryDbContext(_options))
            {
                var booksRepository = new Repository<Book>(context);

                var book = booksRepository.FindByCondition(g => g.Id == 1).SingleOrDefault();

                Assert.AreEqual(1, book.Id);
                Assert.AreEqual("Jon Snow", book.Author);
                Assert.AreEqual("A song of ice and fire", book.Title);
                Assert.AreEqual(1996, book.Year);
            }
        }

        [Test]
        public async Task BookRepository_AddAsync_AddsValueToDatabase()
        {
            using (var context = new LibraryDbContext(_options))
            {
                var booksRepository = new Repository<Book>(context);
                var book = new Book(){Id = 2};

                await booksRepository.AddAsync(book);
                await context.SaveChangesAsync();
                
                Assert.AreEqual(2, context.Books.Count());
            }
        }

        [Test]
        public async Task BookRepository_Delete_DeletesEntity()
        {
            using (var context = new LibraryDbContext(_options))
            {
                var bookRepository = new Repository<Book>(context);
                
                await bookRepository.DeleteById(1);
                await context.SaveChangesAsync();
                
                Assert.AreEqual(0, context.Books.Count());
            }
        }

        [Test]
        public async Task BookRepository_Update_UpdatesEntity()
        {
            using (var context = new LibraryDbContext(_options))
            {
                var booksRepository = new Repository<Book>(context);

                var book = new Book(){ Id = 1, Author = "John Travolta", Title = "Pulp Fiction", Year = 1994};

                booksRepository.Update(book);
                await context.SaveChangesAsync();

                Assert.AreEqual(1, book.Id);
                Assert.AreEqual("John Travolta", book.Author);
                Assert.AreEqual("Pulp Fiction", book.Title);
                Assert.AreEqual(1994, book.Year);
            }
        }

        [Test]
        public void BooksController_GetAll_ReturnsBooksModels()
        {
            //Arrange
            var mockBookService = new Mock<IBooksService>();
            mockBookService
                .Setup(repo => repo.GetAll())
                .Returns(GetTestBookModels());
            var bookController = new BooksController(mockBookService.Object);
            
            //Act
            var result = bookController.GetBooks();
            var values = result.Result as OkObjectResult;
            
            //Assert
            Assert.IsInstanceOf<ActionResult<IEnumerable<BookModel>>>(result);
            Assert.NotNull(values.Value);
        }

        private IEnumerable<BookModel> GetTestBookModels()
        {
            return new List<BookModel>()
            {
                new BookModel(){ Id = 1, Author = "Jon Snow", Title = "A song of ice and fire", Year = 1996},
                new BookModel(){ Id = 2, Author = "John Travolta", Title = "Pulp Fiction", Year = 1994}
            };
        }

        [Test]
        public void BooksService_GetAll_ReturnsBookModels()
        {
            var expected = GetTestBookModels().ToList();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(m => m.BookRepository.FindAll())
                .Returns(GetTestBookEntities());
            var bookService = new BooksService(mockUnitOfWork.Object, GetAutomapperProfile());

            var actual = bookService.GetAll().ToList();
            
            Assert.IsInstanceOf<IEnumerable<BookModel>>(actual);
            Assert.AreEqual(expected[0].Author, actual[0].Author);
            Assert.AreEqual(expected[1].Author, actual[1].Author);
        }

        private Mapper GetAutomapperProfile()
        {
            var myProfile = new AutomapperProfile();
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(myProfile));
            
            return new Mapper(configuration);
        }
        
        private IQueryable<Book> GetTestBookEntities()
        {
            return new List<Book>()
            {
                new Book(){ Id = 1, Author = "Jon Snow", Title = "A song of ice and fire", Year = 1996},
                new Book(){ Id = 2, Author = "John Travolta", Title = "Pulp Fiction", Year = 1994}
            }.AsQueryable();
        }
    }
}