import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'series';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  sortKey: 'position',
  sortDirection: sortDirections.ASCENDING,
  items: [],

  columns: [
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
      name: 'position',
      label: 'Number',
      isSortable: true,
      isVisible: true
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
      isVisible: true
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ]
};

//
// Actions Types

export const FETCH_SERIES = 'series/fetchSeries';
export const SET_SERIES_SORT = 'books/setSeriesSort';
export const SET_SERIES_TABLE_OPTION = 'books/setSeriesTableOption';
export const CLEAR_SERIES = 'series/clearSeries';
export const SET_SERIES_VALUE = 'books/setBookValue';

//
// Action Creators

export const fetchSeries = createThunk(FETCH_SERIES);
export const setSeriesSort = createAction(SET_SERIES_SORT);
export const setSeriesTableOption = createAction(SET_SERIES_TABLE_OPTION);
export const clearSeries = createAction(CLEAR_SERIES);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_SERIES]: createFetchHandler(section, '/series')
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_SERIES_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_SERIES_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_SERIES_VALUE]: createSetSettingValueReducer(section),

  [CLEAR_SERIES]: (state) => {
    return Object.assign({}, state, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: []
    });
  }

}, defaultState, section);
