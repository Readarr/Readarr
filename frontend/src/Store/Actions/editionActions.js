import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import getProviderState from 'Utilities/State/getProviderState';
import { updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createClearReducer from './Creators/Reducers/createClearReducer';

//
// Variables

export const section = 'editions';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],
  itemMap: {}
};

//
// Actions Types

export const FETCH_EDITIONS = 'editions/fetchEditions';
export const CLEAR_EDITIONS = 'editions/clearEditions';
export const SAVE_EDITIONS = 'editions/saveEditions';

//
// Action Creators

export const fetchEditions = createThunk(FETCH_EDITIONS);
export const clearEditions = createAction(CLEAR_EDITIONS);
export const saveEditions = createThunk(SAVE_EDITIONS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_EDITIONS]: createFetchHandler(section, '/edition'),

  [SAVE_EDITIONS]: function(getState, payload, dispatch) {
    const {
      id,
      ...otherPayload
    } = payload;

    const saveData = getProviderState({ id, ...otherPayload }, getState, 'books');

    dispatch(batchActions([
      ...saveData.editions.map((edition) => {
        return updateItem({
          id: edition.id,
          section: 'editions',
          ...edition
        });
      })
    ]));
  }
});

//
// Reducers
export const reducers = createHandleActions({

  [CLEAR_EDITIONS]: createClearReducer(section, {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: [],
    itemMap: {}
  })

}, defaultState, section);
