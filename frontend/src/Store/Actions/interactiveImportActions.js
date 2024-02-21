import moment from 'moment';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import updateSectionState from 'Utilities/State/updateSectionState';
import naturalExpansion from 'Utilities/String/naturalExpansion';
import { set, update, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'interactiveImport';

const booksSection = `${section}.books`;
const bookFilesSection = `${section}.bookFiles`;
let abortCurrentFetchRequest = null;
let abortCurrentRequest = null;
let currentIds = [];

const MAXIMUM_RECENT_FOLDERS = 10;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  isSaving: false,
  error: null,
  items: [],
  pendingChanges: {},
  sortKey: 'path',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'path',
  secondarySortDirection: sortDirections.ASCENDING,
  recentFolders: [],
  importMode: 'chooseImportMode',
  sortPredicates: {
    path: function(item, direction) {
      const path = item.path;

      return naturalExpansion(path.toLowerCase());
    },

    author: function(item, direction) {
      const author = item.author;

      return author ? author.sortName : '';
    },

    quality: function(item, direction) {
      return item.qualityWeight || 0;
    }
  },

  books: {
    isFetching: false,
    isPopulated: false,
    error: null,
    sortKey: 'title',
    sortDirection: sortDirections.ASCENDING,
    items: []
  },

  bookFiles: {
    isFetching: false,
    isPopulated: false,
    error: null,
    sortKey: 'relativePath',
    sortDirection: sortDirections.ASCENDING,
    items: []
  }
};

export const persistState = [
  'interactiveImport.sortKey',
  'interactiveImport.sortDirection',
  'interactiveImport.recentFolders',
  'interactiveImport.importMode'
];

//
// Actions Types

export const FETCH_INTERACTIVE_IMPORT_ITEMS = 'interactiveImport/fetchInteractiveImportItems';
export const SAVE_INTERACTIVE_IMPORT_ITEM = 'interactiveImport/saveInteractiveImportItem';
export const SET_INTERACTIVE_IMPORT_SORT = 'interactiveImport/setInteractiveImportSort';
export const UPDATE_INTERACTIVE_IMPORT_ITEM = 'interactiveImport/updateInteractiveImportItem';
export const UPDATE_INTERACTIVE_IMPORT_ITEMS = 'interactiveImport/updateInteractiveImportItems';
export const CLEAR_INTERACTIVE_IMPORT = 'interactiveImport/clearInteractiveImport';
export const ADD_RECENT_FOLDER = 'interactiveImport/addRecentFolder';
export const REMOVE_RECENT_FOLDER = 'interactiveImport/removeRecentFolder';
export const SET_INTERACTIVE_IMPORT_MODE = 'interactiveImport/setInteractiveImportMode';

export const FETCH_INTERACTIVE_IMPORT_BOOKS = 'interactiveImport/fetchInteractiveImportBooks';
export const SET_INTERACTIVE_IMPORT_BOOKS_SORT = 'interactiveImport/clearInteractiveImportBooksSort';
export const CLEAR_INTERACTIVE_IMPORT_BOOKS = 'interactiveImport/clearInteractiveImportBooks';

export const FETCH_INTERACTIVE_IMPORT_TRACKFILES = 'interactiveImport/fetchInteractiveImportBookFiles';
export const CLEAR_INTERACTIVE_IMPORT_TRACKFILES = 'interactiveImport/clearInteractiveImportBookFiles';

//
// Action Creators

export const fetchInteractiveImportItems = createThunk(FETCH_INTERACTIVE_IMPORT_ITEMS);
export const setInteractiveImportSort = createAction(SET_INTERACTIVE_IMPORT_SORT);
export const updateInteractiveImportItem = createAction(UPDATE_INTERACTIVE_IMPORT_ITEM);
export const updateInteractiveImportItems = createAction(UPDATE_INTERACTIVE_IMPORT_ITEMS);
export const saveInteractiveImportItem = createThunk(SAVE_INTERACTIVE_IMPORT_ITEM);
export const clearInteractiveImport = createAction(CLEAR_INTERACTIVE_IMPORT);
export const addRecentFolder = createAction(ADD_RECENT_FOLDER);
export const removeRecentFolder = createAction(REMOVE_RECENT_FOLDER);
export const setInteractiveImportMode = createAction(SET_INTERACTIVE_IMPORT_MODE);

export const fetchInteractiveImportBooks = createThunk(FETCH_INTERACTIVE_IMPORT_BOOKS);
export const setInteractiveImportBooksSort = createAction(SET_INTERACTIVE_IMPORT_BOOKS_SORT);
export const clearInteractiveImportBooks = createAction(CLEAR_INTERACTIVE_IMPORT_BOOKS);

export const fetchInteractiveImportBookFiles = createThunk(FETCH_INTERACTIVE_IMPORT_TRACKFILES);
export const clearInteractiveImportBookFiles = createAction(CLEAR_INTERACTIVE_IMPORT_TRACKFILES);

//
// Action Handlers
export const actionHandlers = handleThunks({
  [FETCH_INTERACTIVE_IMPORT_ITEMS]: function(getState, payload, dispatch) {
    if (abortCurrentFetchRequest) {
      abortCurrentFetchRequest();
      abortCurrentFetchRequest = null;
    }

    if (!payload.downloadId && !payload.folder) {
      dispatch(set({ section, error: { message: '`downloadId` or `folder` is required.' } }));
      return;
    }

    dispatch(set({ section, isFetching: true }));

    const { request, abortRequest } = createAjaxRequest({
      url: '/manualimport',
      data: payload
    });

    abortCurrentFetchRequest = abortRequest;

    request.done((data) => {
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
      if (xhr.aborted) {
        return;
      }

      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr
      }));
    });
  },

  [SAVE_INTERACTIVE_IMPORT_ITEM]: function(getState, payload, dispatch) {
    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    dispatch(batchActions([
      ...currentIds.map((id) => updateItem({
        section,
        id,
        isReprocessing: false,
        updateOnly: true
      })),
      ...payload.ids.map((id) => updateItem({
        section,
        id,
        isReprocessing: true,
        updateOnly: true
      }))
    ]));

    const items = getState()[section].items;

    const requestPayload = payload.ids.map((id) => {
      const item = items.find((i) => i.id === id);

      return {
        id,
        path: item.path,
        authorId: item.author ? item.author.id : undefined,
        bookId: item.book ? item.book.id : undefined,
        foreignEditionId: item.foreignEditionId ? item.ForeignEditionId : undefined,
        quality: item.quality,
        releaseGroup: item.releaseGroup,
        indexerFlags: item.indexerFlags,
        downloadId: item.downloadId,
        additionalFile: item.additionalFile,
        replaceExistingFiles: item.replaceExistingFiles,
        disableReleaseSwitching: item.disableReleaseSwitching
      };
    });

    const { request, abortRequest } = createAjaxRequest({
      method: 'POST',
      url: '/manualimport',
      contentType: 'application/json',
      data: JSON.stringify(requestPayload)
    });

    abortCurrentRequest = abortRequest;
    currentIds = payload.ids;

    request.done((data) => {
      dispatch(batchActions(
        data.map((item) => updateItem({
          section,
          ...item,
          isReprocessing: false,
          updateOnly: true
        }))
      ));
    });

    request.fail((xhr) => {
      if (xhr.aborted) {
        return;
      }

      dispatch(batchActions(
        payload.ids.map((id) => updateItem({
          section,
          id,
          isReprocessing: false,
          updateOnly: true
        }))
      ));
    });
  },

  [FETCH_INTERACTIVE_IMPORT_BOOKS]: createFetchHandler(booksSection, '/book'),

  [FETCH_INTERACTIVE_IMPORT_TRACKFILES]: createFetchHandler(bookFilesSection, '/bookFile')
});

//
// Reducers

export const reducers = createHandleActions({

  [UPDATE_INTERACTIVE_IMPORT_ITEM]: (state, { payload }) => {
    const id = payload.id;
    const newState = Object.assign({}, state);
    const items = newState.items;
    const index = items.findIndex((item) => item.id === id);
    const item = Object.assign({}, items[index], payload);

    newState.items = [...items];
    newState.items.splice(index, 1, item);

    return newState;
  },

  [UPDATE_INTERACTIVE_IMPORT_ITEMS]: (state, { payload }) => {
    const ids = payload.ids;
    const newState = Object.assign({}, state);
    const items = [...newState.items];

    ids.forEach((id) => {
      const index = items.findIndex((item) => item.id === id);
      const item = Object.assign({}, items[index], payload);

      items.splice(index, 1, item);
    });

    newState.items = items;

    return newState;
  },

  [ADD_RECENT_FOLDER]: function(state, { payload }) {
    const folder = payload.folder;
    const recentFolder = { folder, lastUsed: moment().toISOString() };
    const recentFolders = [...state.recentFolders];
    const index = recentFolders.findIndex((r) => r.folder === folder);

    if (index > -1) {
      recentFolders.splice(index, 1);
    }

    recentFolders.push(recentFolder);

    const sliceIndex = Math.max(recentFolders.length - MAXIMUM_RECENT_FOLDERS, 0);

    return Object.assign({}, state, { recentFolders: recentFolders.slice(sliceIndex) });
  },

  [REMOVE_RECENT_FOLDER]: function(state, { payload }) {
    const folder = payload.folder;
    const recentFolders = [...state.recentFolders];
    const index = recentFolders.findIndex((r) => r.folder === folder);

    recentFolders.splice(index, 1);

    return Object.assign({}, state, { recentFolders });
  },

  [CLEAR_INTERACTIVE_IMPORT]: function(state) {
    const newState = {
      ...defaultState,
      recentFolders: state.recentFolders,
      importMode: state.importMode
    };

    return newState;
  },

  [SET_INTERACTIVE_IMPORT_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_INTERACTIVE_IMPORT_MODE]: function(state, { payload }) {
    return Object.assign({}, state, { importMode: payload.importMode });
  },

  [SET_INTERACTIVE_IMPORT_BOOKS_SORT]: createSetClientSideCollectionSortReducer(booksSection),

  [CLEAR_INTERACTIVE_IMPORT_BOOKS]: (state) => {
    return updateSectionState(state, booksSection, {
      ...defaultState.books
    });
  },

  [CLEAR_INTERACTIVE_IMPORT_TRACKFILES]: (state) => {
    return updateSectionState(state, bookFilesSection, {
      ...defaultState.bookFiles
    });
  }

}, defaultState, section);
