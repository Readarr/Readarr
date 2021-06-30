import getNewAuthor from 'Utilities/Author/getNewAuthor';

function getNewBook(book, payload) {
  const {
    searchForNewBook = false
  } = payload;

  if (!('id' in book.author) || book.author.id === 0) {
    getNewAuthor(book.author, payload);
  }

  book.addOptions = {
    searchForNewBook
  };
  book.monitored = true;

  return book;
}

export default getNewBook;
