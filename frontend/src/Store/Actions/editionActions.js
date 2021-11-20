import { createAction } from 'redux-actions';
import { createThunk, handleThunks } from 'Store/thunks';
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

//
// Action Creators

export const fetchEditions = createThunk(FETCH_EDITIONS);
export const clearEditions = createAction(CLEAR_EDITIONS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_EDITIONS]: createFetchHandler(section, '/edition')
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
