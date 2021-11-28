import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import bookEntities from 'Book/bookEntities';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import { removeItem, set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'books';

export const filters = [
  {
    key: 'all',
    label: 'All',
    filters: []
  },
  {
    key: 'monitored',
    label: 'Monitored Only',
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'unmonitored',
    label: 'Unmonitored Only',
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: 'Missing Books',
    filters: [
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  }
];

export const filterPredicates = {
  missing: function(item) {
    const { statistics = {} } = item;

    return statistics.bookFileCount === 0;
  },

  releaseDate: function(item, filterValue, type) {
    return dateFilterPredicate(item.releaseDate, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  qualityProfileId: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.author.qualityProfileId, filterValue);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  path: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.author.path, filterValue);
  },

  bookFileCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const bookCount = item.statistics ? item.statistics.bookFileCount : 0;

    return predicate(bookCount, filterValue);
  },

  sizeOnDisk: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const sizeOnDisk = item.statistics && item.statistics.sizeOnDisk ?
      item.statistics.sizeOnDisk :
      0;

    return predicate(sizeOnDisk, filterValue);
  }
};

export const sortPredicates = {
  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  },

  path: function(item) {
    return item.author.path;
  },

  series: function(item) {
    return item.seriesTitle;
  },

  rating: function(item) {
    return item.ratings.value;
  },

  status: function(item) {
    let result = 0;

    const hasBookFile = !!item.statistics.bookFileCount;
    const isAvailable = Date.parse(item.releaseDate) < new Date();

    if (isAvailable) {
      result++;
    }

    if (item.monitored) {
      result += 2;
    }

    if (hasBookFile) {
      result += 4;
    }

    return result;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  items: [],
  pendingChanges: {},
  sortPredicates: {
    rating: function(item) {
      return item.ratings.value;
    }
  },

  columns: [
    {
      name: 'select',
      columnLabel: 'Select',
      isSortable: false,
      isVisible: true,
      isModifiable: false,
      isHidden: true
    },
    {
      name: 'monitored',
      columnLabel: 'Monitored',
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'title',
      label: 'Title',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'series',
      label: 'Series',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'pageCount',
      label: 'Pages',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'rating',
      label: 'Rating',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'status',
      label: 'Status',
      isVisible: true,
      isSortable: true
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ]
};

export const persistState = [
  'books.sortKey',
  'books.sortDirection',
  'books.columns'
];

//
// Actions Types

export const FETCH_BOOKS = 'books/fetchBooks';
export const SET_BOOKS_SORT = 'books/setBooksSort';
export const SET_BOOKS_TABLE_OPTION = 'books/setBooksTableOption';
export const CLEAR_BOOKS = 'books/clearBooks';
export const SET_BOOK_VALUE = 'books/setBookValue';
export const SAVE_BOOK = 'books/saveBook';
export const DELETE_BOOK = 'books/deleteBook';
export const DELETE_AUTHOR_BOOKS = 'books/deleteAuthorBooks';
export const TOGGLE_BOOK_MONITORED = 'books/toggleBookMonitored';
export const TOGGLE_BOOKS_MONITORED = 'books/toggleBooksMonitored';

//
// Action Creators

export const fetchBooks = createThunk(FETCH_BOOKS);
export const setBooksSort = createAction(SET_BOOKS_SORT);
export const setBooksTableOption = createAction(SET_BOOKS_TABLE_OPTION);
export const clearBooks = createAction(CLEAR_BOOKS);
export const toggleBookMonitored = createThunk(TOGGLE_BOOK_MONITORED);
export const toggleBooksMonitored = createThunk(TOGGLE_BOOKS_MONITORED);

export const saveBook = createThunk(SAVE_BOOK);

export const deleteBook = createThunk(DELETE_BOOK, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const deleteAuthorBooks = createThunk(DELETE_AUTHOR_BOOKS, (payload) => {
  return {
    ...payload,
    queryParams: {
      authorId: payload.authorId
    }
  };
});

export const setBookValue = createAction(SET_BOOK_VALUE, (payload) => {
  return {
    section: 'books',
    ...payload
  };
});

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_BOOKS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const { request, abortRequest } = createAjaxRequest({
      url: '/book',
      data: payload,
      traditional: true
    });

    request.done((data) => {
      // Preserve books for other authors we didn't fetch
      if (payload.hasOwnProperty('authorId')) {
        const oldBooks = getState().books.items;
        const newBooks = oldBooks.filter((x) => x.authorId !== payload.authorId);
        data = newBooks.concat(data);
      }

      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [SAVE_BOOK]: createSaveProviderHandler(section, '/book'),
  [DELETE_BOOK]: createRemoveItemHandler(section, '/book'),

  [DELETE_AUTHOR_BOOKS]: function(getState, payload, dispatch) {
    const { authorId } = payload;
    const books = getState().books.items;

    const toDelete = books.filter((x) => x.authorId === authorId);

    dispatch(batchActions(toDelete.map((b) => removeItem({ section, id: b.id }))));
  },

  [TOGGLE_BOOK_MONITORED]: function(getState, payload, dispatch) {
    const {
      bookId,
      bookEntity = bookEntities.BOOKS,
      monitored
    } = payload;

    const bookSection = _.last(bookEntity.split('.'));

    dispatch(updateItem({
      id: bookId,
      section: bookSection,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/book/${bookId}`,
      method: 'PUT',
      data: JSON.stringify({ monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id: bookId,
        section: bookSection,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id: bookId,
        section: bookSection,
        isSaving: false
      }));
    });
  },

  [TOGGLE_BOOKS_MONITORED]: function(getState, payload, dispatch) {
    const {
      bookIds,
      bookEntity = bookEntities.BOOKS,
      monitored
    } = payload;

    dispatch(batchActions(
      bookIds.map((bookId) => {
        return updateItem({
          id: bookId,
          section: bookEntity,
          isSaving: true
        });
      })
    ));

    const promise = createAjaxRequest({
      url: '/book/monitor',
      method: 'PUT',
      data: JSON.stringify({ bookIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions(
        bookIds.map((bookId) => {
          return updateItem({
            id: bookId,
            section: bookEntity,
            isSaving: false,
            monitored
          });
        })
      ));
    });

    promise.fail((xhr) => {
      dispatch(batchActions(
        bookIds.map((bookId) => {
          return updateItem({
            id: bookId,
            section: bookEntity,
            isSaving: false
          });
        })
      ));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_BOOKS_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_BOOKS_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_BOOK_VALUE]: createSetSettingValueReducer(section),

  [CLEAR_BOOKS]: (state) => {
    return Object.assign({}, state, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: []
    });
  }

}, defaultState, section);
