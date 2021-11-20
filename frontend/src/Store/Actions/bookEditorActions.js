import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'bookEditor';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null
};

//
// Actions Types

export const SAVE_BOOK_EDITOR = 'bookEditor/saveBookEditor';
export const BULK_DELETE_BOOK = 'bookEditor/bulkDeleteBook';

//
// Action Creators

export const saveBookEditor = createThunk(SAVE_BOOK_EDITOR);
export const bulkDeleteBook = createThunk(BULK_DELETE_BOOK);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SAVE_BOOK_EDITOR]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/book/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((book) => {
          return updateItem({
            id: book.id,
            section: 'books',
            ...book
          });
        }),

        set({
          section,
          isSaving: false,
          saveError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  },

  [BULK_DELETE_BOOK]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isDeleting: true
    }));

    const promise = createAjaxRequest({
      url: '/book/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done(() => {
      // SignalR will take care of removing the book from the collection

      dispatch(set({
        section,
        isDeleting: false,
        deleteError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({}, defaultState, section);
