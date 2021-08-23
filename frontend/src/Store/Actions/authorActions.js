import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import { set, updateItem } from './baseActions';
import { fetchBooks } from './bookActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'authors';

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
    key: 'continuing',
    label: 'Continuing Only',
    filters: [
      {
        key: 'status',
        value: 'continuing',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'ended',
    label: 'Ended Only',
    filters: [
      {
        key: 'status',
        value: 'ended',
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

    return statistics.bookCount - statistics.bookFileCount > 0;
  },

  nextBook: function(item, filterValue, type) {
    return dateFilterPredicate(item.nextBook, filterValue, type);
  },

  lastBook: function(item, filterValue, type) {
    return dateFilterPredicate(item.lastBook, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  bookCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const bookCount = item.statistics ? item.statistics.bookCount : 0;

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
  status: function(item) {
    let result = 0;

    if (item.monitored) {
      result += 2;
    }

    if (item.status === 'continuing') {
      result++;
    }

    return result;
  },

  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
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
  items: [],
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {}
};

//
// Actions Types

export const FETCH_AUTHOR = 'authors/fetchAuthor';
export const SET_AUTHOR_VALUE = 'authors/setAuthorValue';
export const SAVE_AUTHOR = 'authors/saveAuthor';
export const DELETE_AUTHOR = 'authors/deleteAuthor';

export const TOGGLE_AUTHOR_MONITORED = 'authors/toggleAuthorMonitored';
export const TOGGLE_BOOK_MONITORED = 'authors/toggleBookMonitored';
export const UPDATE_BOOK_MONITORED = 'authors/updateBookMonitored';

//
// Action Creators

export const fetchAuthor = createThunk(FETCH_AUTHOR);
export const saveAuthor = createThunk(SAVE_AUTHOR, (payload) => {
  const newPayload = {
    ...payload
  };

  if (payload.moveFiles) {
    newPayload.queryParams = {
      moveFiles: true
    };
  }

  delete newPayload.moveFiles;

  return newPayload;
});

export const deleteAuthor = createThunk(DELETE_AUTHOR, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const toggleAuthorMonitored = createThunk(TOGGLE_AUTHOR_MONITORED);
export const toggleBookMonitored = createThunk(TOGGLE_BOOK_MONITORED);
export const updateBookMonitor = createThunk(UPDATE_BOOK_MONITORED);

export const setAuthorValue = createAction(SET_AUTHOR_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Helpers

function getSaveAjaxOptions({ ajaxOptions, payload }) {
  if (payload.moveFolder) {
    ajaxOptions.url = `${ajaxOptions.url}?moveFolder=true`;
  }

  return ajaxOptions;
}

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_AUTHOR]: createFetchHandler(section, '/author'),
  [SAVE_AUTHOR]: createSaveProviderHandler(section, '/author', { getAjaxOptions: getSaveAjaxOptions }),
  [DELETE_AUTHOR]: createRemoveItemHandler(section, '/author'),

  [TOGGLE_AUTHOR_MONITORED]: (getState, payload, dispatch) => {
    const {
      authorId: id,
      monitored
    } = payload;

    const author = _.find(getState().authors.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/author/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...author,
        monitored
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false
      }));
    });
  },

  [TOGGLE_BOOK_MONITORED]: function(getState, payload, dispatch) {
    const {
      authorId: id,
      seasonNumber,
      monitored
    } = payload;

    const author = _.find(getState().authors.items, { id });
    const seasons = _.cloneDeep(author.seasons);
    const season = _.find(seasons, { seasonNumber });

    season.isSaving = true;

    dispatch(updateItem({
      id,
      section,
      seasons
    }));

    season.monitored = monitored;

    const promise = createAjaxRequest({
      url: `/author/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...author,
        seasons
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      const books = _.filter(getState().books.items, { authorId: id, seasonNumber });

      dispatch(batchActions([
        updateItem({
          id,
          section,
          ...data
        }),

        ...books.map((book) => {
          return updateItem({
            id: book.id,
            section: 'books',
            monitored
          });
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        seasons: author.seasons
      }));
    });
  },

  [UPDATE_BOOK_MONITORED]: function(getState, payload, dispatch) {
    const {
      id,
      monitor
    } = payload;

    const authorToUpdate = { id };

    if (monitor !== 'None') {
      authorToUpdate.monitored = true;
    }

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/bookshelf',
      method: 'POST',
      data: JSON.stringify({
        authors: [{ id }],
        monitoringOptions: { monitor }
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(fetchBooks({ authorId: id }));

      dispatch(set({
        section,
        isSaving: false,
        saveError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_AUTHOR_VALUE]: createSetSettingValueReducer(section)

}, defaultState, section);
