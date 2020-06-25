using System;
using System.Collections.Generic;
using Business.Models;

namespace Business.Interfaces
{
    public interface IHistoryService
    {
        IEnumerable<ReaderModel> GetReadersThatDontReturnBooks();

        IEnumerable<BookModel> GetTheMostPopularBooks();

        DateTime GetBookReturningDate(int bookId);
    }
}