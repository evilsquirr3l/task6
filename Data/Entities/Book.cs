﻿using System.Collections.Generic;

namespace Data.Entities
{
    public class Book : BaseEntity
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public string Author { get; set; }

        public ICollection<BookCard> Cards { get; set; } = new List<BookCard>();
    }
}
